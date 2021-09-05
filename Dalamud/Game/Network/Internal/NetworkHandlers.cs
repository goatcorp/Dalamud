using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Dalamud.Configuration.Internal;
using Dalamud.Data;
using Dalamud.Game.Gui;
using Dalamud.Game.Network.Internal.MarketBoardUploaders;
using Dalamud.Game.Network.Internal.MarketBoardUploaders.Universalis;
using Dalamud.Game.Network.Structures;
using Lumina.Excel.GeneratedSheets;
using Serilog;

namespace Dalamud.Game.Network.Internal
{
    /// <summary>
    /// This class handles network notifications and uploading market board data.
    /// </summary>
    internal class NetworkHandlers
    {
        private readonly List<MarketBoardItemRequest> marketBoardRequests = new();

        private readonly bool optOutMbUploads;
        private readonly IMarketBoardUploader uploader;

        private MarketBoardPurchaseHandler marketBoardPurchaseHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkHandlers"/> class.
        /// </summary>
        public NetworkHandlers()
        {
            this.optOutMbUploads = Service<DalamudStartInfo>.Get().OptOutMbCollection;

            this.uploader = new UniversalisMarketBoardUploader();

            Service<GameNetwork>.Get().NetworkMessage += this.OnNetworkMessage;
        }

        /// <summary>
        /// Event which gets fired when a duty is ready.
        /// </summary>
        public event EventHandler<ContentFinderCondition> CfPop;

        private void OnNetworkMessage(IntPtr dataPtr, ushort opCode, uint sourceActorId, uint targetActorId, NetworkMessageDirection direction)
        {
            var dataManager = Service<DataManager>.GetNullable();

            if (dataManager?.IsDataReady == false)
                return;

            var configuration = Service<DalamudConfiguration>.Get();

            if (direction == NetworkMessageDirection.ZoneUp)
            {
                if (!this.optOutMbUploads)
                {
                    if (opCode == Service<DataManager>.Get().ClientOpCodes["MarketBoardPurchaseHandler"])
                    {
                        this.marketBoardPurchaseHandler = MarketBoardPurchaseHandler.Read(dataPtr);
                    }
                }

                return;
            }

            if (opCode == dataManager.ServerOpCodes["CfNotifyPop"])
            {
                var data = new byte[64];
                Marshal.Copy(dataPtr, data, 0, 64);

                var notifyType = data[0];
                var contentFinderConditionId = BitConverter.ToUInt16(data, 0x14);

                if (notifyType != 3)
                    return;

                var contentFinderCondition = dataManager.GetExcelSheet<ContentFinderCondition>().GetRow(contentFinderConditionId);

                if (contentFinderCondition == null)
                {
                    Log.Error("CFC key {0} not in lumina data.", contentFinderConditionId);
                    return;
                }

                var cfcName = contentFinderCondition.Name.ToString();
                if (string.IsNullOrEmpty(contentFinderCondition.Name))
                {
                    cfcName = "Duty Roulette";
                    contentFinderCondition.Image = 112324;
                }

                if (configuration.DutyFinderTaskbarFlash && !NativeFunctions.ApplicationIsActivated())
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
                    if (configuration.DutyFinderChatMessage)
                    {
                        Service<ChatGui>.Get().Print("Duty pop: " + cfcName);
                    }

                    this.CfPop?.Invoke(this, contentFinderCondition);
                });

                return;
            }

