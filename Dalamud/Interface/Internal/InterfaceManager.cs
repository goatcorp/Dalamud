using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using CheapLoc;

using Dalamud.Configuration.Internal;
using Dalamud.Game;
using Dalamud.Game.ClientState.GamePad;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Hooking;
using Dalamud.Hooking.Internal;
using Dalamud.Hooking.WndProcHook;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.ImGuiNotification.Internal;
using Dalamud.Interface.Internal.DesignSystem;
using Dalamud.Interface.Internal.ReShadeHandling;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.ManagedFontAtlas.Internals;
using Dalamud.Interface.Style;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.Logging.Internal;
using Dalamud.Utility;
using Dalamud.Utility.Timing;

using ImGuiNET;

using ImGuiScene;

using JetBrains.Annotations;

using PInvoke;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

// general dev notes, here because it's easiest

/*
 * - Hooking ResizeBuffers seemed to be unnecessary, though I'm not sure why.  Left out for now since it seems to work without it.
 * - We may want to build our ImGui command list in a thread to keep it divorced from present.  We'd still have to block in present to
 *   synchronize on the list and render it, but ideally the overall delay we add to present would then be shorter.  This may cause minor
 *   timing issues with anything animated inside ImGui, but that is probably rare and may not even be noticeable.
 * - Our hook is too low level to really work well with debugging, as we only have access to the 'real' dx objects and not any
 *   that have been hooked/wrapped by tools.
 * - Might eventually want to render to a separate target and composite, especially with reshade etc in the mix.
 */

namespace Dalamud.Interface.Internal;

