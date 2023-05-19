using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

using Dalamud.Configuration.Internal;
using Dalamud.Game;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Gui;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Internal.ManagedAsserts;
using Dalamud.Interface.Internal.Notifications;
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
    private readonly Stopwatch stopwatch;
    private readonly HitchDetector hitchDetector;
    private readonly string namespaceName;
    private readonly InterfaceManager interfaceManager = Service<InterfaceManager>.Get();
    private readonly GameFontManager gameFontManager = Service<GameFontManager>.Get();

    private bool hasErrorWindow = false;
    private bool lastFrameUiHideState = false;

    [ServiceManager.ServiceDependency]
    private readonly DalamudConfiguration configuration = Service<DalamudConfiguration>.Get();

    /// <summary>
    /// Initializes a new instance of the <see cref="UiBuilder"/> class and registers it.
    /// You do not have to call this manually.
    /// </summary>
    /// <param name="namespaceName">The plugin namespace.</param>
    internal UiBuilder(string namespaceName)
    {
        this.stopwatch = new Stopwatch();
        this.hitchDetector = new HitchDetector($"UiBuilder({namespaceName})", this.configuration.UiBuilderHitch);
        this.namespaceName = namespaceName;

        this.interfaceManager.Draw += this.OnDraw;
        this.interfaceManager.BuildFonts += this.OnBuildFonts;
        this.interfaceManager.AfterBuildFonts += this.OnAfterBuildFonts;
        this.interfaceManager.ResizeBuffers += this.OnResizeBuffers;
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
    /// Gets or sets an action that is called any time ImGui fonts need to be rebuilt.<br/>
    /// Any ImFontPtr objects that you store <strong>can be invalidated</strong> when fonts are rebuilt
    /// (at any time), so you should both reload your custom fonts and restore those
    /// pointers inside this handler.<br/>
    /// <strong>PLEASE remove this handler inside Dispose, or when you no longer need your fonts!</strong>
    /// </summary>
    public event Action BuildFonts;

    /// <summary>
    /// Gets or sets an action that is called any time right after ImGui fonts are rebuilt.<br/>
    /// Any ImFontPtr objects that you store <strong>can be invalidated</strong> when fonts are rebuilt
    /// (at any time), so you should both reload your custom fonts and restore those
    /// pointers inside this handler.<br/>
    /// <strong>PLEASE remove this handler inside Dispose, or when you no longer need your fonts!</strong>
    /// </summary>
    public event Action AfterBuildFonts;

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
    /// Gets the default Dalamud font based on Noto Sans CJK Medium in 17pt - supporting all game languages and icons.
    /// </summary>
    public static ImFontPtr DefaultFont => InterfaceManager.DefaultFont;

    /// <summary>
    /// Gets the default Dalamud icon font based on FontAwesome 5 Free solid in 17pt.
    /// </summary>
    public static ImFontPtr IconFont => InterfaceManager.IconFont;

    /// <summary>
    /// Gets the default Dalamud monospaced font based on Inconsolata Regular in 16pt.
    /// </summary>
    public static ImFontPtr MonoFont => InterfaceManager.MonoFont;

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
    /// Gets a value indicating whether or not gpose is active.
    /// </summary>
    public bool GposeActive
    {
        get
        {
            var condition = Service<Condition>.GetNullable();
            if (condition == null)
                return false;
            return condition[ConditionFlag.WatchingCutscene];
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
    public TextureWrap LoadImage(string filePath)
        => this.InterfaceManagerWithScene?.LoadImage(filePath)
           ?? throw new InvalidOperationException("Load failed.");

    /// <summary>
    /// Loads an image from a byte stream, such as a png downloaded into memory.
    /// </summary>
    /// <param name="imageData">A byte array containing the raw image data.</param>
    /// <returns>A <see cref="TextureWrap"/> object wrapping the created image.  Use <see cref="TextureWrap.ImGuiHandle"/> inside ImGui.Image().</returns>
    public TextureWrap LoadImage(byte[] imageData)
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
    public TextureWrap LoadImageRaw(byte[] imageData, int width, int height, int numChannels)
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
    public Task<TextureWrap> LoadImageAsync(string filePath) => Task.Run(
        async () =>
            (await this.InterfaceManagerWithSceneAsync).LoadImage(filePath)
            ?? throw new InvalidOperationException("Load failed."));

    /// <summary>
    /// Asynchronously loads an image from a byte stream, such as a png downloaded into memory, when it's possible to do so.
    /// </summary>
    /// <param name="imageData">A byte array containing the raw image data.</param>
    /// <returns>A <see cref="TextureWrap"/> object wrapping the created image.  Use <see cref="TextureWrap.ImGuiHandle"/> inside ImGui.Image().</returns>
    public Task<TextureWrap> LoadImageAsync(byte[] imageData) => Task.Run(
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
    public Task<TextureWrap> LoadImageRawAsync(byte[] imageData, int width, int height, int numChannels) => Task.Run(
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
                       .ContinueWith(_ => Service<Framework>.Get().RunOnFrameworkThread(func))
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
                       .ContinueWith(_ => Service<Framework>.Get().RunOnFrameworkThread(func))
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
    public GameFontHandle GetGameFontHandle(GameFontStyle style) => this.gameFontManager.NewFontRef(style);

    /// <summary>
    /// Call this to queue a rebuild of the font atlas.<br/>
    /// This will invoke any <see cref="OnBuildFonts"/> handlers and ensure that any loaded fonts are
    /// ready to be used on the next UI frame.
    /// </summary>
    public void RebuildFonts()
    {
        Log.Verbose("[FONT] {0} plugin is initiating FONT REBUILD", this.namespaceName);
        this.interfaceManager.RebuildFonts();
    }

    /// <summary>
    /// Add a notification to the notification queue.
    /// </summary>
    /// <param name="content">The content of the notification.</param>
    /// <param name="title">The title of the notification.</param>
    /// <param name="type">The type of the notification.</param>
    /// <param name="msDelay">The time the notification should be displayed for.</param>
    public void AddNotification(
        string content, string? title = null, NotificationType type = NotificationType.None, uint msDelay = 3000)
    {
        Service<NotificationManager>
            .GetAsync()
            .ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully)
                    task.Result.AddNotification(content, title, type, msDelay);
            });
    }

    /// <summary>
    /// Unregister the UiBuilder. Do not call this in plugin code.
    /// </summary>
    void IDisposable.Dispose()
    {
        this.interfaceManager.Draw -= this.OnDraw;
        this.interfaceManager.BuildFonts -= this.OnBuildFonts;
        this.interfaceManager.ResizeBuffers -= this.OnResizeBuffers;
    }

    /// <summary>
    /// Open the registered configuration UI, if it exists.
    /// </summary>
    internal void OpenConfig()
    {
        this.OpenConfigUi?.InvokeSafely();
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

        var configuration = Service<DalamudConfiguration>.Get();
        var gameGui = Service<GameGui>.GetNullable();
        if (gameGui == null)
            return;

        if ((gameGui.GameUiHidden && configuration.ToggleUiHide &&
             !(this.DisableUserUiHide || this.DisableAutomaticUiHide)) ||
            (this.CutsceneActive && configuration.ToggleUiHideDuringCutscenes &&
             !(this.DisableCutsceneUiHide || this.DisableAutomaticUiHide)) ||
            (this.GposeActive && configuration.ToggleUiHideDuringGpose &&
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

        if (!this.interfaceManager.FontsReady)
            return;

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

    private void OnBuildFonts()
    {
        this.BuildFonts?.InvokeSafely();
    }

    private void OnAfterBuildFonts()
    {
        this.AfterBuildFonts?.InvokeSafely();
    }

    private void OnResizeBuffers()
    {
        this.ResizeBuffers?.InvokeSafely();
    }
}
