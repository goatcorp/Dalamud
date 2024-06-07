using System.Runtime.InteropServices;

using Dalamud.Game.Gui.PartyFinder.Internal;
using Dalamud.Game.Gui.PartyFinder.Types;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Plugin.Services;

using Serilog;

namespace Dalamud.Game.Gui.PartyFinder;

/// <summary>
/// This class handles interacting with the native PartyFinder window.
/// </summary>
[InterfaceVersion("1.0")]
[ServiceManager.EarlyLoadedService]
internal sealed class PartyFinderGui : IInternalDisposableService, IPartyFinderGui
{
    private readonly PartyFinderAddressResolver address;
    private readonly IntPtr memory;

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
    private delegate void ReceiveListingDelegate(IntPtr managerPtr, IntPtr data);

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

    private void HandleReceiveListingDetour(IntPtr managerPtr, IntPtr data)
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

    private void HandleListingEvents(IntPtr data)
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
[InterfaceVersion("1.0")]
[ServiceManager.ScopedService]
#pragma warning disable SA1015
[ResolveVia<IPartyFinderGui>]
#pragma warning restore SA1015
internal class PartyFinderGuiPluginScoped : IInternalDisposableService, IPartyFinderGui
{
    [ServiceManager.ServiceDependency]
    private readonly PartyFinderGui partyFinderGuiService = Service<PartyFinderGui>.Get();

    /// <summary>
    /// Initializes a new instance of the <see cref="PartyFinderGuiPluginScoped"/> class.
    /// </summary>
    internal PartyFinderGuiPluginScoped()
    {
        this.partyFinderGuiService.ReceiveListing += this.ReceiveListingForward;
    }

    /// <inheritdoc/>
    public event IPartyFinderGui.PartyFinderListingEventDelegate? ReceiveListing;

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        this.partyFinderGuiService.ReceiveListing -= this.ReceiveListingForward;

        this.ReceiveListing = null;
    }

    private void ReceiveListingForward(PartyFinderListing listing, PartyFinderListingEventArgs args) => this.ReceiveListing?.Invoke(listing, args);
}
