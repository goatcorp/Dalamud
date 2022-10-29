using System.Collections.Generic;

using Newtonsoft.Json;

namespace Dalamud.Game.Network.Internal.MarketBoardUploaders.Universalis.Types;

/// <summary>
/// A Universalis API structure.
/// </summary>
internal class UniversalisItemListingsEntry
{
    /// <summary>
    /// Gets or sets the listing ID.
    /// </summary>
    [JsonProperty("listingID")]
    public string ListingId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the item is HQ.
    /// </summary>
    [JsonProperty("hq")]
    public bool Hq { get; set; }

    /// <summary>
    /// Gets or sets the item price per unit.
    /// </summary>
    [JsonProperty("pricePerUnit")]
    public uint PricePerUnit { get; set; }

    /// <summary>
    /// Gets or sets the item quantity.
    /// </summary>
    [JsonProperty("quantity")]
    public uint Quantity { get; set; }

    /// <summary>
    /// Gets or sets the name of the retainer selling the item.
    /// </summary>
    [JsonProperty("retainerName")]
    public string RetainerName { get; set; }

    /// <summary>
    /// Gets or sets the ID of the retainer selling the item.
    /// </summary>
    [JsonProperty("retainerID")]
    public string RetainerId { get; set; }

    /// <summary>
    /// Gets or sets the name of the user who created the entry.
    /// </summary>
    [JsonProperty("creatorName")]
    public string CreatorName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the item is on a mannequin.
    /// </summary>
    [JsonProperty("onMannequin")]
    public bool OnMannequin { get; set; }

    /// <summary>
    /// Gets or sets the seller ID.
    /// </summary>
    [JsonProperty("sellerID")]
    public string SellerId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who created the entry.
    /// </summary>
    [JsonProperty("creatorID")]
    public string CreatorId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the dye on the item.
    /// </summary>
    [JsonProperty("stainID")]
    public int StainId { get; set; }

    /// <summary>
    /// Gets or sets the city where the selling retainer resides.
    /// </summary>
    [JsonProperty("retainerCity")]
    public int RetainerCity { get; set; }

    /// <summary>
    /// Gets or sets the last time the entry was reviewed.
    /// </summary>
    [JsonProperty("lastReviewTime")]
    public long LastReviewTime { get; set; }

    /// <summary>
    /// Gets or sets the materia attached to the item.
    /// </summary>
    [JsonProperty("materia")]
    public List<UniversalisItemMateria> Materia { get; set; }
}
