using System;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Game;
using Dalamud.Game.Internal.DXGI;
using Dalamud.Hooking;
using EasyHook;
using ImGuiNET;
using ImGuiScene;
using Serilog;

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

namespace Dalamud.Interface
{
    public class InterfaceManager : IDisposable
    {
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate IntPtr PresentDelegate(IntPtr swapChain, uint syncInterval, uint presentFlags);

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate IntPtr ResizeBuffersDelegate(IntPtr swapChain, uint bufferCount, uint width, uint height, uint newFormat, uint swapChainFlags);

        private readonly Hook<PresentDelegate> presentHook;
        private readonly Hook<ResizeBuffersDelegate> resizeBuffersHook;

        private readonly Hook<SetCursorDelegate> setCursorHook;

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate IntPtr SetCursorDelegate(IntPtr hCursor);

        private ISwapChainAddressResolver Address { get; }

        private Dalamud dalamud;
        private RawDX11Scene scene;

        /// <summary>
        /// This event gets called by a plugin UiBuilder when read
        /// </summary>
        public event RawDX11Scene.BuildUIDelegate OnDraw;

        public InterfaceManager(Dalamud dalamud, SigScanner scanner)
        {
            this.dalamud = dalamud;

            try {
                var sigResolver = new SwapChainSigResolver();
                sigResolver.Setup(scanner);

                Log.Verbose("Found SwapChain via signatures.");

                Address = sigResolver;
            } catch (Exception ex) {
                // The SigScanner method fails on wine/proton since DXGI is not a real DLL. We fall back to vtable to detect our Present function address.
                Log.Error(ex, "Could not get SwapChain address via sig method, falling back to vtable...");

                var vtableResolver = new SwapChainVtableResolver();
                vtableResolver.Setup(scanner);

                Log.Verbose("Found SwapChain via vtable.");

                Address = vtableResolver;
            }

            var setCursorAddr = LocalHook.GetProcAddress("user32.dll", "SetCursor");

            Log.Verbose("===== S W A P C H A I N =====");
            Log.Verbose("SetCursor address {SetCursor}", setCursorAddr);
            Log.Verbose("Present address {Present}", Address.Present);
            Log.Verbose("ResizeBuffers address {ResizeBuffers}", Address.ResizeBuffers);

            this.setCursorHook = new Hook<SetCursorDelegate>(setCursorAddr, new SetCursorDelegate(SetCursorDetour), this);

            this.presentHook =
                new Hook<PresentDelegate>(Address.Present, 
                    new PresentDelegate(PresentDetour),
                    this);

            this.resizeBuffersHook =
                new Hook<ResizeBuffersDelegate>(Address.ResizeBuffers,
                    new ResizeBuffersDelegate(ResizeBuffersDetour),
                    this);
        }

        public void Enable()
        {
            this.setCursorHook.Enable();
            this.presentHook.Enable();
            this.resizeBuffersHook.Enable();
        }

        private void Disable()
        {
            this.setCursorHook.Disable();
            this.presentHook.Disable();
            this.resizeBuffersHook.Disable();
        }

        public void Dispose()
        {
            // HACK: this is usually called on a separate thread from PresentDetour (likely on a dedicated render thread)
            // and if we aren't already disabled, disposing of the scene and hook can frequently crash due to the hook
            // being disposed of in this thread while it is actively in use in the render thread.
            // This is a terrible way to prevent issues, but should basically always work to ensure that all outstanding
            // calls to PresentDetour have finished (and Disable means no new ones will start), before we try to cleanup
            // So... not great, but much better than constantly crashing on unload
            this.Disable();
            System.Threading.Thread.Sleep(100);

            this.scene?.Dispose();
            this.presentHook.Dispose();
            this.resizeBuffersHook.Dispose();
        }

        public TextureWrap LoadImage(string filePath)
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

        public TextureWrap LoadImage(byte[] imageData)
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

