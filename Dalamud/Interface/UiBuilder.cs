using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

using Dalamud.Bindings.ImGui;
using Dalamud.Configuration.Internal;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Gui;
using Dalamud.Interface.FontIdentifier;
using Dalamud.Interface.Internal;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.ManagedFontAtlas.Internals;
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

    /// <summary>
    /// Gets the handle to the default Dalamud font - supporting all game languages and icons.
    /// </summary>
    /// <remarks>
    /// A font handle corresponding to this font can be obtained with:
    /// <code>
    /// fontAtlas.NewDelegateFontHandle(
    ///     e => e.OnPreBuild(
    ///         tk => tk.AddDalamudDefaultFont(UiBuilder.DefaultFontSizePx)));
    /// </code>
    /// </remarks>
    IFontHandle DefaultFontHandle { get; }

    /// <summary>
    /// Gets the default Dalamud icon font based on FontAwesome 5 Free solid.
    /// </summary>
    /// <remarks>
    /// A font handle corresponding to this font can be obtained with:
    /// <code>
    /// fontAtlas.NewDelegateFontHandle(
    ///     e => e.OnPreBuild(
    ///         tk => tk.AddFontAwesomeIconFont(new() { SizePt = UiBuilder.DefaultFontSizePt })));
    /// // or use
    ///         tk => tk.AddFontAwesomeIconFont(new() { SizePx = UiBuilder.DefaultFontSizePx })));
    /// </code>
    /// </remarks>
    IFontHandle IconFontHandle { get; }

    /// <summary>
    /// Gets the default Dalamud monospaced font based on Inconsolata Regular.
    /// </summary>
    /// <remarks>
    /// A font handle corresponding to this font can be obtained with:
    /// <code>
    /// fontAtlas.NewDelegateFontHandle(
    ///     e => e.OnPreBuild(
    ///         tk => tk.AddDalamudAssetFont(
    ///             DalamudAsset.InconsolataRegular,
    ///             new() { SizePt = UiBuilder.DefaultFontSizePt })));
    /// // or use
    ///             new() { SizePx = UiBuilder.DefaultFontSizePx })));
    /// </code>
    /// </remarks>
    IFontHandle MonoFontHandle { get; }

    /// <summary>
    /// Gets the default Dalamud icon font based on FontAwesome 5 free solid with a fixed width and vertically centered glyphs.
    /// </summary>
    IFontHandle IconFontFixedWidthHandle { get; }

    /// <summary>
    /// Gets the default font specifications.
    /// </summary>
    IFontSpec DefaultFontSpec { get; }

    /// <summary>
    /// Gets the game's active Direct3D device.
    /// </summary>
    // TODO: Remove it on API11/APIXI, and remove SharpDX/PInvoke/etc. dependency from Dalamud.
    [Obsolete($"Use {nameof(DeviceHandle)} and wrap it using DirectX wrapper library of your choice.")]
    SharpDX.Direct3D11.Device Device { get; }

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
    /// Gets the plugin-private font atlas.
    /// </summary>
    IFontAtlas FontAtlas { get; }

    /// <summary>
    /// Gets a value indicating whether to use "reduced motion". This usually means that you should use less
    /// intrusive animations, or disable them entirely.
    /// </summary>
    bool ShouldUseReducedMotion { get; }

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

    /// <summary>
    /// Creates an isolated <see cref="IFontAtlas"/>.
    /// </summary>
    /// <param name="autoRebuildMode">Specify when and how to rebuild this atlas.</param>
    /// <param name="isGlobalScaled">Whether the fonts in the atlas is global scaled.</param>
    /// <param name="debugName">Name for debugging purposes.</param>
    /// <returns>A new instance of <see cref="IFontAtlas"/>.</returns>
    /// <remarks>
    /// Use this to create extra font atlases, if you want to create and dispose fonts without having to rebuild all
    /// other fonts together.<br />
    /// If <paramref name="autoRebuildMode"/> is not <see cref="FontAtlasAutoRebuildMode.OnNewFrame"/>,
    /// the font rebuilding functions must be called manually.
    /// </remarks>
    IFontAtlas CreateFontAtlas(
        FontAtlasAutoRebuildMode autoRebuildMode,
        bool isGlobalScaled = true,
        string? debugName = null);
}

/// <summary>
/// This class represents the Dalamud UI that is drawn on top of the game.
/// It can be used to draw custom windows and overlays.
/// </summary>
public sealed class UiBuilder : IDisposable, IUiBuilder
{
    private readonly LocalPlugin plugin;
    private readonly Stopwatch stopwatch;
    private readonly HitchDetector hitchDetector;
    private readonly string namespaceName;
    private readonly InterfaceManager interfaceManager = Service<InterfaceManager>.Get();
    private readonly Framework framework = Service<Framework>.Get();

