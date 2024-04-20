using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

using Dalamud.Configuration.Internal;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Gui;
using Dalamud.Interface.FontIdentifier;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.ImGuiNotification.Internal;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Internal.ManagedAsserts;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.ManagedFontAtlas.Internals;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Plugin.Services;
using Dalamud.Utility;

using ImGuiNET;

using ImGuiScene;

using Serilog;

using SharpDX.Direct3D11;

namespace Dalamud.Interface;

/// <summary>
/// This class represents the Dalamud UI that is drawn on top of the game.
/// It can be used to draw custom windows and overlays.
/// </summary>
public sealed class UiBuilder : IDisposable
{
    private readonly LocalPlugin localPlugin;
    private readonly Stopwatch stopwatch;
    private readonly HitchDetector hitchDetector;
    private readonly string namespaceName;
    private readonly InterfaceManager interfaceManager = Service<InterfaceManager>.Get();
    private readonly Framework framework = Service<Framework>.Get();
    private readonly ConcurrentDictionary<IActiveNotification, int> notifications = new();

    [ServiceManager.ServiceDependency]
    private readonly DalamudConfiguration configuration = Service<DalamudConfiguration>.Get();

    private readonly DisposeSafety.ScopedFinalizer scopedFinalizer = new();

    private bool hasErrorWindow = false;
    private bool lastFrameUiHideState = false;

    private IFontHandle? defaultFontHandle;
    private IFontHandle? iconFontHandle;
    private IFontHandle? monoFontHandle;
    private IFontHandle? iconFontFixedWidthHandle;

    /// <summary>
    /// Initializes a new instance of the <see cref="UiBuilder"/> class and registers it.
    /// You do not have to call this manually.
    /// </summary>
    /// <param name="namespaceName">The plugin namespace.</param>
    /// <param name="localPlugin">The relevant local plugin.</param>
    internal UiBuilder(string namespaceName, LocalPlugin localPlugin)
    {
        this.localPlugin = localPlugin;
        try
        {
            this.stopwatch = new Stopwatch();
            this.hitchDetector = new HitchDetector($"UiBuilder({namespaceName})", this.configuration.UiBuilderHitch);
            this.namespaceName = namespaceName;

            this.interfaceManager.Draw += this.OnDraw;
            this.scopedFinalizer.Add(() => this.interfaceManager.Draw -= this.OnDraw);

            this.interfaceManager.ResizeBuffers += this.OnResizeBuffers;
            this.scopedFinalizer.Add(() => this.interfaceManager.ResizeBuffers -= this.OnResizeBuffers);

            this.FontAtlas =
                this.scopedFinalizer
                    .Add(
                        Service<FontAtlasFactory>
                            .Get()
                            .CreateFontAtlas(namespaceName, FontAtlasAutoRebuildMode.Disable));
            this.FontAtlas.BuildStepChange += this.PrivateAtlasOnBuildStepChange;
            this.FontAtlas.RebuildRecommend += this.RebuildFonts;
        }
        catch
        {
            this.scopedFinalizer.Dispose();
            throw;
        }
    }

    /// <summary>
    /// The event that gets called when Dalamud is ready to draw your windows or overlays.
    /// When it is called, you can use static ImGui calls.
    /// </summary>
    public event Action Draw;

    /// <summary>
    /// The event that is called when the game's DirectX device is requesting you to resize your buffers.
    /// </summary>
    public event Action ResizeBuffers;

    /// <summary>
    /// Event that is fired when the plugin should open its configuration interface.
    /// </summary>
    public event Action OpenConfigUi;

    /// <summary>
    /// Event that is fired when the plugin should open its main interface.
    /// </summary>
    public event Action OpenMainUi;