/// <summary>
/// This class manages interaction with the ImGui interface.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal partial class InterfaceManager : IInternalDisposableService
{
    /// <summary>
    /// The default font size, in points.
    /// </summary>
    public const float DefaultFontSizePt = 12.0f;

    /// <summary>
    /// The default font size, in pixels.
    /// </summary>
    public const float DefaultFontSizePx = (DefaultFontSizePt * 4.0f) / 3.0f;

    private static readonly ModuleLog Log = new("INTERFACE");
    
    private readonly ConcurrentBag<IDeferredDisposable> deferredDisposeTextures = new();
    private readonly ConcurrentBag<IDisposable> deferredDisposeDisposables = new();

    [ServiceManager.ServiceDependency]
    private readonly DalamudConfiguration dalamudConfiguration = Service<DalamudConfiguration>.Get();

    [ServiceManager.ServiceDependency]
    private readonly Framework framework = Service<Framework>.Get();

    // ReShadeAddonInterface requires hooks to be alive to unregister itself.
    [ServiceManager.ServiceDependency]
    [UsedImplicitly]
    private readonly HookManager hookManager = Service<HookManager>.Get();

    [ServiceManager.ServiceDependency]
    private readonly WndProcHookManager wndProcHookManager = Service<WndProcHookManager>.Get();

    private readonly ConcurrentQueue<Action> runBeforeImGuiRender = new();
    private readonly ConcurrentQueue<Action> runAfterImGuiRender = new();
    
    private readonly AssertHandler assertHandler = new();

    private RawDX11Scene? scene;

    private Hook<SetCursorDelegate>? setCursorHook;
    private Hook<ReShadeDxgiSwapChainPresentDelegate>? reShadeDxgiSwapChainPresentHook;
    private Hook<DxgiSwapChainPresentDelegate>? dxgiSwapChainPresentHook;
    private Hook<ResizeBuffersDelegate>? dxgiSwapChainResizeBuffersHook;
    private ObjectVTableHook<IDXGISwapChain4.Vtbl<IDXGISwapChain4>>? dxgiSwapChainHook;
    private ReShadeAddonInterface? reShadeAddonInterface;

    private IFontAtlas? dalamudAtlas;
    private ILockedImFont? defaultFontResourceLock;

    // can't access imgui IO before first present call
    private bool lastWantCapture = false;
    private bool isOverrideGameCursor = true;
    private IntPtr gameWindowHandle;

    [ServiceManager.ServiceConstructor]
    private InterfaceManager()
    {
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate IntPtr SetCursorDelegate(IntPtr hCursor);

    /// <summary>
    /// This event gets called each frame to facilitate ImGui drawing.
    /// </summary>
    public event RawDX11Scene.BuildUIDelegate? Draw;

    /// <summary>
    /// This event gets called when ResizeBuffers is called.
    /// </summary>
    public event Action? ResizeBuffers;

    /// <summary>
    /// Gets or sets an action that is executed right after fonts are rebuilt.
    /// </summary>
    public event Action? AfterBuildFonts;

    /// <summary>
    /// Gets the default ImGui font.<br />
    /// <strong>Accessing this static property outside of the main thread is dangerous and not supported.</strong>
    /// </summary>
    public static ImFontPtr DefaultFont =>
        WhenFontsReady().DefaultFontHandle!.LockUntilPostFrame().OrElse(ImGui.GetIO().FontDefault);

    /// <summary>
    /// Gets an included FontAwesome icon font.<br />
    /// <strong>Accessing this static property outside of the main thread is dangerous and not supported.</strong>
    /// </summary>
    public static ImFontPtr IconFont =>
        WhenFontsReady().IconFontHandle!.LockUntilPostFrame().OrElse(ImGui.GetIO().FontDefault);

    /// <summary>
    /// Gets an included FontAwesome icon font with fixed width.
    /// <strong>Accessing this static property outside of the main thread is dangerous and not supported.</strong>
    /// </summary>
    public static ImFontPtr IconFontFixedWidth =>
        WhenFontsReady().IconFontFixedWidthHandle!.LockUntilPostFrame().OrElse(ImGui.GetIO().FontDefault);

    /// <summary>
    /// Gets an included monospaced font.<br />
    /// <strong>Accessing this static property outside of the main thread is dangerous and not supported.</strong>
    /// </summary>
    public static ImFontPtr MonoFont =>
        WhenFontsReady().MonoFontHandle!.LockUntilPostFrame().OrElse(ImGui.GetIO().FontDefault);

    /// <summary>
    /// Gets the default font handle.
    /// </summary>
    public FontHandle? DefaultFontHandle { get; private set; }

    /// <summary>
    /// Gets the icon font handle.
    /// </summary>
    public FontHandle? IconFontHandle { get; private set; }

    /// <summary>
    /// Gets the icon font handle with fixed width.
    /// </summary>
    public FontHandle? IconFontFixedWidthHandle { get; private set; }

    /// <summary>
    /// Gets the mono font handle.
    /// </summary>
    public FontHandle? MonoFontHandle { get; private set; }

    /// <summary>
    /// Gets or sets the pointer to ImGui.IO(), when it was last used.
    /// </summary>
    public ImGuiIOPtr LastImGuiIoPtr { get; set; }

    /// <summary>
    /// Gets the DX11 scene.
    /// </summary>
    public RawDX11Scene? Scene => this.scene;

    /// <summary>
    /// Gets the D3D11 device instance.
    /// </summary>
    public SharpDX.Direct3D11.Device? Device => this.scene?.Device;

    /// <summary>
    /// Gets the address handle to the main process window.
    /// </summary>
    public IntPtr WindowHandlePtr => this.scene?.WindowHandlePtr ?? IntPtr.Zero;

    /// <summary>
    /// Gets or sets a value indicating whether or not the game's cursor should be overridden with the ImGui cursor.
    /// </summary>
    public bool OverrideGameCursor
    {
        get => this.scene?.UpdateCursor ?? this.isOverrideGameCursor;
        set
        {
            this.isOverrideGameCursor = value;
            if (this.scene != null)
                this.scene.UpdateCursor = value;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the Dalamud interface ready to use.
    /// </summary>
    public bool IsReady => this.scene != null;

    /// <summary>
    /// Gets or sets a value indicating whether or not Draw events should be dispatched.
    /// </summary>
    public bool IsDispatchingEvents { get; set; } = true;

    /// <summary>Gets a value indicating whether the main thread is executing <see cref="DxgiSwapChainPresentDetour"/>.</summary>
    /// <remarks>This still will be <c>true</c> even when queried off the main thread.</remarks>
    public bool IsMainThreadInPresent { get; private set; }

    /// <summary>
    /// Gets a value indicating the native handle of the game main window.
    /// </summary>
    public IntPtr GameWindowHandle
    {
        get
        {
            if (this.gameWindowHandle == 0)
            {
                nint gwh = 0;
                while ((gwh = NativeFunctions.FindWindowEx(0, gwh, "FFXIVGAME", 0)) != 0)
                {
                    _ = User32.GetWindowThreadProcessId(gwh, out var pid);
                    if (pid == Environment.ProcessId && User32.IsWindowVisible(gwh))
                    {
                        this.gameWindowHandle = gwh;
                        break;
                    }
                }
            }

            return this.gameWindowHandle;
        }
    }

    /// <summary>
    /// Gets the font build task.
    /// </summary>
    public Task FontBuildTask => WhenFontsReady().dalamudAtlas!.BuildTask;

    /// <summary>Gets the number of calls to <see cref="DxgiSwapChainPresentDetour"/> so far.</summary>
    /// <remarks>
    /// The value increases even when Dalamud is hidden via &quot;/xlui hide&quot;.
    /// <see cref="DalamudInterface.FrameCount"/> does not.
    /// </remarks>
    public long CumulativePresentCalls { get; private set; }

    /// <inheritdoc cref="AssertHandler.ShowAsserts"/>
    public bool ShowAsserts
    {
        get => this.assertHandler.ShowAsserts;
        set => this.assertHandler.ShowAsserts = value;
    }

    /// <summary>
    /// Dispose of managed and unmanaged resources.
    /// </summary>
    void IInternalDisposableService.DisposeService()
    {
        this.assertHandler.Dispose();

        // Unload hooks from the framework thread if possible.
        // We're currently off the framework thread, as this function can only be called from
        // ServiceManager.UnloadAllServices, which is called from EntryPoint.RunThread.
        // The functions being unhooked are mostly called from the main thread, so unhooking from the main thread when
        // possible would avoid any chance of unhooking a function that currently is being called.
        // If unloading is initiated from "Unload Dalamud" /xldev menu, then the framework would still be running, as
        // Framework.Destroy has never been called and thus Framework.IsFrameworkUnloading cannot be true, and this
        // function will actually run the destroy from the framework thread.
        // Otherwise, as Framework.IsFrameworkUnloading should have been set, this code should run immediately.
        this.framework.RunOnFrameworkThread(ClearHooks).Wait();

        // Below this point, hooks are guaranteed to be no longer called.

        // A font resource lock outlives the parent handle and the owner atlas. It should be disposed.
        Interlocked.Exchange(ref this.defaultFontResourceLock, null)?.Dispose();

        // Font handles become invalid after disposing the atlas, but just to be safe.
        this.DefaultFontHandle?.Dispose();
        this.DefaultFontHandle = null;

        this.MonoFontHandle?.Dispose();
        this.MonoFontHandle = null;

        this.IconFontHandle?.Dispose();
        this.IconFontHandle = null;

        Interlocked.Exchange(ref this.dalamudAtlas, null)?.Dispose();
        Interlocked.Exchange(ref this.scene, null)?.Dispose();

        return;

        void ClearHooks()
        {
            this.wndProcHookManager.PreWndProc -= this.WndProcHookManagerOnPreWndProc;
            Interlocked.Exchange(ref this.setCursorHook, null)?.Dispose();
            Interlocked.Exchange(ref this.dxgiSwapChainPresentHook, null)?.Dispose();
            Interlocked.Exchange(ref this.reShadeDxgiSwapChainPresentHook, null)?.Dispose();
            Interlocked.Exchange(ref this.dxgiSwapChainResizeBuffersHook, null)?.Dispose();
            Interlocked.Exchange(ref this.dxgiSwapChainHook, null)?.Dispose();
            Interlocked.Exchange(ref this.reShadeAddonInterface, null)?.Dispose();
        }
    }

    /// <summary>
    /// Sets up a deferred invocation of font rebuilding, before the next render frame.
    /// </summary>
    public void RebuildFonts()
    {
        Log.Verbose("[FONT] RebuildFonts() called");
        this.dalamudAtlas?.BuildFontsAsync();
    }

    /// <summary>
    /// Enqueue a texture to be disposed at the end of the frame.
    /// </summary>
    /// <param name="wrap">The texture.</param>
    public void EnqueueDeferredDispose(IDeferredDisposable wrap)
    {
        this.deferredDisposeTextures.Add(wrap);
    }

    /// <summary>
    /// Enqueue an <see cref="ILockedImFont"/> to be disposed at the end of the frame.
    /// </summary>
    /// <param name="locked">The disposable.</param>
    public void EnqueueDeferredDispose(IDisposable locked)
    {
        this.deferredDisposeDisposables.Add(locked);
    }

    /// <summary>Queues an action to be run before <see cref="ImGui.Render"/> call.</summary>
    /// <param name="action">The action.</param>
    /// <returns>A <see cref="Task"/> that resolves once <paramref name="action"/> is run.</returns>
    public Task RunBeforeImGuiRender(Action action)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        this.runBeforeImGuiRender.Enqueue(
            () =>
            {
                try
                {
                    action();
                    tcs.SetResult();
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }
            });
        return tcs.Task;
    }

    /// <summary>Queues a function to be run before <see cref="ImGui.Render"/> call.</summary>
    /// <typeparam name="T">The type of the return value.</typeparam>
    /// <param name="func">The function.</param>
    /// <returns>A <see cref="Task"/> that resolves once <paramref name="func"/> is run.</returns>
    public Task<T> RunBeforeImGuiRender<T>(Func<T> func)
    {
        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        this.runBeforeImGuiRender.Enqueue(
            () =>
            {
                try
                {
                    tcs.SetResult(func());
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }
            });
        return tcs.Task;
    }

    /// <summary>Queues an action to be run after <see cref="ImGui.Render"/> call.</summary>
    /// <param name="action">The action.</param>
    /// <returns>A <see cref="Task"/> that resolves once <paramref name="action"/> is run.</returns>
    public Task RunAfterImGuiRender(Action action)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        this.runAfterImGuiRender.Enqueue(
            () =>
            {
                try
                {
                    action();
                    tcs.SetResult();
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }
            });
        return tcs.Task;
    }

    /// <summary>Queues a function to be run after <see cref="ImGui.Render"/> call.</summary>
    /// <typeparam name="T">The type of the return value.</typeparam>
    /// <param name="func">The function.</param>
    /// <returns>A <see cref="Task"/> that resolves once <paramref name="func"/> is run.</returns>
    public Task<T> RunAfterImGuiRender<T>(Func<T> func)
    {
        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        this.runAfterImGuiRender.Enqueue(
            () =>
            {
                try
                {
                    tcs.SetResult(func());
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }
            });
        return tcs.Task;
    }

    /// <summary>
    /// Get video memory information.
    /// </summary>
    /// <returns>The currently used video memory, or null if not available.</returns>
    public (long Used, long Available)? GetD3dMemoryInfo()
    {
        if (this.Device == null)
            return null;

        try
        {
            var dxgiDev = this.Device.QueryInterfaceOrNull<SharpDX.DXGI.Device>();
            var dxgiAdapter = dxgiDev?.Adapter.QueryInterfaceOrNull<SharpDX.DXGI.Adapter4>();
            if (dxgiAdapter == null)
                return null;

            var memInfo = dxgiAdapter.QueryVideoMemoryInfo(0, SharpDX.DXGI.MemorySegmentGroup.Local);
            return (memInfo.CurrentUsage, memInfo.CurrentReservation);
        }
        catch
        {
            // ignored
        }

        return null;
    }

    /// <summary>
    /// Clear font, style, and color stack. Dangerous, only use when you know
    /// no one else has something pushed they may try to pop.
    /// </summary>
    public void ClearStacks()
    {
        this.scene?.ClearStacksOnContext();
    }

    /// <summary>
    /// Toggle Windows 11 immersive mode on the game window.
    /// </summary>
    /// <param name="enabled">Value.</param>
    internal void SetImmersiveMode(bool enabled)
    {
        if (this.GameWindowHandle == 0)
            throw new InvalidOperationException("Game window is not yet ready.");
        var value = enabled ? 1 : 0;
        ((HRESULT)NativeFunctions.DwmSetWindowAttribute(
                    this.GameWindowHandle,
                    NativeFunctions.DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE,
                    ref value,
                    sizeof(int))).ThrowOnError();
    }

    private static InterfaceManager WhenFontsReady()
    {
        var im = Service<InterfaceManager>.GetNullable();
        if (im?.dalamudAtlas is not { } atlas)
            throw new InvalidOperationException($"Tried to access fonts before {nameof(ContinueConstruction)} call.");

        if (!atlas.HasBuiltAtlas)
            atlas.BuildTask.GetAwaiter().GetResult();
        return im;
    }

    /// <summary>Checks if the provided swap chain is the target that Dalamud should draw its interface onto,
    /// and initializes ImGui for drawing.</summary>
    /// <param name="swapChain">The swap chain to test and initialize ImGui with if conditions are met.</param>
    /// <param name="flags">Flags passed to <see cref="IDXGISwapChain.Present"/>.</param>
    /// <returns>An initialized instance of <see cref="RawDX11Scene"/>, or <c>null</c> if <paramref name="swapChain"/>
    /// is not the main swap chain.</returns>
    private unsafe RawDX11Scene? RenderDalamudCheckAndInitialize(IDXGISwapChain* swapChain, uint flags)
    {
        // Quoting ReShade dxgi_swapchain.cpp DXGISwapChain::on_present:
        // > Some D3D11 games test presentation for timing and composition purposes
        // > These calls are not rendering related, but rather a status request for the D3D runtime and as such should be ignored
        if ((flags & DXGI.DXGI_PRESENT_TEST) != 0)
            return null;

        if (!SwapChainHelper.IsGameDeviceSwapChain(swapChain))
            return null;

        Debug.Assert(this.dalamudAtlas is not null, "dalamudAtlas should have been set already");

        var activeScene = this.scene ?? this.InitScene(swapChain);

        if (!this.dalamudAtlas!.HasBuiltAtlas)
        {
            if (this.dalamudAtlas.BuildTask.Exception != null)
            {
                // TODO: Can we do something more user-friendly here? Unload instead?
                Log.Error(this.dalamudAtlas.BuildTask.Exception, "Failed to initialize Dalamud base fonts");
                Util.Fatal("Failed to initialize Dalamud base fonts.\nPlease report this error.", "Dalamud");
            }

            return null;
        }

        return activeScene;
    }

    /// <summary>Draws Dalamud to the given scene representing the ImGui context.</summary>
    /// <param name="activeScene">The scene to draw to.</param>
    private void RenderDalamudDraw(RawDX11Scene activeScene)
    {
        this.CumulativePresentCalls++;
        this.IsMainThreadInPresent = true;

        while (this.runBeforeImGuiRender.TryDequeue(out var action))
            action.InvokeSafely();

        // Process information needed by ImGuiHelpers each frame.
        ImGuiHelpers.NewFrame();

        // Enable viewports if there are no issues.
        var viewportsEnable = this.dalamudConfiguration.IsDisableViewport ||
                              activeScene.SwapChain.IsFullScreen ||
                              ImGui.GetPlatformIO().Monitors.Size == 1;
        if (viewportsEnable)
            ImGui.GetIO().ConfigFlags &= ~ImGuiConfigFlags.ViewportsEnable;
        else
            ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;

        // Call drawing functions, which in turn will call Draw event.
        activeScene.Render();

        this.PostImGuiRender();
        this.IsMainThreadInPresent = false;
    }

    private unsafe RawDX11Scene InitScene(IDXGISwapChain* swapChain)
    {
        RawDX11Scene newScene;
        using (Timings.Start("IM Scene Init"))
        {
            try
            {
                this.assertHandler.Setup();
                newScene = new RawDX11Scene((nint)swapChain);
            }
            catch (DllNotFoundException ex)
            {
                Service<InterfaceManagerWithScene>.ProvideException(ex);
                Log.Error(ex, "Could not load ImGui dependencies.");

                var res = User32.MessageBox(
                    IntPtr.Zero,
                    "Dalamud plugins require the Microsoft Visual C++ Redistributable to be installed.\nPlease install the runtime from the official Microsoft website or disable Dalamud.\n\nDo you want to download the redistributable now?",
                    "Dalamud Error",
                    User32.MessageBoxOptions.MB_YESNO | User32.MessageBoxOptions.MB_TOPMOST | User32.MessageBoxOptions.MB_ICONERROR);

                if (res == User32.MessageBoxResult.IDYES)
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "https://aka.ms/vs/16/release/vc_redist.x64.exe",
                        UseShellExecute = true,
                    };
                    Process.Start(psi);
                }

                Environment.Exit(-1);

                // Doesn't reach here, but to make the compiler not complain
                throw new InvalidOperationException();
            }

            var startInfo = Service<Dalamud>.Get().StartInfo;
            var configuration = Service<DalamudConfiguration>.Get();

            var iniFileInfo = new FileInfo(Path.Combine(Path.GetDirectoryName(startInfo.ConfigurationPath)!, "dalamudUI.ini"));

            try
            {
                if (iniFileInfo.Length > 1200000)
                {
                    Log.Warning("dalamudUI.ini was over 1mb, deleting");
                    iniFileInfo.CopyTo(
                        Path.Combine(
                            iniFileInfo.DirectoryName!,
                            $"dalamudUI-{DateTimeOffset.Now.ToUnixTimeSeconds()}.ini"));
                    iniFileInfo.Delete();
                }
            }
            catch (FileNotFoundException)
            {
                Log.Warning("dalamudUI.ini did not exist, ImGUI will create a new one.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Could not delete dalamudUI.ini");
            }

            newScene.UpdateCursor = this.isOverrideGameCursor;
            newScene.ImGuiIniPath = iniFileInfo.FullName;
            newScene.OnBuildUI += this.Display;
            newScene.OnNewInputFrame += this.OnNewInputFrame;

            StyleModel.TransferOldModels();

            if (configuration.SavedStyles == null || configuration.SavedStyles.All(x => x.Name != StyleModelV1.DalamudStandard.Name))
            {
                configuration.SavedStyles = new List<StyleModel> { StyleModelV1.DalamudStandard, StyleModelV1.DalamudClassic };
                configuration.ChosenStyle = StyleModelV1.DalamudStandard.Name;
            }
            else if (configuration.SavedStyles.Count == 1)
            {
                configuration.SavedStyles.Add(StyleModelV1.DalamudClassic);
            }
            else if (configuration.SavedStyles[1].Name != StyleModelV1.DalamudClassic.Name)
            {
                configuration.SavedStyles.Insert(1, StyleModelV1.DalamudClassic);
            }

            configuration.SavedStyles[0] = StyleModelV1.DalamudStandard;
            configuration.SavedStyles[1] = StyleModelV1.DalamudClassic;

            var style = configuration.SavedStyles.FirstOrDefault(x => x.Name == configuration.ChosenStyle);
            if (style == null)
            {
                style = StyleModelV1.DalamudStandard;
                configuration.ChosenStyle = style.Name;
                configuration.QueueSave();
            }

            style.Apply();

            ImGui.GetIO().FontGlobalScale = configuration.GlobalUiScale;

            if (!configuration.IsDocking)
            {
                ImGui.GetIO().ConfigFlags &= ~ImGuiConfigFlags.DockingEnable;
            }
            else
            {
                ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DockingEnable;
            }

            // NOTE (Chiv) Toggle gamepad navigation via setting
            if (!configuration.IsGamepadNavigationEnabled)
            {
                ImGui.GetIO().BackendFlags &= ~ImGuiBackendFlags.HasGamepad;
                ImGui.GetIO().ConfigFlags &= ~ImGuiConfigFlags.NavEnableSetMousePos;
            }
            else
            {
                ImGui.GetIO().BackendFlags |= ImGuiBackendFlags.HasGamepad;
                ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.NavEnableSetMousePos;
            }

            // NOTE (Chiv) Explicitly deactivate on dalamud boot
            ImGui.GetIO().ConfigFlags &= ~ImGuiConfigFlags.NavEnableGamepad;

            ImGuiHelpers.MainViewport = ImGui.GetMainViewport();

            Log.Information("[IM] Scene & ImGui setup OK!");
        }

        this.scene = newScene;
        Service<InterfaceManagerWithScene>.Provide(new(this));

        this.wndProcHookManager.PreWndProc += this.WndProcHookManagerOnPreWndProc;
        return newScene;
    }

    private unsafe void WndProcHookManagerOnPreWndProc(WndProcEventArgs args)
    {
        var r = this.scene?.ProcessWndProcW(args.Hwnd, (User32.WindowMessage)args.Message, args.WParam, args.LParam);
        if (r is not null)
            args.SuppressWithValue(r.Value);
    }

    private void PostImGuiRender()
    {
        while (this.runAfterImGuiRender.TryDequeue(out var action))
            action.InvokeSafely();

        if (!this.deferredDisposeTextures.IsEmpty)
        {
            var count = 0;
            while (this.deferredDisposeTextures.TryTake(out var d))
            {
                count++;
                d.RealDispose();
            }

            Log.Verbose("[IM] Disposing {Count} textures", count);
        }

        if (!this.deferredDisposeDisposables.IsEmpty)
        {
            // Not logging; the main purpose of this is to keep resources used for rendering the frame to be kept
            // referenced until the resources are actually done being used, and it is expected that this will be
            // frequent.
            while (this.deferredDisposeDisposables.TryTake(out var d))
                d.Dispose();
        }
    }

    [ServiceManager.CallWhenServicesReady(
        "InterfaceManager accepts event registration and stuff even when the game window is not ready.")]
    private unsafe void ContinueConstruction(
        TargetSigScanner sigScanner,
        FontAtlasFactory fontAtlasFactory)
    {
        this.dalamudAtlas = fontAtlasFactory
            .CreateFontAtlas(nameof(InterfaceManager), FontAtlasAutoRebuildMode.Disable);
        using (this.dalamudAtlas.SuppressAutoRebuild())
        {
            this.DefaultFontHandle = (FontHandle)this.dalamudAtlas.NewDelegateFontHandle(
                e => e.OnPreBuild(tk => tk.AddDalamudDefaultFont(-1)));
            this.IconFontHandle = (FontHandle)this.dalamudAtlas.NewDelegateFontHandle(
                e => e.OnPreBuild(
                    tk => tk.AddFontAwesomeIconFont(
                        new()
                        {
                            SizePx = Service<FontAtlasFactory>.Get().DefaultFontSpec.SizePx,
                            GlyphMinAdvanceX = DefaultFontSizePx,
                            GlyphMaxAdvanceX = DefaultFontSizePx,
                        })));
            this.IconFontFixedWidthHandle = (FontHandle)this.dalamudAtlas.NewDelegateFontHandle(
                e => e.OnPreBuild(tk => tk.AddDalamudAssetFont(
                    DalamudAsset.FontAwesomeFreeSolid,
                    new()
                    {
                        SizePx = Service<FontAtlasFactory>.Get().DefaultFontSpec.SizePx,
                        GlyphRanges = new ushort[] { 0x20, 0x20, 0x00 },
                    })));
            this.MonoFontHandle = (FontHandle)this.dalamudAtlas.NewDelegateFontHandle(
                e => e.OnPreBuild(
                    tk => tk.AddDalamudAssetFont(
                        DalamudAsset.InconsolataRegular,
                        new()
                        {
                            SizePx = Service<FontAtlasFactory>.Get().DefaultFontSpec.SizePx,
                        })));
            this.dalamudAtlas.BuildStepChange += e => e.OnPostBuild(
                tk =>
                {
                    // Fill missing glyphs in MonoFont from DefaultFont.
                    tk.CopyGlyphsAcrossFonts(
                        tk.GetFont(this.DefaultFontHandle),
                        tk.GetFont(this.MonoFontHandle),
                        missingOnly: true);

                    // Fill missing glyphs in IconFontFixedWidth with IconFont and fit ratio
                    tk.CopyGlyphsAcrossFonts(
                        tk.GetFont(this.IconFontHandle),
                        tk.GetFont(this.IconFontFixedWidthHandle),
                        missingOnly: true);
                    tk.FitRatio(tk.GetFont(this.IconFontFixedWidthHandle));
                });
            this.DefaultFontHandle.ImFontChanged += (_, font) =>
            {
                var fontLocked = font.NewRef();
                this.framework.RunOnFrameworkThread(
                    () =>
                    {
                        // Update the ImGui default font.
                        ImGui.GetIO().NativePtr->FontDefault = fontLocked.ImFont;

                        // Update the reference to the resources of the default font.
                        this.defaultFontResourceLock?.Dispose();
                        this.defaultFontResourceLock = fontLocked;

                        // Broadcast to auto-rebuilding instances.
                        this.AfterBuildFonts?.Invoke();
                    });
            };
        }
        
        // This will wait for scene on its own. We just wait for this.dalamudAtlas.BuildTask in this.InitScene.
        _ = this.dalamudAtlas.BuildFontsAsync();

        SwapChainHelper.BusyWaitForGameDeviceSwapChain();
        var swapChainDesc = default(DXGI_SWAP_CHAIN_DESC);
        if (SwapChainHelper.GameDeviceSwapChain->GetDesc(&swapChainDesc).SUCCEEDED)
            this.gameWindowHandle = swapChainDesc.OutputWindow; 

        try
        {
            // Requires that game window to be there, which will be the case once game swap chain is initialized.
            if (Service<DalamudConfiguration>.Get().WindowIsImmersive)
                this.SetImmersiveMode(true);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not enable immersive mode");
        }

        this.setCursorHook = Hook<SetCursorDelegate>.FromImport(
            null,
            "user32.dll",
            "SetCursor",
            0,
            this.SetCursorDetour);

        if (ReShadeAddonInterface.ReShadeIsSignedByReShade &&
            this.dalamudConfiguration.ReShadeHandlingMode is ReShadeHandlingMode.ReShadeAddonPresent or ReShadeHandlingMode.ReShadeAddonReShadeOverlay)
        {
            Log.Warning("Signed ReShade binary detected");
            Service<NotificationManager>
                .GetAsync()
                .ContinueWith(
                    nmt => nmt.Result.AddNotification(
                        new()
                        {
                            MinimizedText = Loc.Localize(
                                "ReShadeNoAddonSupportNotificationMinimizedText",
                                "Wrong ReShade installation"),
                            Content = Loc.Localize(
                                "ReShadeNoAddonSupportNotificationContent",
                                "Your installation of ReShade does not have full addon support, and may not work with Dalamud and/or the game.\n" +
                                "Download and install ReShade with full addon-support."),
                            Type = NotificationType.Warning,
                            InitialDuration = TimeSpan.MaxValue,
                            ShowIndeterminateIfNoExpiry = false,
                        })).ContinueWith(
                    t =>
                    {
                        t.Result.DrawActions += _ =>
                        {
                            ImGuiHelpers.ScaledDummy(2);
                            if (DalamudComponents.PrimaryButton(Loc.Localize("LearnMore", "Learn more...")))
                            {
                                Util.OpenLink("https://dalamud.dev/news/2024/07/23/reshade/");
                            }
                        };
                    });
        }

        Log.Information("===== S W A P C H A I N =====");
        var sb = new StringBuilder();
        foreach (var m in ReShadeAddonInterface.AllReShadeModules)
        {
            sb.Clear();
            sb.Append("ReShade detected: ");
            sb.Append(m.FileName).Append('(');
            sb.Append(m.FileVersionInfo.OriginalFilename);
            sb.Append("; ").Append(m.FileVersionInfo.ProductName);
            sb.Append("; ").Append(m.FileVersionInfo.ProductVersion);
            sb.Append("; ").Append(m.FileVersionInfo.FileDescription);
            sb.Append("; ").Append(m.FileVersionInfo.FileVersion);
            sb.Append($"@ 0x{m.BaseAddress:X}");
            if (!ReferenceEquals(m, ReShadeAddonInterface.ReShadeModule))
                sb.Append(" [ignored by Dalamud]");
            Log.Information(sb.ToString());
        }

        if (ReShadeAddonInterface.AllReShadeModules.Length > 1)
            Log.Warning("Multiple ReShade dlls are detected.");

        ResizeBuffersDelegate dxgiSwapChainResizeBuffersDelegate;
        ReShadeDxgiSwapChainPresentDelegate? reShadeDxgiSwapChainPresentDelegate = null;
        DxgiSwapChainPresentDelegate? dxgiSwapChainPresentDelegate = null;
        nint pfnReShadeDxgiSwapChainPresent = 0;
        switch (this.dalamudConfiguration.ReShadeHandlingMode)
        {
            // If ReShade is not found, do no special handling.
            case var _ when ReShadeAddonInterface.ReShadeModule is null:
                goto default;

            // This is the only mode honored when SwapChainHookMode is set to VTable.
            case ReShadeHandlingMode.Default:
            case ReShadeHandlingMode.UnwrapReShade:
                if (SwapChainHelper.UnwrapReShade())
                    Log.Information("Unwrapped ReShade");
                else
                    Log.Warning("Could not unwrap ReShade");
                goto default;

            // Do no special ReShade handling.
            // If SwapChainHookMode is set to VTable, do no special handling.
            case ReShadeHandlingMode.None:
            case var _ when this.dalamudConfiguration.SwapChainHookMode == SwapChainHelper.HookMode.VTable:
            default:
                dxgiSwapChainResizeBuffersDelegate = this.AsHookDxgiSwapChainResizeBuffersDetour;
                dxgiSwapChainPresentDelegate = this.DxgiSwapChainPresentDetour;
                break;

            // Register Dalamud as a ReShade addon.
            case ReShadeHandlingMode.ReShadeAddonPresent:
            case ReShadeHandlingMode.ReShadeAddonReShadeOverlay:
                if (!ReShadeAddonInterface.TryRegisterAddon(out this.reShadeAddonInterface))
                {
                    Log.Warning("Could not register as ReShade addon");
                    goto default;
                }

                Log.Information("Registered as a ReShade addon");
                this.reShadeAddonInterface.InitSwapChain += this.ReShadeAddonInterfaceOnInitSwapChain;
                this.reShadeAddonInterface.DestroySwapChain += this.ReShadeAddonInterfaceOnDestroySwapChain;
                if (this.dalamudConfiguration.ReShadeHandlingMode == ReShadeHandlingMode.ReShadeAddonPresent)
                    this.reShadeAddonInterface.Present += this.ReShadeAddonInterfaceOnPresent;
                else
                    this.reShadeAddonInterface.ReShadeOverlay += this.ReShadeAddonInterfaceOnReShadeOverlay;

                dxgiSwapChainResizeBuffersDelegate = this.AsReShadeAddonDxgiSwapChainResizeBuffersDetour;
                break;

            // Hook ReShade's DXGISwapChain::on_present. This is the legacy and the default option.
            case ReShadeHandlingMode.HookReShadeDxgiSwapChainOnPresent:
                pfnReShadeDxgiSwapChainPresent = ReShadeAddonInterface.FindReShadeDxgiSwapChainOnPresent();

                if (pfnReShadeDxgiSwapChainPresent == 0)
                {
                    Log.Warning("ReShade::DXGISwapChain::on_present could not be found");
                    goto default;
                }

                Log.Information(
                    "Found ReShade::DXGISwapChain::on_present at {addr}",
                    Util.DescribeAddress(pfnReShadeDxgiSwapChainPresent));
                reShadeDxgiSwapChainPresentDelegate = this.ReShadeDxgiSwapChainOnPresentDetour;
                dxgiSwapChainResizeBuffersDelegate = this.AsHookDxgiSwapChainResizeBuffersDetour;
                break;
        }

        switch (this.dalamudConfiguration.SwapChainHookMode)
        {
            case SwapChainHelper.HookMode.ByteCode: 
            default:
            {
                Log.Information("Hooking using bytecode...");
                this.dxgiSwapChainResizeBuffersHook = Hook<ResizeBuffersDelegate>.FromAddress(
                    (nint)SwapChainHelper.GameDeviceSwapChainVtbl->ResizeBuffers,
                    dxgiSwapChainResizeBuffersDelegate);
                Log.Information(
                    "Hooked IDXGISwapChain::ResizeBuffers using bytecode: {addr}",
                    Util.DescribeAddress(this.dxgiSwapChainResizeBuffersHook.Address));

                if (dxgiSwapChainPresentDelegate is not null)
                {
                    this.dxgiSwapChainPresentHook = Hook<DxgiSwapChainPresentDelegate>.FromAddress(
                        (nint)SwapChainHelper.GameDeviceSwapChainVtbl->Present,
                        dxgiSwapChainPresentDelegate);
                    Log.Information(
                        "Hooked IDXGISwapChain::Present using bytecode: {addr}",
                        Util.DescribeAddress(this.dxgiSwapChainPresentHook.Address));
                }

                if (reShadeDxgiSwapChainPresentDelegate is not null && pfnReShadeDxgiSwapChainPresent != 0)
                {
                    this.reShadeDxgiSwapChainPresentHook = Hook<ReShadeDxgiSwapChainPresentDelegate>.FromAddress(
                        pfnReShadeDxgiSwapChainPresent,
                        reShadeDxgiSwapChainPresentDelegate);
                    Log.Information(
                        "Hooked ReShade::DXGISwapChain::on_present using bytecode: {addr}",
                        Util.DescribeAddress(this.reShadeDxgiSwapChainPresentHook.Address));
                }

                break;
            }

            case SwapChainHelper.HookMode.VTable:
            {
                Log.Information("Hooking using VTable...");
                this.dxgiSwapChainHook = new(SwapChainHelper.GameDeviceSwapChain);
                this.dxgiSwapChainResizeBuffersHook = this.dxgiSwapChainHook.CreateHook(
                    nameof(IDXGISwapChain.ResizeBuffers),
                    dxgiSwapChainResizeBuffersDelegate);
                Log.Information(
                    "Hooked IDXGISwapChain::ResizeBuffers using VTable: {addr}",
                    Util.DescribeAddress(this.dxgiSwapChainResizeBuffersHook.Address));

                if (dxgiSwapChainPresentDelegate is not null)
                {
                    this.dxgiSwapChainPresentHook = this.dxgiSwapChainHook.CreateHook(
                        nameof(IDXGISwapChain.Present),
                        dxgiSwapChainPresentDelegate);
                    Log.Information(
                        "Hooked IDXGISwapChain::Present using VTable: {addr}",
                        Util.DescribeAddress(this.dxgiSwapChainPresentHook.Address));
                }

                Log.Information(
                    "Detouring vtable at {addr}: {prev} to {new}",
                    Util.DescribeAddress(this.dxgiSwapChainHook.Address),
                    Util.DescribeAddress(this.dxgiSwapChainHook.OriginalVTableAddress),
                    Util.DescribeAddress(this.dxgiSwapChainHook.OverridenVTableAddress));
                break;
            }
        }

        this.setCursorHook.Enable();
        this.reShadeDxgiSwapChainPresentHook?.Enable();
        this.dxgiSwapChainResizeBuffersHook.Enable();
        this.dxgiSwapChainPresentHook?.Enable();
        this.dxgiSwapChainHook?.Enable();
    }

    private IntPtr SetCursorDetour(IntPtr hCursor)
    {
        if (this.lastWantCapture && (!this.scene?.IsImGuiCursor(hCursor) ?? false) && this.OverrideGameCursor)
            return IntPtr.Zero;

        return this.setCursorHook?.IsDisposed is not false
                   ? User32.SetCursor(new(hCursor, false)).DangerousGetHandle()
                   : this.setCursorHook.Original(hCursor);
    }

    private void OnNewInputFrame()
    {
        var io = ImGui.GetIO();
        var dalamudInterface = Service<DalamudInterface>.GetNullable();
        var gamepadState = Service<GamepadState>.GetNullable();
        var keyState = Service<KeyState>.GetNullable();

        if (dalamudInterface == null || gamepadState == null || keyState == null)
            return;

        // Prevent setting the footgun from ImGui Demo; the Space key isn't removing the flag at the moment.
        io.ConfigFlags &= ~ImGuiConfigFlags.NoMouse;

        // fix for keys in game getting stuck, if you were holding a game key (like run)
        // and then clicked on an imgui textbox - imgui would swallow the keyup event,
        // so the game would think the key remained pressed continuously until you left
        // imgui and pressed and released the key again
        if (io.WantTextInput)
        {
            keyState.ClearAll();
        }

        // TODO: mouse state?

        var gamepadEnabled = (io.BackendFlags & ImGuiBackendFlags.HasGamepad) > 0;

        // NOTE (Chiv) Activate ImGui navigation  via L1+L3 press
        // (mimicking how mouse navigation is activated via L1+R3 press in game).
        if (gamepadEnabled
            && gamepadState.Raw(GamepadButtons.L1) > 0
            && gamepadState.Pressed(GamepadButtons.L3) > 0)
        {
            io.ConfigFlags ^= ImGuiConfigFlags.NavEnableGamepad;
            gamepadState.NavEnableGamepad ^= true;
            dalamudInterface.ToggleGamepadModeNotifierWindow();
        }

        if (gamepadEnabled && (io.ConfigFlags & ImGuiConfigFlags.NavEnableGamepad) > 0)
        {
            var northButton = gamepadState.Raw(GamepadButtons.North) != 0;
            var eastButton = gamepadState.Raw(GamepadButtons.East) != 0;
            var southButton = gamepadState.Raw(GamepadButtons.South) != 0;
            var westButton = gamepadState.Raw(GamepadButtons.West) != 0;
            var dPadUp = gamepadState.Raw(GamepadButtons.DpadUp) != 0;
            var dPadRight = gamepadState.Raw(GamepadButtons.DpadRight) != 0;
            var dPadDown = gamepadState.Raw(GamepadButtons.DpadDown) != 0;
            var dPadLeft = gamepadState.Raw(GamepadButtons.DpadLeft) != 0;
            var leftStickUp = gamepadState.LeftStick.Y > 0 ? gamepadState.LeftStick.Y / 100f : 0;
            var leftStickRight = gamepadState.LeftStick.X > 0 ? gamepadState.LeftStick.X / 100f : 0;
            var leftStickDown = gamepadState.LeftStick.Y < 0 ? -gamepadState.LeftStick.Y / 100f : 0;
            var leftStickLeft = gamepadState.LeftStick.X < 0 ? -gamepadState.LeftStick.X / 100f : 0;
            var l1Button = gamepadState.Raw(GamepadButtons.L1) != 0;
            var l2Button = gamepadState.Raw(GamepadButtons.L2) != 0;
            var r1Button = gamepadState.Raw(GamepadButtons.R1) != 0;
            var r2Button = gamepadState.Raw(GamepadButtons.R2) != 0;

            io.AddKeyEvent(ImGuiKey.GamepadFaceUp, northButton);
            io.AddKeyEvent(ImGuiKey.GamepadFaceRight, eastButton);
            io.AddKeyEvent(ImGuiKey.GamepadFaceDown, southButton);
            io.AddKeyEvent(ImGuiKey.GamepadFaceLeft, westButton);

            io.AddKeyEvent(ImGuiKey.GamepadDpadUp, dPadUp);
            io.AddKeyEvent(ImGuiKey.GamepadDpadRight, dPadRight);
            io.AddKeyEvent(ImGuiKey.GamepadDpadDown, dPadDown);
            io.AddKeyEvent(ImGuiKey.GamepadDpadLeft, dPadLeft);

            io.AddKeyAnalogEvent(ImGuiKey.GamepadLStickUp, leftStickUp != 0, leftStickUp);
            io.AddKeyAnalogEvent(ImGuiKey.GamepadLStickRight, leftStickRight != 0, leftStickRight);
            io.AddKeyAnalogEvent(ImGuiKey.GamepadLStickDown, leftStickDown != 0, leftStickDown);
            io.AddKeyAnalogEvent(ImGuiKey.GamepadLStickLeft, leftStickLeft != 0, leftStickLeft);

            io.AddKeyEvent(ImGuiKey.GamepadL1, l1Button);
            io.AddKeyEvent(ImGuiKey.GamepadL2, l2Button);
            io.AddKeyEvent(ImGuiKey.GamepadR1, r1Button);
            io.AddKeyEvent(ImGuiKey.GamepadR2, r2Button);

            if (gamepadState.Pressed(GamepadButtons.R3) > 0)
            {
                var configuration = Service<DalamudConfiguration>.Get();
                dalamudInterface.TogglePluginInstallerWindowTo(configuration.PluginInstallerOpen);
            }
        }
    }

    private void Display()
    {
        // this is more or less part of what reshade/etc do to avoid having to manually
        // set the cursor inside the ui
        // This will just tell ImGui to draw its own software cursor instead of using the hardware cursor
        // The scene internally will handle hiding and showing the hardware (game) cursor
        // If the player has the game software cursor enabled, we can't really do anything about that and
        // they will see both cursors.
        // Doing this here because it's somewhat application-specific behavior
        // ImGui.GetIO().MouseDrawCursor = ImGui.GetIO().WantCaptureMouse;
        this.LastImGuiIoPtr = ImGui.GetIO();
        this.lastWantCapture = this.LastImGuiIoPtr.WantCaptureMouse;

        WindowSystem.HasAnyWindowSystemFocus = false;
        WindowSystem.FocusedWindowSystemNamespace = string.Empty;

        if (this.IsDispatchingEvents)
        {
            this.Draw?.Invoke();
            Service<NotificationManager>.GetNullable()?.Draw();
        }
    }

    /// <summary>
    /// Represents an instance of InstanceManager with scene ready for use.
    /// </summary>
    [ServiceManager.ProvidedService]
    public class InterfaceManagerWithScene : IServiceType
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InterfaceManagerWithScene"/> class.
        /// </summary>
        /// <param name="interfaceManager">An instance of <see cref="InterfaceManager"/>.</param>
        internal InterfaceManagerWithScene(InterfaceManager interfaceManager)
        {
            this.Manager = interfaceManager;
        }

        /// <summary>
        /// Gets the associated InterfaceManager.
        /// </summary>
        public InterfaceManager Manager { get; init; }
    }
}
