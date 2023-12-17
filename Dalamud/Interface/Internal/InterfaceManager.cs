using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Unicode;
using System.Threading;

using Dalamud.Configuration.Internal;
using Dalamud.Game;
using Dalamud.Game.ClientState.GamePad;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.Internal.DXGI;
using Dalamud.Hooking;
using Dalamud.Hooking.WndProcHook;
using Dalamud.ImGuiScene;
using Dalamud.ImGuiScene.Implementations;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.Internal.ManagedAsserts;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Interface.Style;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.Storage.Assets;
using Dalamud.Utility;
using Dalamud.Utility.Timing;

using ImGuiNET;

using Serilog;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

using Win32 = TerraFX.Interop.Windows.Windows;

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
[ServiceManager.BlockingEarlyLoadedService]
internal class InterfaceManager : IDisposable, IServiceType
{
    /// <summary>
    /// The default font size, in points.
    /// </summary>
    public const float DefaultFontSizePt = 12.0f;

    /// <summary>
    /// The default font size, in pixels.
    /// </summary>
    public const float DefaultFontSizePx = (DefaultFontSizePt * 4.0f) / 3.0f;

    private const ushort Fallback1Codepoint = 0x3013; // Geta mark; FFXIV uses this to indicate that a glyph is missing.
    private const ushort Fallback2Codepoint = '-';    // FFXIV uses dash if Geta mark is unavailable.

    private readonly HashSet<SpecialGlyphRequest> glyphRequests = new();
    private readonly Dictionary<ImFontPtr, TargetFontModification> loadedFontInfo = new();

    private readonly List<DalamudTextureWrap> deferredDisposeTextures = new();

    [ServiceManager.ServiceDependency]
    private readonly Framework framework = Service<Framework>.Get();
    
    [ServiceManager.ServiceDependency]
    private readonly WndProcHookManager wndProcHookManager = Service<WndProcHookManager>.Get();
    
    [ServiceManager.ServiceDependency]
    private readonly DalamudIme dalamudIme = Service<DalamudIme>.Get();

    private readonly ManualResetEvent fontBuildSignal;
    private readonly SwapChainVtableResolver address;
    private readonly Hook<SetCursorDelegate> setCursorHook;
    private IWin32Scene? scene;
    // private Dx12OnDx11Win32Scene? scene;

    private Hook<PresentDelegate>? presentHook;
    private Hook<ResizeBuffersDelegate>? resizeBuffersHook;

    // can't access imgui IO before first present call
    private bool lastWantCapture = false;
    private bool isRebuildingFonts = false;
    private bool isOverrideGameCursor = true;

    [ServiceManager.ServiceConstructor]
    private unsafe InterfaceManager()
    {
        this.setCursorHook = Hook<SetCursorDelegate>.FromImport(
            null, "user32.dll", "SetCursor", 0, this.SetCursorDetour);

        this.fontBuildSignal = new ManualResetEvent(false);

        this.address = new SwapChainVtableResolver();
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private unsafe delegate HRESULT PresentDelegate(IDXGISwapChain* swapChain, uint syncInterval, uint presentFlags);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private unsafe delegate HRESULT ResizeBuffersDelegate(IDXGISwapChain* swapChain, uint bufferCount, uint width, uint height, uint newFormat, uint swapChainFlags);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate HCURSOR SetCursorDelegate(HCURSOR hCursor);

    /// <summary>
    /// This event gets called each frame to facilitate ImGui drawing.
    /// </summary>
    public event IImGuiScene.BuildUiDelegate Draw;

    /// <summary>
    /// This event gets called when ResizeBuffers is called.
    /// </summary>
    public event Action ResizeBuffers;

    /// <summary>
    /// Gets or sets an action that is executed right before fonts are rebuilt.
    /// </summary>
    public event Action BuildFonts;

    /// <summary>
    /// Gets or sets an action that is executed right after fonts are rebuilt.
    /// </summary>
    public event Action AfterBuildFonts;

    /// <summary>
    /// Gets the default ImGui font.
    /// </summary>
    public static ImFontPtr DefaultFont { get; private set; }

    /// <summary>
    /// Gets an included FontAwesome icon font.
    /// </summary>
    public static ImFontPtr IconFont { get; private set; }

    /// <summary>
    /// Gets an included monospaced font.
    /// </summary>
    public static ImFontPtr MonoFont { get; private set; }

    /// <summary>
    /// Gets or sets the pointer to ImGui.IO(), when it was last used.
    /// </summary>
    public ImGuiIOPtr LastImGuiIoPtr { get; set; }

    /// <summary>
    /// Gets the device instance.
    /// </summary>
    public nint Device => this.scene?.DeviceHandle ?? 0;

    /// <summary>
    /// Gets the address handle to the main process window.
    /// </summary>
    public IntPtr WindowHandlePtr => this.GameWindowHandle;

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
    /// Gets or sets a value indicating whether the fonts are built and ready to use.
    /// </summary>
    public bool FontsReady { get; set; } = false;

    /// <summary>
    /// Gets a value indicating whether the Dalamud interface ready to use.
    /// </summary>
    public bool IsReady => this.scene != null;

    /// <summary>
    /// Gets or sets a value indicating whether or not Draw events should be dispatched.
    /// </summary>
    public bool IsDispatchingEvents { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to override configuration for UseAxis.
    /// </summary>
    public bool? UseAxisOverride { get; set; } = null;

    /// <summary>
    /// Gets a value indicating whether to use AXIS fonts.
    /// </summary>
    public bool UseAxis => this.UseAxisOverride ?? Service<DalamudConfiguration>.Get().UseAxisFontsFromGame;

    /// <summary>
    /// Gets or sets the overrided font gamma value, instead of using the value from configuration.
    /// </summary>
    public float? FontGammaOverride { get; set; } = null;

    /// <summary>
    /// Gets the font gamma value to use.
    /// </summary>
    public float FontGamma => Math.Max(0.1f, this.FontGammaOverride.GetValueOrDefault(Service<DalamudConfiguration>.Get().FontGammaLevel));

    /// <summary>
    /// Gets a value indicating whether we're building fonts but haven't generated atlas yet.
    /// </summary>
    public bool IsBuildingFontsBeforeAtlasBuild => this.isRebuildingFonts && !this.fontBuildSignal.WaitOne(0);

    /// <summary>
    /// Gets a value indicating the native handle of the game main window.
    /// </summary>
    public HWND GameWindowHandle { get; private set; }

    /// <summary>
    /// Dispose of managed and unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        this.framework.RunOnFrameworkThread(() =>
        {
            this.setCursorHook.Dispose();
            this.presentHook?.Dispose();
            this.resizeBuffersHook?.Dispose();
        }).Wait();

        this.wndProcHookManager.PreWndProc -= this.WndProcHookManagerOnPreWndProc;
        this.scene?.Dispose();
    }

