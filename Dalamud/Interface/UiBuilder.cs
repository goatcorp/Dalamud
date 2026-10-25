using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

using CheapLoc;

using Dalamud.Bindings.ImGui;
using Dalamud.Configuration.Internal;
using Dalamud.Game;
using Dalamud.Game.Gui;
using Dalamud.Interface.Fonts;
using Dalamud.Interface.Fonts.Internal;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.ImGuiNotification.Internal;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Internal.DesignSystem;
using Dalamud.Interface.Utility;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Utility;

using Serilog;

namespace Dalamud.Interface;

/// <summary>
/// This interface represents the Dalamud UI that is drawn on top of the game.
/// It can be used to draw custom windows and overlays.
/// </summary>
public interface IUiBuilder
{
    /// <summary>
    /// The event that gets called when Dalamud is ready to draw your windows or overlays.
    /// When it is called, you can use static ImGui calls.
    /// </summary>
    event Action? Draw;

    /// <summary>
    /// The event that is called when the game's DirectX device is requesting you to resize your buffers.
    /// </summary>
    event Action? ResizeBuffers;

    /// <summary>
    /// Event that is fired when the plugin should open its configuration interface.
    /// </summary>
    event Action? OpenConfigUi;

    /// <summary>
    /// Event that is fired when the plugin should open its main interface.
    /// </summary>
    event Action? OpenMainUi;

    /// <summary>
    /// Gets or sets an action that is called when plugin UI or interface modifications are supposed to be shown.
    /// These may be fired consecutively.
    /// </summary>
    event Action? ShowUi;

    /// <summary>
    /// Gets or sets an action that is called when plugin UI or interface modifications are supposed to be hidden.
    /// These may be fired consecutively.
    /// </summary>
    event Action? HideUi;

    /// <inheritdoc cref="InterfaceManager.DefaultGlobalScaleChanged"/>
    event Action? DefaultGlobalScaleChanged;

    /// <inheritdoc cref="InterfaceManager.DefaultFontChanged"/>
    event Action? DefaultFontChanged;

    /// <inheritdoc cref="InterfaceManager.DefaultStyleChanged"/>
    event Action? DefaultStyleChanged;

    /// <summary>Gets the style of the default font as configured in the settings.</summary>
    /// <remarks>It is without the fallback fonts that the default font has for other languages. Use
    /// <see cref="TextStyle.Default"/> to push the default font.</remarks>
    TextStyle DefaultTextStyle { get; }

    /// <summary>Gets the game's active Direct3D device.</summary>
    /// <value>Pointer to the instance of IUnknown that the game is using and should be containing an ID3D11Device,
    /// or 0 if it is not available yet.</value>
    /// <remarks>Use
    /// <a href="https://learn.microsoft.com/en-us/windows/win32/api/unknwn/nf-unknwn-iunknown-queryinterface(q)">
    /// QueryInterface</a> with IID of <c>IID_ID3D11Device</c> if you want to ensure that the interface type contained
    /// within is indeed an instance of ID3D11Device.</remarks>
    nint DeviceHandle { get; }

    /// <summary>Gets the game's main window handle.</summary>
    /// <value>HWND of the main game window, or 0 if it is not available yet.</value>
    nint WindowHandlePtr { get; }

