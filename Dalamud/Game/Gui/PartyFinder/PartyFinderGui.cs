using Dalamud.Game.Gui.PartyFinder.Types;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.Game.Network;
using FFXIVClientStructs.FFXIV.Client.UI.Info;

using Serilog;

namespace Dalamud.Game.Gui.PartyFinder;

/// <summary>
/// This class handles interacting with the native PartyFinder window.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal sealed unsafe class PartyFinderGui : IInternalDisposableService, IPartyFinderGui
{
    private readonly Hook<InfoProxyCrossRealm.Delegates.ReceiveListing> receiveListingHook;

    /// <summary>
    /// Initializes a new instance of the <see cref="PartyFinderGui"/> class.
    /// </summary>
    [ServiceManager.ServiceConstructor]
    private PartyFinderGui()
    {
        this.receiveListingHook = Hook<InfoProxyCrossRealm.Delegates.ReceiveListing>.FromAddress(
            InfoProxyCrossRealm.Addresses.ReceiveListing.Value,
            this.HandleReceiveListingDetour);
        this.receiveListingHook.Enable();
    }

    /// <inheritdoc/>
    public event IPartyFinderGui.PartyFinderListingEventDelegate? ReceiveListing;

    /// <summary>
    /// Dispose of managed and unmanaged resources.
    /// </summary>
    void IInternalDisposableService.DisposeService()
    {
        this.receiveListingHook.Dispose();
    }

    private void HandleReceiveListingDetour(InfoProxyCrossRealm* infoProxy, ServerIpcSegment<CrossRealmListingSegmentPacket>* packet)
    {
        try
        {
            this.HandleListingEvents(packet);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Exception on ReceiveListing hook.");
        }

        this.receiveListingHook.Original(infoProxy, packet);
    }

    private void HandleListingEvents(ServerIpcSegment<CrossRealmListingSegmentPacket>* packet)
    {
        for (var i = 0; i < packet->Payload.Entries.Length; i++)
        {
            ref var entry = ref packet->Payload.Entries[i];

            // these are empty slots that are not shown to the player
            if (entry.ListingId == 0)
                continue;

            var listing = new PartyFinderListing(ref entry);
            var args = new PartyFinderListingEventArgs(packet->Payload.SegmentIndex);
            foreach (var d in Delegate.EnumerateInvocationList(this.ReceiveListing))
            {
                try
                {
                    d(listing, args);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Exception during raise of {handler}", d.Method);
                }
            }

            if (!args.Visible)
            {
                // hide the listing from the player by setting it to a null listing
                packet->Payload.Entries[i] = default;
            }
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

    private void ReceiveListingForward(IPartyFinderListing listing, IPartyFinderListingEventArgs args) => this.ReceiveListing?.Invoke(listing, args);
}
