using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Dalamud.Configuration.Internal;
using Dalamud.Data;
using Dalamud.Game.Gui;
using Dalamud.Game.Network.Internal.MarketBoardUploaders;
using Dalamud.Game.Network.Internal.MarketBoardUploaders.Universalis;
using Dalamud.Game.Network.Structures;
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

    private readonly List<MarketBoardItemRequest> marketBoardRequests;
    private readonly ISubject<NetworkMessage> messages;

    private readonly IDisposable handleMarketBoardItemRequest;
    private readonly IDisposable handleMarketBoardOfferings;
    private readonly IDisposable handleMarketBoardHistory;
    private readonly IDisposable handleMarketTaxRates;
    private readonly IDisposable handleMarketBoardPurchaseHandler;
    private readonly IDisposable handleMarketBoardPurchase;
    private readonly IDisposable handleCfPop;

    [ServiceManager.ServiceDependency]
    private readonly DalamudConfiguration configuration = Service<DalamudConfiguration>.Get();

    private MarketBoardPurchaseHandler? marketBoardPurchaseHandler;

    private bool disposing;

    [ServiceManager.ServiceConstructor]
    private NetworkHandlers(GameNetwork gameNetwork)
    {
        this.uploader = new UniversalisMarketBoardUploader();
        this.marketBoardRequests = new List<MarketBoardItemRequest>();
        this.CfPop = (_, _) => { };

        this.messages = new Subject<NetworkMessage>();

        this.handleMarketBoardItemRequest = this.HandleMarketBoardItemRequest();
        this.handleMarketBoardOfferings = this.HandleMarketBoardOfferings();
        this.handleMarketBoardHistory = this.HandleMarketBoardHistory();
        this.handleMarketTaxRates = this.HandleMarketTaxRates();
        this.handleMarketBoardPurchaseHandler = this.HandleMarketBoardPurchaseHandler();
        this.handleMarketBoardPurchase = this.HandleMarketBoardPurchase();
        this.handleCfPop = this.HandleCfPop();

        gameNetwork.NetworkMessage += this.ObserveNetworkMessage;
    }

    /// <summary>
    /// Event which gets fired when a duty is ready.
    /// </summary>
    public event EventHandler<ContentFinderCondition> CfPop;

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
        this.handleMarketBoardOfferings.Dispose();
        this.handleMarketBoardHistory.Dispose();
        this.handleMarketTaxRates.Dispose();
        this.handleMarketBoardPurchaseHandler.Dispose();
        this.handleMarketBoardPurchase.Dispose();
        this.handleCfPop.Dispose();
    }

    private void ObserveNetworkMessage(
        IntPtr dataPtr, ushort opCode, uint sourceActorId, uint targetActorId, NetworkMessageDirection direction)
    {
        var dataManager = Service<DataManager>.GetNullable();
        this.messages.OnNext(new NetworkMessage
        {
            DataManager = dataManager,
            Data = dataPtr,
            Opcode = opCode,
            SourceActorId = sourceActorId,
            TargetActorId = targetActorId,
            Direction = direction,
        });
    }

    private IObservable<NetworkMessage> OnNetworkMessage()
    {
        return this.messages.Where(message => message.DataManager?.IsDataReady == true);
    }

    private IObservable<MarketBoardItemRequest> OnMarketBoardItemRequest()
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

    private IDisposable HandleMarketBoardItemRequest()
    {
        return this.OnMarketBoardItemRequest()
                   .Where(_ => this.configuration.IsMbCollect)
                   .Subscribe(request =>
                   {
                       this.marketBoardRequests.Add(request);
                       Log.Verbose($"NEW MB REQUEST START: item#{request.CatalogId} amount#{request.AmountToArrive}");
                   });
    }

    private IDisposable HandleMarketBoardOfferings()
    {
        return this.OnMarketBoardOfferings()
                   .Where(_ => this.configuration.IsMbCollect)
                   .Subscribe(listing =>
                   {
                       var request =
                           this.marketBoardRequests.LastOrDefault(
                               r => r.CatalogId == listing.ItemListings[0].CatalogId && !r.IsDone);

                       if (request == default)
                       {
                           Log.Error(
                               $"Market Board data arrived without a corresponding request: item#{listing.ItemListings[0].CatalogId}");
                           return;
                       }

                       if (request.Listings.Count + listing.ItemListings.Count > request.AmountToArrive)
                       {
                           Log.Error(
                               $"Too many Market Board listings received for request: {request.Listings.Count + listing.ItemListings.Count} > {request.AmountToArrive} item#{listing.ItemListings[0].CatalogId}");
                           return;
                       }

                       if (request.ListingsRequestId != -1 && request.ListingsRequestId != listing.RequestId)
                       {
                           Log.Error(
                               $"Non-matching RequestIds for Market Board data request: {request.ListingsRequestId}, {listing.RequestId}");
                           return;
                       }

                       if (request.ListingsRequestId == -1 && request.Listings.Count > 0)
                       {
                           Log.Error(
                               $"Market Board data request sequence break: {request.ListingsRequestId}, {request.Listings.Count}");
                           return;
                       }

                       if (request.ListingsRequestId == -1)
                       {
                           request.ListingsRequestId = listing.RequestId;
                           Log.Verbose($"First Market Board packet in sequence: {listing.RequestId}");
                       }

                       request.Listings.AddRange(listing.ItemListings);

                       Log.Verbose(
                           "Added {0} ItemListings to request#{1}, now {2}/{3}, item#{4}",
                           listing.ItemListings.Count,
                           request.ListingsRequestId,
                           request.Listings.Count,
                           request.AmountToArrive,
                           request.CatalogId);

                       if (request.IsDone)
                       {
                           Log.Verbose(
                               "Market Board request finished, starting upload: request#{0} item#{1} amount#{2}",
                               request.ListingsRequestId,
                               request.CatalogId,
                               request.AmountToArrive);

                           Task.Run(() => this.uploader.Upload(request))
                               .ContinueWith(
                                   task => Log.Error(task.Exception, "Market Board offerings data upload failed."),
                                   TaskContinuationOptions.OnlyOnFaulted);
                       }
                   });
    }

    private IDisposable HandleMarketBoardHistory()
    {
        return this.OnMarketBoardHistory()
                   .Where(_ => this.configuration.IsMbCollect)
                   .Subscribe(listing =>
                   {
                       var request = this.marketBoardRequests.LastOrDefault(r => r.CatalogId == listing.CatalogId);

                       if (request == default)
                       {
                           Log.Error(
                               $"Market Board data arrived without a corresponding request: item#{listing.CatalogId}");
                           return;
                       }

                       if (request.ListingsRequestId != -1)
                       {
                           Log.Error(
                               $"Market Board data history sequence break: {request.ListingsRequestId}, {request.Listings.Count}");
                           return;
                       }

                       request.History.AddRange(listing.HistoryListings);

                       Log.Verbose("Added history for item#{0}", listing.CatalogId);

                       if (request.AmountToArrive == 0)
                       {
                           Log.Verbose("Request had 0 amount, uploading now");

                           Task.Run(() => this.uploader.Upload(request))
                               .ContinueWith(
                                   (task) => Log.Error(task.Exception, "Market Board history data upload failed."),
                                   TaskContinuationOptions.OnlyOnFaulted);
                       }
                   });
    }

    private IDisposable HandleMarketTaxRates()
    {
        return this.OnMarketTaxRates()
                   .Where(_ => this.configuration.IsMbCollect)
                   .Subscribe(taxes =>
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
                               task => Log.Error(task.Exception, "Market Board tax data upload failed."),
                               TaskContinuationOptions.OnlyOnFaulted);
                   });
    }

    private IDisposable HandleMarketBoardPurchaseHandler()
    {
        return this.OnMarketBoardPurchaseHandler()
                   .Where(_ => this.configuration.IsMbCollect)
                   .Subscribe(handler => { this.marketBoardPurchaseHandler = handler; });
    }

    private IDisposable HandleMarketBoardPurchase()
    {
        return this.OnMarketBoardPurchase()
                   .Where(_ => this.configuration.IsMbCollect)
                   .Subscribe(purchase =>
                   {
                       if (this.marketBoardPurchaseHandler == null)
                           return;

                       var sameQty = purchase.ItemQuantity == this.marketBoardPurchaseHandler.ItemQuantity;
                       var itemMatch = purchase.CatalogId == this.marketBoardPurchaseHandler.CatalogId;
                       var itemMatchHq = purchase.CatalogId == this.marketBoardPurchaseHandler.CatalogId + 1_000_000;

                       // Transaction succeeded
                       if (sameQty && (itemMatch || itemMatchHq))
                       {
                           Log.Verbose(
                               $"Bought {purchase.ItemQuantity}x {this.marketBoardPurchaseHandler.CatalogId} for {this.marketBoardPurchaseHandler.PricePerUnit * purchase.ItemQuantity} gils, listing id is {this.marketBoardPurchaseHandler.ListingId}");

                           var handler =
                               this.marketBoardPurchaseHandler; // Capture the object so that we don't pass in a null one when the task starts.

                           Task.Run(() => this.uploader.UploadPurchase(handler))
                               .ContinueWith(
                                   task => Log.Error(task.Exception, "Market Board purchase data upload failed."),
                                   TaskContinuationOptions.OnlyOnFaulted);
                       }

                       this.marketBoardPurchaseHandler = null;
                   });
    }

    private unsafe IDisposable HandleCfPop()
    {
        return this.OnCfNotifyPop()
                   .Subscribe(message =>
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
                           Log.Error($"CFC key {conditionId} not in Lumina data.");
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

                           this.CfPop.InvokeSafely(this, cfCondition);
                       }).ContinueWith(
                           task => Log.Error(task.Exception, "CfPop.Invoke failed."),
                           TaskContinuationOptions.OnlyOnFaulted);
                   });
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
