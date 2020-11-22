using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Dalamud.Game.Network.MarketBoardUploaders;
using Dalamud.Game.Network.Structures;
using Dalamud.Game.Network.Universalis.MarketBoardUploaders;
using Dalamud.Hooking;
using Lumina.Excel.GeneratedSheets;
using Serilog;

namespace Dalamud.Game.Network {
    public class NetworkHandlers : IDisposable {
        private readonly Dalamud dalamud;

        private readonly List<MarketBoardItemRequest> marketBoardRequests = new List<MarketBoardItemRequest>();

        private readonly bool optOutMbUploads;
        private readonly IMarketBoardUploader uploader;

        private delegate IntPtr OnCfNotifyPopDelegate(IntPtr dataPtr);
        private readonly Hook<OnCfNotifyPopDelegate> onCfNotifyPop;

        private delegate IntPtr OnMarketBoardRequestItemDelegate(IntPtr a1, IntPtr dataPtr);
        private readonly Hook<OnMarketBoardRequestItemDelegate> onMarketBoardRequestItem;

        private delegate IntPtr OnMarketBoardOfferingsDelegate(IntPtr a1, IntPtr dataPtr);
        private readonly Hook<OnMarketBoardOfferingsDelegate> onMarketBoardOfferings;

        private delegate IntPtr OnMarketBoardHistoryDelegate(IntPtr a1, IntPtr dataPtr);
        private readonly Hook<OnMarketBoardHistoryDelegate> onMarketBoardHistory;

        private delegate IntPtr OnMarketBoardTaxRatesDelegate(IntPtr a1, int a2, IntPtr dataPtr);
        private readonly Hook<OnMarketBoardTaxRatesDelegate> onMarketBoardTaxRates;

        public delegate Task CfPop(ContentFinderCondition cfc);
        public event CfPop ProcessCfPop;

        public NetworkHandlers(Dalamud dalamud, bool optOutMbUploads) {
            this.dalamud = dalamud;
            this.optOutMbUploads = optOutMbUploads;

            this.uploader = new UniversalisMarketBoardUploader(dalamud);

            var onCfNotifyPopPtr = dalamud.SigScanner.ScanText("48 89 5C 24 ?? 57 48 83 EC 60 48 8B D9 48 8D 0D ?? ?? ?? ??");
            if (onCfNotifyPopPtr == IntPtr.Zero) {
                Log.Error("Could not find OnCfNotifyPop pointer from signature");
            } else {
                this.onCfNotifyPop = new Hook<OnCfNotifyPopDelegate>(onCfNotifyPopPtr, new OnCfNotifyPopDelegate(this.OnCfNotifyPop));
                this.onCfNotifyPop.Enable();
            }

            var onMbReqItemPtr = dalamud.SigScanner.ScanText("48 89 5C 24 ?? 57 48 83 EC 40 48 8B 0D ?? ?? ?? ?? 48 8B DA E8 ?? ?? ?? ?? 48 8B F8");
            if (onMbReqItemPtr == IntPtr.Zero) {
                Log.Error("Could not find OnMarketBoardRequestItem pointer from signature");
            } else {
                this.onMarketBoardRequestItem = new Hook<OnMarketBoardRequestItemDelegate>(onMbReqItemPtr, new OnMarketBoardRequestItemDelegate(this.OnMarketBoardRequestItem));
                this.onMarketBoardRequestItem.Enable();
            }

            var onMbOfferingsPtr = dalamud.SigScanner.ScanText("40 53 48 83 EC 20 48 8B 0D ?? ?? ?? ?? 48 8B DA E8 ?? ?? ?? ?? 48 85 C0 74 ?? 4C 8B 00 48 8B C8 41 FF 90 ?? ?? ?? ?? 48 8B C8 BA 09 00 00 00 E8 ?? ?? ?? ?? 48 85 C0 74 ?? 4C 8B 00");
            if (onMbOfferingsPtr == IntPtr.Zero) {
                Log.Error("Could not find OnMarketBoardOfferings pointer from signature");
            } else {
                this.onMarketBoardOfferings = new Hook<OnMarketBoardOfferingsDelegate>(onMbOfferingsPtr, new OnMarketBoardOfferingsDelegate(this.OnMarketBoardOfferings));
                this.onMarketBoardOfferings.Enable();
            }

            // there is not a unique signature with wildcards for this function
            var onMbHistoryPtr = dalamud.SigScanner.ScanText("40 53 48 83 EC 20 48 8B 0D CB 2A B9 00");
            if (onMbHistoryPtr == IntPtr.Zero) {
                Log.Error("Could not find OnMarketBoardHistory pointer from signature");
            } else {
                this.onMarketBoardHistory = new Hook<OnMarketBoardHistoryDelegate>(onMbHistoryPtr, new OnMarketBoardHistoryDelegate(this.OnMarketBoardHistory));
                this.onMarketBoardHistory.Enable();
            }

            var onMbTaxRatesPtr = dalamud.SigScanner.ScanText("48 89 5C 24 ?? 57 48 83 EC 20 49 8B F8 48 8B D9 49 83 C0 08 E8 ?? ?? ?? ?? 0F B6 07 84 C0 74 ?? 44 0F B6 C0 48 8D 8B ?? ?? ?? ?? B2 04");
            if (onMbTaxRatesPtr == IntPtr.Zero) {
                Log.Error("Could not find OnMarketBoardTaxRates pointer from signature");
            } else {
                this.onMarketBoardTaxRates = new Hook<OnMarketBoardTaxRatesDelegate>(onMbTaxRatesPtr, new OnMarketBoardTaxRatesDelegate(this.OnMarketBoardTaxRates));
                this.onMarketBoardTaxRates.Enable();
            }
        }

