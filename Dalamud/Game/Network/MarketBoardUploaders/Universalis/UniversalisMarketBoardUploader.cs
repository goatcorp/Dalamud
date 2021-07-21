using System;
using System.Collections.Generic;
using System.Net.Http;

using Dalamud.Game.Network.MarketBoardUploaders;
using Dalamud.Game.Network.MarketBoardUploaders.Universalis;
using Dalamud.Game.Network.Structures;
using Newtonsoft.Json;
using Serilog;

namespace Dalamud.Game.Network.Universalis.MarketBoardUploaders
{
    /// <summary>
    /// This class represents an uploader for contributing data to Universalis.
    /// </summary>
    internal class UniversalisMarketBoardUploader : IMarketBoardUploader
    {
        private const string ApiBase = "https://universalis.app";

        // private const string ApiBase = "https://127.0.0.1:443";
        private const string ApiKey = "GGD6RdSfGyRiHM5WDnAo0Nj9Nv7aC5NDhMj3BebT";

        private readonly Dalamud dalamud;

        /// <summary>
        /// Initializes a new instance of the <see cref="UniversalisMarketBoardUploader"/> class.
        /// </summary>
        /// <param name="dalamud">The Dalamud instance.</param>
        public UniversalisMarketBoardUploader(Dalamud dalamud)
        {
            this.dalamud = dalamud;
        }

        /// <inheritdoc/>
        public void Upload(MarketBoardItemRequest request)
        {
            using var client = new HttpClient();

            client.DefaultRequestHeaders.Add("Content-Type", "application/json");

            Log.Verbose("Starting Universalis upload.");
            var uploader = this.dalamud.ClientState.LocalContentId;

            var listingsRequestObject = new UniversalisItemListingsUploadRequest();
            listingsRequestObject.WorldId = this.dalamud.ClientState.LocalPlayer?.CurrentWorld.Id ?? 0;
            listingsRequestObject.UploaderId = uploader.ToString();
            listingsRequestObject.ItemId = request.CatalogId;

            listingsRequestObject.Listings = new List<UniversalisItemListingsEntry>();
            foreach (var marketBoardItemListing in request.Listings)
            {
                var universalisListing = new UniversalisItemListingsEntry
                {
                    Hq = marketBoardItemListing.IsHq,
                    SellerId = marketBoardItemListing.RetainerOwnerId.ToString(),
                    RetainerName = marketBoardItemListing.RetainerName,
                    RetainerId = marketBoardItemListing.RetainerId.ToString(),
                    CreatorId = marketBoardItemListing.ArtisanId.ToString(),
                    CreatorName = marketBoardItemListing.PlayerName,
                    OnMannequin = marketBoardItemListing.OnMannequin,
                    LastReviewTime = ((DateTimeOffset)marketBoardItemListing.LastReviewTime).ToUnixTimeSeconds(),
                    PricePerUnit = marketBoardItemListing.PricePerUnit,
                    Quantity = marketBoardItemListing.ItemQuantity,
                    RetainerCity = marketBoardItemListing.RetainerCityId,
                };

                universalisListing.Materia = new List<UniversalisItemMateria>();
                foreach (var itemMateria in marketBoardItemListing.Materia)
                {
                    universalisListing.Materia.Add(new UniversalisItemMateria
                    {
                        MateriaId = itemMateria.MateriaId,
                        SlotId = itemMateria.Index,
                    });
                }

                listingsRequestObject.Listings.Add(universalisListing);
            }

            var upload = JsonConvert.SerializeObject(listingsRequestObject);
            client.PostAsync(ApiBase + $"/upload/{ApiKey}", new StringContent(upload)).GetAwaiter().GetResult();
            Log.Verbose(upload);

            var historyRequestObject = new UniversalisHistoryUploadRequest();
            historyRequestObject.WorldId = this.dalamud.ClientState.LocalPlayer?.CurrentWorld.Id ?? 0;
            historyRequestObject.UploaderId = uploader.ToString();
            historyRequestObject.ItemId = request.CatalogId;

            historyRequestObject.Entries = new List<UniversalisHistoryEntry>();
            foreach (var marketBoardHistoryListing in request.History)
            {
                historyRequestObject.Entries.Add(new UniversalisHistoryEntry
                {
                    BuyerName = marketBoardHistoryListing.BuyerName,
                    Hq = marketBoardHistoryListing.IsHq,
                    OnMannequin = marketBoardHistoryListing.OnMannequin,
                    PricePerUnit = marketBoardHistoryListing.SalePrice,
                    Quantity = marketBoardHistoryListing.Quantity,
                    Timestamp = ((DateTimeOffset)marketBoardHistoryListing.PurchaseTime).ToUnixTimeSeconds(),
                });
            }

            var historyUpload = JsonConvert.SerializeObject(historyRequestObject);
            client.PostAsync(ApiBase + $"/upload/{ApiKey}", new StringContent(historyUpload)).GetAwaiter().GetResult();
            Log.Verbose(historyUpload);

            Log.Verbose("Universalis data upload for item#{0} completed.", request.CatalogId);
        }

        /// <inheritdoc/>
        public void UploadTax(MarketTaxRates taxRates)
        {
            using var client = new HttpClient();

            client.DefaultRequestHeaders.Add("Content-Type", "application/json");

            var taxRatesRequest = new UniversalisTaxUploadRequest();
            taxRatesRequest.WorldId = this.dalamud.ClientState.LocalPlayer?.CurrentWorld.Id ?? 0;
            taxRatesRequest.UploaderId = this.dalamud.ClientState.LocalContentId.ToString();

            taxRatesRequest.TaxData = new UniversalisTaxData
            {
                LimsaLominsa = taxRates.LimsaLominsaTax,
                Gridania = taxRates.GridaniaTax,
                Uldah = taxRates.UldahTax,
                Ishgard = taxRates.IshgardTax,
                Kugane = taxRates.KuganeTax,
                Crystarium = taxRates.CrystariumTax,
            };

            var historyUpload = JsonConvert.SerializeObject(taxRatesRequest);
            client.PostAsync(ApiBase + $"/upload/{ApiKey}", new StringContent(historyUpload)).GetAwaiter().GetResult();
            Log.Verbose(historyUpload);

            Log.Verbose("Universalis tax upload completed.");
        }
    }
}
