using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
                    if (opCode == dataManager.ClientOpCodes["MarketBoardPurchaseHandler"])
                    {
                        this.marketBoardPurchaseHandler = MarketBoardPurchaseHandler.Read(dataPtr);
                        return;
                    }
                }

                return;
            }

            if (opCode == dataManager.ServerOpCodes["CfNotifyPop"])
            {
                this.HandleCfPop(dataPtr);
                return;
            }

            if (!this.optOutMbUploads)
            {
                if (opCode == dataManager.ServerOpCodes["MarketBoardItemRequestStart"])
                {
                    var data = MarketBoardItemRequest.Read(dataPtr);
                    this.marketBoardRequests.Add(data);

                    Log.Verbose($"NEW MB REQUEST START: item#{data.CatalogId} amount#{data.AmountToArrive}");
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

                        Task.Run(() => this.uploader.Upload(request))
                            .ContinueWith((task) => Log.Error(task.Exception, "Market Board offerings data upload failed."), TaskContinuationOptions.OnlyOnFaulted);
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

                        Task.Run(() => this.uploader.Upload(request))
                            .ContinueWith((task) => Log.Error(task.Exception, "Market Board history data upload failed."), TaskContinuationOptions.OnlyOnFaulted);
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

                    if (taxes.Category != 0xb0009)
                        return;

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
                        .ContinueWith((task) => Log.Error(task.Exception, "Market Board tax data upload failed."), TaskContinuationOptions.OnlyOnFaulted);

                    return;
                }

                if (opCode == dataManager.ServerOpCodes["MarketBoardPurchase"])
                {
                    if (this.marketBoardPurchaseHandler == null)
                        return;

                    var purchase = MarketBoardPurchase.Read(dataPtr);

                    var sameQty = purchase.ItemQuantity == this.marketBoardPurchaseHandler.ItemQuantity;
                    var itemMatch = purchase.CatalogId == this.marketBoardPurchaseHandler.CatalogId;
                    var itemMatchHq = purchase.CatalogId == this.marketBoardPurchaseHandler.CatalogId + 1_000_000;

                    // Transaction succeeded
                    if (sameQty && (itemMatch || itemMatchHq))
                    {
                        Log.Verbose($"Bought {purchase.ItemQuantity}x {this.marketBoardPurchaseHandler.CatalogId} for {this.marketBoardPurchaseHandler.PricePerUnit * purchase.ItemQuantity} gils, listing id is {this.marketBoardPurchaseHandler.ListingId}");

                        var handler = this.marketBoardPurchaseHandler; // Capture the object so that we don't pass in a null one when the task starts.

                        Task.Run(() => this.uploader.UploadPurchase(handler))
                            .ContinueWith((task) => Log.Error(task.Exception, "Market Board purchase data upload failed."), TaskContinuationOptions.OnlyOnFaulted);
                    }

                    this.marketBoardPurchaseHandler = null;
                    return;
                }
            }
        }

        private unsafe void HandleCfPop(IntPtr dataPtr)
        {
            var dataManager = Service<DataManager>.Get();
            var configuration = Service<DalamudConfiguration>.Get();

            using var stream = new UnmanagedMemoryStream((byte*)dataPtr.ToPointer(), 64);
            using var reader = new BinaryReader(stream);

            var notifyType = reader.ReadByte();
            stream.Position += 0x13;
            var conditionId = reader.ReadUInt16();

            if (notifyType != 3)
                return;

            var cfConditionSheet = dataManager.GetExcelSheet<ContentFinderCondition>()!;
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
                    Service<ChatGui>.Get().Print($"Duty pop: {cfcName}");
                }

                this.CfPop?.Invoke(this, cfCondition);
            }).ContinueWith((task) => Log.Error(task.Exception, "CfPop.Invoke failed."), TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}
