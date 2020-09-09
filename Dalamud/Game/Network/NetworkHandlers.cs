using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Dalamud.Game.Internal.Network;
using Dalamud.Game.Network.MarketBoardUploaders;
using Dalamud.Game.Network.Structures;
using Dalamud.Game.Network.Universalis.MarketBoardUploaders;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Dalamud.Game.Network {
    public class NetworkHandlers {
        private readonly Dalamud dalamud;

        private readonly List<MarketBoardItemRequest> marketBoardRequests = new List<MarketBoardItemRequest>();

        private readonly bool optOutMbUploads;
        private readonly IMarketBoardUploader uploader;

        public delegate Task CfPop(ContentFinderCondition cfc);
        public event CfPop ProcessCfPop;

        public NetworkHandlers(Dalamud dalamud, bool optOutMbUploads) {
            this.dalamud = dalamud;
            this.optOutMbUploads = optOutMbUploads;

            this.uploader = new UniversalisMarketBoardUploader(dalamud);

            dalamud.Framework.Network.OnNetworkMessage += OnNetworkMessage;

        }

        private void OnNetworkMessage(IntPtr dataPtr, ushort opCode, uint sourceActorId, uint targetActorId, NetworkMessageDirection direction) {
            if (direction != NetworkMessageDirection.ZoneDown)
                return;

            if (!this.dalamud.Data.IsDataReady)
                return;

            if (opCode == this.dalamud.Data.ServerOpCodes["CfNotifyPop"]) {
                var data = new byte[64];
                Marshal.Copy(dataPtr, data, 0, 64);

                var notifyType = data[0];
                var contentFinderConditionId = BitConverter.ToUInt16(data, 0x14);

                if (notifyType != 3)
                    return;

                var contentFinderCondition = this.dalamud.Data.GetExcelSheet<ContentFinderCondition>().GetRow(contentFinderConditionId);

                if (contentFinderCondition == null)
                {
                    Log.Error("CFC key {0} not in lumina data.", contentFinderConditionId);
                    return;
                }

                if (string.IsNullOrEmpty(contentFinderCondition.Name)) {
                    contentFinderCondition.Name = "Duty Roulette";
                    contentFinderCondition.Image = 112324;
                }

                if (this.dalamud.Configuration.DutyFinderTaskbarFlash && !NativeFunctions.ApplicationIsActivated()) {
                    var flashInfo = new NativeFunctions.FLASHWINFO
                    {
                        cbSize = (uint)Marshal.SizeOf<NativeFunctions.FLASHWINFO>(),
                        uCount = uint.MaxValue,
                        dwTimeout = 0,
                        dwFlags = NativeFunctions.FlashWindow.FLASHW_ALL |
                                        NativeFunctions.FlashWindow.FLASHW_TIMERNOFG,
                        hwnd = Process.GetCurrentProcess().MainWindowHandle
                    };
                    NativeFunctions.FlashWindowEx(ref flashInfo);
                }

                Task.Run(async () => {
                    if(this.dalamud.Configuration.DutyFinderChatMessage)
                        this.dalamud.Framework.Gui.Chat.Print("Duty pop: " + contentFinderCondition.Name);

                    await this.ProcessCfPop?.Invoke(contentFinderCondition);
                });

                return;
            }

            if (!this.optOutMbUploads) {
                if (opCode == this.dalamud.Data.ServerOpCodes["MarketBoardItemRequestStart"]) {
                    var catalogId = (uint) Marshal.ReadInt32(dataPtr);
                    var amount = Marshal.ReadByte(dataPtr + 0xB);

                    this.marketBoardRequests.Add(new MarketBoardItemRequest {
                        CatalogId = catalogId,
                        AmountToArrive = amount,
                        Listings = new List<MarketBoardCurrentOfferings.MarketBoardItemListing>(),
                        History = new List<MarketBoardHistory.MarketBoardHistoryListing>()
                    });

                    Log.Verbose($"NEW MB REQUEST START: item#{catalogId} amount#{amount}");
                    return;
                }

                if (opCode == this.dalamud.Data.ServerOpCodes["MarketBoardOfferings"]) {
                    var listing = MarketBoardCurrentOfferings.Read(dataPtr);

                    var request =
                        this.marketBoardRequests.LastOrDefault(
                            r => r.CatalogId == listing.ItemListings[0].CatalogId && !r.IsDone);

                    if (request == null) {
                        Log.Error(
                            $"Market Board data arrived without a corresponding request: item#{listing.ItemListings[0].CatalogId}");
                        return;
                    }

                    if (request.Listings.Count + listing.ItemListings.Count > request.AmountToArrive) {
                        Log.Error(
                            $"Too many Market Board listings received for request: {request.Listings.Count + listing.ItemListings.Count} > {request.AmountToArrive} item#{listing.ItemListings[0].CatalogId}");
                        return;
                    }

                    if (request.ListingsRequestId != -1 && request.ListingsRequestId != listing.RequestId) {
                        Log.Error(
                            $"Non-matching RequestIds for Market Board data request: {request.ListingsRequestId}, {listing.RequestId}");
                        return;
                    }

                    if (request.ListingsRequestId == -1 && request.Listings.Count > 0) {
                        Log.Error(
                            $"Market Board data request sequence break: {request.ListingsRequestId}, {request.Listings.Count}");
                        return;
                    }

                    if (request.ListingsRequestId == -1) {
                        request.ListingsRequestId = listing.RequestId;
                        Log.Verbose($"First Market Board packet in sequence: {listing.RequestId}");
                    }

                    request.Listings.AddRange(listing.ItemListings);

                    Log.Verbose("Added {0} ItemListings to request#{1}, now {2}/{3}, item#{4}",
                                listing.ItemListings.Count, request.ListingsRequestId, request.Listings.Count,
                                request.AmountToArrive, request.CatalogId);

                    if (request.IsDone) {
                        Log.Verbose("Market Board request finished, starting upload: request#{0} item#{1} amount#{2}",
                                    request.ListingsRequestId, request.CatalogId, request.AmountToArrive);
                        try {
                            Task.Run(() => this.uploader.Upload(request));
                        } catch (Exception ex) {
                            Log.Error(ex, "Market Board data upload failed.");
                        }
                    }

                    return;
                }

                if (opCode == this.dalamud.Data.ServerOpCodes["MarketBoardHistory"]) {
                    var listing = MarketBoardHistory.Read(dataPtr);

                    var request = this.marketBoardRequests.LastOrDefault(r => r.CatalogId == listing.CatalogId);

                    if (request == null) {
                        Log.Error(
                            $"Market Board data arrived without a corresponding request: item#{listing.CatalogId}");
                        return;
                    }

                    if (request.ListingsRequestId != -1) {
                        Log.Error(
                            $"Market Board data history sequence break: {request.ListingsRequestId}, {request.Listings.Count}");
                        return;
                    }

                    request.History.AddRange(listing.HistoryListings);

                    Log.Verbose("Added history for item#{0}", listing.CatalogId);

                    if (request.AmountToArrive == 0) {
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

                if (opCode == this.dalamud.Data.ServerOpCodes["MarketTaxRates"])
                {
                    var taxes = MarketTaxRates.Read(dataPtr);

                    Log.Verbose("MarketTaxRates: limsa#{0} grid#{1} uldah#{2} ish#{3} kugane#{4} cr#{5}",
                                taxes.LimsaLominsaTax, taxes.GridaniaTax, taxes.UldahTax, taxes.IshgardTax, taxes.KuganeTax, taxes.CrystariumTax);
                    try
                    {
                        Task.Run(() => this.uploader.UploadTax(taxes));
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Market Board data upload failed.");
                    }
                }
            }
        }
    }
}