    /// <summary>
    /// Gets or sets an action that is called any time ImGui fonts need to be rebuilt.<br/>
    /// Any ImFontPtr objects that you store <b>can be invalidated</b> when fonts are rebuilt
    /// (at any time), so you should both reload your custom fonts and restore those
    /// pointers inside this handler.
    /// </summary>
    /// <remarks>
    /// To add your custom font, use <see cref="FontAtlas"/>.<see cref="IFontAtlas.NewDelegateFontHandle"/> or
    /// <see cref="IFontAtlas.NewGameFontHandle"/>.<br />
    /// To be notified on font changes after fonts are built, use
    /// <see cref="DefaultFontHandle"/>.<see cref="IFontHandle.ImFontChanged"/>.<br />
    /// For all other purposes, use <see cref="FontAtlas"/>.<see cref="IFontAtlas.BuildStepChange"/>.<br />
    /// <br />
    /// Note that you will be calling above functions once, instead of every time inside a build step change callback.
    /// For example, you can make all font handles from your plugin constructor, and then use the created handles during
    /// <see cref="Draw"/> event, by using <see cref="IFontHandle.Push"/> in a scope.<br />
    /// You may dispose your font handle anytime, as long as it's not in use in <see cref="Draw"/>.
    /// Font handles may be constructed anytime, as long as the owner <see cref="IFontAtlas"/> or
    /// <see cref="UiBuilder"/> is not disposed.<br />
    /// <br />
    /// If you were storing <see cref="ImFontPtr"/>, consider if the job can be achieved solely by using
    /// <see cref="IFontHandle"/> without directly using an instance of <see cref="ImFontPtr"/>.<br />
    /// If you do need it, evaluate if you need to access fonts outside the main thread.<br />
    /// If it is the case, use <see cref="IFontHandle.Lock"/> to obtain a safe-to-access instance of
    /// <see cref="ImFontPtr"/>, once <see cref="IFontHandle.WaitAsync"/> resolves.<br />
    /// Otherwise, use <see cref="IFontHandle.Push"/>, and obtain the instance of <see cref="ImFontPtr"/> via
    /// <see cref="ImGui.GetFont"/>. Do not let the <see cref="ImFontPtr"/> escape the <c>using</c> scope.<br />
    /// <br />
    /// If your plugin sets <see cref="PluginManifest.LoadRequiredState"/> to a non-default value, then
    /// <see cref="DefaultFontHandle"/> should be accessed using
    /// <see cref="RunWhenUiPrepared{T}(System.Func{T},bool)"/>, as the font handle member variables are only available
    /// once drawing facilities are available.<br />
    /// <br />
    /// <b>Examples:</b><br />
    /// * <see cref="InterfaceManager.ContinueConstruction"/>.<br />
    /// * <see cref="Interface.Internal.Windows.Data.Widgets.GamePrebakedFontsTestWidget"/>.<br />
    /// * <see cref="Interface.Internal.Windows.TitleScreenMenuWindow"/> ctor.<br />
    /// * <see cref="Interface.Internal.Windows.Settings.Tabs.SettingsTabAbout"/>:
    /// note how the construction of a new instance of <see cref="IFontAtlas"/> and
    /// call of <see cref="IFontAtlas.NewGameFontHandle"/> are done in different functions,
    /// without having to manually initiate font rebuild process.
    /// </remarks>
    [Obsolete("See remarks.", false)]
    [Api10ToDo(Api10ToDoAttribute.DeleteCompatBehavior)]
    public event Action? BuildFonts;

    /// <summary>
    /// Gets or sets an action that is called any time right after ImGui fonts are rebuilt.<br/>
    /// Any ImFontPtr objects that you store <b>can be invalidated</b> when fonts are rebuilt
    /// (at any time), so you should both reload your custom fonts and restore those
    /// pointers inside this handler.
    /// </summary>
    [Obsolete($"See remarks for {nameof(BuildFonts)}.", false)]
    [Api10ToDo(Api10ToDoAttribute.DeleteCompatBehavior)]
    public event Action? AfterBuildFonts;

