using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
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
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate IntPtr PresentDelegate(IntPtr swapChain, uint syncInterval, uint presentFlags);

        private readonly Hook<PresentDelegate> presentHook;

        private SwapChainAddressResolver Address { get; }


        //private Task _task;
        private RawDX11Scene _scene;
        private Dalamud _dalamud;

        public InterfaceManager(Dalamud dalamud, SigScanner scanner)
        {
            this._dalamud = dalamud;

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
            this._scene.Dispose();
            this.presentHook.Dispose();

            //_task?.Wait();
            //_task = null;
        }

        //public void Start()
        //{
        //    if (_task == null || _task.IsCompleted || _task.IsFaulted || _task.IsCanceled)
        //    {
        //        _task = new Task(Display);
        //        _task.Start();
        //    }
        //}

        //private void Display()
        //{
        //    using (var scene = SimpleImGuiScene.CreateOverlay(RendererFactory.RendererBackend.DirectX11))
        //    {
        //        // this basically pauses background rendering to reduce cpu load by the scene when it isn't actively in focus
        //        // the impact is generally pretty minor, but it's probably best to enable when we can
        //        // If we have any windows that we want to update dynamically even when the game is the focus
        //        // and not the overlay, this should be disabled.
        //        // It is dynamic, so we could disable it only when dynamic windows are open etc
        //        scene.PauseWhenUnfocused = true;

        //        scene.OnBuildUI += DrawUI;
        //        scene.Run();
        //    }
        //}

        //private void DrawUI()
        //{
        //    ImGui.ShowDemoWindow();
        //}

        private IntPtr PresentDetour(IntPtr swapChain, uint syncInterval, uint presentFlags)
        {
            if (_scene == null)
            {
                _scene = new RawDX11Scene(swapChain);
            }

            _scene.Render();

            return this.presentHook.Original(swapChain, syncInterval, presentFlags);
        }
    }
}
