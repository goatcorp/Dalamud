using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

using Dalamud.Configuration.Internal;
using Dalamud.Game;
using Dalamud.Game.ClientState.GamePad;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.Gui.Internal;
using Dalamud.Game.Internal.DXGI;
using Dalamud.Hooking;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.Internal.ManagedAsserts;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Interface.Style;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using Dalamud.Utility.Timing;
using ImGuiNET;
using ImGuiScene;
using PInvoke;
using Serilog;
using SharpDX.Direct3D11;

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

namespace Dalamud.Interface.Internal
{
    /// <summary>
    /// This class manages interaction with the ImGui interface.
    /// </summary>
    internal class InterfaceManager : IDisposable
    {
        private const float MinimumFallbackFontSizePt = 9.6f;  // Game's minimum AXIS font size
        private const float MinimumFallbackFontSizePx = MinimumFallbackFontSizePt * 4.0f / 3.0f;
        private const float DefaultFontSizePt = 12.0f;
        private const float DefaultFontSizePx = DefaultFontSizePt * 4.0f / 3.0f;
        private const ushort Fallback1Codepoint = 0x3013; // Geta mark; FFXIV uses this to indicate that a glyph is missing.
        private const ushort Fallback2Codepoint = '-'; // FFXIV uses dash if Geta mark is unavailable.

        private readonly string rtssPath;

        private readonly HashSet<SpecialGlyphRequest> glyphRequests = new();
        private readonly Dictionary<ImFontPtr, TargetFontModification> loadedFontInfo = new();

        private readonly Hook<PresentDelegate> presentHook;
        private readonly Hook<ResizeBuffersDelegate> resizeBuffersHook;
        private readonly Hook<SetCursorDelegate> setCursorHook;

        private readonly ManualResetEvent fontBuildSignal;
        private readonly SwapChainVtableResolver address;
        private RawDX11Scene? scene;

        // can't access imgui IO before first present call
        private bool lastWantCapture = false;
        private bool isRebuildingFonts = false;

        private bool isFallbackFontMode = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="InterfaceManager"/> class.
        /// </summary>
        public InterfaceManager()
        {
            Service<NotificationManager>.Set();

            var scanner = Service<SigScanner>.Get();

            this.fontBuildSignal = new ManualResetEvent(false);

            this.address = new SwapChainVtableResolver();
            this.address.Setup(scanner);

            try
            {
                var rtss = NativeFunctions.GetModuleHandleW("RTSSHooks64.dll");

                if (rtss != IntPtr.Zero)
                {
                    var fileName = new StringBuilder(255);
                    _ = NativeFunctions.GetModuleFileNameW(rtss, fileName, fileName.Capacity);
                    this.rtssPath = fileName.ToString();
                    Log.Verbose($"RTSS at {this.rtssPath}");

                    if (!NativeFunctions.FreeLibrary(rtss))
                        throw new Win32Exception();
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "RTSS Free failed");
            }

            this.setCursorHook = Hook<SetCursorDelegate>.FromSymbol("user32.dll", "SetCursor", this.SetCursorDetour, true);
            this.presentHook = new Hook<PresentDelegate>(this.address.Present, this.PresentDetour);
            this.resizeBuffersHook = new Hook<ResizeBuffersDelegate>(this.address.ResizeBuffers, this.ResizeBuffersDetour);

            var setCursorAddress = this.setCursorHook?.Address ?? IntPtr.Zero;

            Log.Verbose("===== S W A P C H A I N =====");
            Log.Verbose($"SetCursor address 0x{setCursorAddress.ToInt64():X}");
            Log.Verbose($"Present address 0x{this.presentHook.Address.ToInt64():X}");
            Log.Verbose($"ResizeBuffers address 0x{this.resizeBuffersHook.Address.ToInt64():X}");
        }

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate IntPtr PresentDelegate(IntPtr swapChain, uint syncInterval, uint presentFlags);

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate IntPtr ResizeBuffersDelegate(IntPtr swapChain, uint bufferCount, uint width, uint height, uint newFormat, uint swapChainFlags);

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate IntPtr SetCursorDelegate(IntPtr hCursor);

