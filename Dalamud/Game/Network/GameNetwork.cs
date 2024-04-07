using System.Runtime.InteropServices;

using Dalamud.Configuration.Internal;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using Serilog;

namespace Dalamud.Game.Network;

/// <summary>
/// This class handles interacting with game network events.
/// </summary>
[InterfaceVersion("1.0")]
[ServiceManager.EarlyLoadedService]
internal sealed class GameNetwork : IInternalDisposableService, IGameNetwork
{
    private readonly GameNetworkAddressResolver address;
    private readonly Hook<ProcessZonePacketDownDelegate> processZonePacketDownHook;
    private readonly Hook<ProcessZonePacketUpDelegate> processZonePacketUpHook;

    private readonly HitchDetector hitchDetectorUp;
    private readonly HitchDetector hitchDetectorDown;

    [ServiceManager.ServiceDependency]
    private readonly DalamudConfiguration configuration = Service<DalamudConfiguration>.Get();

    private IntPtr baseAddress;

    [ServiceManager.ServiceConstructor]
    private GameNetwork(TargetSigScanner sigScanner)
    {
        this.hitchDetectorUp = new HitchDetector("GameNetworkUp", this.configuration.GameNetworkUpHitch);
        this.hitchDetectorDown = new HitchDetector("GameNetworkDown", this.configuration.GameNetworkDownHitch);

        this.address = new GameNetworkAddressResolver();
        this.address.Setup(sigScanner);

        Log.Verbose("===== G A M E N E T W O R K =====");
        Log.Verbose($"ProcessZonePacketDown address 0x{this.address.ProcessZonePacketDown.ToInt64():X}");
        Log.Verbose($"ProcessZonePacketUp address 0x{this.address.ProcessZonePacketUp.ToInt64():X}");

        this.processZonePacketDownHook = Hook<ProcessZonePacketDownDelegate>.FromAddress(this.address.ProcessZonePacketDown, this.ProcessZonePacketDownDetour);
        this.processZonePacketUpHook = Hook<ProcessZonePacketUpDelegate>.FromAddress(this.address.ProcessZonePacketUp, this.ProcessZonePacketUpDetour);

        this.processZonePacketDownHook.Enable();
        this.processZonePacketUpHook.Enable();
    }

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate void ProcessZonePacketDownDelegate(IntPtr a, uint targetId, IntPtr dataPtr);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate byte ProcessZonePacketUpDelegate(IntPtr a1, IntPtr dataPtr, IntPtr a3, byte a4);

    /// <inheritdoc/>
    public event IGameNetwork.OnNetworkMessageDelegate? NetworkMessage;

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        this.processZonePacketDownHook.Dispose();
        this.processZonePacketUpHook.Dispose();
    }

    private void ProcessZonePacketDownDetour(IntPtr a, uint targetId, IntPtr dataPtr)
    {
        this.baseAddress = a;

        this.hitchDetectorDown.Start();

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

/// <summary>
/// Plugin-scoped version of a AddonLifecycle service.
/// </summary>
[PluginInterface]
[InterfaceVersion("1.0")]
[ServiceManager.ScopedService]
#pragma warning disable SA1015
[ResolveVia<IGameNetwork>]
#pragma warning restore SA1015
internal class GameNetworkPluginScoped : IInternalDisposableService, IGameNetwork
{
    [ServiceManager.ServiceDependency]
    private readonly GameNetwork gameNetworkService = Service<GameNetwork>.Get();

    /// <summary>
    /// Initializes a new instance of the <see cref="GameNetworkPluginScoped"/> class.
    /// </summary>
    internal GameNetworkPluginScoped()
    {
        this.gameNetworkService.NetworkMessage += this.NetworkMessageForward;
    }
    
    /// <inheritdoc/>
    public event IGameNetwork.OnNetworkMessageDelegate? NetworkMessage;

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        this.gameNetworkService.NetworkMessage -= this.NetworkMessageForward;

        this.NetworkMessage = null;
    }

    private void NetworkMessageForward(nint dataPtr, ushort opCode, uint sourceActorId, uint targetActorId, NetworkMessageDirection direction)
        => this.NetworkMessage?.Invoke(dataPtr, opCode, sourceActorId, targetActorId, direction);
}
