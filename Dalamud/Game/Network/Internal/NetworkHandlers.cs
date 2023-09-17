using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Dalamud.Configuration.Internal;
using Dalamud.Data;
using Dalamud.Game.Gui;
using Dalamud.Game.Network.Internal.MarketBoardUploaders;
using Dalamud.Game.Network.Internal.MarketBoardUploaders.Universalis;
using Dalamud.Game.Network.Structures;
using Dalamud.Hooking;
using Dalamud.Utility;
using Lumina.Excel.GeneratedSheets;
using Serilog;

namespace Dalamud.Game.Network.Internal;

/// <summary>
/// This class handles network notifications and uploading market board data.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal class NetworkHandlers : IDisposable, IServiceType
{
    private readonly IMarketBoardUploader uploader;

    private readonly IObservable<NetworkMessage> messages;

    private readonly IDisposable handleMarketBoardItemRequest;
    private readonly IDisposable handleMarketTaxRates;
    private readonly IDisposable handleMarketBoardPurchaseHandler;
    
    private delegate nint CfPopDelegate(nint packetData);

    private readonly NetworkHandlersAddressResolver addressResolver;

    private readonly Hook<CfPopDelegate> cfPopHook;

    [ServiceManager.ServiceDependency]
    private readonly DalamudConfiguration configuration = Service<DalamudConfiguration>.Get();

    private bool disposing;

    [ServiceManager.ServiceConstructor]
    private NetworkHandlers(GameNetwork gameNetwork, SigScanner sigScanner)
    {
        this.uploader = new UniversalisMarketBoardUploader();

        this.addressResolver = new NetworkHandlersAddressResolver();
        this.addressResolver.Setup(sigScanner);

        this.CfPop = _ => { };

        this.messages = Observable.Create<NetworkMessage>(observer =>
        {
            void Observe(IntPtr dataPtr, ushort opCode, uint sourceActorId, uint targetActorId, NetworkMessageDirection direction)
            {
                var dataManager = Service<DataManager>.GetNullable();
                observer.OnNext(new NetworkMessage
                {
                    DataManager = dataManager,
                    Data = dataPtr,
                    Opcode = opCode,
                    SourceActorId = sourceActorId,
                    TargetActorId = targetActorId,
                    Direction = direction,
                });
            }

            gameNetwork.NetworkMessage += Observe;
            return () => { gameNetwork.NetworkMessage -= Observe; };
        });

        this.handleMarketBoardItemRequest = this.HandleMarketBoardItemRequest();
        this.handleMarketTaxRates = this.HandleMarketTaxRates();
        this.handleMarketBoardPurchaseHandler = this.HandleMarketBoardPurchaseHandler();

        this.cfPopHook = Hook<CfPopDelegate>.FromAddress(this.addressResolver.CfPopPacketHandler, this.CfPopDetour);
        this.cfPopHook.Enable();
    }

    /// <summary>
    /// Event which gets fired when a duty is ready.
    /// </summary>
    public event Action<ContentFinderCondition> CfPop;

    /// <summary>
    /// Disposes of managed and unmanaged resources.
    /// </summary>
    public void Dispose()
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

        this.cfPopHook.Dispose();
    }

    private unsafe nint CfPopDetour(nint packetData)
    {
        using var stream = new UnmanagedMemoryStream((byte*)packetData, 64);
        using var reader = new BinaryReader(stream);

        var notifyType = reader.ReadByte();
        stream.Position += 0x1B;
        var conditionId = reader.ReadUInt16();

        if (notifyType != 3)
            goto ORIGINAL;

        if (this.configuration.DutyFinderTaskbarFlash)
            Util.FlashWindow();

        var cfConditionSheet = Service<DataManager>.Get().GetExcelSheet<ContentFinderCondition>()!;
        var cfCondition = cfConditionSheet.GetRow(conditionId);

        if (cfCondition == null)
        {
            Log.Error("CFC key {ConditionId} not in Lumina data", conditionId);
            goto ORIGINAL;
        }

        var cfcName = cfCondition.Name.ToString();
        if (cfcName.IsNullOrEmpty())
        {
            cfcName = "Duty Roulette";
            cfCondition.Image = 112324;
        }

        Task.Run(() =>
        {
            if (this.configuration.DutyFinderChatMessage)
            {
                Service<ChatGui>.GetNullable()?.Print($"Duty pop: {cfcName}");
            }

            this.CfPop.InvokeSafely(this, cfCondition);
        }).ContinueWith(
            task => Log.Error(task.Exception, "CfPop.Invoke failed"),
            TaskContinuationOptions.OnlyOnFaulted);

        ORIGINAL:
        return this.cfPopHook.OriginalDisposeSafe(packetData);
    }

    private IObservable<NetworkMessage> OnNetworkMessage()
    {
        return this.messages.Where(message => message.DataManager?.IsDataReady == true);
    }

    private IObservable<MarketBoardItemRequest> OnMarketBoardItemRequestStart()
    {
        return this.OnNetworkMessage()
                   .Where(message => message.Direction == NetworkMessageDirection.ZoneDown)
                   .Where(message => message.Opcode ==
                                     message.DataManager?.ServerOpCodes["MarketBoardItemRequestStart"])
                   .Select(message => MarketBoardItemRequest.Read(message.Data));
    }

    private IObservable<MarketBoardCurrentOfferings> OnMarketBoardOfferings()
    {
        return this.OnNetworkMessage()
                   .Where(message => message.Direction == NetworkMessageDirection.ZoneDown)
                   .Where(message => message.Opcode == message.DataManager?.ServerOpCodes["MarketBoardOfferings"])
                   .Select(message => MarketBoardCurrentOfferings.Read(message.Data));
    }

    private IObservable<MarketBoardHistory> OnMarketBoardHistory()
    {
        return this.OnNetworkMessage()
                   .Where(message => message.Direction == NetworkMessageDirection.ZoneDown)
                   .Where(message => message.Opcode == message.DataManager?.ServerOpCodes["MarketBoardHistory"])
                   .Select(message => MarketBoardHistory.Read(message.Data));
    }

    private IObservable<MarketTaxRates> OnMarketTaxRates()
    {
        return this.OnNetworkMessage()
                   .Where(message => message.Direction == NetworkMessageDirection.ZoneDown)
                   .Where(message => message.Opcode == message.DataManager?.ServerOpCodes["MarketTaxRates"])
                   .Where(message =>
                   {
                       // Only some categories of the result dialog packet contain market tax rates
                       var category = (uint)Marshal.ReadInt32(message.Data);
                       return category == 720905;
                   })
                   .Select(message => MarketTaxRates.Read(message.Data))
                   .Where(taxes => taxes.Category == 0xb0009);
    }

    private IObservable<MarketBoardPurchaseHandler> OnMarketBoardPurchaseHandler()
    {
        return this.OnNetworkMessage()
                   .Where(message => message.Direction == NetworkMessageDirection.ZoneUp)
                   .Where(message => message.Opcode == message.DataManager?.ClientOpCodes["MarketBoardPurchaseHandler"])
                   .Select(message => MarketBoardPurchaseHandler.Read(message.Data));
    }

    private IObservable<MarketBoardPurchase> OnMarketBoardPurchase()
    {
        return this.OnNetworkMessage()
                   .Where(message => message.Direction == NetworkMessageDirection.ZoneDown)
                   .Where(message => message.Opcode == message.DataManager?.ServerOpCodes["MarketBoardPurchase"])
                   .Select(message => MarketBoardPurchase.Read(message.Data));
    }

    private IObservable<NetworkMessage> OnCfNotifyPop()
    {
        return this.OnNetworkMessage()
                   .Where(message => message.Direction == NetworkMessageDirection.ZoneDown)
                   .Where(message => message.Opcode == message.DataManager?.ServerOpCodes["CfNotifyPop"]);
    }

    private IObservable<List<MarketBoardCurrentOfferings.MarketBoardItemListing>> OnMarketBoardListingsBatch(
        IObservable<MarketBoardItemRequest> start)
    {
        var offeringsObservable = this.OnMarketBoardOfferings().Publish().RefCount();

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
        var historyObservable = this.OnMarketBoardHistory().Publish().RefCount();

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

        var startObservable = this.OnMarketBoardItemRequestStart()
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
            Log.Error("Wrong number of Market Board listings received for request: {ListingsCount} != {RequestAmountToArrive} item#{RequestCatalogId}", listings.Count, request.AmountToArrive, request.CatalogId);
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
        return this.OnMarketTaxRates()
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
        return this.OnMarketBoardPurchaseHandler()
                   .Zip(this.OnMarketBoardPurchase())
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

    private unsafe IDisposable HandleCfPop()
    {
        return this.OnCfNotifyPop()
                   .SubscribeOn(ThreadPoolScheduler.Instance)
                   .Subscribe(
                       message =>
                       {
                           using var stream = new UnmanagedMemoryStream((byte*)message.Data.ToPointer(), 64);
                           using var reader = new BinaryReader(stream);

                           var notifyType = reader.ReadByte();
                           stream.Position += 0x1B;
                           var conditionId = reader.ReadUInt16();

                           if (notifyType != 3)
                               return;

                           var cfConditionSheet = message.DataManager!.GetExcelSheet<ContentFinderCondition>()!;
                           var cfCondition = cfConditionSheet.GetRow(conditionId);

                           if (cfCondition == null)
                           {
                               Log.Error("CFC key {ConditionId} not in Lumina data", conditionId);
                               return;
                           }

                           var cfcName = cfCondition.Name.ToString();
                           if (cfcName.IsNullOrEmpty())
                           {
                               cfcName = "Duty Roulette";
                               cfCondition.Image = 112324;
                           }

                           // Flash window
                           if (this.configuration.DutyFinderTaskbarFlash && !NativeFunctions.ApplicationIsActivated())
                           {
                               var flashInfo = new NativeFunctions.FlashWindowInfo
                               {
                                   Size = (uint)Marshal.SizeOf<NativeFunctions.FlashWindowInfo>(),
                                   Count = uint.MaxValue,
                                   Timeout = 0,
                                   Flags = NativeFunctions.FlashWindow.All | NativeFunctions.FlashWindow.TimerNoFG,
                                   Hwnd = Process.GetCurrentProcess().MainWindowHandle,
                               };
                               NativeFunctions.FlashWindowEx(ref flashInfo);
                           }

                           Task.Run(() =>
                           {
                               if (this.configuration.DutyFinderChatMessage)
                               {
                                   Service<ChatGui>.GetNullable()?.Print($"Duty pop: {cfcName}");
                               }

                               this.CfPop.InvokeSafely(cfCondition);
                           }).ContinueWith(
                               task => Log.Error(task.Exception, "CfPop.Invoke failed"),
                               TaskContinuationOptions.OnlyOnFaulted);
                       },
                       ex => Log.Error(ex, "Failed to handle Market Board purchase event"));
    }

    private bool ShouldUpload<T>(T any)
    {
        return this.configuration.IsMbCollect;
    }

    private class NetworkMessage
    {
        public DataManager? DataManager { get; init; }

        public IntPtr Data { get; init; }

        public ushort Opcode { get; init; }

        public uint SourceActorId { get; init; }

        public uint TargetActorId { get; init; }

        public NetworkMessageDirection Direction { get; init; }
    }
}
