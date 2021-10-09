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
using Dalamud.Hooking.Internal;
using Dalamud.Interface.Internal.ManagedAsserts;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Interface.Internal.Windows.StyleEditor;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
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
        private readonly string rtssPath;

        private readonly Hook<PresentDelegate> presentHook;
        private readonly Hook<ResizeBuffersDelegate> resizeBuffersHook;
        private readonly Hook<SetCursorDelegate> setCursorHook;

        private readonly ManualResetEvent fontBuildSignal;
        private readonly SwapChainVtableResolver address;
        private RawDX11Scene? scene;

        // can't access imgui IO before first present call
        private bool lastWantCapture = false;
        private bool isRebuildingFonts = false;

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

            this.setCursorHook = HookManager.DirtyLinuxUser ? null
                : Hook<SetCursorDelegate>.FromSymbol("user32.dll", "SetCursor", this.SetCursorDetour);
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
        /// Gets or sets an action that is executed when fonts are rebuilt.
        /// </summary>
        public event Action BuildFonts;

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
                try
                {
                    this.scene = new RawDX11Scene(swapChain);
                }
                catch (DllNotFoundException)
                {
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

                this.scene.ImGuiIniPath = Path.Combine(Path.GetDirectoryName(startInfo.ConfigurationPath), "dalamudUI.ini");
                this.scene.OnBuildUI += this.Display;
                this.scene.OnNewInputFrame += this.OnNewInputFrame;

                this.SetupFonts();

                if (configuration.SavedStyles == null || configuration.SavedStyles.All(x => x.Name != StyleModel.DalamudStandard.Name))
                {
                    configuration.SavedStyles = new List<StyleModel> { StyleModel.DalamudStandard, StyleModel.DalamudClassic };
                    configuration.ChosenStyle = StyleModel.DalamudStandard.Name;
                }
                else if (configuration.SavedStyles.Count == 1)
                {
                    configuration.SavedStyles.Add(StyleModel.DalamudClassic);
                }
                else if (configuration.SavedStyles[1].Name != StyleModel.DalamudClassic.Name)
                {
                    configuration.SavedStyles.Insert(1, StyleModel.DalamudClassic);
                }

                configuration.SavedStyles[0] = StyleModel.DalamudStandard;
                configuration.SavedStyles[1] = StyleModel.DalamudClassic;

                var style = configuration.SavedStyles.FirstOrDefault(x => x.Name == configuration.ChosenStyle);
                if (style == null)
                {
                    style = StyleModel.DalamudStandard;
                    configuration.ChosenStyle = style.Name;
                    configuration.Save();
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

                Service<DalamudIME>.Get().Enable();
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

        private unsafe void SetupFonts()
        {
            var dalamud = Service<Dalamud>.Get();

            this.fontBuildSignal.Reset();

            ImGui.GetIO().Fonts.Clear();

            ImFontConfigPtr fontConfig = ImGuiNative.ImFontConfig_ImFontConfig();
            fontConfig.MergeMode = true;
            fontConfig.PixelSnapH = true;

            var fontPathJp = Path.Combine(dalamud.AssetDirectory.FullName, "UIRes", "NotoSansCJKjp-Medium.otf");

            if (!File.Exists(fontPathJp))
                ShowFontError(fontPathJp);

            var japaneseRangeHandle = GCHandle.Alloc(GlyphRangesJapanese.GlyphRanges, GCHandleType.Pinned);

            DefaultFont = ImGui.GetIO().Fonts.AddFontFromFileTTF(fontPathJp, 17.0f, null, japaneseRangeHandle.AddrOfPinnedObject());

            var fontPathGame = Path.Combine(dalamud.AssetDirectory.FullName, "UIRes", "gamesym.ttf");

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

            var fontPathIcon = Path.Combine(dalamud.AssetDirectory.FullName, "UIRes", "FontAwesome5FreeSolid.otf");

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

            var fontPathMono = Path.Combine(dalamud.AssetDirectory.FullName, "UIRes", "Inconsolata-Regular.ttf");

            if (!File.Exists(fontPathMono))
                ShowFontError(fontPathMono);

            MonoFont = ImGui.GetIO().Fonts.AddFontFromFileTTF(fontPathMono, 16.0f);

            Log.Verbose("[FONT] Invoke OnBuildFonts");
            this.BuildFonts?.Invoke();
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
            this.scene.InvalidateFonts();

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
            this.Draw?.Invoke();
            ImGuiManagedAsserts.ReportProblems("Dalamud Core", snap);

            Service<NotificationManager>.Get().Draw();
        }
    }
}
