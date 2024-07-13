using System.Runtime.InteropServices;

using Dalamud.Game.Gui.PartyFinder.Internal;
using Dalamud.Game.Gui.PartyFinder.Types;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Plugin.Services;

using Serilog;

namespace Dalamud.Game.Gui.PartyFinder;

/// <summary>
/// This class handles interacting with the native PartyFinder window.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal sealed class PartyFinderGui : IInternalDisposableService, IPartyFinderGui
{
    private readonly PartyFinderAddressResolver address;
    private readonly nint memory;

    private readonly Hook<ReceiveListingDelegate> receiveListingHook;

    /// <summary>
    /// Initializes a new instance of the <see cref="PartyFinderGui"/> class.
    /// </summary>
    /// <param name="sigScanner">Sig scanner to use.</param>
    [ServiceManager.ServiceConstructor]
    private PartyFinderGui(TargetSigScanner sigScanner)
    {
        this.address = new PartyFinderAddressResolver();
        this.address.Setup(sigScanner);

        this.memory = Marshal.AllocHGlobal(PartyFinderPacket.PacketSize);

        this.receiveListingHook = Hook<ReceiveListingDelegate>.FromAddress(this.address.ReceiveListing, this.HandleReceiveListingDetour);
        this.receiveListingHook.Enable();
    }

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate void ReceiveListingDelegate(nint managerPtr, nint data);

    /// <inheritdoc/>
    public event IPartyFinderGui.PartyFinderListingEventDelegate? ReceiveListing;

    /// <summary>
    /// Dispose of managed and unmanaged resources.
    /// </summary>
    void IInternalDisposableService.DisposeService()
    {
        this.receiveListingHook.Dispose();

        try
        {
            Marshal.FreeHGlobal(this.memory);
        }
        catch (BadImageFormatException)
        {
            Log.Warning("Could not free PartyFinderGui memory.");
        }
    }

    private void HandleReceiveListingDetour(nint managerPtr, nint data)
    {
        try
        {
            this.HandleListingEvents(data);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Exception on ReceiveListing hook.");
        }

        this.receiveListingHook.Original(managerPtr, data);
    }

    private void HandleListingEvents(nint data)
    {
        var dataPtr = data + 0x10;

        var packet = Marshal.PtrToStructure<PartyFinderPacket>(dataPtr);

        // rewriting is an expensive operation, so only do it if necessary
        var needToRewrite = false;

        for (var i = 0; i < packet.Listings.Length; i++)
        {
            // these are empty slots that are not shown to the player
            if (packet.Listings[i].IsNull())
            {
                continue;
            }

            var listing = new PartyFinderListing(packet.Listings[i]);
            var args = new PartyFinderListingEventArgs(packet.BatchNumber);
            this.ReceiveListing?.Invoke(listing, args);

            if (args.Visible)
            {
                continue;
            }

            // hide the listing from the player by setting it to a null listing
            packet.Listings[i] = default;
            needToRewrite = true;
        }

        if (!needToRewrite)
        {
            return;
        }

        // write our struct into the memory (doing this directly crashes the game)
        Marshal.StructureToPtr(packet, this.memory, false);

        // copy our new memory over the game's
        unsafe
        {
            Buffer.MemoryCopy((void*)this.memory, (void*)dataPtr, PartyFinderPacket.PacketSize, PartyFinderPacket.PacketSize);
        }
    }
}

/// <summary>
/// A scoped variant of the PartyFinderGui service.
/// </summary>
[PluginInterface]
[ServiceManager.ScopedService]
#pragma warning disable SA1015
[ResolveVia<IPartyFinderGui>]
#pragma warning restore SA1015
internal class PartyFinderGuiPluginScoped : IInternalDisposableService, IPartyFinderGui
{
    private static readonly ModuleLog Log = new("PartyFinderGui");
    private readonly LocalPlugin plugin;

    [ServiceManager.ServiceDependency]
    private readonly PartyFinderGui partyFinderGuiService = Service<PartyFinderGui>.Get();

    /// <summary>
    /// Initializes a new instance of the <see cref="PartyFinderGuiPluginScoped"/> class.
    /// </summary>
    /// <param name="plugin">Information about the plugin using this service.</param>
    internal PartyFinderGuiPluginScoped(LocalPlugin plugin)
    {
        this.plugin = plugin;
        this.partyFinderGuiService.ReceiveListing += this.ReceiveListingForward;
    }

    /// <inheritdoc/>
    public event IPartyFinderGui.PartyFinderListingEventDelegate? ReceiveListing;

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        if (this.ReceiveListing?.GetInvocationList().Length > 0)
        {
            Log.Warning($"{this.plugin.InternalName} is leaking {this.ReceiveListing?.GetInvocationList().Length} ReceiveListing listeners! Make sure that all of them are unregistered properly.");
        }
        
        this.partyFinderGuiService.ReceiveListing -= this.ReceiveListingForward;

        this.ReceiveListing = null;
    }

    private void ReceiveListingForward(IPartyFinderListing listing, IPartyFinderListingEventArgs args) => this.ReceiveListing?.Invoke(listing, args);
}
