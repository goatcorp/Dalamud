using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

using Dalamud.Game.Network.Internal.MarketBoardUploaders.Universalis.Types;
using Dalamud.Game.Network.Structures;
using Newtonsoft.Json;
using Serilog;

namespace Dalamud.Game.Network.Internal.MarketBoardUploaders.Universalis
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

            Log.Verbose("Starting Universalis upload.");
            var uploader = this.dalamud.ClientState.LocalContentId;

            // ====================================================================================

            var listingsUploadObject = new UniversalisItemListingsUploadRequest
            {
                WorldId = this.dalamud.ClientState.LocalPlayer?.CurrentWorld.Id ?? 0,
                UploaderId = uploader.ToString(),
                ItemId = request.CatalogId,
                Listings = new List<UniversalisItemListingsEntry>(),
            };

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
                    Materia = new List<UniversalisItemMateria>(),
                };

                foreach (var itemMateria in marketBoardItemListing.Materia)
                {
                    universalisListing.Materia.Add(new UniversalisItemMateria
                    {
                        MateriaId = itemMateria.MateriaId,
                        SlotId = itemMateria.Index,
                    });
                }

                listingsUploadObject.Listings.Add(universalisListing);
            }

            var listingPath = "/upload";
            var listingUpload = JsonConvert.SerializeObject(listingsUploadObject);
            Log.Verbose($"{listingPath}: {listingUpload}");
            client.PostAsync($"{ApiBase}{listingPath}/{ApiKey}", new StringContent(listingUpload, Encoding.UTF8, "application/json")).GetAwaiter().GetResult();

            // ====================================================================================

            var historyUploadObject = new UniversalisHistoryUploadRequest
            {
                WorldId = this.dalamud.ClientState.LocalPlayer?.CurrentWorld.Id ?? 0,
                UploaderId = uploader.ToString(),
                ItemId = request.CatalogId,
                Entries = new List<UniversalisHistoryEntry>(),
            };

            foreach (var marketBoardHistoryListing in request.History)
            {
                historyUploadObject.Entries.Add(new UniversalisHistoryEntry
                {
                    BuyerName = marketBoardHistoryListing.BuyerName,
                    Hq = marketBoardHistoryListing.IsHq,
                    OnMannequin = marketBoardHistoryListing.OnMannequin,
                    PricePerUnit = marketBoardHistoryListing.SalePrice,
                    Quantity = marketBoardHistoryListing.Quantity,
                    Timestamp = ((DateTimeOffset)marketBoardHistoryListing.PurchaseTime).ToUnixTimeSeconds(),
                });
            }

            var historyPath = "/upload";
            var historyUpload = JsonConvert.SerializeObject(historyUploadObject);
            Log.Verbose($"{historyPath}: {historyUpload}");
            client.PostAsync($"{ApiBase}{historyPath}/{ApiKey}", new StringContent(historyUpload, Encoding.UTF8, "application/json")).GetAwaiter().GetResult();

            // ====================================================================================

            Log.Verbose("Universalis data upload for item#{0} completed.", request.CatalogId);
        }

        /// <inheritdoc/>
        public void UploadTax(MarketTaxRates taxRates)
        {
            using var client = new HttpClient();

            // ====================================================================================

            var taxUploadObject = new UniversalisTaxUploadRequest
            {
                WorldId = this.dalamud.ClientState.LocalPlayer?.CurrentWorld.Id ?? 0,
                UploaderId = this.dalamud.ClientState.LocalContentId.ToString(),
                TaxData = new UniversalisTaxData
                {
                    LimsaLominsa = taxRates.LimsaLominsaTax,
                    Gridania = taxRates.GridaniaTax,
                    Uldah = taxRates.UldahTax,
                    Ishgard = taxRates.IshgardTax,
                    Kugane = taxRates.KuganeTax,
                    Crystarium = taxRates.CrystariumTax,
                },
            };

            var taxPath = "/upload";
            var taxUpload = JsonConvert.SerializeObject(taxUploadObject);
            Log.Verbose($"{taxPath}: {taxUpload}");

            client.PostAsync($"{ApiBase}{taxPath}/{ApiKey}", new StringContent(taxUpload, Encoding.UTF8, "application/json")).GetAwaiter().GetResult();

            // ====================================================================================

            Log.Verbose("Universalis tax upload completed.");
        }

        /// <inheritdoc/>
        /// <remarks>
        /// It may seem backwards that an upload only performs a delete request, however this is not trying
        /// to track the available listings, that is done via the listings packet. All this does is remove
        /// a listing, or delete it, when a purchase has been made.
        /// </remarks>
        public void UploadPurchase(MarketBoardPurchaseHandler purchaseHandler)
        {
            using var client = new HttpClient();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ApiKey);

            var itemId = purchaseHandler.CatalogId;
            var worldId = this.dalamud.ClientState.LocalPlayer?.CurrentWorld.Id ?? 0;

            // ====================================================================================

            var deleteListingObject = new UniversalisItemListingDeleteRequest
            {
                PricePerUnit = purchaseHandler.PricePerUnit,
                Quantity = purchaseHandler.ItemQuantity,
                ListingId = purchaseHandler.ListingId.ToString(),
                RetainerId = purchaseHandler.RetainerId.ToString(),
                UploaderId = this.dalamud.ClientState.LocalContentId.ToString(),
            };

            var deletePath = $"/api/{worldId}/{itemId}/delete";
            var deleteListing = JsonConvert.SerializeObject(deleteListingObject);
            Log.Verbose($"{deletePath}: {deleteListing}");

            client.PostAsync($"{ApiBase}{deletePath}", new StringContent(deleteListing, Encoding.UTF8, "application/json")).GetAwaiter().GetResult();

            // ====================================================================================

            Log.Verbose("Universalis purchase upload completed.");
        }
    }
}