    /// <inheritdoc cref="IImGuiScene.SupportsTextureFormat"/>
    public bool SupportsTextureFormat(int format) => 
        this.scene?.SupportsTextureFormat(format) ?? throw new InvalidOperationException("Scene isn't ready.");

    /// <inheritdoc cref="IImGuiScene.CreateTexture2DFromFile"/>
    public IDalamudTextureWrap CreateTexture2DFromFile(string filePath, string debugName) =>
        new DalamudTextureWrap(
            this.scene?.CreateTexture2DFromFile(filePath, debugName)
            ?? throw new InvalidOperationException("Scene isn't ready."));

    /// <inheritdoc cref="IImGuiScene.CreateTexture2DFromBytes"/>
    public IDalamudTextureWrap CreateTexture2DFromBytes(ReadOnlySpan<byte> imageData, string debugName) =>
        new DalamudTextureWrap(
            this.scene?.CreateTexture2DFromBytes(imageData, debugName)
            ?? throw new InvalidOperationException("Scene isn't ready."));

    /// <inheritdoc cref="IImGuiScene.CreateTexture2DFromRaw"/>
    public IDalamudTextureWrap CreateTexture2DFromRaw(
        Span<byte> data,
        int pitch,
        int width,
        int height,
        int format,
        string debugName) =>
        new DalamudTextureWrap(
            this.scene?.CreateTexture2DFromRaw(data, pitch, width, height, format, debugName)
            ?? throw new InvalidOperationException("Scene isn't ready."));

    /// <summary>
    /// Sets up a deferred invocation of font rebuilding, before the next render frame.
    /// </summary>
    public void RebuildFonts()
    {
        if (this.scene == null)
        {
            Log.Verbose("[FONT] RebuildFonts(): scene not ready, doing nothing");
            return;
        }

        Log.Verbose("[FONT] RebuildFonts() called");

        // don't invoke this multiple times per frame, in case multiple plugins call it
        if (!this.isRebuildingFonts)
        {
            Log.Verbose("[FONT] RebuildFonts() trigger");
            this.isRebuildingFonts = true;
            this.scene.NewRenderFrame += this.RebuildFontsInternal;
        }
    }

    /// <summary>
    /// Wait for the rebuilding fonts to complete.
    /// </summary>
    public void WaitForFontRebuild()
    {
        this.fontBuildSignal.WaitOne();
    }

    /// <summary>
    /// Requests a default font of specified size to exist.
    /// </summary>
    /// <param name="size">Font size in pixels.</param>
    /// <param name="ranges">Ranges of glyphs.</param>
    /// <returns>Requets handle.</returns>
    public SpecialGlyphRequest NewFontSizeRef(float size, List<Tuple<ushort, ushort>> ranges)
    {
        var allContained = false;
        var fonts = ImGui.GetIO().Fonts.Fonts;
        ImFontPtr foundFont = null;
        unsafe
        {
            for (int i = 0, i_ = fonts.Size; i < i_; i++)
            {
                if (!this.glyphRequests.Any(x => x.FontInternal.NativePtr == fonts[i].NativePtr))
                    continue;

                allContained = true;
                foreach (var range in ranges)
                {
                    if (!allContained)
                        break;

                    for (var j = range.Item1; j <= range.Item2 && allContained; j++)
                        allContained &= fonts[i].FindGlyphNoFallback(j).NativePtr != null;
                }

                if (allContained)
                    foundFont = fonts[i];

                break;
            }
        }

        var req = new SpecialGlyphRequest(this, size, ranges);
        req.FontInternal = foundFont;

        if (!allContained)
            this.RebuildFonts();

        return req;
    }

    /// <summary>
    /// Requests a default font of specified size to exist.
    /// </summary>
    /// <param name="size">Font size in pixels.</param>
    /// <param name="text">Text to calculate glyph ranges from.</param>
    /// <returns>Requets handle.</returns>
    public SpecialGlyphRequest NewFontSizeRef(float size, string text)
    {
        List<Tuple<ushort, ushort>> ranges = new();
        foreach (var c in new SortedSet<char>(text.ToHashSet()))
        {
            if (ranges.Any() && ranges[^1].Item2 + 1 == c)
                ranges[^1] = Tuple.Create<ushort, ushort>(ranges[^1].Item1, c);
            else
                ranges.Add(Tuple.Create<ushort, ushort>(c, c));
        }

        return this.NewFontSizeRef(size, ranges);
    }

