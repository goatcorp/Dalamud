//#define RENDERDOC_HACKS

using System;
using System.Runtime.InteropServices;
using Dalamud.Game;
using Dalamud.Game.Internal.DXGI;
using Dalamud.Hooking;
using ImGuiNET;
using ImGuiScene;
using Serilog;

// general dev notes, here because it's easiest
/*
 * - Hooking ResizeBuffers seemed to be unnecessary, though I'm not sure why.  Left out for now since it seems to work without it.
 * - It's probably virtually impossible to remove the present hook once we set it, which again may lead to crashes in various situations.
 * - We may want to build our ImGui command list in a thread to keep it divorced from present.  We'd still have to block in present to
 *   synchronize on the list and render it, but ideally the overall delay we add to present would then be shorter.  This may cause minor
 *   timing issues with anything animated inside ImGui, but that is probably rare and may not even be noticeable.
 * - Our hook is too low level to really work well with debugging, as we only have access to the 'real' dx objects and not any
 *   that have been hooked/wrapped by tools.
 * - ^ May actually mean that we bypass things like reshade through sheer luck... but that may also mean that we'll have to do extra
 *   work to play nicely with them.
 * - Might need to render to a separate target and composite, especially with reshade etc in the mix.
 */

namespace Dalamud.Interface
{
    public class InterfaceManager : IDisposable
    {
#if RENDERDOC_HACKS
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("RDocHelper.dll")]
        static extern IntPtr GetWrappedDevice(IntPtr window);

        [DllImport("RDocHelper.dll")]
        static extern void StartCapture(IntPtr device, IntPtr window);

        [DllImport("RDocHelper.dll")]
        static extern uint EndCapture();
#endif

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate IntPtr PresentDelegate(IntPtr swapChain, uint syncInterval, uint presentFlags);

        private readonly Hook<PresentDelegate> presentHook;

        private SwapChainAddressResolver Address { get; }

        private RawDX11Scene scene;

        /// <summary>
        /// This event gets called when ImGUI is ready to draw your UI.
        /// </summary>
        public event RawDX11Scene.BuildUIDelegate OnBuildUi
        {
            add => this.scene.OnBuildUI += value;
            remove => this.scene.OnBuildUI -= value;
        }

        public EventHandler ReadyToDraw;

        public InterfaceManager(SigScanner scanner)
        {
            Address = new SwapChainAddressResolver();
            Address.Setup(scanner);

            Log.Verbose("===== S W A P C H A I N =====");
            Log.Verbose("Present address {Present}", Address.Present);

            this.presentHook =
                new Hook<PresentDelegate>(Address.Present,
                                                    new PresentDelegate(PresentDetour),
                                                    this);
        }

        public void Enable()
        {
            this.presentHook.Enable();
        }

        public void Disable()
        {
            this.presentHook.Disable();
        }

        public void Dispose()
        {
            this.scene.Dispose();
            // this will almost certainly crash or otherwise break
            // we might be able to mitigate it by properly cleaning up in the detour first
            // and essentially blocking until that completes... but I'm skeptical that would work either
            this.presentHook.Dispose();
        }

        private IntPtr PresentDetour(IntPtr swapChain, uint syncInterval, uint presentFlags)
        {
            if (this.scene == null)
            {
#if RENDERDOC_HACKS
                var hWnd = FindWindow(null, "FINAL FANTASY XIV");
                var device = GetWrappedDevice(hWnd);
                this.scene = new RawDX11Scene(device, swapChain);
#else
                this.scene = new RawDX11Scene(swapChain);
#endif
                this.scene.OnBuildUI += HandleMouseUI;
                this.ReadyToDraw?.Invoke(this, null);
            }

            this.scene.Render();

            return this.presentHook.Original(swapChain, syncInterval, presentFlags);
        }

        private void HandleMouseUI()
        {
            // this is more or less part of what reshade/etc do to avoid having to manually
            // set the cursor inside the ui
            // This effectively means that when the ui is hovered, there will be 2 cursors -
            // the normal one from the game, and the one for ImGui
            // Doing this here because it's somewhat application-specific behavior
            ImGui.GetIO().MouseDrawCursor = ImGui.GetIO().WantCaptureMouse;
        }
    }
}