    /// <summary>
    /// Gets or sets a value indicating whether this plugin should hide its UI automatically when the game's UI is hidden.
    /// </summary>
    bool DisableAutomaticUiHide { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this plugin should hide its UI automatically when the user toggles the UI.
    /// </summary>
    bool DisableUserUiHide { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this plugin should hide its UI automatically during cutscenes.
    /// </summary>
    bool DisableCutsceneUiHide { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this plugin should hide its UI automatically while gpose is active.
    /// </summary>
    bool DisableGposeUiHide { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the game's cursor should be overridden with the ImGui cursor.
    /// </summary>
    bool OverrideGameCursor { get; set; }

    /// <summary>
    /// Gets the count of Draw calls made since plugin creation.
    /// </summary>
    ulong FrameCount { get; }

    /// <summary>
    /// Gets a value indicating whether a cutscene is playing.
    /// </summary>
    bool CutsceneActive { get; }

    /// <summary>
    /// Gets a value indicating whether this plugin should modify the game's interface at this time.
    /// </summary>
    bool ShouldModifyUi { get; }

    /// <summary>
    /// Gets a value indicating whether UI functions can be used.
    /// </summary>
    bool UiPrepared { get; }

    /// <summary>
    /// Gets a value indicating whether to use "reduced motion". This usually means that you should use less
    /// intrusive animations, or disable them entirely.
    /// </summary>
    bool ShouldUseReducedMotion { get; }

    /// <summary>
    /// Gets a value indicating whether the user has enabled the "Enable sound effects for plugin windows" setting.<br />
    /// This setting is effected by the in-game "System Sounds" option and volume.
    /// </summary>
    bool PluginUISoundEffectsEnabled { get; }

    /// <summary>
    /// Loads an ULD file that can load textures containing multiple icons in a single texture.
    /// </summary>
    /// <param name="uldPath">The path of the requested ULD file.</param>
    /// <returns>A wrapper around said ULD file.</returns>
    UldWrapper LoadUld(string uldPath);

    /// <summary>
    /// Waits for UI to become available for use.
    /// </summary>
    /// <returns>A task that completes when the game's Present has been called at least once.</returns>
    Task WaitForUi();

    /// <summary>
    /// Waits for UI to become available for use.
    /// </summary>
    /// <param name="func">Function to call.</param>
    /// <param name="runInFrameworkThread">Specifies whether to call the function from the framework thread.</param>
    /// <returns>A task that completes when the game's Present has been called at least once.</returns>
    /// <typeparam name="T">Return type.</typeparam>
    Task<T> RunWhenUiPrepared<T>(Func<T> func, bool runInFrameworkThread = false);

    /// <summary>
    /// Waits for UI to become available for use.
    /// </summary>
    /// <param name="func">Function to call.</param>
    /// <param name="runInFrameworkThread">Specifies whether to call the function from the framework thread.</param>
    /// <returns>A task that completes when the game's Present has been called at least once.</returns>
    /// <typeparam name="T">Return type.</typeparam>
    Task<T> RunWhenUiPrepared<T>(Func<Task<T>> func, bool runInFrameworkThread = false);

    /// <summary>Finds the font behind an ImGui font, such as <see cref="ImGui.GetFont"/>.</summary>
    /// <param name="font">The ImGui font.</param>
    /// <returns>The font, or <see langword="null"/> if <paramref name="font"/> is not Dalamud's.</returns>
    /// <remarks>It never loads a font. To keep a font alive, keep its <see cref="TextStyle"/> or the returned
    /// <see cref="ImFontRef"/>.</remarks>
    ImFontRef? FindFont(ImFontPtr font);

    /// <summary>Pushes a callback that decides how text with a markup is drawn.</summary>
    /// <param name="callback">The callback. It can be <see langword="null"/> if <paramref name="inherit"/> is
    /// <see langword="false"/>, to only cut off the earlier callbacks.</param>
    /// <param name="inherit">Whether the callbacks pushed earlier are still asked. Set it to <see langword="false"/> to
    /// keep an outer callback away from a widget.</param>
    /// <returns>A disposable object that pops the callback.</returns>
    /// <remarks>Callbacks are ignored for text with <see cref="TextMarkup.Plain"/> as its
    /// <see cref="TextStyle.Markup"/>.</remarks>
    ImPopFontDisposable PushTextCallback(ITextMarkupCallback? callback, bool inherit = true);

    /// <summary>Makes layouts made with a callback stale, so that its answers are asked again.</summary>
    /// <param name="callback">The callback.</param>
    void InvalidateTextCallback(ITextMarkupCallback callback);

    /// <summary>Registers a text decoration, to use as <see cref="TextStyle.ExtraDecoration"/> with
    /// <see cref="TextDecorationRegistration.Id"/>.</summary>
    /// <param name="drawer">Draws the decoration, on the main thread.</param>
    /// <param name="flags">The layers the decoration is drawn in. <see cref="TextDecorationFlags.None"/> is taken as
    /// <see cref="TextDecorationFlags.OverText"/>.</param>
    /// <param name="bands">The number of times the decoration is drawn in each layer, from 1 to 16. Each time is for
    /// all the spans of a text before the next, as <see cref="TextDecorationSpan.Band"/>, so that what is drawn later
    /// covers what is drawn earlier everywhere in the text.</param>
    /// <returns>The registration, which unregisters the decoration when disposed or when the plugin unloads.</returns>
    /// <exception cref="ObjectDisposedException">If the plugin has unloaded.</exception>
    /// <remarks>Decorations are not drawn for vertical text. Keep what the drawer needs in
    /// <see cref="TextDecoration.Context"/> and <see cref="TextDecoration.Data"/>.</remarks>
    /// <example>
    /// See <see cref="Internal.Windows.Data.Widgets.FontSamplesImGuiWidget.DrawRainbowEdge"/> for an example.
    /// <code>
    /// var squiggle = uiBuilder.RegisterTextDecoration(
    ///     (in TextDecorationSpan span) =&gt; { /* span.AddPolyline(...) */ },
    ///     TextDecorationFlags.BelowText);
    /// using (ImGui.PushTextDecoration(squiggle.Id, 0xFF0000FF))
    ///     ImGui.Text("Misspeled"u8);
    ///
    /// // A rainbow around the glyphs, widest band first, all under the fill of every glyph.
    /// uint[] colors = [0xFF0000FF, 0xFF00A5FF, 0xFF00FFFF, 0xFF00FF00, 0xFFFF0000, 0xFF82004B, 0xFFEE82EE];
    /// var rainbow = uiBuilder.RegisterTextDecoration(
    ///     (in TextDecorationSpan span) =&gt; span.DrawGlyphs(colors[span.Band], colors.Length - span.Band),
    ///     TextDecorationFlags.BelowFill,
    ///     colors.Length);
    /// </code></example>
    TextDecorationRegistration RegisterTextDecoration(
        TextDecorationDrawer drawer, TextDecorationFlags flags = TextDecorationFlags.OverText, int bands = 1);
}

/// <summary>
/// This class represents the Dalamud UI that is drawn on top of the game.
/// It can be used to draw custom windows and overlays.
/// </summary>
public sealed class UiBuilder : IDisposable, IUiBuilder
{
    private readonly LocalPlugin plugin;
    private readonly HitchDetector hitchDetector;
    private readonly string namespaceName;
    private readonly InterfaceManager interfaceManager = Service<InterfaceManager>.Get();
    private readonly Framework framework = Service<Framework>.Get();

    [ServiceManager.ServiceDependency]
    private readonly DalamudConfiguration configuration = Service<DalamudConfiguration>.Get();

    private readonly DisposeSafety.ScopedFinalizer scopedFinalizer = new();
    private readonly Lock decorationsLock = new();

    private HashSet<TextDecorationRegistration>? decorations = [];
    private bool hasErrorWindow = false;
    private Exception? lastError = null;
    private bool lastFrameUiHideState = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="UiBuilder"/> class and registers it.
    /// You do not have to call this manually.
    /// </summary>
    /// <param name="plugin">The plugin.</param>
    /// <param name="namespaceName">The plugin namespace.</param>
    internal UiBuilder(LocalPlugin plugin, string namespaceName)
    {
        try
        {
            this.hitchDetector = new HitchDetector($"UiBuilder({namespaceName})", this.configuration.UiBuilderHitch);
            this.namespaceName = namespaceName;
            this.plugin = plugin;

            this.interfaceManager.Draw += this.OnDraw;
            this.scopedFinalizer.Add(() => this.interfaceManager.Draw -= this.OnDraw);

            this.interfaceManager.ResizeBuffers += this.OnResizeBuffers;
            this.scopedFinalizer.Add(() => this.interfaceManager.ResizeBuffers -= this.OnResizeBuffers);

            this.interfaceManager.DefaultStyleChanged += this.OnDefaultStyleChanged;
            this.scopedFinalizer.Add(() => this.interfaceManager.DefaultStyleChanged -= this.OnDefaultStyleChanged);

            this.interfaceManager.DefaultGlobalScaleChanged += this.OnDefaultGlobalScaleChanged;
            this.scopedFinalizer.Add(() => this.interfaceManager.DefaultGlobalScaleChanged -= this.OnDefaultGlobalScaleChanged);

            this.interfaceManager.DefaultFontChanged += this.OnDefaultFontChanged;
            this.scopedFinalizer.Add(() => this.interfaceManager.DefaultFontChanged -= this.OnDefaultFontChanged);

            this.scopedFinalizer.Add(this.DisposeDecorations);
        }
        catch
        {
            this.scopedFinalizer.Dispose();
            throw;
        }
    }

    /// <inheritdoc/>
    public event Action? Draw;

    /// <inheritdoc/>
    public event Action? ResizeBuffers;

    /// <inheritdoc/>
    public event Action? OpenConfigUi;

    /// <inheritdoc/>
    public event Action? OpenMainUi;

    /// <inheritdoc/>
    public event Action? ShowUi;

    /// <inheritdoc/>
    public event Action? HideUi;

    /// <inheritdoc/>
    public event Action? DefaultGlobalScaleChanged;

    /// <inheritdoc/>
    public event Action? DefaultFontChanged;

    /// <inheritdoc/>
    public event Action? DefaultStyleChanged;

    /// <summary>Gets the default Dalamud font size in points, before the global scale.</summary>
    public static float DefaultFontSizePt => (DefaultFontSizePx * 3) / 4;

    /// <summary>Gets the default Dalamud font size in pixels, before the global scale.</summary>
    /// <remarks><see cref="TextStyle"/> says how font sizes relate.</remarks>
    public static float DefaultFontSizePx => Service<FontFactory>.Get().DefaultSizePx;

    /// <summary>Gets the built-in default font at <see cref="DefaultFontSizePx"/>, supporting all game languages and
    /// icons.</summary>
    /// <remarks>Prefer pushing the text style, which keeps no font alive longer than needed.</remarks>
    /// <seealso cref="TextStyle.Default"/>
    public static ImFontRef DefaultFont => InterfaceManager.DefaultFont;

    /// <summary>Gets the built-in FontAwesome icon font at <see cref="DefaultFontSizePx"/>.</summary>
    /// <remarks>Prefer pushing the text style, which keeps no font alive longer than needed.</remarks>
    /// <seealso cref="TextStyle.Icon"/>
    public static ImFontRef IconFont => InterfaceManager.IconFont;

    /// <summary>Gets the built-in monospaced font at <see cref="DefaultFontSizePx"/>.</summary>
    /// <remarks>Prefer pushing the text style, which keeps no font alive longer than needed.</remarks>
    /// <seealso cref="TextStyle.Mono"/>
    public static ImFontRef MonoFont => InterfaceManager.MonoFont;

    /// <summary>Gets the built-in FontAwesome icon font at <see cref="DefaultFontSizePx"/>, with each icon fit into a
    /// square.</summary>
    /// <remarks>Prefer pushing the text style, which keeps no font alive longer than needed.</remarks>
    /// <seealso cref="TextStyle.IconFixedWidth"/>
    public static ImFontRef IconFontFixedWidth => InterfaceManager.IconFontFixedWidth;

    /// <inheritdoc/>
    public TextStyle DefaultTextStyle => Service<FontFactory>.Get().DefaultTextStyle;

    /// <inheritdoc/>
    public nint DeviceHandle => this.InterfaceManagerWithScene?.Backend?.DeviceHandle ?? 0;

    /// <inheritdoc/>
    public nint WindowHandlePtr => this.InterfaceManagerWithScene is { } imws ? imws.GameWindowHandle : 0;

    /// <summary>
    /// Gets or sets a value indicating whether this plugin should hide its UI automatically when the game's UI is hidden.
    /// </summary>
    public bool DisableAutomaticUiHide { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether this plugin should hide its UI automatically when the user toggles the UI.
    /// </summary>
    public bool DisableUserUiHide { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether this plugin should hide its UI automatically during cutscenes.
    /// </summary>
    public bool DisableCutsceneUiHide { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether this plugin should hide its UI automatically while gpose is active.
    /// </summary>
    public bool DisableGposeUiHide { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether the game's cursor should be overridden with the ImGui cursor.
    /// </summary>
    public bool OverrideGameCursor
    {
        get => this.interfaceManager.OverrideGameCursor;
        set => this.interfaceManager.OverrideGameCursor = value;
    }

    /// <summary>
    /// Gets the count of Draw calls made since plugin creation.
    /// </summary>
    public ulong FrameCount { get; private set; } = 0;

    /// <summary>
    /// Gets a value indicating whether a cutscene is playing.
    /// </summary>
    public bool CutsceneActive => UiHideState.CutsceneActive;

    /// <summary>
    /// Gets a value indicating whether this plugin should modify the game's interface at this time.
    /// </summary>
    public bool ShouldModifyUi => this.interfaceManager.IsDispatchingEvents;

    /// <summary>
    /// Gets a value indicating whether UI functions can be used.
    /// </summary>
    public bool UiPrepared =>
        Service<InterfaceManager.InterfaceManagerWithScene>.GetNullable(ExceptionPropagationMode.None) != null;

    /// <summary>
    /// Gets a value indicating whether to use "reduced motion". This usually means that you should use less
    /// intrusive animations, or disable them entirely.
    /// </summary>
    public bool ShouldUseReducedMotion => Service<DalamudConfiguration>.Get().ReduceMotions ?? false;

    /// <inheritdoc />
    public bool PluginUISoundEffectsEnabled => Service<DalamudConfiguration>.Get().EnablePluginUISoundEffects;

    /// <summary>
    /// Gets or sets a value indicating whether statistics about UI draw time should be collected.
    /// </summary>
#if DEBUG
    internal static bool DoStats { get; set; } = true;
#else
    internal static bool DoStats { get; set; } = false;
#endif

    /// <summary> Gets draw statistics for this plugin. </summary>
    internal PluginDrawStatistics PluginDrawStatistics { get; } = new();

    /// <summary>
    /// Gets a value indicating whether this UiBuilder has a configuration UI registered.
    /// </summary>
    internal bool HasConfigUi => this.OpenConfigUi != null;

    /// <summary>
    /// Gets a value indicating whether this UiBuilder has a configuration UI registered.
    /// </summary>
    internal bool HasMainUi => this.OpenMainUi != null;

    private InterfaceManager? InterfaceManagerWithScene =>
        Service<InterfaceManager.InterfaceManagerWithScene>.GetNullable(ExceptionPropagationMode.None)?.Manager;

    private Task<InterfaceManager> InterfaceManagerWithSceneAsync =>
        Service<InterfaceManager.InterfaceManagerWithScene>.GetAsync().ContinueWith(task => task.Result.Manager);

    /// <summary>
    /// Loads an ULD file that can load textures containing multiple icons in a single texture.
    /// </summary>
    /// <param name="uldPath">The path of the requested ULD file.</param>
    /// <returns>A wrapper around said ULD file.</returns>
    public UldWrapper LoadUld(string uldPath)
        => new(this, uldPath);

    /// <summary>
    /// Waits for UI to become available for use.
    /// </summary>
    /// <returns>A task that completes when the game's Present has been called at least once.</returns>
    public Task WaitForUi() => this.InterfaceManagerWithSceneAsync;

    /// <summary>
    /// Waits for UI to become available for use.
    /// </summary>
    /// <param name="func">Function to call.</param>
    /// <param name="runInFrameworkThread">Specifies whether to call the function from the framework thread.</param>
    /// <returns>A task that completes when the game's Present has been called at least once.</returns>
    /// <typeparam name="T">Return type.</typeparam>
    public Task<T> RunWhenUiPrepared<T>(Func<T> func, bool runInFrameworkThread = false)
    {
        if (runInFrameworkThread)
        {
            return this.InterfaceManagerWithSceneAsync
                       .ContinueWith(_ => this.framework.RunOnFrameworkThread(func))
                       .Unwrap();
        }
        else
        {
            return this.InterfaceManagerWithSceneAsync
                       .ContinueWith(_ => func());
        }
    }

    /// <summary>
    /// Waits for UI to become available for use.
    /// </summary>
    /// <param name="func">Function to call.</param>
    /// <param name="runInFrameworkThread">Specifies whether to call the function from the framework thread.</param>
    /// <returns>A task that completes when the game's Present has been called at least once.</returns>
    /// <typeparam name="T">Return type.</typeparam>
    public Task<T> RunWhenUiPrepared<T>(Func<Task<T>> func, bool runInFrameworkThread = false)
    {
        if (runInFrameworkThread)
        {
            return this.InterfaceManagerWithSceneAsync
                       .ContinueWith(_ => this.framework.RunOnFrameworkThread(func))
                       .Unwrap();
        }
        else
        {
            return this.InterfaceManagerWithSceneAsync
                       .ContinueWith(_ => func())
                       .Unwrap();
        }
    }

    /// <inheritdoc/>
    public TextDecorationRegistration RegisterTextDecoration(
        TextDecorationDrawer drawer, TextDecorationFlags flags = TextDecorationFlags.OverText, int bands = 1)
    {
        lock (this.decorationsLock)
        {
            ObjectDisposedException.ThrowIf(this.decorations is null, this);
            var registration = new TextDecorationRegistration(
                TextDecorationRegistry.Register(drawer, flags, bands),
                this.OnDecorationDisposed);
            this.decorations.Add(registration);
            return registration;
        }
    }

    /// <summary>
    /// Unregister the UiBuilder. Do not call this in plugin code.
    /// </summary>
    void IDisposable.Dispose()
    {
        this.scopedFinalizer.Dispose();
    }

    /// <summary>Clean up resources allocated by this instance of <see cref="UiBuilder"/>.</summary>
    /// <remarks>Dalamud internal use only.</remarks>
    internal void DisposeInternal() => this.scopedFinalizer.Dispose();

    /// <summary>
    /// Open the registered configuration UI, if it exists.
    /// </summary>
    internal void OpenConfig()
    {
        this.OpenConfigUi?.InvokeSafely();
    }

    /// <summary>
    /// Open the registered configuration UI, if it exists.
    /// </summary>
    internal void OpenMain()
    {
        this.OpenMainUi?.InvokeSafely();
    }

    /// <summary>
    /// Notify this UiBuilder about plugin UI being hidden.
    /// </summary>
    internal void NotifyHideUi()
    {
        this.HideUi?.InvokeSafely();
    }

    /// <summary>
    /// Notify this UiBuilder about plugin UI being shown.
    /// </summary>
    internal void NotifyShowUi()
    {
        this.ShowUi?.InvokeSafely();
    }

    private void OnDraw()
    {
        this.hitchDetector.Start();

        var gameGui = Service<GameGui>.GetNullable();
        if (gameGui == null)
            return;

        if ((UiHideState.GameUiHidden && this.configuration.ToggleUiHide &&
             !(this.DisableUserUiHide || this.DisableAutomaticUiHide)) ||
            (UiHideState.CutsceneActive && this.configuration.ToggleUiHideDuringCutscenes &&
             !(this.DisableCutsceneUiHide || this.DisableAutomaticUiHide)) ||
            (UiHideState.GPosing && this.configuration.ToggleUiHideDuringGpose &&
             !(this.DisableGposeUiHide || this.DisableAutomaticUiHide)))
        {
            if (!this.lastFrameUiHideState)
            {
                this.lastFrameUiHideState = true;
                this.HideUi?.InvokeSafely();
            }

            return;
        }

        if (this.lastFrameUiHideState)
        {
            this.lastFrameUiHideState = false;
            this.ShowUi?.InvokeSafely();
        }

        if (DoStats) this.PluginDrawStatistics.StartUpdate();

        if (this.hasErrorWindow)
        {
            this.DrawErrorWindow();
        }
        else
        {
            try
            {
                this.Draw?.Invoke();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[{0}] UiBuilder OnBuildUi caught exception", this.namespaceName);
                this.hasErrorWindow = true;
                this.lastError = ex;
            }
        }

        this.FrameCount++;

        if (DoStats) this.PluginDrawStatistics.EndUpdate();

        this.hitchDetector.Stop();
    }

    private void DrawErrorWindow()
    {
        ImGui.SetNextWindowPos(ImGuiHelpers.MainViewport.GetCenter(), ImGuiCond.Appearing, new Vector2(0.5f));
        ImGui.SetNextWindowSizeConstraints(new Vector2(800 * ImGuiHelpers.GlobalScale, 0), new Vector2(float.MaxValue));
        ImGuiHelpers.ForceNextWindowMainViewport();
        if (!ImGui.Begin($"{this.namespaceName} Error", ref this.hasErrorWindow, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.End();
            return;
        }

        DalamudComponents.DrawErrorDisplay(
            Loc.Localize("UiBuilderErrorOccurred", "An error occurred while rendering this plugin. Please contact the developer for details."),
            this.lastError,
            [
                (Loc.Localize("UiBuilderErrorRecoverButton", "Attempt to retry"), () =>
                    {
                        this.hasErrorWindow = false;
                        this.lastError = null;
                    }),
                (Loc.Localize("UiBuilderErrorReloadButton", "Reload Plugin"), () =>
                {
                    var pluginName = this.plugin.Name;
                    this.hasErrorWindow = false;
                    this.lastError = null;
                    _ = this.plugin.ReloadAsync().ContinueWith(
                        t =>
                        {
                            var notificationManager = Service<NotificationManager>.Get();
                            if (t.IsCompletedSuccessfully)
                            {
                                notificationManager.AddNotification(
                                    string.Format(
                                        Loc.Localize("UiBuilderReloadSuccess", "The plugin '{0}' was reloaded successfully."),
                                        pluginName),
                                    Loc.Localize("UiBuilderReloadSuccessTitle", "Plugin reloaded!"),
                                    NotificationType.Success);
                            }
                            else
                            {
                                notificationManager.AddNotification(
                                    string.Format(
                                        Loc.Localize("UiBuilderReloadFailure", "The plugin '{0}' could not be reloaded."),
                                        pluginName),
                                    Loc.Localize("UiBuilderReloadFailureTitle", "Plugin reload failed!"),
                                    NotificationType.Error);
                            }
                        });
                })
            ]);

        ImGui.End();
    }

    private void OnResizeBuffers()
    {
        this.ResizeBuffers?.InvokeSafely();
    }

    private void OnDefaultStyleChanged()
        => this.DefaultStyleChanged.InvokeSafely();

    private void OnDefaultGlobalScaleChanged()
        => this.DefaultGlobalScaleChanged.InvokeSafely();

    private void OnDefaultFontChanged()
        => this.DefaultFontChanged.InvokeSafely();

    private void OnDecorationDisposed(TextDecorationRegistration registration)
    {
        lock (this.decorationsLock)
            this.decorations?.Remove(registration);
    }

    private void DisposeDecorations()
    {
        HashSet<TextDecorationRegistration>? decorations;
        lock (this.decorationsLock)
            (decorations, this.decorations) = (this.decorations, null);
        foreach (var registration in decorations ?? [])
            registration.Dispose();
    }
}