    /// <summary>
    /// Gets or sets an action that is called when plugin UI or interface modifications are supposed to be shown.
    /// These may be fired consecutively.
    /// </summary>
    public event Action ShowUi;

    /// <summary>
    /// Gets or sets an action that is called when plugin UI or interface modifications are supposed to be hidden.
    /// These may be fired consecutively.
    /// </summary>
    public event Action HideUi;

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

    /// <summary>
    /// Gets the game's active Direct3D device.
    /// </summary>
    public Device Device => this.InterfaceManagerWithScene.Device!;

    /// <summary>
    /// Gets the game's main window handle.
    /// </summary>
    public IntPtr WindowHandlePtr => this.InterfaceManagerWithScene.WindowHandlePtr;

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
    /// Gets or sets a value indicating whether or not the game's cursor should be overridden with the ImGui cursor.
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
    /// Gets a value indicating whether or not a cutscene is playing.
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
    /// Gets a value indicating whether or not to use "reduced motion". This usually means that you should use less
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
    /// Loads an image from the specified file.
    /// </summary>
    /// <param name="filePath">The full filepath to the image.</param>
    /// <returns>A <see cref="TextureWrap"/> object wrapping the created image.  Use <see cref="TextureWrap.ImGuiHandle"/> inside ImGui.Image().</returns>
    public IDalamudTextureWrap LoadImage(string filePath)
        => this.InterfaceManagerWithScene?.LoadImage(filePath)
           ?? throw new InvalidOperationException("Load failed.");

    /// <summary>
    /// Loads an image from a byte stream, such as a png downloaded into memory.
    /// </summary>
    /// <param name="imageData">A byte array containing the raw image data.</param>
    /// <returns>A <see cref="TextureWrap"/> object wrapping the created image.  Use <see cref="TextureWrap.ImGuiHandle"/> inside ImGui.Image().</returns>
    public IDalamudTextureWrap LoadImage(byte[] imageData)
        => this.InterfaceManagerWithScene?.LoadImage(imageData)
           ?? throw new InvalidOperationException("Load failed.");

    /// <summary>
    /// Loads an image from raw unformatted pixel data, with no type or header information.  To load formatted data, use <see cref="LoadImage(byte[])"/>.
    /// </summary>
    /// <param name="imageData">A byte array containing the raw pixel data.</param>
    /// <param name="width">The width of the image contained in <paramref name="imageData"/>.</param>
    /// <param name="height">The height of the image contained in <paramref name="imageData"/>.</param>
    /// <param name="numChannels">The number of channels (bytes per pixel) of the image contained in <paramref name="imageData"/>.  This should usually be 4.</param>
    /// <returns>A <see cref="TextureWrap"/> object wrapping the created image.  Use <see cref="TextureWrap.ImGuiHandle"/> inside ImGui.Image().</returns>
    public IDalamudTextureWrap LoadImageRaw(byte[] imageData, int width, int height, int numChannels)
        => this.InterfaceManagerWithScene?.LoadImageRaw(imageData, width, height, numChannels)
           ?? throw new InvalidOperationException("Load failed.");

    /// <summary>
    /// Loads an ULD file that can load textures containing multiple icons in a single texture.
    /// </summary>
    /// <param name="uldPath">The path of the requested ULD file.</param>
    /// <returns>A wrapper around said ULD file.</returns>
    public UldWrapper LoadUld(string uldPath)
        => new(this, uldPath);

    /// <summary>
    /// Asynchronously loads an image from the specified file, when it's possible to do so.
    /// </summary>
    /// <param name="filePath">The full filepath to the image.</param>
    /// <returns>A <see cref="TextureWrap"/> object wrapping the created image.  Use <see cref="TextureWrap.ImGuiHandle"/> inside ImGui.Image().</returns>
    public Task<IDalamudTextureWrap> LoadImageAsync(string filePath) => Task.Run(
        async () =>
            (await this.InterfaceManagerWithSceneAsync).LoadImage(filePath)
            ?? throw new InvalidOperationException("Load failed."));

