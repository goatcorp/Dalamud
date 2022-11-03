using Newtonsoft.Json;

namespace Dalamud.Game.Network.Internal.MarketBoardUploaders.Universalis.Types;

/// <summary>
/// Request payload for market board purchases.
/// </summary>
internal class UniversalisItemListingDeleteRequest
{
    /// <summary>
    /// Gets or sets the object ID of the retainer associated with the sale.
    /// </summary>
    [JsonProperty("retainerID")]
    public string RetainerId { get; set; }

    /// <summary>
    /// Gets or sets the object ID of the item listing.
    /// </summary>
    [JsonProperty("listingID")]
    public string ListingId { get; set; }

    /// <summary>
    /// Gets or sets the quantity of the item that was purchased.
    /// </summary>
    [JsonProperty("quantity")]
    public uint Quantity { get; set; }

    /// <summary>
    /// Gets or sets the unit price of the item.
    /// </summary>
    [JsonProperty("pricePerUnit")]
    public uint PricePerUnit { get; set; }

    /// <summary>
    /// Gets or sets the uploader ID.
    /// </summary>
    [JsonProperty("uploaderID")]
    public string UploaderId { get; set; }
}