    /// <summary>
    /// Enqueue a texture to be disposed at the end of the frame.
    /// </summary>
    /// <param name="wrap">The texture.</param>
    public void EnqueueDeferredDispose(DalamudTextureWrap wrap)
    {
        this.deferredDisposeTextures.Add(wrap);
    }

    /// <summary>
    /// Get video memory information.
    /// </summary>
    /// <returns>The currently used video memory, or null if not available.</returns>
    public unsafe (long Used, long Available)? GetD3dMemoryInfo()
    {
        if (this.Device == default)
            return null;

        using var device = default(ComPtr<IDXGIDevice>);
        using var adapter = default(ComPtr<IDXGIAdapter>);
        using var adapter4 = default(ComPtr<IDXGIAdapter4>);

        if (new ComPtr<IUnknown>((IUnknown*)this.Device).As(&device).FAILED)
            return null;

        if (device.Get()->GetAdapter(adapter.GetAddressOf()).FAILED)
            return null;

        if (adapter.As(&adapter4).FAILED)
            return null;

        var vmi = default(DXGI_QUERY_VIDEO_MEMORY_INFO);
        adapter4.Get()->QueryVideoMemoryInfo(0, DXGI_MEMORY_SEGMENT_GROUP.DXGI_MEMORY_SEGMENT_GROUP_LOCAL, &vmi);
        return ((long)vmi.CurrentUsage, (long)vmi.CurrentReservation);
    }

    /// <summary>
    /// Clear font, style, and color stack. Dangerous, only use when you know
    /// no one else has something pushed they may try to pop.
    /// </summary>
    public void ClearStacks()
    {
        ImGuiHelpers.ClearStacksOnContext();
    }

    /// <summary>
    /// Toggle Windows 11 immersive mode on the game window.
    /// </summary>
    /// <param name="enabled">Value.</param>
    internal void SetImmersiveMode(bool enabled)
    {
        if (this.GameWindowHandle == nint.Zero)
            return;

        int value = enabled ? 1 : 0;
        var hr = NativeFunctions.DwmSetWindowAttribute(
            this.GameWindowHandle,
            NativeFunctions.DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE,
            ref value,
            sizeof(int));
    }

    private static void ShowFontError(string path)
    {
        Util.Fatal($"One or more files required by XIVLauncher were not found.\nPlease restart and report this error if it occurs again.\n\n{path}", "Error");
    }