    /// <summary>
    /// Asynchronously loads an image from a byte stream, such as a png downloaded into memory, when it's possible to do so.
    /// </summary>
    /// <param name="imageData">A byte array containing the raw image data.</param>
    /// <returns>A <see cref="TextureWrap"/> object wrapping the created image.  Use <see cref="TextureWrap.ImGuiHandle"/> inside ImGui.Image().</returns>
    public Task<IDalamudTextureWrap> LoadImageAsync(byte[] imageData) => Task.Run(
        async () =>
            (await this.InterfaceManagerWithSceneAsync).LoadImage(imageData)
            ?? throw new InvalidOperationException("Load failed."));

    /// <summary>
    /// Asynchronously loads an image from raw unformatted pixel data, with no type or header information, when it's possible to do so.  To load formatted data, use <see cref="LoadImage(byte[])"/>.
    /// </summary>
    /// <param name="imageData">A byte array containing the raw pixel data.</param>
    /// <param name="width">The width of the image contained in <paramref name="imageData"/>.</param>
    /// <param name="height">The height of the image contained in <paramref name="imageData"/>.</param>
    /// <param name="numChannels">The number of channels (bytes per pixel) of the image contained in <paramref name="imageData"/>.  This should usually be 4.</param>
    /// <returns>A <see cref="TextureWrap"/> object wrapping the created image.  Use <see cref="TextureWrap.ImGuiHandle"/> inside ImGui.Image().</returns>
    public Task<IDalamudTextureWrap> LoadImageRawAsync(byte[] imageData, int width, int height, int numChannels) => Task.Run(
        async () =>
            (await this.InterfaceManagerWithSceneAsync).LoadImageRaw(imageData, width, height, numChannels)
            ?? throw new InvalidOperationException("Load failed."));

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
    /// Gets a game font.
    /// </summary>
    /// <param name="style">Font to get.</param>
    /// <returns>Handle to the game font which may or may not be available for use yet.</returns>
    [Obsolete($"Use {nameof(this.FontAtlas)}.{nameof(IFontAtlas.NewGameFontHandle)} instead.", false)]
    [Api10ToDo(Api10ToDoAttribute.DeleteCompatBehavior)]
    public GameFontHandle GetGameFontHandle(GameFontStyle style)
    {
        var prevValue = FontAtlasFactory.IsBuildInProgressForTask.Value;
        FontAtlasFactory.IsBuildInProgressForTask.Value = false;
        var v = new GameFontHandle(
            (GamePrebakedFontHandle)this.FontAtlas.NewGameFontHandle(style),
            Service<FontAtlasFactory>.Get());
        FontAtlasFactory.IsBuildInProgressForTask.Value = prevValue;
        return v;
    }

