using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Dalamud.Hooking;
using Serilog;

namespace Dalamud.Game.Internal.Network {
    public sealed class GameNetwork : IDisposable {
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate void ProcessZonePacketDelegate(IntPtr a, IntPtr b, IntPtr dataPtr);

        private readonly Hook<ProcessZonePacketDelegate> processZonePacketHook;

        private GameNetworkAddressResolver Address { get; }
        private IntPtr baseAddress;

        public delegate void OnZonePacketDelegate(IntPtr dataPtr);

        public OnZonePacketDelegate OnZonePacket;

        private readonly Dalamud dalamud;

        private readonly Queue<byte[]> zoneInjectQueue = new Queue<byte[]>();

        public GameNetwork(Dalamud dalamud, SigScanner scanner) {
            this.dalamud = dalamud;
            Address = new GameNetworkAddressResolver();
            Address.Setup(scanner);

            Log.Verbose("===== G A M E N E T W O R K =====");
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
            this.baseAddress = a;

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

#if DEBUG
        public void InjectZoneProtoPacket(byte[] data) {
            this.zoneInjectQueue.Enqueue(data);
        }

        private void InjectActorControl(short cat, int param1) {
            var packetData = new byte[] {
                0x14, 0x00, 0x8D, 0x00, 0x00, 0x00, 0x0E, 0x00, 0x17, 0x7C, 0xC5, 0x5D, 0x00, 0x00, 0x00, 0x00,
                0x05, 0x00, 0x48, 0xB2, 0x0C, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x43, 0x7F, 0x00, 0x00
            };

            BitConverter.GetBytes((short) cat).CopyTo(packetData, 0x10);

            BitConverter.GetBytes((UInt32) param1).CopyTo(packetData, 0x14);

            InjectZoneProtoPacket(packetData);
        }
#endif

        /// <summary>
        ///     Process a chat queue.
        /// </summary>
        public void UpdateQueue(Framework framework)
        {
            while (this.zoneInjectQueue.Count > 0)
            {
                var packetData = this.zoneInjectQueue.Dequeue();

                var unmanagedPacketData = Marshal.AllocHGlobal(packetData.Length);
                Marshal.Copy(packetData, 0, unmanagedPacketData, packetData.Length);

                if (this.baseAddress != IntPtr.Zero) {
                    this.processZonePacketHook.Original(this.baseAddress, IntPtr.Zero, unmanagedPacketData);
                }

                Marshal.FreeHGlobal(unmanagedPacketData);
            }
        }
    }
}
