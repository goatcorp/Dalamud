using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Serilog;

namespace Dalamud.Game.Network
{
    /// <summary>
    /// This class handles interacting with game network events.
    /// </summary>
    [PluginInterface]
    [InterfaceVersion("1.0")]
    public sealed class GameNetwork : IDisposable
    {
        private readonly GameNetworkAddressResolver address;
        private readonly Hook<ProcessZonePacketDownDelegate> processZonePacketDownHook;
        private readonly Hook<ProcessZonePacketUpDelegate> processZonePacketUpHook;
        private readonly Queue<byte[]> zoneInjectQueue = new();

        private IntPtr baseAddress;

        /// <summary>
        /// Initializes a new instance of the <see cref="GameNetwork"/> class.
        /// </summary>
        internal GameNetwork()
        {
            this.address = new GameNetworkAddressResolver();
            this.address.Setup();

            Log.Verbose("===== G A M E N E T W O R K =====");
            Log.Verbose($"ProcessZonePacketDown address 0x{this.address.ProcessZonePacketDown.ToInt64():X}");
            Log.Verbose($"ProcessZonePacketUp address 0x{this.address.ProcessZonePacketUp.ToInt64():X}");

            this.processZonePacketDownHook = new Hook<ProcessZonePacketDownDelegate>(this.address.ProcessZonePacketDown, this.ProcessZonePacketDownDetour);
            this.processZonePacketUpHook = new Hook<ProcessZonePacketUpDelegate>(this.address.ProcessZonePacketUp, this.ProcessZonePacketUpDetour);
        }

        /// <summary>
        /// The delegate type of a network message event.
        /// </summary>
        /// <param name="dataPtr">The pointer to the raw data.</param>
        /// <param name="opCode">The operation ID code.</param>
        /// <param name="sourceActorId">The source actor ID.</param>
        /// <param name="targetActorId">The taret actor ID.</param>
        /// <param name="direction">The direction of the packed.</param>
        public delegate void OnNetworkMessageDelegate(IntPtr dataPtr, ushort opCode, uint sourceActorId, uint targetActorId, NetworkMessageDirection direction);

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate void ProcessZonePacketDownDelegate(IntPtr a, uint targetId, IntPtr dataPtr);

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate byte ProcessZonePacketUpDelegate(IntPtr a1, IntPtr dataPtr, IntPtr a3, byte a4);

        /// <summary>
        /// Event that is called when a network message is sent/received.
        /// </summary>
        public event OnNetworkMessageDelegate NetworkMessage;

        /// <summary>
        /// Enable this module.
        /// </summary>
        public void Enable()
        {
            this.processZonePacketDownHook.Enable();
            this.processZonePacketUpHook.Enable();
        }

        /// <summary>
        /// Dispose of managed and unmanaged resources.
        /// </summary>
        void IDisposable.Dispose()
        {
            this.processZonePacketDownHook.Dispose();
            this.processZonePacketUpHook.Dispose();
        }

        /// <summary>
        /// Process a chat queue.
        /// </summary>
        internal void UpdateQueue()
        {
            while (this.zoneInjectQueue.Count > 0)
            {
                var packetData = this.zoneInjectQueue.Dequeue();

                var unmanagedPacketData = Marshal.AllocHGlobal(packetData.Length);
                Marshal.Copy(packetData, 0, unmanagedPacketData, packetData.Length);

                if (this.baseAddress != IntPtr.Zero)
                {
                    this.processZonePacketDownHook.Original(this.baseAddress, 0, unmanagedPacketData);
                }

                Marshal.FreeHGlobal(unmanagedPacketData);
            }
        }

        private void ProcessZonePacketDownDetour(IntPtr a, uint targetId, IntPtr dataPtr)
        {
            this.baseAddress = a;

            // Go back 0x10 to get back to the start of the packet header
            dataPtr -= 0x10;

            try
            {
                // Call events
                this.NetworkMessage?.Invoke(dataPtr + 0x20, (ushort)Marshal.ReadInt16(dataPtr, 0x12), 0, targetId, NetworkMessageDirection.ZoneDown);

                this.processZonePacketDownHook.Original(a, targetId, dataPtr + 0x10);
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

                Log.Error(ex, "Exception on ProcessZonePacketDown hook. Header: " + header);

                this.processZonePacketDownHook.Original(a, targetId, dataPtr + 0x10);
            }
        }

        private byte ProcessZonePacketUpDetour(IntPtr a1, IntPtr dataPtr, IntPtr a3, byte a4)
        {
            try
            {
                // Call events
                // TODO: Implement actor IDs
                this.NetworkMessage?.Invoke(dataPtr + 0x20, (ushort)Marshal.ReadInt16(dataPtr), 0x0, 0x0, NetworkMessageDirection.ZoneUp);
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

        // private void InjectZoneProtoPacket(byte[] data)
        // {
        //     this.zoneInjectQueue.Enqueue(data);
        // }

        // private void InjectActorControl(short cat, int param1)
        // {
        //     var packetData = new byte[]
        //     {
        //         0x14, 0x00, 0x8D, 0x00, 0x00, 0x00, 0x0E, 0x00, 0x17, 0x7C, 0xC5, 0x5D, 0x00, 0x00, 0x00, 0x00,
        //         0x05, 0x00, 0x48, 0xB2, 0x0C, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        //         0x00, 0x00, 0x00, 0x00, 0x43, 0x7F, 0x00, 0x00,
        //     };
        //
        //     BitConverter.GetBytes((short)cat).CopyTo(packetData, 0x10);
        //
        //     BitConverter.GetBytes((uint)param1).CopyTo(packetData, 0x14);
        //
        //     this.InjectZoneProtoPacket(packetData);
        // }
    }
}
