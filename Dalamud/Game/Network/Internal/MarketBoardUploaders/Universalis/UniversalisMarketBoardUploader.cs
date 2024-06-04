using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Dalamud.Game.Network.Internal.MarketBoardUploaders.Universalis.Types;
using Dalamud.Game.Network.Structures;
using Dalamud.Networking.Http;

using Newtonsoft.Json;
using Serilog;

namespace Dalamud.Game.Network.Internal.MarketBoardUploaders.Universalis;

/// <summary>
/// This class represents an uploader for contributing data to Universalis.
/// </summary>
internal class UniversalisMarketBoardUploader : IMarketBoardUploader
{
    private const string ApiBase = "https://universalis.app";
    // private const string ApiBase = "https://127.0.0.1:443";

    private const string ApiKey = "GGD6RdSfGyRiHM5WDnAo0Nj9Nv7aC5NDhMj3BebT";

    private readonly HttpClient httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="UniversalisMarketBoardUploader"/> class.
    /// </summary>
    /// <param name="happyHttpClient">An instance of <see cref="HappyHttpClient"/>.</param>
    public UniversalisMarketBoardUploader(HappyHttpClient happyHttpClient) =>
        this.httpClient = happyHttpClient.SharedHttpClient;

    /// <inheritdoc/>
    public async Task Upload(MarketBoardItemRequest request)
    {
        var clientState = Service<ClientState.ClientState>.GetNullable();
        if (clientState == null)
            return;

        Log.Verbose("Starting Universalis upload");
        var uploader = clientState.LocalContentId;

        // ====================================================================================

        var uploadObject = new UniversalisItemUploadRequest
        {
            WorldId = clientState.LocalPlayer?.CurrentWorld.Id ?? 0,
            UploaderId = uploader.ToString(),
            ItemId = request.CatalogId,
            Listings = new List<UniversalisItemListingsEntry>(),
            Sales = new List<UniversalisHistoryEntry>(),
        };

        foreach (var marketBoardItemListing in request.Listings)
        {
            var universalisListing = new UniversalisItemListingsEntry
            {
                ListingId = marketBoardItemListing.ListingId.ToString(),
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

            uploadObject.Listings.Add(universalisListing);
        }

        foreach (var marketBoardHistoryListing in request.History)
        {
            uploadObject.Sales.Add(new UniversalisHistoryEntry
            {
                BuyerName = marketBoardHistoryListing.BuyerName,
                Hq = marketBoardHistoryListing.IsHq,
                OnMannequin = marketBoardHistoryListing.OnMannequin,
                PricePerUnit = marketBoardHistoryListing.SalePrice,
                Quantity = marketBoardHistoryListing.Quantity,
                Timestamp = ((DateTimeOffset)marketBoardHistoryListing.PurchaseTime).ToUnixTimeSeconds(),
            });
        }

        var uploadPath = "/upload";
        var uploadData = JsonConvert.SerializeObject(uploadObject);
        Log.Verbose("{ListingPath}: {ListingUpload}", uploadPath, uploadData);
        await this.httpClient.PostAsync($"{ApiBase}{uploadPath}/{ApiKey}", new StringContent(uploadData, Encoding.UTF8, "application/json"));

        // ====================================================================================

        Log.Verbose("Universalis data upload for item#{CatalogId} completed", request.CatalogId);
    }

    /// <inheritdoc/>
    public async Task UploadTax(MarketTaxRates taxRates)
    {
        var clientState = Service<ClientState.ClientState>.GetNullable();
        if (clientState == null)
            return;

        // ====================================================================================

        var taxUploadObject = new UniversalisTaxUploadRequest
        {
            WorldId = clientState.LocalPlayer?.CurrentWorld.Id ?? 0,
            UploaderId = clientState.LocalContentId.ToString(),
            TaxData = new UniversalisTaxData
            {
                LimsaLominsa = taxRates.LimsaLominsaTax,
                Gridania = taxRates.GridaniaTax,
                Uldah = taxRates.UldahTax,
                Ishgard = taxRates.IshgardTax,
                Kugane = taxRates.KuganeTax,
                Crystarium = taxRates.CrystariumTax,
                Sharlayan = taxRates.SharlayanTax,
            },
        };

        var taxPath = "/upload";
        var taxUpload = JsonConvert.SerializeObject(taxUploadObject);
        Log.Verbose("{TaxPath}: {TaxUpload}", taxPath, taxUpload);

        await this.httpClient.PostAsync($"{ApiBase}{taxPath}/{ApiKey}", new StringContent(taxUpload, Encoding.UTF8, "application/json"));

        // ====================================================================================

        Log.Verbose("Universalis tax upload completed");
    }

    /// <inheritdoc/>
    /// <remarks>
    /// It may seem backwards that an upload only performs a delete request, however this is not trying
    /// to track the available listings, that is done via the listings packet. All this does is remove
    /// a listing, or delete it, when a purchase has been made.
    /// </remarks>
    public async Task UploadPurchase(MarketBoardPurchaseHandler purchaseHandler)
    {
        var clientState = Service<ClientState.ClientState>.GetNullable();
        if (clientState == null)
            return;

        var itemId = purchaseHandler.CatalogId;
        var worldId = clientState.LocalPlayer?.CurrentWorld.Id ?? 0;

        // ====================================================================================

        var deleteListingObject = new UniversalisItemListingDeleteRequest
        {
            PricePerUnit = purchaseHandler.PricePerUnit,
            Quantity = purchaseHandler.ItemQuantity,
            ListingId = purchaseHandler.ListingId.ToString(),
            RetainerId = purchaseHandler.RetainerId.ToString(),
            UploaderId = clientState.LocalContentId.ToString(),
        };

        var deletePath = $"/api/{worldId}/{itemId}/delete";
        var deleteListing = JsonConvert.SerializeObject(deleteListingObject);
        Log.Verbose("{DeletePath}: {DeleteListing}", deletePath, deleteListing);

        var content = new StringContent(deleteListing, Encoding.UTF8, "application/json");
        var message = new HttpRequestMessage(HttpMethod.Post, $"{ApiBase}{deletePath}");
        message.Headers.Add("Authorization", ApiKey);
        message.Content = content;

        await this.httpClient.SendAsync(message);

        // ====================================================================================

        Log.Verbose("Universalis purchase upload completed");
    }
}