    [ServiceManager.ServiceDependency]
    private readonly DalamudConfiguration configuration = Service<DalamudConfiguration>.Get();

    private readonly DisposeSafety.ScopedFinalizer scopedFinalizer = new();

    private bool hasErrorWindow = false;
    private bool lastFrameUiHideState = false;

    private IFontHandle? defaultFontHandle;
    private IFontHandle? iconFontHandle;
    private IFontHandle? monoFontHandle;
    private IFontHandle? iconFontFixedWidthHandle;

    private SharpDX.Direct3D11.Device? sdxDevice;

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
            this.stopwatch = new Stopwatch();
            this.hitchDetector = new HitchDetector($"UiBuilder({namespaceName})", this.configuration.UiBuilderHitch);
            this.namespaceName = namespaceName;
            this.plugin = plugin;

            this.interfaceManager.Draw += this.OnDraw;
            this.scopedFinalizer.Add(() => this.interfaceManager.Draw -= this.OnDraw);

            this.interfaceManager.ResizeBuffers += this.OnResizeBuffers;
            this.scopedFinalizer.Add(() => this.interfaceManager.ResizeBuffers -= this.OnResizeBuffers);

            this.FontAtlas =
                this.scopedFinalizer
                    .Add(
                        Service<FontAtlasFactory>
                            .Get()
                            .CreateFontAtlas(namespaceName, FontAtlasAutoRebuildMode.Async));
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

    /// <summary>
    /// Gets the default Dalamud font size in points.
    /// </summary>
    public static float DefaultFontSizePt => Service<FontAtlasFactory>.Get().DefaultFontSpec.SizePt;

    /// <summary>
    /// Gets the default Dalamud font size in pixels.
    /// </summary>
    public static float DefaultFontSizePx => Service<FontAtlasFactory>.Get().DefaultFontSpec.SizePx;

    /// <summary>
    /// Gets the default Dalamud font - supporting all game languages and icons.<br />
    /// <strong>Accessing this static property outside of <see cref="Draw"/> is dangerous and not supported.</strong>
    /// </summary>
    public static ImFontPtr DefaultFont => InterfaceManager.DefaultFont;

    /// <summary>
    /// Gets the default Dalamud icon font based on FontAwesome 5 Free solid.<br />
    /// <strong>Accessing this static property outside of <see cref="Draw"/> is dangerous and not supported.</strong>
    /// </summary>
    public static ImFontPtr IconFont => InterfaceManager.IconFont;

    /// <summary>
    /// Gets the default Dalamud monospaced font based on Inconsolata Regular.<br />
    /// <strong>Accessing this static property outside of <see cref="Draw"/> is dangerous and not supported.</strong>
    /// </summary>
    public static ImFontPtr MonoFont => InterfaceManager.MonoFont;

    /// <summary>
    /// Gets the default font specifications.
    /// </summary>
    public IFontSpec DefaultFontSpec => Service<FontAtlasFactory>.Get().DefaultFontSpec;

    /// <summary>
    /// Gets the handle to the default Dalamud font - supporting all game languages and icons.
    /// </summary>
    /// <remarks>
    /// A font handle corresponding to this font can be obtained with:
    /// <code>
    /// fontAtlas.NewDelegateFontHandle(
    ///     e => e.OnPreBuild(
    ///         tk => tk.AddDalamudDefaultFont(UiBuilder.DefaultFontSizePx)));
    /// </code>
    /// </remarks>
    public IFontHandle DefaultFontHandle =>
        this.defaultFontHandle ??=
            this.scopedFinalizer.Add(
                new FontHandleWrapper(
                    this.InterfaceManagerWithScene?.DefaultFontHandle
                    ?? throw new InvalidOperationException("Scene is not yet ready.")));

    /// <summary>
    /// Gets the default Dalamud icon font based on FontAwesome 5 Free solid.
    /// </summary>
    /// <remarks>
    /// A font handle corresponding to this font can be obtained with:
    /// <code>
    /// fontAtlas.NewDelegateFontHandle(
    ///     e => e.OnPreBuild(
    ///         tk => tk.AddFontAwesomeIconFont(new() { SizePt = UiBuilder.DefaultFontSizePt })));
    /// // or use
    ///         tk => tk.AddFontAwesomeIconFont(new() { SizePx = UiBuilder.DefaultFontSizePx })));
    /// </code>
    /// </remarks>
    public IFontHandle IconFontHandle =>
        this.iconFontHandle ??=
            this.scopedFinalizer.Add(
                new FontHandleWrapper(
                    this.InterfaceManagerWithScene?.IconFontHandle
                    ?? throw new InvalidOperationException("Scene is not yet ready.")));

