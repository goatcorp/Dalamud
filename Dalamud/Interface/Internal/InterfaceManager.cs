using System;
using System.ComponentModel;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.Internal.DXGI;
using Dalamud.Hooking;
using ImGuiNET;
using ImGuiScene;
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
        private readonly Dalamud dalamud;
        private readonly string rtssPath;

        private readonly Hook<PresentDelegate> presentHook;
        private readonly Hook<ResizeBuffersDelegate> resizeBuffersHook;
        private readonly Hook<SetCursorDelegate> setCursorHook;

        private readonly ManualResetEvent fontBuildSignal;
        private readonly ISwapChainAddressResolver address;
        private RawDX11Scene scene;

        // can't access imgui IO before first present call
        private bool lastWantCapture = false;
        private bool isRebuildingFonts = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="InterfaceManager"/> class.
        /// </summary>
        /// <param name="dalamud">The Dalamud instance.</param>
        /// <param name="scanner">The SigScanner instance.</param>
        public InterfaceManager(Dalamud dalamud, SigScanner scanner)
        {
            this.dalamud = dalamud;

            this.fontBuildSignal = new ManualResetEvent(false);

            try
            {
                var sigResolver = new SwapChainSigResolver();
                sigResolver.Setup(scanner);

                Log.Verbose("Found SwapChain via signatures.");

                this.address = sigResolver;
            }
            catch (Exception ex)
            {
                // The SigScanner method fails on wine/proton since DXGI is not a real DLL. We fall back to vtable to detect our Present function address.
                Log.Debug(ex, "Could not get SwapChain address via sig method, falling back to vtable...");

                var vtableResolver = new SwapChainVtableResolver();
                vtableResolver.Setup(scanner);

                Log.Verbose("Found SwapChain via vtable.");

                this.address = vtableResolver;
            }

            try
            {
                var rtss = NativeFunctions.GetModuleHandleW("RTSSHooks64.dll");

                if (rtss != IntPtr.Zero)
                {
                    var fileName = new StringBuilder(255);
                    _ = NativeFunctions.GetModuleFileNameW(rtss, fileName, fileName.Capacity);
                    this.rtssPath = fileName.ToString();
                    Log.Verbose("RTSS at {0}", this.rtssPath);

                    if (!NativeFunctions.FreeLibrary(rtss))
                        throw new Win32Exception();
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "RTSS Free failed");
            }

            var user32 = NativeFunctions.GetModuleHandleW("user32.dll");
            var setCursorAddr = NativeFunctions.GetProcAddress(user32, "SetCursor");

            Log.Verbose("===== S W A P C H A I N =====");
            Log.Verbose($"SetCursor address 0x{setCursorAddr.ToInt64():X}");
            Log.Verbose($"Present address 0x{this.address.Present.ToInt64():X}");
            Log.Verbose($"ResizeBuffers address 0x{this.address.ResizeBuffers.ToInt64():X}");

            this.setCursorHook = new Hook<SetCursorDelegate>(setCursorAddr, this.SetCursorDetour);
            this.presentHook = new Hook<PresentDelegate>(this.address.Present, this.PresentDetour);
            this.resizeBuffersHook = new Hook<ResizeBuffersDelegate>(this.address.ResizeBuffers, this.ResizeBuffersDetour);
        }

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate IntPtr PresentDelegate(IntPtr swapChain, uint syncInterval, uint presentFlags);

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate IntPtr ResizeBuffersDelegate(IntPtr swapChain, uint bufferCount, uint width, uint height, uint newFormat, uint swapChainFlags);

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate IntPtr SetCursorDelegate(IntPtr hCursor);

        private delegate void InstallRTSSHook();

        /// <summary>
        /// This event gets called by a plugin UiBuilder when read
        /// </summary>
        public event RawDX11Scene.BuildUIDelegate OnDraw;

        /// <summary>
        /// Gets the default ImGui font.
        /// </summary>
        public static ImFontPtr DefaultFont { get; private set; }

        /// <summary>
        /// Gets an included FontAwesome icon font.
        /// </summary>
        public static ImFontPtr IconFont { get; private set; }

        /// <summary>
        /// Gets or sets an action that is exexuted when fonts are rebuilt.
        /// </summary>
        public Action OnBuildFonts { get; set; }

        /// <summary>
        /// Gets or sets the pointer to ImGui.IO(), when it was last used.
        /// </summary>
        public ImGuiIOPtr LastImGuiIoPtr { get; set; }

        /// <summary>
        /// Gets the D3D11 device instance.
        /// </summary>
        public Device Device => this.scene.Device;

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
        /// Enable this module.
        /// </summary>
        public void Enable()
        {
            this.setCursorHook.Enable();
            this.presentHook.Enable();
            this.resizeBuffersHook.Enable();

            try
            {
                if (!string.IsNullOrEmpty(this.rtssPath))
                {
                    NativeFunctions.LoadLibraryW(this.rtssPath);
                    var rtssModule = NativeFunctions.GetModuleHandleW("RTSSHooks64.dll");
                    var installAddr = NativeFunctions.GetProcAddress(rtssModule, "InstallRTSSHook");

                    Marshal.GetDelegateForFunctionPointer<InstallRTSSHook>(installAddr).Invoke();
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
            this.setCursorHook.Dispose();
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

        private static void ShowFontError(string path)
        {
            Util.Fatal(
                $"One or more files required by XIVLauncher were not found.\nPlease restart and report this error if it occurs again.\n\n{path}",
                "Error");
        }

        private IntPtr PresentDetour(IntPtr swapChain, uint syncInterval, uint presentFlags)
        {
            if (this.scene == null)
            {
                this.scene = new RawDX11Scene(swapChain);

                this.scene.ImGuiIniPath = Path.Combine(Path.GetDirectoryName(this.dalamud.StartInfo.ConfigurationPath), "dalamudUI.ini");
                this.scene.OnBuildUI += this.Display;
                this.scene.OnNewInputFrame += this.OnNewInputFrame;

                this.SetupFonts();

                ImGui.GetStyle().GrabRounding = 3f;
                ImGui.GetStyle().FrameRounding = 4f;
                ImGui.GetStyle().WindowRounding = 4f;
                ImGui.GetStyle().WindowBorderSize = 0f;
                ImGui.GetStyle().WindowMenuButtonPosition = ImGuiDir.Right;
                ImGui.GetStyle().ScrollbarSize = 16f;

                ImGui.GetStyle().Colors[(int)ImGuiCol.WindowBg] = new Vector4(0.06f, 0.06f, 0.06f, 0.87f);
                ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBg] = new Vector4(0.29f, 0.29f, 0.29f, 0.54f);
                ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBgHovered] = new Vector4(0.54f, 0.54f, 0.54f, 0.40f);
                ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBgActive] = new Vector4(0.64f, 0.64f, 0.64f, 0.67f);
                ImGui.GetStyle().Colors[(int)ImGuiCol.TitleBgActive] = new Vector4(0.29f, 0.29f, 0.29f, 1.00f);
                ImGui.GetStyle().Colors[(int)ImGuiCol.CheckMark] = new Vector4(0.86f, 0.86f, 0.86f, 1.00f);
                ImGui.GetStyle().Colors[(int)ImGuiCol.SliderGrab] = new Vector4(0.54f, 0.54f, 0.54f, 1.00f);
                ImGui.GetStyle().Colors[(int)ImGuiCol.SliderGrabActive] = new Vector4(0.67f, 0.67f, 0.67f, 1.00f);
                ImGui.GetStyle().Colors[(int)ImGuiCol.Button] = new Vector4(0.71f, 0.71f, 0.71f, 0.40f);
                ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonHovered] = new Vector4(0.47f, 0.47f, 0.47f, 1.00f);
                ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonActive] = new Vector4(0.74f, 0.74f, 0.74f, 1.00f);
                ImGui.GetStyle().Colors[(int)ImGuiCol.Header] = new Vector4(0.59f, 0.59f, 0.59f, 0.31f);
                ImGui.GetStyle().Colors[(int)ImGuiCol.HeaderHovered] = new Vector4(0.50f, 0.50f, 0.50f, 0.80f);
                ImGui.GetStyle().Colors[(int)ImGuiCol.HeaderActive] = new Vector4(0.60f, 0.60f, 0.60f, 1.00f);
                ImGui.GetStyle().Colors[(int)ImGuiCol.ResizeGrip] = new Vector4(0.79f, 0.79f, 0.79f, 0.25f);
                ImGui.GetStyle().Colors[(int)ImGuiCol.ResizeGripHovered] = new Vector4(0.78f, 0.78f, 0.78f, 0.67f);
                ImGui.GetStyle().Colors[(int)ImGuiCol.ResizeGripActive] = new Vector4(0.88f, 0.88f, 0.88f, 0.95f);
                ImGui.GetStyle().Colors[(int)ImGuiCol.Tab] = new Vector4(0.23f, 0.23f, 0.23f, 0.86f);
                ImGui.GetStyle().Colors[(int)ImGuiCol.TabHovered] = new Vector4(0.71f, 0.71f, 0.71f, 0.80f);
                ImGui.GetStyle().Colors[(int)ImGuiCol.TabActive] = new Vector4(0.36f, 0.36f, 0.36f, 1.00f);

                ImGui.GetIO().FontGlobalScale = this.dalamud.Configuration.GlobalUiScale;

                if (!this.dalamud.Configuration.IsDocking)
                {
                    ImGui.GetIO().ConfigFlags &= ~ImGuiConfigFlags.DockingEnable;
                }
                else
                {
                    ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DockingEnable;
                }

                // NOTE (Chiv) Toggle gamepad navigation via setting
                if (!this.dalamud.Configuration.IsGamepadNavigationEnabled)
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

            // Process information needed by ImGuiHelpers each frame.
            ImGuiHelpers.NewFrame();

            // Check if we can still enable viewports without any issues.
            this.CheckViewportState();

            this.scene.Render();

            return this.presentHook.Original(swapChain, syncInterval, presentFlags);
        }

        private void CheckViewportState()
        {
            if (this.dalamud.Configuration.IsDisableViewport || this.scene.SwapChain.IsFullScreen || ImGui.GetPlatformIO().Monitors.Size == 1)
            {
                ImGui.GetIO().ConfigFlags &= ~ImGuiConfigFlags.ViewportsEnable;
                return;
            }

            ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;
        }

        private unsafe void SetupFonts()
        {
            this.fontBuildSignal.Reset();

            ImGui.GetIO().Fonts.Clear();

            ImFontConfigPtr fontConfig = ImGuiNative.ImFontConfig_ImFontConfig();
            fontConfig.MergeMode = true;
            fontConfig.PixelSnapH = true;

            var fontPathJp = Path.Combine(this.dalamud.AssetDirectory.FullName, "UIRes", "NotoSansCJKjp-Medium.otf");

            if (!File.Exists(fontPathJp))
                ShowFontError(fontPathJp);

            var japaneseRangeHandle = GCHandle.Alloc(GlyphRangesJapanese.GlyphRanges, GCHandleType.Pinned);

            DefaultFont = ImGui.GetIO().Fonts.AddFontFromFileTTF(fontPathJp, 17.0f, null, japaneseRangeHandle.AddrOfPinnedObject());

            var fontPathGame = Path.Combine(this.dalamud.AssetDirectory.FullName, "UIRes", "gamesym.ttf");

            if (!File.Exists(fontPathGame))
                ShowFontError(fontPathGame);

            var gameRangeHandle = GCHandle.Alloc(
                new ushort[]
                {
                    0xE020,
                    0xE0DB,
                    0,
                },
                GCHandleType.Pinned);

            ImGui.GetIO().Fonts.AddFontFromFileTTF(fontPathGame, 17.0f, fontConfig, gameRangeHandle.AddrOfPinnedObject());

            var fontPathIcon = Path.Combine(this.dalamud.AssetDirectory.FullName, "UIRes", "FontAwesome5FreeSolid.otf");

            if (!File.Exists(fontPathIcon))
                ShowFontError(fontPathIcon);

            var iconRangeHandle = GCHandle.Alloc(
                new ushort[]
                {
                    0xE000,
                    0xF8FF,
                    0,
                },
                GCHandleType.Pinned);
            IconFont = ImGui.GetIO().Fonts.AddFontFromFileTTF(fontPathIcon, 17.0f, null, iconRangeHandle.AddrOfPinnedObject());

            Log.Verbose("[FONT] Invoke OnBuildFonts");
            this.OnBuildFonts?.Invoke();
            Log.Verbose("[FONT] OnBuildFonts OK!");

            for (var i = 0; i < ImGui.GetIO().Fonts.Fonts.Size; i++)
            {
                Log.Verbose("{0} - {1}", i, ImGui.GetIO().Fonts.Fonts[i].GetDebugName());
            }

            ImGui.GetIO().Fonts.Build();

            Log.Verbose("[FONT] Fonts built!");

            this.fontBuildSignal.Set();

            fontConfig.Destroy();
            japaneseRangeHandle.Free();
            gameRangeHandle.Free();
            iconRangeHandle.Free();

            this.FontsReady = true;
        }

        private void Disable()
        {
            this.setCursorHook.Disable();
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
            this.scene.InvalidateFonts();

            Log.Verbose("[FONT] Font Rebuild OK!");

            this.isRebuildingFonts = false;
        }

        private IntPtr ResizeBuffersDetour(IntPtr swapChain, uint bufferCount, uint width, uint height, uint newFormat, uint swapChainFlags)
        {
#if DEBUG
            Log.Verbose($"Calling resizebuffers swap@{swapChain.ToInt64():X}{bufferCount} {width} {height} {newFormat} {swapChainFlags}");
#endif

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
            // fix for keys in game getting stuck, if you were holding a game key (like run)
            // and then clicked on an imgui textbox - imgui would swallow the keyup event,
            // so the game would think the key remained pressed continuously until you left
            // imgui and pressed and released the key again
            if (ImGui.GetIO().WantTextInput)
            {
                this.dalamud.ClientState.KeyState.ClearAll();
            }

            // TODO: mouse state?

            var gamepadEnabled = (ImGui.GetIO().BackendFlags & ImGuiBackendFlags.HasGamepad) > 0;

            // NOTE (Chiv) Activate ImGui navigation  via L1+L3 press
            // (mimicking how mouse navigation is activated via L1+R3 press in game).
            if (gamepadEnabled
                && this.dalamud.ClientState.GamepadState.Raw(GamepadButtons.L1) > 0
                && this.dalamud.ClientState.GamepadState.Pressed(GamepadButtons.L3) > 0)
            {
                ImGui.GetIO().ConfigFlags ^= ImGuiConfigFlags.NavEnableGamepad;
                this.dalamud.ClientState.GamepadState.NavEnableGamepad ^= true;
                this.dalamud.DalamudUi.ToggleGamepadModeNotifierWindow();
            }

            if (gamepadEnabled
                && (ImGui.GetIO().ConfigFlags & ImGuiConfigFlags.NavEnableGamepad) > 0)
            {
                ImGui.GetIO().NavInputs[(int)ImGuiNavInput.Activate] = this.dalamud.ClientState.GamepadState.Raw(GamepadButtons.South);
                ImGui.GetIO().NavInputs[(int)ImGuiNavInput.Cancel] = this.dalamud.ClientState.GamepadState.Raw(GamepadButtons.East);
                ImGui.GetIO().NavInputs[(int)ImGuiNavInput.Input] = this.dalamud.ClientState.GamepadState.Raw(GamepadButtons.North);
                ImGui.GetIO().NavInputs[(int)ImGuiNavInput.Menu] = this.dalamud.ClientState.GamepadState.Raw(GamepadButtons.West);
                ImGui.GetIO().NavInputs[(int)ImGuiNavInput.DpadLeft] = this.dalamud.ClientState.GamepadState.Raw(GamepadButtons.DpadLeft);
                ImGui.GetIO().NavInputs[(int)ImGuiNavInput.DpadRight] = this.dalamud.ClientState.GamepadState.Raw(GamepadButtons.DpadRight);
                ImGui.GetIO().NavInputs[(int)ImGuiNavInput.DpadUp] = this.dalamud.ClientState.GamepadState.Raw(GamepadButtons.DpadUp);
                ImGui.GetIO().NavInputs[(int)ImGuiNavInput.DpadDown] = this.dalamud.ClientState.GamepadState.Raw(GamepadButtons.DpadDown);
                ImGui.GetIO().NavInputs[(int)ImGuiNavInput.LStickLeft] = this.dalamud.ClientState.GamepadState.LeftStickLeft;
                ImGui.GetIO().NavInputs[(int)ImGuiNavInput.LStickRight] = this.dalamud.ClientState.GamepadState.LeftStickRight;
                ImGui.GetIO().NavInputs[(int)ImGuiNavInput.LStickUp] = this.dalamud.ClientState.GamepadState.LeftStickUp;
                ImGui.GetIO().NavInputs[(int)ImGuiNavInput.LStickDown] = this.dalamud.ClientState.GamepadState.LeftStickDown;
                ImGui.GetIO().NavInputs[(int)ImGuiNavInput.FocusPrev] = this.dalamud.ClientState.GamepadState.Raw(GamepadButtons.L1);
                ImGui.GetIO().NavInputs[(int)ImGuiNavInput.FocusNext] = this.dalamud.ClientState.GamepadState.Raw(GamepadButtons.R1);
                ImGui.GetIO().NavInputs[(int)ImGuiNavInput.TweakSlow] = this.dalamud.ClientState.GamepadState.Raw(GamepadButtons.L2);
                ImGui.GetIO().NavInputs[(int)ImGuiNavInput.TweakFast] = this.dalamud.ClientState.GamepadState.Raw(GamepadButtons.R2);

                if (this.dalamud.ClientState.GamepadState.Pressed(GamepadButtons.R3) > 0)
                {
                    this.dalamud.DalamudUi.TogglePluginInstallerWindow();
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

            this.OnDraw?.Invoke();
        }
    }
}
