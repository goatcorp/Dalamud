using System.Runtime.InteropServices;

using Dalamud.Configuration.Internal;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Plugin.Services;
using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Client.Network;

using Serilog;

namespace Dalamud.Game.Network;

/// <summary>
/// This class handles interacting with game network events.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal sealed unsafe class GameNetwork : IInternalDisposableService
{
    private readonly GameNetworkAddressResolver address;
    private readonly Hook<PacketDispatcher.Delegates.OnReceivePacket> processZonePacketDownHook;
    private readonly Hook<ProcessZonePacketUpDelegate> processZonePacketUpHook;

    private readonly HitchDetector hitchDetectorUp;
    private readonly HitchDetector hitchDetectorDown;

    [ServiceManager.ServiceDependency]
    private readonly DalamudConfiguration configuration = Service<DalamudConfiguration>.Get();

    [ServiceManager.ServiceConstructor]
    private unsafe GameNetwork(TargetSigScanner sigScanner)
    {
        this.hitchDetectorUp = new HitchDetector("GameNetworkUp", this.configuration.GameNetworkUpHitch);
        this.hitchDetectorDown = new HitchDetector("GameNetworkDown", this.configuration.GameNetworkDownHitch);

        this.address = new GameNetworkAddressResolver();
        this.address.Setup(sigScanner);

        var onReceivePacketAddress = (nint)PacketDispatcher.StaticVirtualTablePointer->OnReceivePacket;

        Log.Verbose("===== G A M E N E T W O R K =====");
        Log.Verbose($"OnReceivePacket address {Util.DescribeAddress(onReceivePacketAddress)}");
        Log.Verbose($"ProcessZonePacketUp address {Util.DescribeAddress(this.address.ProcessZonePacketUp)}");

        this.processZonePacketDownHook = Hook<PacketDispatcher.Delegates.OnReceivePacket>.FromAddress(onReceivePacketAddress, this.ProcessZonePacketDownDetour);
        this.processZonePacketUpHook = Hook<ProcessZonePacketUpDelegate>.FromAddress(this.address.ProcessZonePacketUp, this.ProcessZonePacketUpDetour);

        this.processZonePacketDownHook.Enable();
        this.processZonePacketUpHook.Enable();
    }

    /// <summary>
    /// The delegate type of a network message event.
    /// </summary>
    /// <param name="dataPtr">The pointer to the raw data.</param>
    /// <param name="opCode">The operation ID code.</param>
    /// <param name="sourceActorId">The source actor ID.</param>
    /// <param name="targetActorId">The taret actor ID.</param>
    /// <param name="direction">The direction of the packed.</param>
    public delegate void OnNetworkMessageDelegate(nint dataPtr, ushort opCode, uint sourceActorId, uint targetActorId, NetworkMessageDirection direction);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate byte ProcessZonePacketUpDelegate(IntPtr a1, IntPtr dataPtr, IntPtr a3, byte a4);

    /// <summary>
    /// Event that is called when a network message is sent/received.
    /// </summary>
    public event OnNetworkMessageDelegate? NetworkMessage;

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        this.processZonePacketDownHook.Dispose();
        this.processZonePacketUpHook.Dispose();
    }

    private void ProcessZonePacketDownDetour(PacketDispatcher* dispatcher, uint targetId, IntPtr dataPtr)
    {
        this.hitchDetectorDown.Start();

        // Go back 0x10 to get back to the start of the packet header
        dataPtr -= 0x10;

        foreach (var d in Delegate.EnumerateInvocationList(this.NetworkMessage))
        {
            try
            {
                d.Invoke(
                    dataPtr + 0x20,
                    (ushort)Marshal.ReadInt16(dataPtr, 0x12),
                    0,
                    targetId,
                    NetworkMessageDirection.ZoneDown);
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
            }
        }

        this.processZonePacketDownHook.Original(dispatcher, targetId, dataPtr + 0x10);
        this.hitchDetectorDown.Stop();
    }

    private byte ProcessZonePacketUpDetour(IntPtr a1, IntPtr dataPtr, IntPtr a3, byte a4)
    {
        this.hitchDetectorUp.Start();

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

        this.hitchDetectorUp.Stop();

        return this.processZonePacketUpHook.Original(a1, dataPtr, a3, a4);
    }
}
