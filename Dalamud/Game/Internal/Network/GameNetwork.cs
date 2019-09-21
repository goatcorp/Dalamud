using System;
using System.Runtime.InteropServices;
using Dalamud.Hooking;
using Serilog;

namespace Dalamud.Game.Internal.Network {
    public sealed class GameNetwork : IDisposable {
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate void ProcessZonePacketDelegate(IntPtr a, IntPtr b, IntPtr dataPtr);

        private readonly Hook<ProcessZonePacketDelegate> processZonePacketHook;

        private GameNetworkAddressResolver Address { get; }

        public delegate void OnZonePacketDelegate(IntPtr dataPtr);

        public OnZonePacketDelegate OnZonePacket;

        private readonly Dalamud dalamud;

        public GameNetwork(Dalamud dalamud, SigScanner scanner) {
            this.dalamud = dalamud;
            Address = new GameNetworkAddressResolver();
            Address.Setup(scanner);

            Log.Verbose("ProcessZonePacket address {ProcessZonePacket}", Address.ProcessZonePacket);

            this.processZonePacketHook =
                new Hook<ProcessZonePacketDelegate>(Address.ProcessZonePacket,
                                                    new ProcessZonePacketDelegate(ProcessZonePacketDetour),
                                                    this);
        }

        public void Enable() {
            this.processZonePacketHook.Enable();
        }

        public void Dispose() {
            this.processZonePacketHook.Dispose();
        }

        private void ProcessZonePacketDetour(IntPtr a, IntPtr b, IntPtr dataPtr) {
            // Call events
            this.OnZonePacket?.Invoke(dataPtr);

            try {
                this.processZonePacketHook.Original(a, b, dataPtr);
            } catch (Exception ex) {
                string header;
                try {
                    var data = new byte[32];
                    Marshal.Copy(dataPtr, data, 0, 32);
                    header = BitConverter.ToString(data);
                } catch (Exception) {
                    header = "failed";
                }

                Log.Error(ex, "Exception on ProcessZonePacket hook. Header: " + header);

                this.processZonePacketHook.Original(a, b, dataPtr);
            }
        }
    }
}
