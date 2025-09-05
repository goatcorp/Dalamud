using System.Collections.Generic;
using System.Linq;

using Dalamud.Bindings.ImGui;
using Dalamud.Configuration.Internal;
using Dalamud.Interface.Windowing.Persistence;
using Serilog;

namespace Dalamud.Interface.Windowing;

/// <inheritdoc/>
public class WindowSystem : IWindowSystem
{
    private static DateTimeOffset lastAnyFocus;

    private readonly List<WindowHost> windows = new();

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
    public IReadOnlyList<IWindow> Windows => this.windows.Select(c => c.Window).ToList();

    /// <inheritdoc/>
    public bool HasAnyFocus { get; private set; }

    /// <inheritdoc/>
    public string? Namespace { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether ATK close events should be inhibited while any window has focus.
    /// Does not respect windows that are pinned or clickthrough.
    /// </summary>
    internal static bool ShouldInhibitAtkCloseEvents { get; set; }

    /// <inheritdoc/>
    public void AddWindow(IWindow window)
    {
        if (this.windows.Any(x => x.Window.WindowName == window.WindowName))
            throw new ArgumentException("A window with this name/ID already exists.");

        this.windows.Add(new WindowHost(window));
    }

    /// <inheritdoc/>
    public void RemoveWindow(IWindow window)
    {
        if (this.windows.All(c => c.Window != window))
            throw new ArgumentException("This window is not registered on this WindowSystem.");

        this.windows.RemoveAll(c => c.Window == window);
    }

    /// <inheritdoc/>
    public void RemoveAllWindows() => this.windows.Clear();

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

        // Shallow clone the list of windows so that we can edit it without modifying it while the loop is iterating
        foreach (var window in this.windows.ToArray())
        {
#if DEBUG
            // Log.Verbose($"[WS{(hasNamespace ? "/" + this.Namespace : string.Empty)}] Drawing {window.WindowName}");
#endif
            window.DrawInternal(flags, persistence);
        }

        var focusedWindow = this.windows.FirstOrDefault(window => window.Window.IsFocused);
        this.HasAnyFocus = focusedWindow != default;

        if (this.HasAnyFocus)
        {
            if (this.lastFocusedWindowName != focusedWindow.Window.WindowName)
            {
                Log.Verbose($"WindowSystem \"{this.Namespace}\" Window \"{focusedWindow.Window.WindowName}\" has focus now");
                this.lastFocusedWindowName = focusedWindow.Window.WindowName;
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

        ShouldInhibitAtkCloseEvents |= this.windows.Any(w => w.Window.IsFocused &&
                                                            w.Window.RespectCloseHotkey &&
                                                            !w.Window.IsPinned &&
                                                            !w.Window.IsClickthrough);

        if (hasNamespace)
            ImGui.PopID();
    }
}