    private unsafe void InitScene(IDXGISwapChain* swapChain)
    {
        IWin32Scene newWin32Scene;
        using (Timings.Start("IM Scene Init"))
        {
            try
            {
                newWin32Scene = Service<DalamudConfiguration>.Get().UseDx12Preview
                                    ? new Dx12OnDx11Win32Scene(swapChain)
                                    : new Dx11Win32Scene(swapChain);
            }
            catch (DllNotFoundException ex)
            {
                Service<InterfaceManagerWithScene>.ProvideException(ex);
                Log.Error(ex, "Could not load ImGui dependencies.");

                int res;
                fixed (char* msg = "Dalamud plugins require the Microsoft Visual C++ Redistributable to be installed.\nPlease install the runtime from the official Microsoft website or disable Dalamud.\n\nDo you want to download the redistributable now?")
                {
                    fixed (char* title = "Dalamud Error")
                    {
                        res = Win32.MessageBoxW(
                            default,
                            (ushort*)msg,
                            (ushort*)title,
                            MB.MB_YESNO | MB.MB_TOPMOST | MB.MB_ICONERROR);
                    }
                }

                if (res == Win32.IDYES)
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
                    iniFileInfo.CopyTo(Path.Combine(iniFileInfo.DirectoryName, $"dalamudUI-{DateTimeOffset.Now.ToUnixTimeSeconds()}.ini"));
                    iniFileInfo.Delete();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Could not delete dalamudUI.ini");
            }

            newWin32Scene.UpdateCursor = this.isOverrideGameCursor;
            newWin32Scene.IniPath = iniFileInfo.FullName;
            newWin32Scene.BuildUi += this.Display;
            newWin32Scene.NewInputFrame += this.OnNewInputFrame;

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

            this.SetupFonts();

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

        this.scene = newWin32Scene;
        Service<InterfaceManagerWithScene>.Provide(new(this));

        this.wndProcHookManager.PreWndProc += this.WndProcHookManagerOnPreWndProc;
    }

    private unsafe void WndProcHookManagerOnPreWndProc(WndProcEventArgs args)
    {
        var r = this.scene?.ProcessWndProcW(args.Hwnd, args.Message, args.WParam, args.LParam);
        if (r is not null)
            args.SuppressWithValue(r.Value);

        this.dalamudIme.ProcessImeMessage(args);
    }

    /*
     * NOTE(goat): When hooking ReShade DXGISwapChain::runtime_present, this is missing the syncInterval arg.
     *             Seems to work fine regardless, I guess, so whatever.
     */
    private unsafe HRESULT PresentDetour(IDXGISwapChain* swapChain, uint syncInterval, uint presentFlags)
    {
        if (this.scene == null)
            this.InitScene(swapChain);

        if (!this.scene.IsAttachedToPresentationTarget((nint)swapChain))
            return this.presentHook!.Original(swapChain, syncInterval, presentFlags);

        if (this.address.IsReshade)
        {
            var pRes = this.presentHook.Original(swapChain, syncInterval, presentFlags);

            this.RenderImGui();
            this.DisposeTextures();

            return pRes;
        }

        this.RenderImGui();
        this.DisposeTextures();

        return this.presentHook.Original(swapChain, syncInterval, presentFlags);
    }

    private void DisposeTextures()
    {
        if (this.deferredDisposeTextures.Count > 0)
        {
            Log.Verbose("[IM] Disposing {Count} textures", this.deferredDisposeTextures.Count);
            foreach (var texture in this.deferredDisposeTextures)
            {
                texture.RealDispose();
            }

            this.deferredDisposeTextures.Clear();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RenderImGui()
    {
        // Process information needed by ImGuiHelpers each frame.
        ImGuiHelpers.NewFrame();

        // Check if we can still enable viewports without any issues.
        this.CheckViewportState();

        this.scene.Render();
    }

    private unsafe void CheckViewportState()
    {
        var configuration = Service<DalamudConfiguration>.Get();

        if (configuration.IsDisableViewport
            || this.scene.IsMainViewportFullScreen()
            || ImGui.GetPlatformIO().Monitors.Size == 1)
        {
            ImGui.GetIO().ConfigFlags &= ~ImGuiConfigFlags.ViewportsEnable;
            return;
        }

        ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;
    }

    /// <summary>
    /// Loads font for use in ImGui text functions.
    /// </summary>
    private unsafe void SetupFonts()
    {
        using var setupFontsTimings = Timings.Start("IM SetupFonts");

        var gameFontManager = Service<GameFontManager>.Get();
        var dalamud = Service<Dalamud>.Get();
        var io = ImGui.GetIO();
        var ioFonts = io.Fonts;

        var fontGamma = this.FontGamma;

        this.fontBuildSignal.Reset();
        ioFonts.Clear();
        ioFonts.TexDesiredWidth = 4096;

        Log.Verbose("[FONT] SetupFonts - 1");

        foreach (var v in this.loadedFontInfo)
            v.Value.Dispose();

        this.loadedFontInfo.Clear();

        Log.Verbose("[FONT] SetupFonts - 2");

        ImFontConfigPtr fontConfig = null;
        List<GCHandle> garbageList = new();

        try
        {
            var dummyRangeHandle = GCHandle.Alloc(new ushort[] { '0', '0', 0 }, GCHandleType.Pinned);
            garbageList.Add(dummyRangeHandle);

            fontConfig = ImGuiNative.ImFontConfig_ImFontConfig();
            fontConfig.OversampleH = 1;
            fontConfig.OversampleV = 1;

            var fontPathJp = Path.Combine(dalamud.AssetDirectory.FullName, "UIRes", "NotoSansCJKjp-Regular.otf");
            if (!File.Exists(fontPathJp))
                fontPathJp = Path.Combine(dalamud.AssetDirectory.FullName, "UIRes", "NotoSansCJKjp-Medium.otf");
            if (!File.Exists(fontPathJp))
                ShowFontError(fontPathJp);
            Log.Verbose("[FONT] fontPathJp = {0}", fontPathJp);

            var fontPathKr = Path.Combine(dalamud.AssetDirectory.FullName, "UIRes", "NotoSansCJKkr-Regular.otf");
            if (!File.Exists(fontPathKr))
                fontPathKr = Path.Combine(dalamud.AssetDirectory.FullName, "UIRes", "NotoSansKR-Regular.otf");
            if (!File.Exists(fontPathKr))
                fontPathKr = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts", "malgun.ttf");
            if (!File.Exists(fontPathKr))
                fontPathKr = null;
            Log.Verbose("[FONT] fontPathKr = {0}", fontPathKr);

            var fontPathChs = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts", "msyh.ttc");
            if (!File.Exists(fontPathChs))
                fontPathChs = null;
            Log.Verbose("[FONT] fontPathChs = {0}", fontPathChs);

            var fontPathCht = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts", "msjh.ttc");
            if (!File.Exists(fontPathCht))
                fontPathCht = null;
            Log.Verbose("[FONT] fontPathChs = {0}", fontPathCht);

            // Default font
            Log.Verbose("[FONT] SetupFonts - Default font");
            var fontInfo = new TargetFontModification(
                "Default",
                this.UseAxis ? TargetFontModification.AxisMode.Overwrite : TargetFontModification.AxisMode.GameGlyphsOnly,
                this.UseAxis ? DefaultFontSizePx : DefaultFontSizePx + 1,
                io.FontGlobalScale);
            Log.Verbose("[FONT] SetupFonts - Default corresponding AXIS size: {0}pt ({1}px)", fontInfo.SourceAxis.Style.BaseSizePt, fontInfo.SourceAxis.Style.BaseSizePx);
            fontConfig.SizePixels = fontInfo.TargetSizePx * io.FontGlobalScale;
            if (this.UseAxis)
            {
                fontConfig.GlyphRanges = dummyRangeHandle.AddrOfPinnedObject();
                fontConfig.PixelSnapH = false;
                DefaultFont = ioFonts.AddFontDefault(fontConfig);
                this.loadedFontInfo[DefaultFont] = fontInfo;
            }
            else
            {
                var rangeHandle = gameFontManager.ToGlyphRanges(GameFontFamilyAndSize.Axis12);
                garbageList.Add(rangeHandle);

                fontConfig.GlyphRanges = rangeHandle.AddrOfPinnedObject();
                fontConfig.PixelSnapH = true;
                DefaultFont = ioFonts.AddFontFromFileTTF(fontPathJp, fontConfig.SizePixels, fontConfig);
                this.loadedFontInfo[DefaultFont] = fontInfo;
            }

            if (fontPathKr != null
                && (Service<DalamudConfiguration>.Get().EffectiveLanguage == "ko" || this.dalamudIme.EncounteredHangul))
            {
                fontConfig.MergeMode = true;
                fontConfig.GlyphRanges = ioFonts.GetGlyphRangesKorean();
                fontConfig.PixelSnapH = true;
                ioFonts.AddFontFromFileTTF(fontPathKr, fontConfig.SizePixels, fontConfig);
                fontConfig.MergeMode = false;
            }

            if (fontPathCht != null && Service<DalamudConfiguration>.Get().EffectiveLanguage == "tw")
            {
                fontConfig.MergeMode = true;
                var rangeHandle = GCHandle.Alloc(new ushort[]
                {
                    (ushort)UnicodeRanges.CjkUnifiedIdeographs.FirstCodePoint,
                    (ushort)(UnicodeRanges.CjkUnifiedIdeographs.FirstCodePoint +
                             (UnicodeRanges.CjkUnifiedIdeographs.Length - 1)),
                    (ushort)UnicodeRanges.CjkUnifiedIdeographsExtensionA.FirstCodePoint,
                    (ushort)(UnicodeRanges.CjkUnifiedIdeographsExtensionA.FirstCodePoint +
                             (UnicodeRanges.CjkUnifiedIdeographsExtensionA.Length - 1)),
                    0,
                }, GCHandleType.Pinned);
                garbageList.Add(rangeHandle);
                fontConfig.GlyphRanges = rangeHandle.AddrOfPinnedObject();
                fontConfig.PixelSnapH = true;
                ioFonts.AddFontFromFileTTF(fontPathCht, fontConfig.SizePixels, fontConfig);
                fontConfig.MergeMode = false;
            }
            else if (fontPathChs != null && (Service<DalamudConfiguration>.Get().EffectiveLanguage == "zh"
                                               || this.dalamudIme.EncounteredHan))
            {
                fontConfig.MergeMode = true;
                var rangeHandle = GCHandle.Alloc(new ushort[]
                {
                    (ushort)UnicodeRanges.CjkUnifiedIdeographs.FirstCodePoint,
                    (ushort)(UnicodeRanges.CjkUnifiedIdeographs.FirstCodePoint +
                             (UnicodeRanges.CjkUnifiedIdeographs.Length - 1)),
                    (ushort)UnicodeRanges.CjkUnifiedIdeographsExtensionA.FirstCodePoint,
                    (ushort)(UnicodeRanges.CjkUnifiedIdeographsExtensionA.FirstCodePoint +
                             (UnicodeRanges.CjkUnifiedIdeographsExtensionA.Length - 1)),
                    0,
                }, GCHandleType.Pinned);
                garbageList.Add(rangeHandle);
                fontConfig.GlyphRanges = rangeHandle.AddrOfPinnedObject();
                fontConfig.PixelSnapH = true;
                ioFonts.AddFontFromFileTTF(fontPathChs, fontConfig.SizePixels, fontConfig);
                fontConfig.MergeMode = false;
            }

            // FontAwesome icon font
            Log.Verbose("[FONT] SetupFonts - FontAwesome icon font");
            {
                var fontPathIcon = Path.Combine(dalamud.AssetDirectory.FullName, "UIRes", "FontAwesomeFreeSolid.otf");
                if (!File.Exists(fontPathIcon))
                    ShowFontError(fontPathIcon);

                var iconRangeHandle = GCHandle.Alloc(new ushort[] { 0xE000, 0xF8FF, 0, }, GCHandleType.Pinned);
                garbageList.Add(iconRangeHandle);

                fontConfig.GlyphRanges = iconRangeHandle.AddrOfPinnedObject();
                fontConfig.PixelSnapH = true;
                IconFont = ioFonts.AddFontFromFileTTF(fontPathIcon, DefaultFontSizePx * io.FontGlobalScale, fontConfig);
                this.loadedFontInfo[IconFont] = new("Icon", TargetFontModification.AxisMode.GameGlyphsOnly, DefaultFontSizePx, io.FontGlobalScale);
            }

            // Monospace font
            Log.Verbose("[FONT] SetupFonts - Monospace font");
            {
                var fontPathMono = Path.Combine(dalamud.AssetDirectory.FullName, "UIRes", "Inconsolata-Regular.ttf");
                if (!File.Exists(fontPathMono))
                    ShowFontError(fontPathMono);

                fontConfig.GlyphRanges = IntPtr.Zero;
                fontConfig.PixelSnapH = true;
                MonoFont = ioFonts.AddFontFromFileTTF(fontPathMono, DefaultFontSizePx * io.FontGlobalScale, fontConfig);
                this.loadedFontInfo[MonoFont] = new("Mono", TargetFontModification.AxisMode.GameGlyphsOnly, DefaultFontSizePx, io.FontGlobalScale);
            }

            // Default font but in requested size for requested glyphs
            Log.Verbose("[FONT] SetupFonts - Default font but in requested size for requested glyphs");
            {
                Dictionary<float, List<SpecialGlyphRequest>> extraFontRequests = new();
                foreach (var extraFontRequest in this.glyphRequests)
                {
                    if (!extraFontRequests.ContainsKey(extraFontRequest.Size))
                        extraFontRequests[extraFontRequest.Size] = new();
                    extraFontRequests[extraFontRequest.Size].Add(extraFontRequest);
                }

                foreach (var (fontSize, requests) in extraFontRequests)
                {
                    List<(ushort, ushort)> codepointRanges = new(4 + requests.Sum(x => x.CodepointRanges.Count))
                    {
                        new(Fallback1Codepoint, Fallback1Codepoint),
                        new(Fallback2Codepoint, Fallback2Codepoint),
                        // ImGui default ellipsis characters
                        new(0x2026, 0x2026),
                        new(0x0085, 0x0085),
                    };

                    foreach (var request in requests)
                        codepointRanges.AddRange(request.CodepointRanges.Select(x => (From: x.Item1, To: x.Item2)));

                    codepointRanges.Sort();
                    List<ushort> flattenedRanges = new();
                    foreach (var range in codepointRanges)
                    {
                        if (flattenedRanges.Any() && flattenedRanges[^1] >= range.Item1 - 1)
                        {
                            flattenedRanges[^1] = Math.Max(flattenedRanges[^1], range.Item2);
                        }
                        else
                        {
                            flattenedRanges.Add(range.Item1);
                            flattenedRanges.Add(range.Item2);
                        }
                    }

                    flattenedRanges.Add(0);

                    fontInfo = new(
                        $"Requested({fontSize}px)",
                        this.UseAxis ? TargetFontModification.AxisMode.Overwrite : TargetFontModification.AxisMode.GameGlyphsOnly,
                        fontSize,
                        io.FontGlobalScale);
                    if (this.UseAxis)
                    {
                        fontConfig.GlyphRanges = dummyRangeHandle.AddrOfPinnedObject();
                        fontConfig.SizePixels = fontInfo.SourceAxis.Style.BaseSizePx;
                        fontConfig.PixelSnapH = false;

                        var sizedFont = ioFonts.AddFontDefault(fontConfig);
                        this.loadedFontInfo[sizedFont] = fontInfo;
                        foreach (var request in requests)
                            request.FontInternal = sizedFont;
                    }
                    else
                    {
                        var rangeHandle = GCHandle.Alloc(flattenedRanges.ToArray(), GCHandleType.Pinned);
                        garbageList.Add(rangeHandle);
                        fontConfig.PixelSnapH = true;

                        var sizedFont = ioFonts.AddFontFromFileTTF(fontPathJp, fontSize * io.FontGlobalScale, fontConfig, rangeHandle.AddrOfPinnedObject());
                        this.loadedFontInfo[sizedFont] = fontInfo;
                        foreach (var request in requests)
                            request.FontInternal = sizedFont;
                    }
                }
            }

            gameFontManager.BuildFonts();

            var customFontFirstConfigIndex = ioFonts.ConfigData.Size;

            Log.Verbose("[FONT] Invoke OnBuildFonts");
            this.BuildFonts?.InvokeSafely();
            Log.Verbose("[FONT] OnBuildFonts OK!");

            for (int i = customFontFirstConfigIndex, i_ = ioFonts.ConfigData.Size; i < i_; i++)
            {
                var config = ioFonts.ConfigData[i];
                if (gameFontManager.OwnsFont(config.DstFont))
                    continue;

                config.OversampleH = 1;
                config.OversampleV = 1;

                var name = Encoding.UTF8.GetString((byte*)config.Name.Data, config.Name.Count).TrimEnd('\0');
                if (name.IsNullOrEmpty())
                    name = $"{config.SizePixels}px";

                // ImFont information is reflected only if corresponding ImFontConfig has MergeMode not set.
                if (config.MergeMode)
                {
                    if (!this.loadedFontInfo.ContainsKey(config.DstFont.NativePtr))
                    {
                        Log.Warning("MergeMode specified for {0} but not found in loadedFontInfo. Skipping.", name);
                        continue;
                    }
                }
                else
                {
                    if (this.loadedFontInfo.ContainsKey(config.DstFont.NativePtr))
                    {
                        Log.Warning("MergeMode not specified for {0} but found in loadedFontInfo. Skipping.", name);
                        continue;
                    }

                    // While the font will be loaded in the scaled size after FontScale is applied, the font will be treated as having the requested size when used from plugins.
                    this.loadedFontInfo[config.DstFont.NativePtr] = new($"PlReq({name})", config.SizePixels);
                }

                config.SizePixels = config.SizePixels * io.FontGlobalScale;
            }

            for (int i = 0, i_ = ioFonts.ConfigData.Size; i < i_; i++)
            {
                var config = ioFonts.ConfigData[i];
                config.RasterizerGamma *= fontGamma;
            }

            Log.Verbose("[FONT] ImGui.IO.Build will be called.");
            ioFonts.Build();
            gameFontManager.AfterIoFontsBuild();
            this.ClearStacks();
            Log.Verbose("[FONT] ImGui.IO.Build OK!");

            gameFontManager.AfterBuildFonts();

            foreach (var (font, mod) in this.loadedFontInfo)
            {
                // I have no idea what's causing NPE, so just to be safe
                try
                {
                    if (font.NativePtr != null && font.NativePtr->ConfigData != null)
                    {
                        var nameBytes = Encoding.UTF8.GetBytes($"{mod.Name}\0");
                        Marshal.Copy(nameBytes, 0, (IntPtr)font.ConfigData.Name.Data, Math.Min(nameBytes.Length, font.ConfigData.Name.Count));
                    }
                }
                catch (NullReferenceException)
                {
                    // do nothing
                }

                Log.Verbose("[FONT] {0}: Unscale with scale value of {1}", mod.Name, mod.Scale);
                GameFontManager.UnscaleFont(font, mod.Scale, false);

                if (mod.Axis == TargetFontModification.AxisMode.Overwrite)
                {
                    Log.Verbose("[FONT] {0}: Overwrite from AXIS of size {1}px (was {2}px)", mod.Name, mod.SourceAxis.ImFont.FontSize, font.FontSize);
                    GameFontManager.UnscaleFont(font, font.FontSize / mod.SourceAxis.ImFont.FontSize, false);
                    var ascentDiff = mod.SourceAxis.ImFont.Ascent - font.Ascent;
                    font.Ascent += ascentDiff;
                    font.Descent = ascentDiff;
                    font.FallbackChar = mod.SourceAxis.ImFont.FallbackChar;
                    font.EllipsisChar = mod.SourceAxis.ImFont.EllipsisChar;
                    ImGuiHelpers.CopyGlyphsAcrossFonts(mod.SourceAxis.ImFont, font, false, false);
                }
                else if (mod.Axis == TargetFontModification.AxisMode.GameGlyphsOnly)
                {
                    Log.Verbose("[FONT] {0}: Overwrite game specific glyphs from AXIS of size {1}px", mod.Name, mod.SourceAxis.ImFont.FontSize, font.FontSize);
                    if (!this.UseAxis && font.NativePtr == DefaultFont.NativePtr)
                        mod.SourceAxis.ImFont.FontSize -= 1;
                    ImGuiHelpers.CopyGlyphsAcrossFonts(mod.SourceAxis.ImFont, font, true, false, 0xE020, 0xE0DB);
                    if (!this.UseAxis && font.NativePtr == DefaultFont.NativePtr)
                        mod.SourceAxis.ImFont.FontSize += 1;
                }

                Log.Verbose("[FONT] {0}: Resize from {1}px to {2}px", mod.Name, font.FontSize, mod.TargetSizePx);
                GameFontManager.UnscaleFont(font, font.FontSize / mod.TargetSizePx, false);
            }

            // Fill missing glyphs in MonoFont from DefaultFont
            ImGuiHelpers.CopyGlyphsAcrossFonts(DefaultFont, MonoFont, true, false);

            for (int i = 0, i_ = ioFonts.Fonts.Size; i < i_; i++)
            {
                var font = ioFonts.Fonts[i];
                if (font.Glyphs.Size == 0)
                {
                    Log.Warning("[FONT] Font has no glyph: {0}", font.GetDebugName());
                    continue;
                }

                if (font.FindGlyphNoFallback(Fallback1Codepoint).NativePtr != null)
                    font.FallbackChar = Fallback1Codepoint;

                font.BuildLookupTableNonstandard();
            }

            Log.Verbose("[FONT] Invoke OnAfterBuildFonts");
            this.AfterBuildFonts?.InvokeSafely();
            Log.Verbose("[FONT] OnAfterBuildFonts OK!");

            if (ioFonts.Fonts[0].NativePtr != DefaultFont.NativePtr)
                Log.Warning("[FONT] First font is not DefaultFont");

            Log.Verbose("[FONT] Fonts built!");

            this.fontBuildSignal.Set();

            this.FontsReady = true;
        }
        finally
        {
            if (fontConfig.NativePtr != null)
                fontConfig.Destroy();

            foreach (var garbage in garbageList)
                garbage.Free();
        }
    }

    [ServiceManager.CallWhenServicesReady(
        "InterfaceManager accepts event registration and stuff even when the game window is not ready.")]
    private unsafe void ContinueConstruction(
        TargetSigScanner sigScanner,
        DalamudAssetManager dalamudAssetManager,
        DalamudConfiguration configuration)
    {
        this.address.Setup(sigScanner);
        this.framework.RunOnFrameworkThread(() =>
        {
            fixed (char* ffxivgame = "FFXIVGAME\0")
            {
                while ((this.GameWindowHandle = Win32.FindWindowExW(
                            default,
                            this.GameWindowHandle,
                            (ushort*)ffxivgame,
                            default)) != default)
                {
                    uint pid;
                    _ = Win32.GetWindowThreadProcessId(this.GameWindowHandle, &pid);

                    if (pid == Environment.ProcessId && Win32.IsWindowVisible(this.GameWindowHandle))
                        break;
                }
            }

            try
            {
                if (configuration.WindowIsImmersive)
                    this.SetImmersiveMode(true);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Could not enable immersive mode");
            }

            this.presentHook = Hook<PresentDelegate>.FromAddress(this.address.Present, this.PresentDetour);
            this.resizeBuffersHook = Hook<ResizeBuffersDelegate>.FromAddress(this.address.ResizeBuffers, this.ResizeBuffersDetour);

            Log.Verbose("===== S W A P C H A I N =====");
            Log.Verbose($"Present address 0x{this.presentHook!.Address.ToInt64():X}");
            Log.Verbose($"ResizeBuffers address 0x{this.resizeBuffersHook!.Address.ToInt64():X}");

            this.setCursorHook.Enable();
            this.presentHook.Enable();
            this.resizeBuffersHook.Enable();
        });
    }

    // This is intended to only be called as a handler attached to scene.OnNewRenderFrame
    private void RebuildFontsInternal()
    {
        Log.Verbose("[FONT] RebuildFontsInternal() called");
        this.SetupFonts();

        Log.Verbose("[FONT] RebuildFontsInternal() detaching");
        this.scene!.NewRenderFrame -= this.RebuildFontsInternal;

        Log.Verbose("[FONT] Calling InvalidateFonts");
        this.scene.InvalidateFonts();

        Log.Verbose("[FONT] Font Rebuild OK!");

        this.isRebuildingFonts = false;
    }

    private unsafe HRESULT ResizeBuffersDetour(IDXGISwapChain* swapChain, uint bufferCount, uint width, uint height, uint newFormat, uint swapChainFlags)
    {
#if DEBUG
        Log.Verbose($"Calling resizebuffers swap@{(nint)swapChain:X}{bufferCount} {width} {height} {newFormat} {swapChainFlags}");
#endif

        this.ResizeBuffers?.InvokeSafely();

        // We have to ensure we're working with the main swapchain,
        // as viewports might be resizing as well
        if (this.scene.IsAttachedToPresentationTarget((nint)swapChain) is not true)
            return this.resizeBuffersHook!.Original(swapChain, bufferCount, width, height, newFormat, swapChainFlags);

        this.scene?.OnPreResize();

        var ret = this.resizeBuffersHook!.Original(swapChain, bufferCount, width, height, newFormat, swapChainFlags);
        if (ret == 0x887A0001)
        {
            Log.Error("invalid call to resizeBuffers");
        }

        this.scene?.OnPostResize((int)width, (int)height);

        return ret;
    }

    private HCURSOR SetCursorDetour(HCURSOR hCursor)
    {
        if (this.lastWantCapture == true && (!this.scene?.IsImGuiCursor(hCursor) ?? false) && this.OverrideGameCursor)
            return default;

        return this.setCursorHook.IsDisposed ? Win32.SetCursor(hCursor) : this.setCursorHook.Original(hCursor);
    }

    private void OnNewInputFrame()
    {
        var dalamudInterface = Service<DalamudInterface>.GetNullable();
        var gamepadState = Service<GamepadState>.GetNullable();
        var keyState = Service<KeyState>.GetNullable();

        if (dalamudInterface == null || gamepadState == null || keyState == null)
            return;

        // fix for keys in game getting stuck, if you were holding a game key (like run)
        // and then clicked on an imgui textbox - imgui would swallow the keyup event,
        // so the game would think the key remained pressed continuously until you left
        // imgui and pressed and released the key again
        if (ImGui.GetIO().WantTextInput)
        {
            keyState.ClearAll();
        }

        // TODO: mouse state?

        var gamepadEnabled = (ImGui.GetIO().BackendFlags & ImGuiBackendFlags.HasGamepad) > 0;

        // NOTE (Chiv) Activate ImGui navigation  via L1+L3 press
        // (mimicking how mouse navigation is activated via L1+R3 press in game).
        if (gamepadEnabled
            && gamepadState.Raw(GamepadButtons.L1) > 0
            && gamepadState.Pressed(GamepadButtons.L3) > 0)
        {
            ImGui.GetIO().ConfigFlags ^= ImGuiConfigFlags.NavEnableGamepad;
            gamepadState.NavEnableGamepad ^= true;
            dalamudInterface.ToggleGamepadModeNotifierWindow();
        }

        if (gamepadEnabled && (ImGui.GetIO().ConfigFlags & ImGuiConfigFlags.NavEnableGamepad) > 0)
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

            var io = ImGui.GetIO();
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
            this.Draw?.Invoke();

        ImGuiManagedAsserts.ReportProblems("Dalamud Core", snap);

        Service<NotificationManager>.Get().Draw();
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

    /// <summary>
    /// Represents a glyph request.
    /// </summary>
    public class SpecialGlyphRequest : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SpecialGlyphRequest"/> class.
        /// </summary>
        /// <param name="manager">InterfaceManager to associate.</param>
        /// <param name="size">Font size in pixels.</param>
        /// <param name="ranges">Codepoint ranges.</param>
        internal SpecialGlyphRequest(InterfaceManager manager, float size, List<Tuple<ushort, ushort>> ranges)
        {
            this.Manager = manager;
            this.Size = size;
            this.CodepointRanges = ranges;
            this.Manager.glyphRequests.Add(this);
        }

        /// <summary>
        /// Gets the font of specified size, or DefaultFont if it's not ready yet.
        /// </summary>
        public ImFontPtr Font
        {
            get
            {
                unsafe
                {
                    return this.FontInternal.NativePtr == null ? DefaultFont : this.FontInternal;
                }
            }
        }

        /// <summary>
        /// Gets or sets the associated ImFont.
        /// </summary>
        internal ImFontPtr FontInternal { get; set; }

        /// <summary>
        /// Gets associated InterfaceManager.
        /// </summary>
        internal InterfaceManager Manager { get; init; }

        /// <summary>
        /// Gets font size.
        /// </summary>
        internal float Size { get; init; }

        /// <summary>
        /// Gets codepoint ranges.
        /// </summary>
        internal List<Tuple<ushort, ushort>> CodepointRanges { get; init; }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.Manager.glyphRequests.Remove(this);
        }
    }

    private unsafe class TargetFontModification : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TargetFontModification"/> class.
        /// Constructs new target font modification information, assuming that AXIS fonts will not be applied.
        /// </summary>
        /// <param name="name">Name of the font to write to ImGui font information.</param>
        /// <param name="sizePx">Target font size in pixels, which will not be considered for further scaling.</param>
        internal TargetFontModification(string name, float sizePx)
        {
            this.Name = name;
            this.Axis = AxisMode.Suppress;
            this.TargetSizePx = sizePx;
            this.Scale = 1;
            this.SourceAxis = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TargetFontModification"/> class.
        /// Constructs new target font modification information.
        /// </summary>
        /// <param name="name">Name of the font to write to ImGui font information.</param>
        /// <param name="axis">Whether and how to use AXIS fonts.</param>
        /// <param name="sizePx">Target font size in pixels, which will not be considered for further scaling.</param>
        /// <param name="globalFontScale">Font scale to be referred for loading AXIS font of appropriate size.</param>
        internal TargetFontModification(string name, AxisMode axis, float sizePx, float globalFontScale)
        {
            this.Name = name;
            this.Axis = axis;
            this.TargetSizePx = sizePx;
            this.Scale = globalFontScale;
            this.SourceAxis = Service<GameFontManager>.Get().NewFontRef(new(GameFontFamily.Axis, this.TargetSizePx * this.Scale));
        }

        internal enum AxisMode
        {
            Suppress,
            GameGlyphsOnly,
            Overwrite,
        }

        internal string Name { get; private init; }

        internal AxisMode Axis { get; private init; }

        internal float TargetSizePx { get; private init; }

        internal float Scale { get; private init; }

        internal GameFontHandle? SourceAxis { get; private init; }

        internal bool SourceAxisAvailable => this.SourceAxis != null && this.SourceAxis.ImFont.NativePtr != null;

        public void Dispose()
        {
            this.SourceAxis?.Dispose();
        }
    }
}