    /// <summary>
    /// Gets the default Dalamud icon font based on FontAwesome 5 free solid with a fixed width and vertically centered glyphs.
    /// </summary>
    public IFontHandle IconFontFixedWidthHandle =>
        this.iconFontFixedWidthHandle ??=
            this.scopedFinalizer.Add(
                new FontHandleWrapper(
                    this.InterfaceManagerWithScene?.IconFontFixedWidthHandle
                    ?? throw new InvalidOperationException("Scene is not yet ready.")));

    /// <summary>
    /// Gets the default Dalamud monospaced font based on Inconsolata Regular.
    /// </summary>
    /// <remarks>
    /// A font handle corresponding to this font can be obtained with:
    /// <code>
    /// fontAtlas.NewDelegateFontHandle(
    ///     e => e.OnPreBuild(
    ///         tk => tk.AddDalamudAssetFont(
    ///             DalamudAsset.InconsolataRegular,
    ///             new() { SizePt = UiBuilder.DefaultFontSizePt })));
    /// // or use
    ///             new() { SizePx = UiBuilder.DefaultFontSizePx })));
    /// </code>
    /// </remarks>
    public IFontHandle MonoFontHandle =>
        this.monoFontHandle ??=
            this.scopedFinalizer.Add(
                new FontHandleWrapper(
                    this.InterfaceManagerWithScene?.MonoFontHandle
                    ?? throw new InvalidOperationException("Scene is not yet ready.")));

    /// <inheritdoc/>
    // TODO: Remove it on API11/APIXI, and remove SharpDX/PInvoke/etc. dependency from Dalamud.
    [Obsolete($"Use {nameof(DeviceHandle)} and wrap it using DirectX wrapper library of your choice.")]
    public SharpDX.Direct3D11.Device Device =>
        this.sdxDevice ??= new(this.InterfaceManagerWithScene!.Backend!.DeviceHandle);

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
    public bool CutsceneActive
    {
        get
        {
            var condition = Service<Condition>.GetNullable();
            if (condition == null)
                return false;
            return condition[ConditionFlag.OccupiedInCutSceneEvent]
                   || condition[ConditionFlag.WatchingCutscene78];
        }
    }

    /// <summary>
    /// Gets a value indicating whether this plugin should modify the game's interface at this time.
    /// </summary>
    public bool ShouldModifyUi => this.interfaceManager.IsDispatchingEvents;

    /// <summary>
    /// Gets a value indicating whether UI functions can be used.
    /// </summary>
    public bool UiPrepared => Service<InterfaceManager.InterfaceManagerWithScene>.GetNullable() != null;

    /// <summary>
    /// Gets the plugin-private font atlas.
    /// </summary>
    public IFontAtlas FontAtlas { get; }

    /// <summary>
    /// Gets a value indicating whether to use "reduced motion". This usually means that you should use less
    /// intrusive animations, or disable them entirely.
    /// </summary>
    public bool ShouldUseReducedMotion => Service<DalamudConfiguration>.Get().ReduceMotions ?? false;

    /// <summary>
    /// Gets or sets a value indicating whether statistics about UI draw time should be collected.
    /// </summary>
#if DEBUG
    internal static bool DoStats { get; set; } = true;
#else
    internal static bool DoStats { get; set; } = false;
#endif

    /// <summary>
    /// Gets a value indicating whether this UiBuilder has a configuration UI registered.
    /// </summary>
    internal bool HasConfigUi => this.OpenConfigUi != null;

    /// <summary>
    /// Gets a value indicating whether this UiBuilder has a configuration UI registered.
    /// </summary>
    internal bool HasMainUi => this.OpenMainUi != null;

    /// <summary>
    /// Gets or sets the time this plugin took to draw on the last frame.
    /// </summary>
    internal long LastDrawTime { get; set; } = -1;

    /// <summary>
    /// Gets or sets the longest amount of time this plugin ever took to draw.
    /// </summary>
    internal long MaxDrawTime { get; set; } = -1;

    /// <summary>
    /// Gets or sets a history of the last draw times, used to calculate an average.
    /// </summary>
    internal List<long> DrawTimeHistory { get; set; } = new List<long>();

    private InterfaceManager? InterfaceManagerWithScene =>
        Service<InterfaceManager.InterfaceManagerWithScene>.GetNullable()?.Manager;

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

