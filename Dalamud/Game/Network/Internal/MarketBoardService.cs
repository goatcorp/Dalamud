using System.Collections.Generic;
using System.Collections.Immutable;
using Dalamud.Game.Network.Internal.MarketBoardUploaders.Universalis;
using Dalamud.Game.Network.Structures;
using Dalamud.Hooking;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using MarketBoardListing = Dalamud.Game.Network.Structures.MarketBoardListing;

namespace Dalamud.Game.Network.Internal;

/// <summary>
/// An internal service to hook into key marketboard events and process them.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal unsafe class MarketBoardService : IServiceType, IDisposable, IMarketBoard
{
    private static readonly ModuleLog Log = new("MARKETBOARD");

    private readonly Hook<InfoProxyEndRequest> infoProxyItemSearchEndRequestHook;
    private readonly Hook<MarketBoardHistoryPacketHandler> historyPacketHandlerHook;
    private readonly Hook<MarketBoardPurchasePacketHandler> purchasePacketHandlerHook;
    private readonly Hook<CustomTalkReceiveResponse> customTalkReceiveResponseHook;

    private readonly MarketBoardAddressResolver addresses = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="MarketBoardService"/> class.
    /// </summary>
    /// <param name="scanner">The SigScanner to use.</param>
    [ServiceManager.ServiceConstructor]
    internal MarketBoardService(SigScanner scanner)
    {
        this.addresses.Setup(scanner);

        this.infoProxyItemSearchEndRequestHook =
            Hook<InfoProxyEndRequest>.FromAddress(
                (nint)InfoProxy11.StaticVTable.EndRequest,
                this.OnItemSearchEndRequest);
        this.infoProxyItemSearchEndRequestHook.Enable();

        this.historyPacketHandlerHook =
            Hook<MarketBoardHistoryPacketHandler>.FromAddress(
                (nint)InfoProxy11.MemberFunctionPointers.ProcessItemHistory_Internal,
                this.MarketBoardHistoryPacketDetour);
        this.historyPacketHandlerHook.Enable();

        this.purchasePacketHandlerHook =
            Hook<MarketBoardPurchasePacketHandler>.FromAddress(
                this.addresses.MarketBoardPurchasePacketHandler,
                this.MarketBoardPurchasePacketDetour);
        this.purchasePacketHandlerHook.Enable();

        this.customTalkReceiveResponseHook =
            Hook<CustomTalkReceiveResponse>.FromAddress(
                this.addresses.CustomTalkEventResponsePacketHandler,
                this.CustomTalkEventResponseDetour);
        this.customTalkReceiveResponseHook.Enable();
    }

    private delegate void InfoProxyEndRequest();

    private delegate nint MarketBoardPurchasePacketHandler(nint a1, nint packetRef);

    private delegate nint MarketBoardHistoryPacketHandler(InfoProxy11* self, nint packetRef, uint a3, char a4);

    private delegate void CustomTalkReceiveResponse(
        nuint a1, ushort eventId, byte responseId, uint* args, byte argCount);

    /// <inheritdoc />
    public event Action<MarketBoardSearchResults>? OnListingsReceived;

    /// <inheritdoc />
    public event Action<MarketBoardListing>? OnPurchaseCompleted;

    /// <inheritdoc />
    public event Action<MarketBoardHistory>? OnHistoryReceived;

    /// <inheritdoc />
    public event Action<MarketTaxRates>? OnTaxRatesReceived;

    private static InfoProxy11* InfoProxy => (InfoProxy11*)UIModule.Instance()->GetInfoModule()->GetInfoProxyById(11);

    /// <inheritdoc />
    void IDisposable.Dispose()
    {
        this.infoProxyItemSearchEndRequestHook.Dispose();
        this.historyPacketHandlerHook.Dispose();
        this.purchasePacketHandlerHook.Dispose();
        this.customTalkReceiveResponseHook.Dispose();

        GC.SuppressFinalize(this);
    }

    private void OnItemSearchEndRequest()
    {
        this.infoProxyItemSearchEndRequestHook.OriginalDisposeSafe();

        var listings = new List<MarketBoardListing>();
        foreach (var rawListing in InfoProxy->Listings)
        {
            listings.Add(MarketBoardListing.FromInfoProxyEntry(rawListing));
        }

        Log.Warning($"Marketboard request completed. Retrieved {listings.Count} listings.");
        Log.Warning($"INFOPROXY ADDRESS: {(nint)InfoProxy:X}");

        this.OnListingsReceived?.InvokeSafely(new MarketBoardSearchResults
        {
            ItemId = InfoProxy->SelectedItemId,
            Listings = listings.ToImmutableList(),
        });
    }

    private nint MarketBoardHistoryPacketDetour(InfoProxy11* self, nint a2, uint a3, char a4)
    {
        var result = this.historyPacketHandlerHook!.OriginalDisposeSafe(self, a2, a3, a4);

        var data = MarketBoardHistory.Read(a2);
        Log.Information(
            $"Marketboard history for catalog {data.CatalogId} loaded: {data.HistoryListings.Count} entries found.");

        this.OnHistoryReceived?.InvokeSafely(data);

        return result;
    }

    private nint MarketBoardPurchasePacketDetour(nint a1, nint packetPtr)
    {
        var data = MarketBoardPurchase.Read(packetPtr);
        var result = this.purchasePacketHandlerHook.OriginalDisposeSafe(a1, packetPtr);

        if (data.StatusCode != 0)
        {
            Log.Debug($"Marketboard purchase failed - status code {data.StatusCode}.");
            return result;
        }

        var purchasedItem = InfoProxy->LastPurchasedMarketboardItem;
        if (!(data.CatalogId == purchasedItem.ItemId && data.ItemQuantity == purchasedItem.Quantity))
        {
            Log.Warning("Purchased marketboard item did not have proper Item ID or Quantity.");
            return result;
        }

        var totalCost = (purchasedItem.Quantity * purchasedItem.UnitPrice) + purchasedItem.TotalTax;
        Log.Information(
            $"Marketboard purchase for {data.ItemQuantity}x Item#{data.CatalogId} for {totalCost} gil complete.");

        this.OnPurchaseCompleted.InvokeSafely(MarketBoardListing.FromLastPurchasedEntry(purchasedItem));

        return result;
    }

    private void CustomTalkEventResponseDetour(nuint a1, ushort eventId, byte responseId, uint* args, byte argCount)
    {
        // ensure we're actually looking at tax data.
        if (!(eventId == 7 && responseId != 8))
            goto ORIGINAL;

        // update alongside new marketboards
        if (argCount != 8)
        {
            Log.Error($"Marketboard tax data received unexpected length of {argCount}. Cannot upload tax data.");
            goto ORIGINAL;
        }

        var taxData = MarketTaxRates.ReadFromCustomTalk((nint)args);

        Log.Information($"Got tax data: Limsa={taxData.LimsaLominsaTax}");
        this.OnTaxRatesReceived?.InvokeSafely(taxData);

        ORIGINAL:
        this.customTalkReceiveResponseHook.OriginalDisposeSafe(a1, eventId, responseId, args, argCount);
    }
}