        private IntPtr OnCfNotifyPop(IntPtr dataPtr) {
            if (!this.dalamud.Data.IsDataReady)
                goto Return;

            var data = new byte[64];
            Marshal.Copy(dataPtr, data, 0, 64);

            var notifyType = data[0];
            var contentFinderConditionId = BitConverter.ToUInt16(data, 0x14);

            if (notifyType != 3)
                goto Return;

            var contentFinderCondition = this.dalamud.Data.GetExcelSheet<ContentFinderCondition>().GetRow(contentFinderConditionId);

            if (contentFinderCondition == null) {
                Log.Error("CFC key {0} not in lumina data.", contentFinderConditionId);
                goto Return;
            }

            if (string.IsNullOrEmpty(contentFinderCondition.Name)) {
                contentFinderCondition.Name = "Duty Roulette";
                contentFinderCondition.Image = 112324;
            }

            if (this.dalamud.Configuration.DutyFinderTaskbarFlash && !NativeFunctions.ApplicationIsActivated()) {
                var flashInfo = new NativeFunctions.FLASHWINFO {
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
                if (this.dalamud.Configuration.DutyFinderChatMessage)
                    this.dalamud.Framework.Gui.Chat.Print("Duty pop: " + contentFinderCondition.Name);

                await this.ProcessCfPop?.Invoke(contentFinderCondition);
            });

        Return:
            return this.onCfNotifyPop.Original(dataPtr);
        }

        private IntPtr OnMarketBoardRequestItem(IntPtr a1, IntPtr dataPtr) {
            if (!this.dalamud.Data.IsDataReady || this.optOutMbUploads)
                goto Return;

            var catalogId = (uint)Marshal.ReadInt32(dataPtr);
            var amount = Marshal.ReadByte(dataPtr + 0xB);

            this.marketBoardRequests.Add(new MarketBoardItemRequest {
                CatalogId = catalogId,
                AmountToArrive = amount,
                Listings = new List<MarketBoardCurrentOfferings.MarketBoardItemListing>(),
                History = new List<MarketBoardHistory.MarketBoardHistoryListing>()
            });

            Log.Verbose($"NEW MB REQUEST START: item#{catalogId} amount#{amount}");

        Return:
            return this.onMarketBoardRequestItem.Original(a1, dataPtr);
        }

