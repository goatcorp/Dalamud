using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Configuration.Internal;
using Dalamud.Interface.Windowing.Persistence;

using Serilog;

namespace Dalamud.Interface.Windowing;

/// <inheritdoc/>
public class WindowSystem : IWindowSystem
{
    private static DateTimeOffset lastAnyFocus;

    private readonly List<WindowHost> windowHosts = [];
    private readonly List<WindowHost> windowHostsBuffer = [];
    private readonly List<IWindow> windows = [];

    private string lastFocusedWindowName = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowSystem"/> class.
    /// </summary>
    /// <param name="imNamespace">The name/ID-space of this <see cref="WindowSystem"/>.</param>
    public WindowSystem(string? imNamespace = null)
    {
        this.Namespace = imNamespace;
    }

    /// <summary>
    /// Gets a value indicating whether any <see cref="WindowSystem"/> contains any <see cref="IWindow"/>
    /// that has focus and is not marked to be excluded from consideration.
    /// </summary>
    public static bool HasAnyWindowSystemFocus { get; internal set; } = false;

    /// <summary>
    /// Gets the name of the currently focused window system that is redirecting normal escape functionality.
    /// </summary>
    public static string FocusedWindowSystemNamespace { get; internal set; } = string.Empty;

    /// <summary>
    /// Gets the timespan since the last time any window was focused.
    /// </summary>
    public static TimeSpan TimeSinceLastAnyFocus => DateTimeOffset.Now - lastAnyFocus;

    /// <inheritdoc/>
    public IReadOnlyList<IWindow> Windows => this.windows;

    /// <inheritdoc/>
    public bool HasAnyFocus { get; private set; }

    /// <inheritdoc/>
    public string? Namespace { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether ATK close events should be inhibited while any window has focus.
    /// Does not respect windows that are pinned or clickthrough.
    /// </summary>
    internal static bool ShouldInhibitAtkCloseEvents { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether ATK collisions should be inhibited while any window is hovered.
    /// Does not respect windows that are pinned or clickthrough.
    /// </summary>
    internal static bool ShouldInhibitAtkCollisions { get; set; }

    /// <summary>
    /// Gets or sets a value indicating the default blur strength for windows in this <see cref="WindowSystem"/>.
    /// This is used for windows that do not have a specific override set in their preset.
    /// Range [0f,1f].
    /// </summary>
    internal static float DefaultBackgroundBlurStrength { get; set; }

    /// <summary>
    /// Gets or sets a value indicating the default inactive window blur tint for windows in this <see cref="WindowSystem"/>.
    /// </summary>
    internal static Vector4 DefaultBackgroundBlurTint { get; set; }

    /// <summary>
    /// Gets or sets a value indicating the default active window blur tint for windows in this <see cref="WindowSystem"/>.
    /// </summary>
    internal static Vector4 DefaultBackgroundBlurTintActive { get; set; }

    /// <summary>
    /// Gets or sets a value indicating the default blur luminosity adjust for windows in this <see cref="WindowSystem"/>.
    /// </summary>
    internal static Vector4 DefaultBackgroundBlurLuminosity { get; set; }

    /// <inheritdoc/>
    public void AddWindow(IWindow window)
    {
        if (this.windowHosts.Any(host => host.Window.WindowName == window.WindowName))
            throw new ArgumentException("A window with this name/ID already exists.");

        this.windowHosts.Add(new WindowHost(window));
        this.windows.Add(window);
    }

    /// <inheritdoc/>
    public void RemoveWindow(IWindow window)
    {
        if (this.windowHosts.All(host => host.Window != window))
            throw new ArgumentException("This window is not registered on this WindowSystem.");

        this.windowHosts.RemoveAll(host => host.Window == window);
        this.windows.RemoveAll(win => win == window);
    }

    /// <inheritdoc/>
    public void RemoveAllWindows()
    {
        this.windowHosts.Clear();
        this.windows.Clear();
    }

    /// <inheritdoc/>
    public void Draw()
    {
        var hasNamespace = !string.IsNullOrEmpty(this.Namespace);

        if (hasNamespace)
            ImGui.PushID(this.Namespace);

        // These must be nullable, people are using stock WindowSystems and Windows without Dalamud for tests
        var config = Service<DalamudConfiguration>.GetNullable();
        var persistence = Service<WindowSystemPersistence>.GetNullable();

        var flags = WindowHost.WindowDrawFlags.None;

        if (config?.EnablePluginUISoundEffects ?? false)
            flags |= WindowHost.WindowDrawFlags.UseSoundEffects;

        if (config?.EnablePluginUiAdditionalOptions ?? false)
            flags |= WindowHost.WindowDrawFlags.UseAdditionalOptions;

        if (config?.IsFocusManagementEnabled ?? false)
            flags |= WindowHost.WindowDrawFlags.UseFocusManagement;

        if (config?.ReduceMotions ?? false)
            flags |= WindowHost.WindowDrawFlags.IsReducedMotion;

        // Make a copy of the list of WindowHosts, so that we can add/remove windows
        // without modifying the list during iteration
        this.windowHostsBuffer.Clear();
        this.windowHostsBuffer.AddRange(this.windowHosts);

        foreach (var window in this.windowHostsBuffer)
        {
#if DEBUG
            // Log.Verbose($"[WS{(hasNamespace ? "/" + this.Namespace : string.Empty)}] Drawing {window.WindowName}");
#endif
            var parameters = new WindowHost.WindowDrawParameters
            {
                Flags = flags,
                DefaultBackgroundBlurStrength = DefaultBackgroundBlurStrength,
                DefaultBackgroundBlurTint = DefaultBackgroundBlurTint,
                DefaultBackgroundBlurTintActive = DefaultBackgroundBlurTintActive,
                DefaultBackgroundBlurLuminosity = DefaultBackgroundBlurLuminosity,
            };

            window.DrawInternal(parameters, persistence);
        }

        var focusedWindow = this.windows.FirstOrDefault(win => win.IsFocused);
        this.HasAnyFocus = focusedWindow != null;

        if (this.HasAnyFocus)
        {
            if (this.lastFocusedWindowName != focusedWindow.WindowName)
            {
                Log.Verbose($"WindowSystem \"{this.Namespace}\" Window \"{focusedWindow.WindowName}\" has focus now");
                this.lastFocusedWindowName = focusedWindow.WindowName;
            }

            HasAnyWindowSystemFocus = true;
            FocusedWindowSystemNamespace = this.Namespace;

            lastAnyFocus = DateTimeOffset.Now;
        }
        else
        {
            if (this.lastFocusedWindowName != string.Empty)
            {
                Log.Verbose($"WindowSystem \"{this.Namespace}\" Window \"{this.lastFocusedWindowName}\" lost focus");
                this.lastFocusedWindowName = string.Empty;
            }
        }

        ShouldInhibitAtkCloseEvents |= this.windows.Any(
            win => win.IsFocused && win.RespectCloseHotkey && !win.IsPinned && !win.IsClickthrough);

        ShouldInhibitAtkCollisions |= this.windows.Any(
            win => win.IsHovered && win.InhibitAtkCollision && !win.IsPinned && !win.IsClickthrough);

        if (hasNamespace)
            ImGui.PopID();
    }
}