        private delegate void InstallRTSSHook();

        /// <summary>
        /// This event gets called each frame to facilitate ImGui drawing.
        /// </summary>
        public event RawDX11Scene.BuildUIDelegate Draw;

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
        /// Gets or sets an action that is executed right after font fallback mode has been changed.
        /// </summary>
        public event Action<bool> FallbackFontModeChange;

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
        /// Gets the D3D11 device instance.
        /// </summary>
        public Device? Device => this.scene?.Device;

        /// <summary>
        /// Gets the address handle to the main process window.
        /// </summary>
        public IntPtr WindowHandlePtr => this.scene.WindowHandlePtr;

        /// <summary>
        /// Gets or sets a value indicating whether or not the game's cursor should be overridden with the ImGui cursor.
        /// </summary>
        public bool OverrideGameCursor
        {
            get => this.scene.UpdateCursor;
            set => this.scene.UpdateCursor = value;
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
        /// Gets or sets a value indicating whether the font has been loaded in fallback mode.
        /// </summary>
        public bool IsFallbackFontMode
        {
            get => this.isFallbackFontMode;
            internal set
            {
                if (value == this.isFallbackFontMode)
                    return;

                this.isFallbackFontMode = value;
                this.FallbackFontModeChange?.Invoke(value);
            }
        }

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
        /// Gets or sets a value indicating whether to override configuration for FontResolutionLevel.
        /// </summary>
        public int? FontResolutionLevelOverride { get; set; } = null;

        /// <summary>
        /// Gets a value indicating the level of font resolution.
        /// </summary>
        public int FontResolutionLevel => this.FontResolutionLevelOverride ?? Service<DalamudConfiguration>.Get().FontResolutionLevel;

        /// <summary>
        /// Gets a value indicating whether we're building fonts but haven't generated atlas yet.
        /// </summary>
        public bool IsBuildingFontsBeforeAtlasBuild => this.isRebuildingFonts && !this.fontBuildSignal.WaitOne(0);

        /// <summary>
        /// Enable this module.
        /// </summary>
        public void Enable()
        {
            this.setCursorHook?.Enable();
            this.presentHook.Enable();
            this.resizeBuffersHook.Enable();

            try
            {
                if (!string.IsNullOrEmpty(this.rtssPath))
                {
                    NativeFunctions.LoadLibraryW(this.rtssPath);
                    var rtssModule = NativeFunctions.GetModuleHandleW("RTSSHooks64.dll");
                    var installAddr = NativeFunctions.GetProcAddress(rtssModule, "InstallRTSSHook");

                    Log.Debug("Installing RTSS hook");
                    Marshal.GetDelegateForFunctionPointer<InstallRTSSHook>(installAddr).Invoke();
                    Log.Debug("RTSS hook OK!");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Could not reload RTSS");
            }
        }

        /// <summary>
        /// Dispose of managed and unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            // HACK: this is usually called on a separate thread from PresentDetour (likely on a dedicated render thread)
            // and if we aren't already disabled, disposing of the scene and hook can frequently crash due to the hook
            // being disposed of in this thread while it is actively in use in the render thread.
            // This is a terrible way to prevent issues, but should basically always work to ensure that all outstanding
            // calls to PresentDetour have finished (and Disable means no new ones will start), before we try to cleanup
            // So... not great, but much better than constantly crashing on unload
            this.Disable();
            Thread.Sleep(500);

            this.scene?.Dispose();
            this.setCursorHook?.Dispose();
            this.presentHook.Dispose();
            this.resizeBuffersHook.Dispose();
        }

#nullable enable

        /// <summary>
        /// Load an image from disk.
        /// </summary>
        /// <param name="filePath">The filepath to load.</param>
        /// <returns>A texture, ready to use in ImGui.</returns>
        public TextureWrap? LoadImage(string filePath)
        {
            try
            {
                return this.scene?.LoadImage(filePath) ?? null;
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
        public TextureWrap? LoadImage(byte[] imageData)
        {
            try
            {
                return this.scene?.LoadImage(imageData) ?? null;
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
        public TextureWrap? LoadImageRaw(byte[] imageData, int width, int height, int numChannels)
        {
            try
            {
                return this.scene?.LoadImageRaw(imageData, width, height, numChannels) ?? null;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load image from raw data");
            }

            return null;
        }

#nullable restore

        /// <summary>
        /// Sets up a deferred invocation of font rebuilding, before the next render frame.
        /// </summary>
        public void RebuildFonts()
        {
            Log.Verbose("[FONT] RebuildFonts() called");

            // don't invoke this multiple times per frame, in case multiple plugins call it
            if (!this.isRebuildingFonts)
            {
                Log.Verbose("[FONT] RebuildFonts() trigger");
                this.isRebuildingFonts = true;
                this.scene.OnNewRenderFrame += this.RebuildFontsInternal;
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

        private static void ShowFontError(string path)
        {
            Util.Fatal($"One or more files required by XIVLauncher were not found.\nPlease restart and report this error if it occurs again.\n\n{path}", "Error");
        }

        /*
         * NOTE(goat): When hooking ReShade DXGISwapChain::runtime_present, this is missing the syncInterval arg.
         *             Seems to work fine regardless, I guess, so whatever.
         */
        private IntPtr PresentDetour(IntPtr swapChain, uint syncInterval, uint presentFlags)
        {
            if (this.scene != null && swapChain != this.scene.SwapChain.NativePointer)
                return this.presentHook.Original(swapChain, syncInterval, presentFlags);

            if (this.scene == null)
            {
                using (Timings.Start("IM Scene Init"))
                {
                    try
                    {
                        this.scene = new RawDX11Scene(swapChain);
                    }
                    catch (DllNotFoundException ex)
                    {
                        Log.Error(ex, "Could not load ImGui dependencies.");

                        var res = PInvoke.User32.MessageBox(
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
                    }

                    var startInfo = Service<DalamudStartInfo>.Get();
                    var configuration = Service<DalamudConfiguration>.Get();

                    var iniFileInfo = new FileInfo(Path.Combine(Path.GetDirectoryName(startInfo.ConfigurationPath), "dalamudUI.ini"));

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

                    this.scene.ImGuiIniPath = iniFileInfo.FullName;
                    this.scene.OnBuildUI += this.Display;
                    this.scene.OnNewInputFrame += this.OnNewInputFrame;

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
                        configuration.Save();
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

                    Service<DalamudIME>.Get().Enable();
                }
            }

            if (this.address.IsReshade)
            {
                var pRes = this.presentHook.Original(swapChain, syncInterval, presentFlags);

                this.RenderImGui();

                return pRes;
            }

            this.RenderImGui();

            return this.presentHook.Original(swapChain, syncInterval, presentFlags);
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

        private void CheckViewportState()
        {
            var configuration = Service<DalamudConfiguration>.Get();

            if (configuration.IsDisableViewport || this.scene.SwapChain.IsFullScreen || ImGui.GetPlatformIO().Monitors.Size == 1)
            {
                ImGui.GetIO().ConfigFlags &= ~ImGuiConfigFlags.ViewportsEnable;
                return;
            }

            ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;
        }

        /// <summary>
        /// Loads font for use in ImGui text functions.
        /// </summary>
        /// <param name="disableBigFonts">If set, then glyphs will be loaded in smaller resolution to make all glyphs fit into given constraints.</param>
        private unsafe void SetupFonts(bool disableBigFonts = false)
        {
            using var setupFontsTimings = Timings.Start("IM SetupFonts");

            var gameFontManager = Service<GameFontManager>.Get();
            var dalamud = Service<Dalamud>.Get();
            var io = ImGui.GetIO();
            var ioFonts = io.Fonts;

            var maxTexDimension = 1 << (10 + Math.Max(0, Math.Min(4, this.FontResolutionLevel)));
            var fontGamma = this.FontGamma;

            this.fontBuildSignal.Reset();
            ioFonts.Clear();
            ioFonts.TexDesiredWidth = maxTexDimension;

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
                    fontPathKr = null;
                Log.Verbose("[FONT] fontPathKr = {0}", fontPathKr);

                // Default font
                Log.Verbose("[FONT] SetupFonts - Default font");
                var fontInfo = new TargetFontModification(
                    "Default",
                    this.UseAxis ? TargetFontModification.AxisMode.Overwrite : TargetFontModification.AxisMode.GameGlyphsOnly,
                    this.UseAxis ? DefaultFontSizePx : DefaultFontSizePx + 1,
                    io.FontGlobalScale,
                    disableBigFonts);
                Log.Verbose("[FONT] SetupFonts - Default corresponding AXIS size: {0}pt ({1}px)", fontInfo.SourceAxis.Style.BaseSizePt, fontInfo.SourceAxis.Style.BaseSizePx);
                fontConfig.SizePixels = disableBigFonts ? Math.Min(MinimumFallbackFontSizePx, fontInfo.TargetSizePx) : fontInfo.TargetSizePx * io.FontGlobalScale;
                if (this.UseAxis)
                {
                    fontConfig.GlyphRanges = dummyRangeHandle.AddrOfPinnedObject();
                    fontConfig.PixelSnapH = false;
                    DefaultFont = ioFonts.AddFontDefault(fontConfig);
                    this.loadedFontInfo[DefaultFont] = fontInfo;
                }
                else
                {
                    var japaneseRangeHandle = GCHandle.Alloc(GlyphRangesJapanese.GlyphRanges, GCHandleType.Pinned);
                    garbageList.Add(japaneseRangeHandle);

                    fontConfig.GlyphRanges = japaneseRangeHandle.AddrOfPinnedObject();
                    fontConfig.PixelSnapH = true;
                    DefaultFont = ioFonts.AddFontFromFileTTF(fontPathJp, fontConfig.SizePixels, fontConfig);
                    this.loadedFontInfo[DefaultFont] = fontInfo;
                }

                if (fontPathKr != null && Service<DalamudConfiguration>.Get().EffectiveLanguage == "ko")
                {
                    fontConfig.MergeMode = true;
                    fontConfig.GlyphRanges = ioFonts.GetGlyphRangesKorean();
                    fontConfig.PixelSnapH = true;
                    ioFonts.AddFontFromFileTTF(fontPathKr, fontConfig.SizePixels, fontConfig);
                    fontConfig.MergeMode = false;
                }

                // FontAwesome icon font
                Log.Verbose("[FONT] SetupFonts - FontAwesome icon font");
                {
                    var fontPathIcon = Path.Combine(dalamud.AssetDirectory.FullName, "UIRes", "FontAwesome5FreeSolid.otf");
                    if (!File.Exists(fontPathIcon))
                        ShowFontError(fontPathIcon);

                    var iconRangeHandle = GCHandle.Alloc(new ushort[] { 0xE000, 0xF8FF, 0, }, GCHandleType.Pinned);
                    garbageList.Add(iconRangeHandle);

                    fontConfig.GlyphRanges = iconRangeHandle.AddrOfPinnedObject();
                    fontConfig.PixelSnapH = true;
                    IconFont = ioFonts.AddFontFromFileTTF(fontPathIcon, disableBigFonts ? Math.Min(MinimumFallbackFontSizePx, DefaultFontSizePx) : DefaultFontSizePx * io.FontGlobalScale, fontConfig);
                    this.loadedFontInfo[IconFont] = new("Icon", TargetFontModification.AxisMode.GameGlyphsOnly, DefaultFontSizePx, io.FontGlobalScale, disableBigFonts);
                }

                // Monospace font
                Log.Verbose("[FONT] SetupFonts - Monospace font");
                {
                    var fontPathMono = Path.Combine(dalamud.AssetDirectory.FullName, "UIRes", "Inconsolata-Regular.ttf");
                    if (!File.Exists(fontPathMono))
                        ShowFontError(fontPathMono);

                    fontConfig.GlyphRanges = IntPtr.Zero;
                    fontConfig.PixelSnapH = true;
                    MonoFont = ioFonts.AddFontFromFileTTF(fontPathMono, disableBigFonts ? Math.Min(MinimumFallbackFontSizePx, DefaultFontSizePx) : DefaultFontSizePx * io.FontGlobalScale, fontConfig);
                    this.loadedFontInfo[MonoFont] = new("Mono", TargetFontModification.AxisMode.GameGlyphsOnly, DefaultFontSizePx, io.FontGlobalScale, disableBigFonts);
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
                        List<Tuple<ushort, ushort>> codepointRanges = new();
                        codepointRanges.Add(Tuple.Create(Fallback1Codepoint, Fallback1Codepoint));
                        codepointRanges.Add(Tuple.Create(Fallback2Codepoint, Fallback2Codepoint));

                        // ImGui default ellipsis characters
                        codepointRanges.Add(Tuple.Create<ushort, ushort>(0x2026, 0x2026));
                        codepointRanges.Add(Tuple.Create<ushort, ushort>(0x0085, 0x0085));

                        foreach (var request in requests)
                        {
                            foreach (var range in request.CodepointRanges)
                                codepointRanges.Add(range);
                        }

                        codepointRanges.Sort((x, y) => (x.Item1 == y.Item1 ? (x.Item2 < y.Item2 ? -1 : (x.Item2 == y.Item2 ? 0 : 1)) : (x.Item1 < y.Item1 ? -1 : 1)));

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
                            io.FontGlobalScale,
                            disableBigFonts);
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

                            var sizedFont = ioFonts.AddFontFromFileTTF(fontPathJp, disableBigFonts ? Math.Min(MinimumFallbackFontSizePx, fontSize) : fontSize * io.FontGlobalScale, fontConfig, rangeHandle.AddrOfPinnedObject());
                            this.loadedFontInfo[sizedFont] = fontInfo;
                            foreach (var request in requests)
                                request.FontInternal = sizedFont;
                        }
                    }
                }

                gameFontManager.BuildFonts(disableBigFonts);

                var customFontFirstConfigIndex = ioFonts.ConfigData.Size;

                Log.Verbose("[FONT] Invoke OnBuildFonts");
                this.BuildFonts?.Invoke();
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

                    if (disableBigFonts)
                    {
                        // If a plugin has requested a font size that is bigger than current restrictions, load it scaled down.
                        // After loading glyphs onto font atlas, font information will be modified to make it look like the font of original size has been loaded.
                        if (config.SizePixels > MinimumFallbackFontSizePx)
                            config.SizePixels = MinimumFallbackFontSizePx;
                    }
                    else
                    {
                        config.SizePixels = config.SizePixels * io.FontGlobalScale;
                    }
                }

                Log.Verbose("[FONT] ImGui.IO.Build will be called.");
                ioFonts.Build();
                gameFontManager.AfterIoFontsBuild();
                Log.Verbose("[FONT] ImGui.IO.Build OK!");

                if (ioFonts.TexHeight > maxTexDimension)
                {
                    var possibilityForScaling = false;
                    foreach (var x in this.loadedFontInfo.Values)
                    {
                        if (x.TargetSizePx * x.Scale > MinimumFallbackFontSizePx)
                        {
                            possibilityForScaling = true;
                            break;
                        }
                    }

                    if (possibilityForScaling && !disableBigFonts)
                    {
                        Log.Information("[FONT] Atlas size is {0}x{1} which is bigger than allowed {2}x{3}. Retrying with minimized font sizes.", ioFonts.TexWidth, ioFonts.TexHeight, maxTexDimension, maxTexDimension);
                        this.IsFallbackFontMode = true;
                        this.SetupFonts(true);
                        return;
                    }
                    else
                    {
                        Log.Warning("[FONT] Atlas size is {0}x{1} which is bigger than allowed {2}x{3} even when font sizes are minimized up to {4}px. This may result in crash.", ioFonts.TexWidth, ioFonts.TexHeight, maxTexDimension, maxTexDimension, MinimumFallbackFontSizePx);
                    }
                }

                if (!disableBigFonts)
                    this.IsFallbackFontMode = false;

                if (Math.Abs(fontGamma - 1.0f) >= 0.001)
                {
                    // Gamma correction (stbtt/FreeType would output in linear space whereas most real world usages will apply 1.4 or 1.8 gamma; Windows/XIV prebaked uses 1.4)
                    ioFonts.GetTexDataAsRGBA32(out byte* texPixels, out var texWidth, out var texHeight);
                    for (int i = 3, i_ = texWidth * texHeight * 4; i < i_; i += 4)
                        texPixels[i] = (byte)(Math.Pow(texPixels[i] / 255.0f, 1.0f / fontGamma) * 255.0f);
                }

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
                    font.FallbackChar = Fallback1Codepoint;
                    font.BuildLookupTable();
                }

                Log.Verbose("[FONT] Invoke OnAfterBuildFonts");
                this.AfterBuildFonts?.Invoke();
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

        private void Disable()
        {
            this.setCursorHook?.Disable();
            this.presentHook.Disable();
            this.resizeBuffersHook.Disable();
        }

        // This is intended to only be called as a handler attached to scene.OnNewRenderFrame
        private void RebuildFontsInternal()
        {
            Log.Verbose("[FONT] RebuildFontsInternal() called");
            this.SetupFonts();

            Log.Verbose("[FONT] RebuildFontsInternal() detaching");
            this.scene.OnNewRenderFrame -= this.RebuildFontsInternal;

            Log.Verbose("[FONT] Calling InvalidateFonts");
            try
            {
                this.scene.InvalidateFonts();
            }
            catch (Exception ex)
            {
                if (this.FontResolutionLevel > 2)
                {
                    Log.Error(ex, "[FONT] Failed to create font textures; setting font resolution level to 2 and retrying");
                    this.FontResolutionLevelOverride = 2;
                    this.SetupFonts();
                }
                else
                {
                    Log.Error(ex, "[FONT] Failed to create font textures; forcing fallback font mode");
                    this.SetupFonts(true);
                }

                Log.Verbose("[FONT] Calling InvalidateFonts again");
                try
                {
                    this.scene.InvalidateFonts();
                }
                catch (Exception ex2)
                {
                    Log.Error(ex2, "[FONT] Giving up");
                }
            }

            Log.Verbose("[FONT] Font Rebuild OK!");

            this.isRebuildingFonts = false;
        }

        private IntPtr ResizeBuffersDetour(IntPtr swapChain, uint bufferCount, uint width, uint height, uint newFormat, uint swapChainFlags)
        {
#if DEBUG
            Log.Verbose($"Calling resizebuffers swap@{swapChain.ToInt64():X}{bufferCount} {width} {height} {newFormat} {swapChainFlags}");
#endif

            this.ResizeBuffers?.Invoke();

            // We have to ensure we're working with the main swapchain,
            // as viewports might be resizing as well
            if (this.scene == null || swapChain != this.scene.SwapChain.NativePointer)
                return this.resizeBuffersHook.Original(swapChain, bufferCount, width, height, newFormat, swapChainFlags);

            this.scene?.OnPreResize();

            var ret = this.resizeBuffersHook.Original(swapChain, bufferCount, width, height, newFormat, swapChainFlags);
            if (ret.ToInt64() == 0x887A0001)
            {
                Log.Error("invalid call to resizeBuffers");
            }

            this.scene?.OnPostResize((int)width, (int)height);

            return ret;
        }

        private IntPtr SetCursorDetour(IntPtr hCursor)
        {
            if (this.lastWantCapture == true && (!this.scene?.IsImGuiCursor(hCursor) ?? false) && this.OverrideGameCursor)
                return IntPtr.Zero;

            return this.setCursorHook.Original(hCursor);
        }

        private void OnNewInputFrame()
        {
            var dalamudInterface = Service<DalamudInterface>.GetNullable();
            var gamepadState = Service<GamepadState>.GetNullable();
            var keyState = Service<KeyState>.GetNullable();

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
                ImGui.GetIO().NavInputs[(int)ImGuiNavInput.Activate] = gamepadState.Raw(GamepadButtons.South);
                ImGui.GetIO().NavInputs[(int)ImGuiNavInput.Cancel] = gamepadState.Raw(GamepadButtons.East);
                ImGui.GetIO().NavInputs[(int)ImGuiNavInput.Input] = gamepadState.Raw(GamepadButtons.North);
                ImGui.GetIO().NavInputs[(int)ImGuiNavInput.Menu] = gamepadState.Raw(GamepadButtons.West);
                ImGui.GetIO().NavInputs[(int)ImGuiNavInput.DpadLeft] = gamepadState.Raw(GamepadButtons.DpadLeft);
                ImGui.GetIO().NavInputs[(int)ImGuiNavInput.DpadRight] = gamepadState.Raw(GamepadButtons.DpadRight);
                ImGui.GetIO().NavInputs[(int)ImGuiNavInput.DpadUp] = gamepadState.Raw(GamepadButtons.DpadUp);
                ImGui.GetIO().NavInputs[(int)ImGuiNavInput.DpadDown] = gamepadState.Raw(GamepadButtons.DpadDown);
                ImGui.GetIO().NavInputs[(int)ImGuiNavInput.LStickLeft] = gamepadState.LeftStickLeft;
                ImGui.GetIO().NavInputs[(int)ImGuiNavInput.LStickRight] = gamepadState.LeftStickRight;
                ImGui.GetIO().NavInputs[(int)ImGuiNavInput.LStickUp] = gamepadState.LeftStickUp;
                ImGui.GetIO().NavInputs[(int)ImGuiNavInput.LStickDown] = gamepadState.LeftStickDown;
                ImGui.GetIO().NavInputs[(int)ImGuiNavInput.FocusPrev] = gamepadState.Raw(GamepadButtons.L1);
                ImGui.GetIO().NavInputs[(int)ImGuiNavInput.FocusNext] = gamepadState.Raw(GamepadButtons.R1);
                ImGui.GetIO().NavInputs[(int)ImGuiNavInput.TweakSlow] = gamepadState.Raw(GamepadButtons.L2);
                ImGui.GetIO().NavInputs[(int)ImGuiNavInput.TweakFast] = gamepadState.Raw(GamepadButtons.R2);

                if (gamepadState.Pressed(GamepadButtons.R3) > 0)
                {
                    dalamudInterface.TogglePluginInstallerWindow();
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
            /// <param name="disableBigFonts">Whether to enable loading big AXIS fonts.</param>
            internal TargetFontModification(string name, AxisMode axis, float sizePx, float globalFontScale, bool disableBigFonts)
            {
                this.Name = name;
                this.Axis = axis;
                this.TargetSizePx = sizePx;
                this.Scale = disableBigFonts ? MinimumFallbackFontSizePx / sizePx : globalFontScale;
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
}
