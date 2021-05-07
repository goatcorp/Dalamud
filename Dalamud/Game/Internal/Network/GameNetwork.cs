using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Dalamud.Hooking;
using Serilog;

namespace Dalamud.Game.Internal.Network {
    public sealed class GameNetwork : IDisposable {
        #region Hooks

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate void ProcessZonePacketDownDelegate(IntPtr a, uint targetId, IntPtr dataPtr);
        private readonly Hook<ProcessZonePacketDownDelegate> processZonePacketDownHook;

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate byte ProcessZonePacketUpDelegate(IntPtr a1, IntPtr dataPtr, IntPtr a3, byte a4);
        private readonly Hook<ProcessZonePacketUpDelegate> processZonePacketUpHook;

        #endregion

        private GameNetworkAddressResolver Address { get; }
        private IntPtr baseAddress;

        public delegate void OnNetworkMessageDelegate(IntPtr dataPtr, ushort opCode, uint sourceActorId, uint targetActorId, NetworkMessageDirection direction);

        /// <summary>
        /// Event that is called when a network message is sent/received.
        /// </summary>
        public OnNetworkMessageDelegate OnNetworkMessage;



        private readonly Queue<byte[]> zoneInjectQueue = new Queue<byte[]>();

        public GameNetwork(SigScanner scanner) {
            Address = new GameNetworkAddressResolver();
            Address.Setup(scanner);

            Log.Verbose("===== G A M E N E T W O R K =====");
            Log.Verbose("ProcessZonePacketDown address {ProcessZonePacketDown}", Address.ProcessZonePacketDown);
            Log.Verbose("ProcessZonePacketUp address {ProcessZonePacketUp}", Address.ProcessZonePacketUp);

            this.processZonePacketDownHook =
                new Hook<ProcessZonePacketDownDelegate>(Address.ProcessZonePacketDown,
                                                    new ProcessZonePacketDownDelegate(ProcessZonePacketDownDetour),
                                                    this);

            this.processZonePacketUpHook =
                new Hook<ProcessZonePacketUpDelegate>(Address.ProcessZonePacketUp,
                                                        new ProcessZonePacketUpDelegate(ProcessZonePacketUpDetour),
                                                        this);
        }

        public void Enable() {
            this.processZonePacketDownHook.Enable();
            this.processZonePacketUpHook.Enable();
        }

        public void Dispose() {
            this.processZonePacketDownHook.Dispose();
            this.processZonePacketUpHook.Dispose();
        }

        private void ProcessZonePacketDownDetour(IntPtr a, uint targetId, IntPtr dataPtr) {
            this.baseAddress = a;

            // Go back 0x10 to get back to the start of the packet header
            dataPtr -= 0x10;

            try {
                

                // Call events
                this.OnNetworkMessage?.Invoke(dataPtr + 0x20, (ushort) Marshal.ReadInt16(dataPtr, 0x12), 0, targetId, NetworkMessageDirection.ZoneDown);

                this.processZonePacketDownHook.Original(a, targetId, dataPtr + 0x10);
            } catch (Exception ex) {
                string header;
                try {
                    var data = new byte[32];
                    Marshal.Copy(dataPtr, data, 0, 32);
                    header = BitConverter.ToString(data);
                } catch (Exception) {
                    header = "failed";
                }

                Log.Error(ex, "Exception on ProcessZonePacketDown hook. Header: " + header);

                this.processZonePacketDownHook.Original(a, targetId, dataPtr + 0x10);
            }
        }

        private byte ProcessZonePacketUpDetour(IntPtr a1, IntPtr dataPtr, IntPtr a3, byte a4) {

            try
            {
                // Call events
                // TODO: Implement actor IDs
                this.OnNetworkMessage?.Invoke(dataPtr + 0x20, (ushort) Marshal.ReadInt16(dataPtr), 0x0, 0x0, NetworkMessageDirection.ZoneUp);
            }
            catch (Exception ex)
            {
                string header;
                try
                {
                    var data = new byte[32];
                    Marshal.Copy(dataPtr, data, 0, 32);
                    header = BitConverter.ToString(data);
                }
                catch (Exception)
                {
                    header = "failed";
                }

                Log.Error(ex, "Exception on ProcessZonePacketUp hook. Header: " + header);
            }

            return this.processZonePacketUpHook.Original(a1, dataPtr, a3, a4);
        }

#if DEBUG
        public void InjectZoneProtoPacket(byte[] data) {
            this.zoneInjectQueue.Enqueue(data);
        }

        private void InjectActorControl(short cat, int param1) {
            var packetData = new byte[] {
                0x14, 0x00, 0x8D, 0x00, 0x00, 0x00, 0x0E, 0x00, 0x17, 0x7C, 0xC5, 0x5D, 0x00, 0x00, 0x00, 0x00,
                0x05, 0x00, 0x48, 0xB2, 0x0C, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x43, 0x7F, 0x00, 0x00,
            };

            BitConverter.GetBytes((short) cat).CopyTo(packetData, 0x10);

            BitConverter.GetBytes((uint) param1).CopyTo(packetData, 0x14);

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
                    this.processZonePacketDownHook.Original(this.baseAddress, 0, unmanagedPacketData);
                }

                Marshal.FreeHGlobal(unmanagedPacketData);
            }
        }
    }
}
