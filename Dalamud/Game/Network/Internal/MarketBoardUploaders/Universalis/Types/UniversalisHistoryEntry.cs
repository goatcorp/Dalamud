using Newtonsoft.Json;

namespace Dalamud.Game.Network.Internal.MarketBoardUploaders.Universalis.Types;

/// <summary>
/// A Universalis API structure.
/// </summary>
internal class UniversalisHistoryEntry
{
    /// <summary>
    /// Gets or sets a value indicating whether the item is HQ or not.
    /// </summary>
    [JsonProperty("hq")]
    public bool Hq { get; set; }

    /// <summary>
    /// Gets or sets the item price per unit.
    /// </summary>
    [JsonProperty("pricePerUnit")]
    public uint PricePerUnit { get; set; }

    /// <summary>
    /// Gets or sets the quantity of items available.
    /// </summary>
    [JsonProperty("quantity")]
    public uint Quantity { get; set; }

    /// <summary>
    /// Gets or sets the name of the buyer.
    /// </summary>
    [JsonProperty("buyerName")]
    public string BuyerName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this item was on a mannequin.
    /// </summary>
    [JsonProperty("onMannequin")]
    public bool OnMannequin { get; set; }

    /// <summary>
    /// Gets or sets the seller ID.
    /// </summary>
    [JsonProperty("sellerID")]
    public string SellerId { get; set; }

    /// <summary>
    /// Gets or sets the buyer ID.
    /// </summary>
    [JsonProperty("buyerID")]
    public string BuyerId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the transaction.
    /// </summary>
    [JsonProperty("timestamp")]
    public long Timestamp { get; set; }
}
