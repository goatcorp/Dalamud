using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Configuration.Internal;
using Dalamud.Game;
using Dalamud.Game.ClientState.GamePad;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.Internal.DXGI;
using Dalamud.Hooking;
using Dalamud.Hooking.WndProcHook;
using Dalamud.Interface.ImGuiNotification.Internal;
using Dalamud.Interface.Internal.ManagedAsserts;
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

using PInvoke;

using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

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
internal class InterfaceManager : IInternalDisposableService
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
    private readonly ConcurrentBag<ILockedImFont> deferredDisposeImFontLockeds = new();

    [ServiceManager.ServiceDependency]
    private readonly WndProcHookManager wndProcHookManager = Service<WndProcHookManager>.Get();

    [ServiceManager.ServiceDependency]
    private readonly Framework framework = Service<Framework>.Get();

    private readonly SwapChainVtableResolver address = new();
    private RawDX11Scene? scene;

    private Hook<SetCursorDelegate>? setCursorHook;
    private Hook<PresentDelegate>? presentHook;
    private Hook<ResizeBuffersDelegate>? resizeBuffersHook;

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

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate IntPtr PresentDelegate(IntPtr swapChain, uint syncInterval, uint presentFlags);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate IntPtr ResizeBuffersDelegate(IntPtr swapChain, uint bufferCount, uint width, uint height, uint newFormat, uint swapChainFlags);

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

    /// <summary>
    /// Gets the number of calls to <see cref="PresentDetour"/> so far.
    /// </summary>
    public long CumulativePresentCalls { get; private set; }

    /// <summary>
    /// Dispose of managed and unmanaged resources.
    /// </summary>
    void IInternalDisposableService.DisposeService()
    {
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
            Interlocked.Exchange(ref this.presentHook, null)?.Dispose();
            Interlocked.Exchange(ref this.resizeBuffersHook, null)?.Dispose();
        }
    }

#nullable enable

    /// <summary>
    /// Load an image from disk.
    /// </summary>
    /// <param name="filePath">The filepath to load.</param>
    /// <returns>A texture, ready to use in ImGui.</returns>
    public IDalamudTextureWrap? LoadImage(string filePath)
    {
        if (this.scene == null)
            throw new InvalidOperationException("Scene isn't ready.");

        try
        {
            var wrap = this.scene?.LoadImage(filePath);
            return wrap != null ? new DalamudTextureWrap(wrap) : null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Failed to load image from {filePath}");
        }

        return null;
    }

    /// <summary>
    /// Load an image from an array of bytes.
    /// </summary>
    /// <param name="imageData">The data to load.</param>
    /// <returns>A texture, ready to use in ImGui.</returns>
    public IDalamudTextureWrap? LoadImage(byte[] imageData)
    {
        if (this.scene == null)
            throw new InvalidOperationException("Scene isn't ready.");

        try
        {
            var wrap = this.scene?.LoadImage(imageData);
            return wrap != null ? new DalamudTextureWrap(wrap) : null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load image from memory");
        }

        return null;
    }

    /// <summary>
    /// Load an image from an array of bytes.
    /// </summary>
    /// <param name="imageData">The data to load.</param>
    /// <param name="width">The width in pixels.</param>
    /// <param name="height">The height in pixels.</param>
    /// <param name="numChannels">The number of channels.</param>
    /// <returns>A texture, ready to use in ImGui.</returns>
    public IDalamudTextureWrap? LoadImageRaw(byte[] imageData, int width, int height, int numChannels)
    {
        if (this.scene == null)
            throw new InvalidOperationException("Scene isn't ready.");

        try
        {
            var wrap = this.scene?.LoadImageRaw(imageData, width, height, numChannels);
            return wrap != null ? new DalamudTextureWrap(wrap) : null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load image from raw data");
        }

        return null;
    }

    /// <summary>
    /// Check whether the current D3D11 Device supports the given DXGI format.
    /// </summary>
    /// <param name="dxgiFormat">DXGI format to check.</param>
    /// <returns>Whether it is supported.</returns>
    public bool SupportsDxgiFormat(Format dxgiFormat) => this.scene is null
        ? throw new InvalidOperationException("Scene isn't ready.")
        : this.scene.Device.CheckFormatSupport(dxgiFormat).HasFlag(FormatSupport.Texture2D);

    /// <summary>
    /// Load an image from a span of bytes of specified format.
    /// </summary>
    /// <param name="data">The data to load.</param>
    /// <param name="pitch">The pitch(stride) in bytes.</param>
    /// <param name="width">The width in pixels.</param>
    /// <param name="height">The height in pixels.</param>
    /// <param name="dxgiFormat">Format of the texture.</param>
    /// <returns>A texture, ready to use in ImGui.</returns>
    public DalamudTextureWrap LoadImageFromDxgiFormat(Span<byte> data, int pitch, int width, int height, Format dxgiFormat)
    {
        if (this.scene == null)
            throw new InvalidOperationException("Scene isn't ready.");

        ShaderResourceView resView;
        unsafe
        {
            fixed (void* pData = data)
            {
                var texDesc = new Texture2DDescription
                {
                    Width = width,
                    Height = height,
                    MipLevels = 1,
                    ArraySize = 1,
                    Format = dxgiFormat,
                    SampleDescription = new(1, 0),
                    Usage = ResourceUsage.Immutable,
                    BindFlags = BindFlags.ShaderResource,
                    CpuAccessFlags = CpuAccessFlags.None,
                    OptionFlags = ResourceOptionFlags.None,
                };

                using var texture = new Texture2D(this.Device, texDesc, new DataRectangle(new(pData), pitch));
                resView = new(this.Device, texture, new()
                {
                    Format = texDesc.Format,
                    Dimension = ShaderResourceViewDimension.Texture2D,
                    Texture2D = { MipLevels = texDesc.MipLevels },
                });
            }
        }

        // no sampler for now because the ImGui implementation we copied doesn't allow for changing it
        return new DalamudTextureWrap(new D3DTextureWrap(resView, width, height));
    }

