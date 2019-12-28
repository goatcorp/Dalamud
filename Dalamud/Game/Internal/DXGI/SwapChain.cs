using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Dalamud.Hooking;
using ImGuiNET;
using Serilog;
using SharpDX.Direct3D11;

namespace Dalamud.Game.Internal.DXGI {
    public sealed class SwapChain : IDisposable {
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate IntPtr PresentDelegate(IntPtr swapChain, uint a2, uint a3);

        private readonly Hook<PresentDelegate> presentHook;

        private SwapChainAddressResolver Address { get; }

        private readonly Dalamud dalamud;

        private const string deviceguid = "3d3e0379-f9de-4d58-bb6c-18d62992f1a6";

        public SwapChain(Dalamud dalamud, SigScanner scanner) {
            this.dalamud = dalamud;
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

        public void Enable() {
            this.presentHook.Enable();
        }

        public void Dispose() {
            this.presentHook.Dispose();
        }

        private ImGuiImplDx11 impl;

        private IntPtr PresentDetour(IntPtr swapChain, uint a2, uint a3) {
            

            if (this.impl == null) {
                var ret = this.presentHook.Original(swapChain, a2, a3);

                Log.Debug($"CDXGISwapChain::Present: swapChain->{swapChain.ToInt64():X} a2->{a2:X} a3->{a3:X} RET=>{ret.ToInt64():X}");

                var s = new SharpDX.DXGI.SwapChain(swapChain);
                var d = s.GetDevice<Device>();
                var ctx = d.ImmediateContext;
                //s.GetDevice(Guid.Parse(deviceguid), out var device);

                Log.Verbose($"DEVICE: {d.NativePointer.ToInt64():X} CONTEXT: {ctx.NativePointer.ToInt64():X}");

                ImGui.CreateContext();
                ImGui.StyleColorsDark();

                this.impl = new ImGuiImplDx11();
                this.impl.Init(d, d.ImmediateContext);

                Log.Debug("Init OK");

                return ret;
            } else {
                this.impl.NewFrame();
                Log.Debug("IMPL NewFrame OK");
                ImGui.NewFrame();
                Log.Debug("NewFrame OK");

                ImGui.ShowDemoWindow();
                Log.Debug("ShowDemoWindow OK");

                ImGui.Render();
                Log.Debug("Render OK");

                this.impl.RenderDrawData(ImGui.GetDrawData());
                Log.Debug("RenderDrawData OK");

                return this.presentHook.Original(swapChain, a2, a3);
            }
        }
    }
}