        private IntPtr OnMarketBoardOfferings(IntPtr a1, IntPtr dataPtr) {
            if (!this.dalamud.Data.IsDataReady || this.optOutMbUploads)
                goto Return;

            var listing = MarketBoardCurrentOfferings.Read(dataPtr);

            var request =
                this.marketBoardRequests.LastOrDefault(
                    r => r.CatalogId == listing.ItemListings[0].CatalogId && !r.IsDone);

            if (request == null) {
                Log.Error(
                    $"Market Board data arrived without a corresponding request: item#{listing.ItemListings[0].CatalogId}");
                goto Return;
            }

            if (request.Listings.Count + listing.ItemListings.Count > request.AmountToArrive) {
                Log.Error(
                    $"Too many Market Board listings received for request: {request.Listings.Count + listing.ItemListings.Count} > {request.AmountToArrive} item#{listing.ItemListings[0].CatalogId}");
                goto Return;
            }

            if (request.ListingsRequestId != -1 && request.ListingsRequestId != listing.RequestId) {
                Log.Error(
                    $"Non-matching RequestIds for Market Board data request: {request.ListingsRequestId}, {listing.RequestId}");
                goto Return;
            }

            if (request.ListingsRequestId == -1 && request.Listings.Count > 0) {
                Log.Error(
                    $"Market Board data request sequence break: {request.ListingsRequestId}, {request.Listings.Count}");
                goto Return;
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

        Return:
            return this.onMarketBoardOfferings.Original(a1, dataPtr);
        }

        private IntPtr OnMarketBoardHistory(IntPtr a1, IntPtr dataPtr) {
            Log.Verbose("history time");

            if (!this.dalamud.Data.IsDataReady || this.optOutMbUploads)
                goto Return;

            var listing = MarketBoardHistory.Read(dataPtr);

            var request = this.marketBoardRequests.LastOrDefault(r => r.CatalogId == listing.CatalogId);

            if (request == null) {
                Log.Error(
                    $"Market Board data arrived without a corresponding request: item#{listing.CatalogId}");
                goto Return;
            }

            if (request.ListingsRequestId != -1) {
                Log.Error(
                    $"Market Board data history sequence break: {request.ListingsRequestId}, {request.Listings.Count}");
                goto Return;
            }

            request.History.AddRange(listing.HistoryListings);

            Log.Verbose("Added history for item#{0}", listing.CatalogId);

            if (request.AmountToArrive == 0) {
                Log.Verbose("Request had 0 amount, uploading now");

                try {
                    Task.Run(() => this.uploader.Upload(request));
                } catch (Exception ex) {
                    Log.Error(ex, "Market Board data upload failed.");
                }
            }

        Return:
            return this.onMarketBoardHistory.Original(a1, dataPtr);
        }

        private IntPtr OnMarketBoardTaxRates(IntPtr a1, int a2, IntPtr dataPtr) {
            if (!this.dalamud.Data.IsDataReady || this.optOutMbUploads)
                goto Return;

            var taxes = MarketTaxRates.Read(dataPtr);

            Log.Verbose("MarketTaxRates: limsa#{0} grid#{1} uldah#{2} ish#{3} kugane#{4} cr#{5}",
                        taxes.LimsaLominsaTax, taxes.GridaniaTax, taxes.UldahTax, taxes.IshgardTax, taxes.KuganeTax, taxes.CrystariumTax);
            try {
                Task.Run(() => this.uploader.UploadTax(taxes));
            } catch (Exception ex) {
                Log.Error(ex, "Market Board data upload failed.");
            }

        Return:
            return this.onMarketBoardTaxRates.Original(a1, a2, dataPtr);
        }

        public void Dispose() {
            this.onCfNotifyPop?.Dispose();
            this.onMarketBoardRequestItem?.Dispose();
            this.onMarketBoardOfferings?.Dispose();
            this.onMarketBoardHistory?.Dispose();
            this.onMarketBoardTaxRates?.Dispose();
        }
    }
}