#nullable restore

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
    public void EnqueueDeferredDispose(in ILockedImFont locked)
    {
        this.deferredDisposeImFontLockeds.Add(locked);
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
            var dxgiAdapter = dxgiDev?.Adapter.QueryInterfaceOrNull<Adapter4>();
            if (dxgiAdapter == null)
                return null;

            var memInfo = dxgiAdapter.QueryVideoMemoryInfo(0, MemorySegmentGroup.Local);
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
        ((Result)NativeFunctions.DwmSetWindowAttribute(
                    this.GameWindowHandle,
                    NativeFunctions.DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE,
                    ref value,
                    sizeof(int))).CheckError();
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void RenderImGui(RawDX11Scene scene)
    {
        var conf = Service<DalamudConfiguration>.Get();

        // Process information needed by ImGuiHelpers each frame.
        ImGuiHelpers.NewFrame();

        // Enable viewports if there are no issues.
        if (conf.IsDisableViewport || scene.SwapChain.IsFullScreen || ImGui.GetPlatformIO().Monitors.Size == 1)
            ImGui.GetIO().ConfigFlags &= ~ImGuiConfigFlags.ViewportsEnable;
        else
            ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;

        scene.Render();
    }

    private void InitScene(IntPtr swapChain)
    {
        RawDX11Scene newScene;
        using (Timings.Start("IM Scene Init"))
        {
            try
            {
                newScene = new RawDX11Scene(swapChain);
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
                return;
            }

            var startInfo = Service<Dalamud>.Get().StartInfo;
            var configuration = Service<DalamudConfiguration>.Get();

            var iniFileInfo = new FileInfo(Path.Combine(Path.GetDirectoryName(startInfo.ConfigurationPath)!, "dalamudUI.ini"));

            try
            {
                if (iniFileInfo.Length > 1200000)
                {
                    Log.Warning("dalamudUI.ini was over 1mb, deleting");
                    iniFileInfo.CopyTo(Path.Combine(iniFileInfo.DirectoryName!, $"dalamudUI-{DateTimeOffset.Now.ToUnixTimeSeconds()}.ini"));
                    iniFileInfo.Delete();
                }
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
    }

    private unsafe void WndProcHookManagerOnPreWndProc(WndProcEventArgs args)
    {
        var r = this.scene?.ProcessWndProcW(args.Hwnd, (User32.WindowMessage)args.Message, args.WParam, args.LParam);
        if (r is not null)
            args.SuppressWithValue(r.Value);
    }

    /*
     * NOTE(goat): When hooking ReShade DXGISwapChain::runtime_present, this is missing the syncInterval arg.
     *             Seems to work fine regardless, I guess, so whatever.
     */
    private IntPtr PresentDetour(IntPtr swapChain, uint syncInterval, uint presentFlags)
    {
        this.CumulativePresentCalls++;

        Debug.Assert(this.presentHook is not null, "How did PresentDetour get called when presentHook is null?");
        Debug.Assert(this.dalamudAtlas is not null, "dalamudAtlas should have been set already");

        if (this.scene != null && swapChain != this.scene.SwapChain.NativePointer)
            return this.presentHook!.Original(swapChain, syncInterval, presentFlags);

        if (this.scene == null)
            this.InitScene(swapChain);

        Debug.Assert(this.scene is not null, "InitScene did not set the scene field, but did not throw an exception.");

        if (!this.dalamudAtlas!.HasBuiltAtlas)
        {
            if (this.dalamudAtlas.BuildTask.Exception != null)
            {
                // TODO: Can we do something more user-friendly here? Unload instead?
                Log.Error(this.dalamudAtlas.BuildTask.Exception, "Failed to initialize Dalamud base fonts");
                Util.Fatal("Failed to initialize Dalamud base fonts.\nPlease report this error.", "Dalamud");
            }

            return this.presentHook!.Original(swapChain, syncInterval, presentFlags);
        }

        if (this.address.IsReshade)
        {
            var pRes = this.presentHook!.Original(swapChain, syncInterval, presentFlags);

            RenderImGui(this.scene!);
            this.CleanupPostImGuiRender();

            return pRes;
        }

        RenderImGui(this.scene!);
        this.CleanupPostImGuiRender();

        return this.presentHook!.Original(swapChain, syncInterval, presentFlags);
    }

    private void CleanupPostImGuiRender()
    {
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

        if (!this.deferredDisposeImFontLockeds.IsEmpty)
        {
            // Not logging; the main purpose of this is to keep resources used for rendering the frame to be kept
            // referenced until the resources are actually done being used, and it is expected that this will be
            // frequent.
            while (this.deferredDisposeImFontLockeds.TryTake(out var d))
                d.Dispose();
        }
    }

    [ServiceManager.CallWhenServicesReady(
        "InterfaceManager accepts event registration and stuff even when the game window is not ready.")]
    private void ContinueConstruction(
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
                        unsafe
                        {
                            ImGui.GetIO().NativePtr->FontDefault = fontLocked.ImFont;
                        }

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

        this.address.Setup(sigScanner);

        try
        {
            if (Service<DalamudConfiguration>.Get().WindowIsImmersive)
                this.SetImmersiveMode(true);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not enable immersive mode");
        }

        this.setCursorHook = Hook<SetCursorDelegate>.FromImport(null, "user32.dll", "SetCursor", 0, this.SetCursorDetour);
        this.presentHook = Hook<PresentDelegate>.FromAddress(this.address.Present, this.PresentDetour);
        this.resizeBuffersHook = Hook<ResizeBuffersDelegate>.FromAddress(this.address.ResizeBuffers, this.ResizeBuffersDetour);

        Log.Verbose("===== S W A P C H A I N =====");
        Log.Verbose($"Present address 0x{this.presentHook!.Address.ToInt64():X}");
        Log.Verbose($"ResizeBuffers address 0x{this.resizeBuffersHook!.Address.ToInt64():X}");

        this.setCursorHook.Enable();
        this.presentHook.Enable();
        this.resizeBuffersHook.Enable();
    }

    private IntPtr ResizeBuffersDetour(IntPtr swapChain, uint bufferCount, uint width, uint height, uint newFormat, uint swapChainFlags)
    {
#if DEBUG
        Log.Verbose($"Calling resizebuffers swap@{swapChain.ToInt64():X}{bufferCount} {width} {height} {newFormat} {swapChainFlags}");
#endif

        this.ResizeBuffers?.InvokeSafely();

        // We have to ensure we're working with the main swapchain,
        // as viewports might be resizing as well
        if (this.scene == null || swapChain != this.scene.SwapChain.NativePointer)
            return this.resizeBuffersHook!.Original(swapChain, bufferCount, width, height, newFormat, swapChainFlags);

        this.scene?.OnPreResize();

        var ret = this.resizeBuffersHook!.Original(swapChain, bufferCount, width, height, newFormat, swapChainFlags);
        if (ret.ToInt64() == 0x887A0001)
        {
            Log.Error("invalid call to resizeBuffers");
        }

        this.scene?.OnPostResize((int)width, (int)height);

        return ret;
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

        var snap = ImGuiManagedAsserts.GetSnapshot();

        if (this.IsDispatchingEvents)
        {
            this.Draw?.Invoke();
            Service<NotificationManager>.GetNullable()?.Draw();
        }

        ImGuiManagedAsserts.ReportProblems("Dalamud Core", snap);
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
