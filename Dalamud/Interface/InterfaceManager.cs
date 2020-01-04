//#define RENDERDOC_HACKS

using System;
using System.Runtime.InteropServices;
using Dalamud.Game;
using Dalamud.Game.Internal.DXGI;
using Dalamud.Hooking;
using ImGuiNET;
using ImGuiScene;
using Serilog;

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
            Enable();
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
                this.scene.OnBuildUI += DrawUI;
            }

            this.scene.Render();

            return this.presentHook.Original(swapChain, syncInterval, presentFlags);
        }

        private void DrawUI()
        {
            ImGui.ShowDemoWindow();
        }
    }
}