            if (!this.optOutMbUploads)
            {
                if (opCode == dataManager.ServerOpCodes["MarketBoardItemRequestStart"])
                {
                    var catalogId = (uint)Marshal.ReadInt32(dataPtr);
                    var amount = Marshal.ReadByte(dataPtr + 0xB);

                    this.marketBoardRequests.Add(new MarketBoardItemRequest
                    {
                        CatalogId = catalogId,
                        AmountToArrive = amount,
                        Listings = new List<MarketBoardCurrentOfferings.MarketBoardItemListing>(),
                        History = new List<MarketBoardHistory.MarketBoardHistoryListing>(),
                    });

                    Log.Verbose($"NEW MB REQUEST START: item#{catalogId} amount#{amount}");
                    return;
                }

                if (opCode == dataManager.ServerOpCodes["MarketBoardOfferings"])
                {
                    var listing = MarketBoardCurrentOfferings.Read(dataPtr);

                    var request = this.marketBoardRequests.LastOrDefault(r => r.CatalogId == listing.ItemListings[0].CatalogId && !r.IsDone);

                    if (request == default)
                    {
                        Log.Error($"Market Board data arrived without a corresponding request: item#{listing.ItemListings[0].CatalogId}");
                        return;
                    }

                    if (request.Listings.Count + listing.ItemListings.Count > request.AmountToArrive)
                    {
                        Log.Error($"Too many Market Board listings received for request: {request.Listings.Count + listing.ItemListings.Count} > {request.AmountToArrive} item#{listing.ItemListings[0].CatalogId}");
                        return;
                    }

                    if (request.ListingsRequestId != -1 && request.ListingsRequestId != listing.RequestId)
                    {
                        Log.Error($"Non-matching RequestIds for Market Board data request: {request.ListingsRequestId}, {listing.RequestId}");
                        return;
                    }

                    if (request.ListingsRequestId == -1 && request.Listings.Count > 0)
                    {
                        Log.Error($"Market Board data request sequence break: {request.ListingsRequestId}, {request.Listings.Count}");
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
                        try
                        {
                            Task.Run(() => this.uploader.Upload(request));
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Market Board data upload failed.");
                        }
                    }

                    return;
                }

                if (opCode == dataManager.ServerOpCodes["MarketBoardHistory"])
                {
                    var listing = MarketBoardHistory.Read(dataPtr);

                    var request = this.marketBoardRequests.LastOrDefault(r => r.CatalogId == listing.CatalogId);

                    if (request == default)
                    {
                        Log.Error($"Market Board data arrived without a corresponding request: item#{listing.CatalogId}");
                        return;
                    }

                    if (request.ListingsRequestId != -1)
                    {
                        Log.Error($"Market Board data history sequence break: {request.ListingsRequestId}, {request.Listings.Count}");
                        return;
                    }

                    request.History.AddRange(listing.HistoryListings);

                    Log.Verbose("Added history for item#{0}", listing.CatalogId);

                    if (request.AmountToArrive == 0)
                    {
                        Log.Verbose("Request had 0 amount, uploading now");

                        try
                        {
                            Task.Run(() => this.uploader.Upload(request));
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Market Board data upload failed.");
                        }
                    }
                }

                if (opCode == dataManager.ServerOpCodes["MarketTaxRates"])
                {
                    var category = (uint)Marshal.ReadInt32(dataPtr);

                    // Result dialog packet does not contain market tax rates
                    if (category != 720905)
                    {
                        return;
                    }

                    var taxes = MarketTaxRates.Read(dataPtr);

                    Log.Verbose(
                        "MarketTaxRates: limsa#{0} grid#{1} uldah#{2} ish#{3} kugane#{4} cr#{5}",
                        taxes.LimsaLominsaTax,
                        taxes.GridaniaTax,
                        taxes.UldahTax,
                        taxes.IshgardTax,
                        taxes.KuganeTax,
                        taxes.CrystariumTax);
                    try
                    {
                        Task.Run(() => this.uploader.UploadTax(taxes));
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Market Board data upload failed.");
                    }
                }

                if (opCode == dataManager.ServerOpCodes["MarketBoardPurchase"])
                {
                    if (this.marketBoardPurchaseHandler == null)
                        return;

                    var purchase = MarketBoardPurchase.Read(dataPtr);

                    // Transaction succeeded
                    if (purchase.ItemQuantity == this.marketBoardPurchaseHandler.ItemQuantity
                        && (purchase.CatalogId == this.marketBoardPurchaseHandler.CatalogId
                            || purchase.CatalogId == this.marketBoardPurchaseHandler.CatalogId + 1_000_000))
                    { // HQ
                        Log.Verbose($"Bought {purchase.ItemQuantity}x {this.marketBoardPurchaseHandler.CatalogId} for {this.marketBoardPurchaseHandler.PricePerUnit * purchase.ItemQuantity} gils, listing id is {this.marketBoardPurchaseHandler.ListingId}");
                        var handler = this.marketBoardPurchaseHandler; // Capture the object so that we don't pass in a null one when the task starts.
                        Task.Run(() => this.uploader.UploadPurchase(handler));
                    }

                    this.marketBoardPurchaseHandler = null;
                }
            }
        }
    }
}