        private IntPtr PresentDetour(IntPtr swapChain, uint syncInterval, uint presentFlags)
        {
            if (this.scene == null)
            {
                this.scene = new RawDX11Scene(swapChain);
                this.scene.ImGuiIniPath = Path.Combine(Path.GetDirectoryName(this.dalamud.StartInfo.ConfigurationPath), "dalamudUI.ini");
                this.scene.OnBuildUI += Display;

                var fontPathJp = Path.Combine(Path.GetDirectoryName(typeof(InterfaceManager).Assembly.Location), "UIRes", "NotoSansCJKjp-Medium.otf");
                ImGui.GetIO().Fonts.AddFontFromFileTTF(fontPathJp, 17.0f, null, ImGui.GetIO().Fonts.GetGlyphRangesJapanese());

                ImGui.GetIO().Fonts.Build();

                ImGui.GetStyle().GrabRounding = 3f;
                ImGui.GetStyle().FrameRounding = 4f;
                ImGui.GetStyle().WindowRounding = 4f;
                ImGui.GetStyle().WindowBorderSize = 0f;
                ImGui.GetStyle().WindowMenuButtonPosition = ImGuiDir.Right;
                ImGui.GetStyle().ScrollbarSize = 16f;

                ImGui.GetStyle().Colors[(int) ImGuiCol.WindowBg] = new Vector4(0.06f, 0.06f, 0.06f, 0.87f);
                ImGui.GetStyle().Colors[(int) ImGuiCol.FrameBg] = new Vector4(0.29f, 0.29f, 0.29f, 0.54f);
                ImGui.GetStyle().Colors[(int) ImGuiCol.FrameBgHovered] = new Vector4(0.54f, 0.54f, 0.54f, 0.40f);
                ImGui.GetStyle().Colors[(int) ImGuiCol.FrameBgActive] = new Vector4(0.64f, 0.64f, 0.64f, 0.67f);
                ImGui.GetStyle().Colors[(int) ImGuiCol.TitleBgActive] = new Vector4(0.29f, 0.29f, 0.29f, 1.00f);
                ImGui.GetStyle().Colors[(int) ImGuiCol.CheckMark] = new Vector4(0.86f, 0.86f, 0.86f, 1.00f);
                ImGui.GetStyle().Colors[(int) ImGuiCol.SliderGrab] = new Vector4(0.54f, 0.54f, 0.54f, 1.00f);
                ImGui.GetStyle().Colors[(int) ImGuiCol.SliderGrabActive] = new Vector4(0.67f, 0.67f, 0.67f, 1.00f);
                ImGui.GetStyle().Colors[(int) ImGuiCol.Button] = new Vector4(0.71f, 0.71f, 0.71f, 0.40f);
                ImGui.GetStyle().Colors[(int) ImGuiCol.ButtonHovered] = new Vector4(0.47f, 0.47f, 0.47f, 1.00f);
                ImGui.GetStyle().Colors[(int) ImGuiCol.ButtonActive] = new Vector4(0.74f, 0.74f, 0.74f, 1.00f);
                ImGui.GetStyle().Colors[(int) ImGuiCol.Header] = new Vector4(0.59f, 0.59f, 0.59f, 0.31f);
                ImGui.GetStyle().Colors[(int) ImGuiCol.HeaderHovered] = new Vector4(0.50f, 0.50f, 0.50f, 0.80f);
                ImGui.GetStyle().Colors[(int) ImGuiCol.HeaderActive] = new Vector4(0.60f, 0.60f, 0.60f, 1.00f);
                ImGui.GetStyle().Colors[(int) ImGuiCol.ResizeGrip] = new Vector4(0.79f, 0.79f, 0.79f, 0.25f);
                ImGui.GetStyle().Colors[(int) ImGuiCol.ResizeGripHovered] = new Vector4(0.78f, 0.78f, 0.78f, 0.67f);
                ImGui.GetStyle().Colors[(int) ImGuiCol.ResizeGripActive] = new Vector4(0.88f, 0.88f, 0.88f, 0.95f);
                ImGui.GetStyle().Colors[(int) ImGuiCol.Tab] = new Vector4(0.23f, 0.23f, 0.23f, 0.86f);
                ImGui.GetStyle().Colors[(int) ImGuiCol.TabHovered] = new Vector4(0.71f, 0.71f, 0.71f, 0.80f);
                ImGui.GetStyle().Colors[(int) ImGuiCol.TabActive] = new Vector4(0.36f, 0.36f, 0.36f, 1.00f);
            }

            this.scene.Render();

            return this.presentHook.Original(swapChain, syncInterval, presentFlags);
        }

        private IntPtr ResizeBuffersDetour(IntPtr swapChain, uint bufferCount, uint width, uint height, uint newFormat, uint swapChainFlags)
        {
            Log.Verbose($"Calling resizebuffers {bufferCount} {width} {height} {newFormat} {swapChainFlags}");

            this.scene?.OnPreResize();

            var ret = this.resizeBuffersHook.Original(swapChain, bufferCount, width, height, newFormat, swapChainFlags);
            if (ret.ToInt64() == 0x887A0001)
            {
                Log.Error("invalid call to resizeBuffers");
            }

            this.scene?.OnPostResize((int)width, (int)height);

            return ret;
        }

        // can't access imgui IO before first present call
        private bool lastWantCapture = false;

        private IntPtr SetCursorDetour(IntPtr hCursor) {
            if (this.lastWantCapture == true && (!scene?.IsImGuiCursor(hCursor) ?? false))
                return IntPtr.Zero;

            return this.setCursorHook.Original(hCursor);
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
            //ImGui.GetIO().MouseDrawCursor = ImGui.GetIO().WantCaptureMouse;
            this.lastWantCapture = ImGui.GetIO().WantCaptureMouse;

            OnDraw?.Invoke();
        }
    }
}
