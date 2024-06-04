using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;

using Dalamud.Configuration.Internal;
using Dalamud.Data;
using Dalamud.Game.Gui;
using Dalamud.Game.Network.Internal.MarketBoardUploaders;
using Dalamud.Game.Network.Internal.MarketBoardUploaders.Universalis;
using Dalamud.Game.Network.Structures;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using Dalamud.Networking.Http;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using Lumina.Excel.GeneratedSheets;
using Serilog;

namespace Dalamud.Game.Network.Internal;

/// <summary>
/// This class handles network notifications and uploading market board data.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal unsafe class NetworkHandlers : IInternalDisposableService
{
    private readonly IMarketBoardUploader uploader;

    private readonly IObservable<MarketBoardPurchase> mbPurchaseObservable;
    private readonly IObservable<MarketBoardHistory> mbHistoryObservable;
    private readonly IObservable<MarketTaxRates> mbTaxesObservable;
    private readonly IObservable<MarketBoardItemRequest> mbItemRequestObservable;
    private readonly IObservable<MarketBoardCurrentOfferings> mbOfferingsObservable;
    private readonly IObservable<MarketBoardPurchaseHandler> mbPurchaseSentObservable;

    private readonly IDisposable handleMarketBoardItemRequest;
    private readonly IDisposable handleMarketTaxRates;
    private readonly IDisposable handleMarketBoardPurchaseHandler;

    private readonly NetworkHandlersAddressResolver addressResolver;

    private readonly Hook<CfPopDelegate> cfPopHook;
    private readonly Hook<MarketBoardPurchasePacketHandler> mbPurchaseHook;
    private readonly Hook<MarketBoardHistoryPacketHandler> mbHistoryHook;
    private readonly Hook<CustomTalkReceiveResponse> customTalkHook; // used for marketboard taxes
    private readonly Hook<MarketBoardItemRequestStartPacketHandler> mbItemRequestStartHook;
    private readonly Hook<InfoProxyItemSearchAddPage> mbOfferingsHook;
    private readonly Hook<MarketBoardSendPurchaseRequestPacket> mbSendPurchaseRequestHook;

    [ServiceManager.ServiceDependency]
    private readonly DalamudConfiguration configuration = Service<DalamudConfiguration>.Get();

    private bool disposing;

    [ServiceManager.ServiceConstructor]
    private NetworkHandlers(
        GameNetwork gameNetwork,
        TargetSigScanner sigScanner,
        HappyHttpClient happyHttpClient)
    {
        this.uploader = new UniversalisMarketBoardUploader(happyHttpClient);

        this.addressResolver = new NetworkHandlersAddressResolver();
        this.addressResolver.Setup(sigScanner);

        this.CfPop = _ => { };

        this.mbPurchaseObservable = Observable.Create<MarketBoardPurchase>(observer =>
        {
            this.MarketBoardPurchaseReceived += Observe;
            return () => { this.MarketBoardPurchaseReceived -= Observe; };

            void Observe(nint packetPtr)
            {
                observer.OnNext(MarketBoardPurchase.Read(packetPtr));
            }
        });

        this.mbHistoryObservable = Observable.Create<MarketBoardHistory>(observer =>
        {
            this.MarketBoardHistoryReceived += Observe;
            return () => { this.MarketBoardHistoryReceived -= Observe; };

            void Observe(nint packetPtr)
            {
                observer.OnNext(MarketBoardHistory.Read(packetPtr));
            }
        });

        this.mbTaxesObservable = Observable.Create<MarketTaxRates>(observer =>
        {
            this.MarketBoardTaxesReceived += Observe;
            return () => { this.MarketBoardTaxesReceived -= Observe; };

            void Observe(nint dataPtr)
            {
                // n.b. we precleared the packet information so we're sure that this is *just* tax rate info.
                observer.OnNext(MarketTaxRates.ReadFromCustomTalk(dataPtr));
            }
        });

        this.mbItemRequestObservable = Observable.Create<MarketBoardItemRequest>(observer =>
        {
            this.MarketBoardItemRequestStartReceived += Observe;
            return () => this.MarketBoardItemRequestStartReceived -= Observe;

            void Observe(nint dataPtr)
            {
                observer.OnNext(MarketBoardItemRequest.Read(dataPtr));
            }
        });

        this.mbOfferingsObservable = Observable.Create<MarketBoardCurrentOfferings>(observer =>
        {
            this.MarketBoardOfferingsReceived += Observe;
            return () => { this.MarketBoardOfferingsReceived -= Observe; };

            void Observe(nint packetPtr)
            {
                observer.OnNext(MarketBoardCurrentOfferings.Read(packetPtr));
            }
        });

        this.mbPurchaseSentObservable = Observable.Create<MarketBoardPurchaseHandler>(observer =>
        {
            this.MarketBoardPurchaseRequestSent += Observe;
            return () => { this.MarketBoardPurchaseRequestSent -= Observe; };

            void Observe(nint dataPtr)
            {
                // fortunately, this dataptr has the same structure as the sent packet.
                observer.OnNext(MarketBoardPurchaseHandler.Read(dataPtr));
            }
        });

        this.handleMarketBoardItemRequest = this.HandleMarketBoardItemRequest();
        this.handleMarketTaxRates = this.HandleMarketTaxRates();
        this.handleMarketBoardPurchaseHandler = this.HandleMarketBoardPurchaseHandler();

        this.mbPurchaseHook =
            Hook<MarketBoardPurchasePacketHandler>.FromAddress(
                this.addressResolver.MarketBoardPurchasePacketHandler,
                this.MarketPurchasePacketDetour);
        this.mbPurchaseHook.Enable();

        this.mbHistoryHook =
            Hook<MarketBoardHistoryPacketHandler>.FromAddress(
                this.addressResolver.MarketBoardHistoryPacketHandler,
                this.MarketHistoryPacketDetour);
        this.mbHistoryHook.Enable();

        this.customTalkHook =
            Hook<CustomTalkReceiveResponse>.FromAddress(
                this.addressResolver.CustomTalkEventResponsePacketHandler,
                this.CustomTalkReceiveResponseDetour);
        this.customTalkHook.Enable();

        this.mbItemRequestStartHook = Hook<MarketBoardItemRequestStartPacketHandler>.FromAddress(
            this.addressResolver.MarketBoardItemRequestStartPacketHandler,
            this.MarketItemRequestStartDetour);
        this.mbItemRequestStartHook.Enable();

        this.mbOfferingsHook = Hook<InfoProxyItemSearchAddPage>.FromAddress(
            this.addressResolver.InfoProxyItemSearchAddPage,
            this.MarketBoardOfferingsDetour);
        this.mbOfferingsHook.Enable();

        this.mbSendPurchaseRequestHook = Hook<MarketBoardSendPurchaseRequestPacket>.FromAddress(
            this.addressResolver.BuildMarketBoardPurchaseHandlerPacket,
            this.MarketBoardSendPurchaseRequestDetour);
        this.mbSendPurchaseRequestHook.Enable();

        this.cfPopHook = Hook<CfPopDelegate>.FromAddress(this.addressResolver.CfPopPacketHandler, this.CfPopDetour);
        this.cfPopHook.Enable();
    }

    private delegate nint MarketBoardPurchasePacketHandler(nint a1, nint packetRef);

    private delegate nint MarketBoardHistoryPacketHandler(nint self, nint packetData, uint a3, char a4);

    private delegate void CustomTalkReceiveResponse(
        nuint a1, ushort eventId, byte responseId, uint* args, byte argCount);

    private delegate nint MarketBoardItemRequestStartPacketHandler(nint a1, nint packetRef);

    private delegate byte InfoProxyItemSearchAddPage(nint self, nint packetRef);

    private delegate byte MarketBoardSendPurchaseRequestPacket(InfoProxyItemSearch* infoProxy);

    private delegate nint CfPopDelegate(nint packetData);

    /// <summary>
    /// Event which gets fired when a duty is ready.
    /// </summary>
    public event Action<ContentFinderCondition> CfPop;

    private event Action<nint>? MarketBoardPurchaseReceived;

    private event Action<nint>? MarketBoardHistoryReceived;

    private event Action<nint>? MarketBoardTaxesReceived;

    private event Action<nint>? MarketBoardItemRequestStartReceived;

    private event Action<nint>? MarketBoardOfferingsReceived;

    private event Action<nint>? MarketBoardPurchaseRequestSent;

    /// <summary>
    /// Disposes of managed and unmanaged resources.
    /// </summary>
    void IInternalDisposableService.DisposeService()
    {
        this.disposing = true;
        this.Dispose(this.disposing);
    }

    /// <summary>
    /// Disposes of managed and unmanaged resources.
    /// </summary>
    /// <param name="shouldDispose">Whether or not to execute the disposal.</param>
    protected void Dispose(bool shouldDispose)
    {
        if (!shouldDispose)
            return;

        this.handleMarketBoardItemRequest.Dispose();
        this.handleMarketTaxRates.Dispose();
        this.handleMarketBoardPurchaseHandler.Dispose();

        this.mbPurchaseHook.Dispose();
        this.mbHistoryHook.Dispose();
        this.customTalkHook.Dispose();
        this.mbItemRequestStartHook.Dispose();
        this.mbOfferingsHook.Dispose();
        this.mbSendPurchaseRequestHook.Dispose();
        this.cfPopHook.Dispose();
    }

    private unsafe nint CfPopDetour(nint packetData)
    {
        var result = this.cfPopHook.OriginalDisposeSafe(packetData);

        try
        {
            using var stream = new UnmanagedMemoryStream((byte*)packetData, 64);
            using var reader = new BinaryReader(stream);

            var notifyType = reader.ReadByte();
            stream.Position += 0x1B;
            var conditionId = reader.ReadUInt16();

            if (notifyType != 3)
                return result;

            if (this.configuration.DutyFinderTaskbarFlash)
                Util.FlashWindow();

            var cfConditionSheet = Service<DataManager>.Get().GetExcelSheet<ContentFinderCondition>()!;
            var cfCondition = cfConditionSheet.GetRow(conditionId);

            if (cfCondition == null)
            {
                Log.Error("CFC key {ConditionId} not in Lumina data", conditionId);
                return result;
            }

            var cfcName = cfCondition.Name.ToDalamudString();
            if (cfcName.Payloads.Count == 0)
            {
                cfcName = "Duty Roulette";
                cfCondition.Image = 112324;
            }

            Task.Run(() =>
            {
                if (this.configuration.DutyFinderChatMessage)
                {
                    var b = new SeStringBuilder();
                    b.Append("Duty pop: ");
                    b.Append(cfcName);
                    Service<ChatGui>.GetNullable()?.Print(b.Build());
                }

                this.CfPop.InvokeSafely(cfCondition);
            }).ContinueWith(
                task => Log.Error(task.Exception, "CfPop.Invoke failed"),
                TaskContinuationOptions.OnlyOnFaulted);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "CfPopDetour threw an exception");   
        }

        return result;
    }

    private IObservable<List<MarketBoardCurrentOfferings.MarketBoardItemListing>> OnMarketBoardListingsBatch(
        IObservable<MarketBoardItemRequest> start)
    {
        var offeringsObservable = this.mbOfferingsObservable.Publish().RefCount();

        void LogEndObserved(MarketBoardCurrentOfferings offerings)
        {
            Log.Verbose(
                "Observed end of request {RequestId}",
                offerings.RequestId);
        }

        void LogOfferingsObserved(MarketBoardCurrentOfferings offerings)
        {
            Log.Verbose(
                "Observed element of request {RequestId} with {NumListings} listings",
                offerings.RequestId,
                offerings.ItemListings.Count);
        }

        IObservable<MarketBoardCurrentOfferings> UntilBatchEnd(MarketBoardItemRequest request)
        {
            var totalPackets = Convert.ToInt32(Math.Ceiling((double)request.AmountToArrive / 10));
            if (totalPackets == 0)
            {
                return Observable.Empty<MarketBoardCurrentOfferings>();
            }

            return offeringsObservable
                   .Where(offerings => offerings.ItemListings.All(l => l.CatalogId == request.CatalogId))
                   .Skip(totalPackets - 1)
                   .Do(LogEndObserved);
        }

        // When a start packet is observed, begin observing a window of listings packets
        // according to the count described by the start packet. Aggregate the listings
        // packets, and then flatten them to the listings themselves.
        return offeringsObservable
               .Do(LogOfferingsObserved)
               .Window(start, UntilBatchEnd)
               .SelectMany(
                   o => o.Aggregate(
                       new List<MarketBoardCurrentOfferings.MarketBoardItemListing>(),
                       (agg, next) =>
                       {
                           agg.AddRange(next.ItemListings);
                           return agg;
                       }));
    }

    private IObservable<List<MarketBoardHistory.MarketBoardHistoryListing>> OnMarketBoardSalesBatch(
        IObservable<MarketBoardItemRequest> start)
    {
        var historyObservable = this.mbHistoryObservable.Publish().RefCount();

        void LogHistoryObserved(MarketBoardHistory history)
        {
            Log.Verbose(
                "Observed history for item {CatalogId} with {NumSales} sales",
                history.CatalogId,
                history.HistoryListings.Count);
        }

        IObservable<MarketBoardHistory> UntilBatchEnd(MarketBoardItemRequest request)
        {
            return historyObservable
                   .Where(history => history.CatalogId == request.CatalogId)
                   .Take(1);
        }

        // When a start packet is observed, begin observing a window of history packets.
        // We should only get one packet, which the window closing function ensures.
        // This packet is flattened to its sale entries and emitted.
        return historyObservable
               .Do(LogHistoryObserved)
               .Window(start, UntilBatchEnd)
               .SelectMany(
                   o => o.Aggregate(
                       new List<MarketBoardHistory.MarketBoardHistoryListing>(),
                       (agg, next) =>
                       {
                           agg.AddRange(next.HistoryListings);
                           return agg;
                       }));
    }

    private IDisposable HandleMarketBoardItemRequest()
    {
        void LogStartObserved(MarketBoardItemRequest request)
        {
            Log.Verbose(
                "Observed start of request for item#{CatalogId} with {NumListings} expected listings",
                request.CatalogId,
                request.AmountToArrive);
        }

        var startObservable = this.mbItemRequestObservable
                                  .Where(request => request.Ok).Do(LogStartObserved)
                                  .Publish()
                                  .RefCount();
        return Observable.When(
                             startObservable
                                 .And(this.OnMarketBoardSalesBatch(startObservable))
                                 .And(this.OnMarketBoardListingsBatch(startObservable))
                                 .Then((request, sales, listings) => (request, sales, listings)))
                         .Where(this.ShouldUpload)
                         .SubscribeOn(ThreadPoolScheduler.Instance)
                         .Subscribe(
                             data =>
                             {
                                 var (request, sales, listings) = data;
                                 this.UploadMarketBoardData(request, sales, listings);
                             },
                             ex => Log.Error(ex, "Failed to handle Market Board item request event"));
    }

    private void UploadMarketBoardData(
        MarketBoardItemRequest request,
        ICollection<MarketBoardHistory.MarketBoardHistoryListing> sales,
        ICollection<MarketBoardCurrentOfferings.MarketBoardItemListing> listings)
    {
        if (listings.Count != request.AmountToArrive)
        {
            Log.Error(
                "Wrong number of Market Board listings received for request: {ListingsCount} != {RequestAmountToArrive} item#{RequestCatalogId}",
                listings.Count, request.AmountToArrive, request.CatalogId);
            return;
        }

        if (listings.Any(listing => listing.CatalogId != request.CatalogId))
        {
            Log.Error("Received listings with mismatched item IDs for item#{RequestCatalogId}", request.CatalogId);
            return;
        }

        Log.Verbose(
            "Market Board request resolved, starting upload: item#{CatalogId} listings#{ListingsObserved} sales#{SalesObserved}",
            request.CatalogId,
            listings.Count,
            sales.Count);

        request.Listings.AddRange(listings);
        request.History.AddRange(sales);

        Task.Run(() => this.uploader.Upload(request))
            .ContinueWith(
                task => Log.Error(task.Exception, "Market Board offerings data upload failed"),
                TaskContinuationOptions.OnlyOnFaulted);
    }

    private IDisposable HandleMarketTaxRates()
    {
        return this.mbTaxesObservable
                   .Where(this.ShouldUpload)
                   .SubscribeOn(ThreadPoolScheduler.Instance)
                   .Subscribe(
                       taxes =>
                       {
                           Log.Verbose(
                               "MarketTaxRates: limsa#{0} grid#{1} uldah#{2} ish#{3} kugane#{4} cr#{5} sh#{6}",
                               taxes.LimsaLominsaTax,
                               taxes.GridaniaTax,
                               taxes.UldahTax,
                               taxes.IshgardTax,
                               taxes.KuganeTax,
                               taxes.CrystariumTax,
                               taxes.SharlayanTax);

                           Task.Run(() => this.uploader.UploadTax(taxes))
                               .ContinueWith(
                                   task => Log.Error(task.Exception, "Market Board tax data upload failed"),
                                   TaskContinuationOptions.OnlyOnFaulted);
                       },
                       ex => Log.Error(ex, "Failed to handle Market Board tax data event"));
    }

    private IDisposable HandleMarketBoardPurchaseHandler()
    {
        return this.mbPurchaseSentObservable
                   .Zip(this.mbPurchaseObservable)
                   .Where(this.ShouldUpload)
                   .SubscribeOn(ThreadPoolScheduler.Instance)
                   .Subscribe(
                       data =>
                       {
                           var (handler, purchase) = data;

                           var sameQty = purchase.ItemQuantity == handler.ItemQuantity;
                           var itemMatch = purchase.CatalogId == handler.CatalogId;
                           var itemMatchHq = purchase.CatalogId == handler.CatalogId + 1_000_000;

                           // Transaction succeeded
                           if (sameQty && (itemMatch || itemMatchHq))
                           {
                               Log.Verbose(
                                   "Bought {PurchaseItemQuantity}x {HandlerCatalogId} for {HandlerPricePerUnit} gils, listing id is {HandlerListingId}",
                                   purchase.ItemQuantity,
                                   handler.CatalogId,
                                   handler.PricePerUnit * purchase.ItemQuantity,
                                   handler.ListingId);
                               Task.Run(() => this.uploader.UploadPurchase(handler))
                                   .ContinueWith(
                                       task => Log.Error(task.Exception, "Market Board purchase data upload failed"),
                                       TaskContinuationOptions.OnlyOnFaulted);
                           }
                       },
                       ex => Log.Error(ex, "Failed to handle Market Board purchase event"));
    }

    private bool ShouldUpload<T>(T any)
    {
        return this.configuration.IsMbCollect;
    }

    private nint MarketPurchasePacketDetour(nint a1, nint packetData)
    {
        try
        {
            this.MarketBoardPurchaseReceived?.InvokeSafely(packetData);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "MarketPurchasePacketHandler threw an exception");
        }
        
        return this.mbPurchaseHook.OriginalDisposeSafe(a1, packetData);
    }

    private nint MarketHistoryPacketDetour(nint a1, nint packetData, uint a3, char a4)
    {
        try
        {
            this.MarketBoardHistoryReceived?.InvokeSafely(packetData);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "MarketHistoryPacketDetour threw an exception");
        }
        
        return this.mbHistoryHook.OriginalDisposeSafe(a1, packetData, a3, a4);
    }

    private void CustomTalkReceiveResponseDetour(nuint a1, ushort eventId, byte responseId, uint* args, byte argCount)
    {
        try
        {
            if (eventId == 7 && responseId == 8)
                this.MarketBoardTaxesReceived?.InvokeSafely((nint)args);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "CustomTalkReceiveResponseDetour threw an exception");
        }

        this.customTalkHook.OriginalDisposeSafe(a1, eventId, responseId, args, argCount);
    }

    private nint MarketItemRequestStartDetour(nint a1, nint packetRef)
    {
        try
        {
            this.MarketBoardItemRequestStartReceived?.InvokeSafely(packetRef);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "MarketItemRequestStartDetour threw an exception");
        }
        
        return this.mbItemRequestStartHook.OriginalDisposeSafe(a1, packetRef);
    }

    private byte MarketBoardOfferingsDetour(nint a1, nint packetRef)
    {
        try
        {
            this.MarketBoardOfferingsReceived?.InvokeSafely(packetRef);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "MarketBoardOfferingsDetour threw an exception");
        }
        
        return this.mbOfferingsHook.OriginalDisposeSafe(a1, packetRef);
    }

    private byte MarketBoardSendPurchaseRequestDetour(InfoProxyItemSearch* infoProxyItemSearch)
    {
        try
        {
            this.MarketBoardPurchaseRequestSent?.InvokeSafely((nint)infoProxyItemSearch + 0x5680);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "MarketBoardSendPurchaseRequestDetour threw an exception");
        }
        
        return this.mbSendPurchaseRequestHook.OriginalDisposeSafe(infoProxyItemSearch);
    }
}
