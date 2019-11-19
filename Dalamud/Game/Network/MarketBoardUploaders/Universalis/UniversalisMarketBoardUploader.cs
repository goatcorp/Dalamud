using System;
using System.Collections.Generic;
using System.Net;
using Dalamud.Game.Network.MarketBoardUploaders;
using Dalamud.Game.Network.MarketBoardUploaders.Universalis;
using Dalamud.Game.Network.Structures;
using Newtonsoft.Json;
using Serilog;

namespace Dalamud.Game.Network.Universalis.MarketBoardUploaders {
    internal class UniversalisMarketBoardUploader : IMarketBoardUploader {
        private const string ApiBase = "https://universalis.app";

        //private const string ApiBase = "https://127.0.0.1:443";
        private const string ApiKey = "GGD6RdSfGyRiHM5WDnAo0Nj9Nv7aC5NDhMj3BebT";

        private readonly Dalamud dalamud;

        public UniversalisMarketBoardUploader(Dalamud dalamud) {
            this.dalamud = dalamud;
        }

        public void Upload(MarketBoardItemRequest request) {
            using (var client = new WebClient()) {
                client.Headers.Add(HttpRequestHeader.ContentType, "application/json");

                Log.Verbose("Starting Universalis upload.");
                var uploader = this.dalamud.ClientState.LocalContentId;

                var listingsRequestObject = new UniversalisItemListingsUploadRequest();
                listingsRequestObject.WorldId = this.dalamud.ClientState.LocalPlayer.CurrentWorld.Id;
                listingsRequestObject.UploaderId = uploader;
                listingsRequestObject.ItemId = request.CatalogId;

                listingsRequestObject.Listings = new List<UniversalisItemListingsEntry>();
                foreach (var marketBoardItemListing in request.Listings) {
                    var universalisListing = new UniversalisItemListingsEntry {
                        Hq = marketBoardItemListing.IsHq,
                        SellerId = marketBoardItemListing.RetainerOwnerId,
                        RetainerName = marketBoardItemListing.RetainerName,
                        RetainerId = marketBoardItemListing.RetainerId,
                        CreatorId = marketBoardItemListing.ArtisanId,
                        CreatorName = marketBoardItemListing.PlayerName,
                        OnMannequin = marketBoardItemListing.OnMannequin,
                        LastReviewTime = ((DateTimeOffset) marketBoardItemListing.LastReviewTime).ToUnixTimeSeconds(),
                        PricePerUnit = marketBoardItemListing.PricePerUnit,
                        Quantity = marketBoardItemListing.ItemQuantity,
                        RetainerCity = marketBoardItemListing.RetainerCityId
                    };

                    universalisListing.Materia = new List<UniversalisItemMateria>();
                    foreach (var itemMateria in marketBoardItemListing.Materia)
                        universalisListing.Materia.Add(new UniversalisItemMateria {
                            MateriaId = itemMateria.MateriaId,
                            SlotId = itemMateria.Index
                        });

                    listingsRequestObject.Listings.Add(universalisListing);
                }

                var upload = JsonConvert.SerializeObject(listingsRequestObject);
                client.UploadString(ApiBase + $"/upload/{ApiKey}", "POST", upload);
                Log.Verbose(upload);

                var historyRequestObject = new UniversalisHistoryUploadRequest();
                historyRequestObject.WorldId = this.dalamud.ClientState.LocalPlayer.CurrentWorld.Id;
                historyRequestObject.UploaderId = uploader;
                historyRequestObject.ItemId = request.CatalogId;

                historyRequestObject.Entries = new List<UniversalisHistoryEntry>();
                foreach (var marketBoardHistoryListing in request.History)
                    historyRequestObject.Entries.Add(new UniversalisHistoryEntry {
                        BuyerName = marketBoardHistoryListing.BuyerName,
                        Hq = marketBoardHistoryListing.IsHq,
                        OnMannequin = marketBoardHistoryListing.OnMannequin,
                        PricePerUnit = marketBoardHistoryListing.SalePrice,
                        Quantity = marketBoardHistoryListing.Quantity,
                        Timestamp = ((DateTimeOffset) marketBoardHistoryListing.PurchaseTime).ToUnixTimeSeconds()
                    });

                client.Headers.Add(HttpRequestHeader.ContentType, "application/json");

                var historyUpload = JsonConvert.SerializeObject(historyRequestObject);
                client.UploadString(ApiBase + $"/upload/{ApiKey}", "POST", historyUpload);
                Log.Verbose(historyUpload);

                Log.Verbose("Universalis data upload for item#{0} completed.", request.CatalogId);
            }
        }

        public void UploadTax(MarketTaxRates taxRates) {
            using (var client = new WebClient())
            {
                var taxRatesRequest = new UniversalisTaxUploadRequest();
                taxRatesRequest.WorldId = this.dalamud.ClientState.LocalPlayer.CurrentWorld.Id;
                taxRatesRequest.UploaderId = this.dalamud.ClientState.LocalContentId;

                taxRatesRequest.TaxData = new UniversalisTaxData {
                    LimsaLominsa = taxRates.LimsaLominsaTax,
                    Gridania = taxRates.GridaniaTax,
                    Uldah = taxRates.UldahTax,
                    Ishgard = taxRates.IshgardTax,
                    Kugane = taxRates.KuganeTax,
                    Crystarium = taxRates.CrystariumTax
                };

                client.Headers.Add(HttpRequestHeader.ContentType, "application/json");

                var historyUpload = JsonConvert.SerializeObject(taxRatesRequest);
                client.UploadString(ApiBase + $"/upload/{ApiKey}", "POST", historyUpload);
                Log.Verbose(historyUpload);

                Log.Verbose("Universalis tax upload completed.");
            }
        }
    }
}