    /// <summary>
    /// Creates an isolated <see cref="IFontAtlas"/>.
    /// </summary>
    /// <param name="autoRebuildMode">Specify when and how to rebuild this atlas.</param>
    /// <param name="isGlobalScaled">Whether the fonts in the atlas is global scaled.</param>
    /// <param name="debugName">Name for debugging purposes.</param>
    /// <returns>A new instance of <see cref="IFontAtlas"/>.</returns>
    /// <remarks>
    /// Use this to create extra font atlases, if you want to create and dispose fonts without having to rebuild all
    /// other fonts together.<br />
    /// If <paramref name="autoRebuildMode"/> is not <see cref="FontAtlasAutoRebuildMode.OnNewFrame"/>,
    /// the font rebuilding functions must be called manually.
    /// </remarks>
    public IFontAtlas CreateFontAtlas(
        FontAtlasAutoRebuildMode autoRebuildMode,
        bool isGlobalScaled = true,
        string? debugName = null) =>
        this.scopedFinalizer.Add(Service<FontAtlasFactory>
                                 .Get()
                                 .CreateFontAtlas(
                                     this.namespaceName + ":" + (debugName ?? "custom"),
                                     autoRebuildMode,
                                     isGlobalScaled,
                                     this.plugin));

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

        var clientState = Service<ClientState>.Get();
        var gameGui = Service<GameGui>.GetNullable();
        if (gameGui == null)
            return;

        if ((gameGui.GameUiHidden && this.configuration.ToggleUiHide &&
             !(this.DisableUserUiHide || this.DisableAutomaticUiHide)) ||
            (this.CutsceneActive && this.configuration.ToggleUiHideDuringCutscenes &&
             !(this.DisableCutsceneUiHide || this.DisableAutomaticUiHide)) ||
            (clientState.IsGPosing && this.configuration.ToggleUiHideDuringGpose &&
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

        ImGui.PushID(this.namespaceName);
        if (DoStats)
        {
            this.stopwatch.Restart();
        }

        if (this.hasErrorWindow && ImGui.Begin($"{this.namespaceName} Error", ref this.hasErrorWindow, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize))
        {
            ImGui.Text($"The plugin {this.namespaceName} ran into an error.\nContact the plugin developer for support.\n\nPlease try restarting your game.");
            ImGui.Spacing();

            if (ImGui.Button("OK"))
            {
                this.hasErrorWindow = false;
            }

            ImGui.End();
        }

        try
        {
            this.Draw?.InvokeSafely();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[{0}] UiBuilder OnBuildUi caught exception", this.namespaceName);
            this.Draw = null;
            this.OpenConfigUi = null;

            this.hasErrorWindow = true;
        }

        this.FrameCount++;

        if (DoStats)
        {
            this.stopwatch.Stop();
            this.LastDrawTime = this.stopwatch.ElapsedTicks;
            this.MaxDrawTime = Math.Max(this.LastDrawTime, this.MaxDrawTime);
            this.DrawTimeHistory.Add(this.LastDrawTime);
            while (this.DrawTimeHistory.Count > 100) this.DrawTimeHistory.RemoveAt(0);
        }

        ImGui.PopID();

        this.hitchDetector.Stop();
    }

    private void OnResizeBuffers()
    {
        this.ResizeBuffers?.InvokeSafely();
    }

    private class FontHandleWrapper : IFontHandle
    {
        private IFontHandle? wrapped;

        public FontHandleWrapper(IFontHandle wrapped)
        {
            this.wrapped = wrapped;
            this.wrapped.ImFontChanged += this.WrappedOnImFontChanged;
        }

        public event IFontHandle.ImFontChangedDelegate? ImFontChanged;

        public Exception? LoadException => this.WrappedNotDisposed.LoadException;

        public bool Available => this.WrappedNotDisposed.Available;

        private IFontHandle WrappedNotDisposed =>
            this.wrapped ?? throw new ObjectDisposedException(nameof(FontHandleWrapper));

        public void Dispose()
        {
            if (this.wrapped is not { } w)
                return;

            this.wrapped = null;
            w.ImFontChanged -= this.WrappedOnImFontChanged;
            // Note: do not dispose w; we do not own it
        }

        public ILockedImFont Lock() =>
            this.wrapped?.Lock() ?? throw new ObjectDisposedException(nameof(FontHandleWrapper));

        public IDisposable Push() => this.WrappedNotDisposed.Push();

        public void Pop() => this.WrappedNotDisposed.Pop();

        public Task<IFontHandle> WaitAsync() =>
            this.WrappedNotDisposed.WaitAsync().ContinueWith(_ => (IFontHandle)this);

        public override string ToString() =>
            $"{nameof(FontHandleWrapper)}({this.wrapped?.ToString() ?? "disposed"})";

        private void WrappedOnImFontChanged(IFontHandle obj, ILockedImFont lockedFont) =>
            this.ImFontChanged?.Invoke(obj, lockedFont);
    }
}