    /// <summary>
    /// Call this to queue a rebuild of the font atlas.<br/>
    /// This will invoke any <see cref="BuildFonts"/> and <see cref="AfterBuildFonts"/> handlers and ensure that any
    /// loaded fonts are ready to be used on the next UI frame.
    /// </summary>
    public void RebuildFonts()
    {
        Log.Verbose("[FONT] {0} plugin is initiating FONT REBUILD", this.namespaceName);
        if (this.AfterBuildFonts is null && this.BuildFonts is null)
            this.FontAtlas.BuildFontsAsync();
        else
            this.FontAtlas.BuildFontsOnNextFrame();
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
                                     isGlobalScaled));

    /// <summary>
    /// Add a notification to the notification queue.
    /// </summary>
    /// <param name="content">The content of the notification.</param>
    /// <param name="title">The title of the notification.</param>
    /// <param name="type">The type of the notification.</param>
    /// <param name="msDelay">The time the notification should be displayed for.</param>
    [Obsolete($"Use {nameof(INotificationManager)}.", false)]
    [Api10ToDo(Api10ToDoAttribute.DeleteCompatBehavior)]
    public async void AddNotification(
        string content,
        string? title = null,
        NotificationType type = NotificationType.None,
        uint msDelay = 3000)
    {
        var nm = await Service<NotificationManager>.GetAsync();
        var an = nm.AddNotification(
            new()
            {
                Content = content,
                Title = title,
                Type = type,
                InitialDuration = TimeSpan.FromMilliseconds(msDelay),
            },
            this.localPlugin);
        _ = this.notifications.TryAdd(an, 0);
        an.Dismiss += a => this.notifications.TryRemove(a.Notification, out _);
    }

    /// <summary>
    /// Unregister the UiBuilder. Do not call this in plugin code.
    /// </summary>
    void IDisposable.Dispose()
    {
        this.scopedFinalizer.Dispose();

        // Taken from NotificationManagerPluginScoped.
        // TODO: remove on API 10.
        while (!this.notifications.IsEmpty)
        {
            foreach (var n in this.notifications.Keys)
            {
                this.notifications.TryRemove(n, out _);
                ((ActiveNotification)n).RemoveNonDalamudInvocations();
            }
        }
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
        var configuration = Service<DalamudConfiguration>.Get();
        var gameGui = Service<GameGui>.GetNullable();
        if (gameGui == null)
            return;

        if ((gameGui.GameUiHidden && configuration.ToggleUiHide &&
             !(this.DisableUserUiHide || this.DisableAutomaticUiHide)) ||
            (this.CutsceneActive && configuration.ToggleUiHideDuringCutscenes &&
             !(this.DisableCutsceneUiHide || this.DisableAutomaticUiHide)) ||
            (clientState.IsGPosing && configuration.ToggleUiHideDuringGpose &&
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

        // just in case, if something goes wrong, prevent drawing; otherwise it probably will crash.
        if (!this.FontAtlas.BuildTask.IsCompletedSuccessfully
            && (this.BuildFonts is not null || this.AfterBuildFonts is not null))
        {
            return;
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

        ImGuiManagedAsserts.ImGuiContextSnapshot snapshot = null;
        if (this.Draw != null)
        {
            snapshot = ImGuiManagedAsserts.GetSnapshot();
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

        // Only if Draw was successful
        if (this.Draw != null)
        {
            ImGuiManagedAsserts.ReportProblems(this.namespaceName, snapshot);
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

    [Api10ToDo(Api10ToDoAttribute.DeleteCompatBehavior)]
    private unsafe void PrivateAtlasOnBuildStepChange(IFontAtlasBuildToolkit e)
    {
        if (e.IsAsyncBuildOperation)
            return;

        ThreadSafety.AssertMainThread();

        if (this.BuildFonts is not null)
        {
            e.OnPreBuild(
                _ =>
                {
                    var prev = ImGui.GetIO().NativePtr->Fonts;
                    ImGui.GetIO().NativePtr->Fonts = e.NewImAtlas.NativePtr;
                    ((IFontAtlasBuildToolkit.IApi9Compat)e)
                        .FromUiBuilderObsoleteEventHandlers(() => this.BuildFonts?.InvokeSafely());
                    ImGui.GetIO().NativePtr->Fonts = prev;
                });
        }

        if (this.AfterBuildFonts is not null)
        {
            e.OnPostBuild(
                _ =>
                {
                    var prev = ImGui.GetIO().NativePtr->Fonts;
                    ImGui.GetIO().NativePtr->Fonts = e.NewImAtlas.NativePtr;
                    ((IFontAtlasBuildToolkit.IApi9Compat)e)
                        .FromUiBuilderObsoleteEventHandlers(() => this.AfterBuildFonts?.InvokeSafely());
                    ImGui.GetIO().NativePtr->Fonts = prev;
                });
        }
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
