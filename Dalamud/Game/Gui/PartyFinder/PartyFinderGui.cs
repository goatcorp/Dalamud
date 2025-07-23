using System.Runtime.InteropServices;

using Dalamud.Game.Gui.PartyFinder.Internal;
using Dalamud.Game.Gui.PartyFinder.Types;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.UI.Info;

using Serilog;

namespace Dalamud.Game.Gui.PartyFinder;

/// <summary>
/// This class handles interacting with the native PartyFinder window.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal sealed unsafe class PartyFinderGui : IInternalDisposableService, IPartyFinderGui
{
    private readonly nint memory;

    private readonly Hook<InfoProxyCrossRealm.Delegates.ReceiveListing> receiveListingHook;

    /// <summary>
    /// Initializes a new instance of the <see cref="PartyFinderGui"/> class.
    /// </summary>
    /// <param name="sigScanner">Sig scanner to use.</param>
    [ServiceManager.ServiceConstructor]
    private PartyFinderGui(TargetSigScanner sigScanner)
    {
        this.memory = Marshal.AllocHGlobal(PartyFinderPacket.PacketSize);

        this.receiveListingHook = Hook<InfoProxyCrossRealm.Delegates.ReceiveListing>.FromAddress(InfoProxyCrossRealm.Addresses.ReceiveListing.Value, this.HandleReceiveListingDetour);
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

        try
        {
            Marshal.FreeHGlobal(this.memory);
        }
        catch (BadImageFormatException)
        {
            Log.Warning("Could not free PartyFinderGui memory.");
        }
    }

    private void HandleReceiveListingDetour(InfoProxyCrossRealm* infoProxy, nint packet)
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
