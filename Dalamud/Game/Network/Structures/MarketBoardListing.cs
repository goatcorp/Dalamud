using Dalamud.Data;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using Lumina.Excel.GeneratedSheets;

namespace Dalamud.Game.Network.Structures;

/// <summary>
/// A listing from the Market Board.
/// </summary>
public record MarketBoardListing
{
    /// <summary>
    /// Gets a unique global identifier for this specific marketboard listing.
    /// </summary>
    public ulong ListingId { get; init; }

    /// <summary>
    /// Gets the Content ID of the player that is selling this item.
    /// </summary>
    public ulong SellingPlayerId { get; init; }

    /// <summary>
    /// Gets the Content ID of the retainer that is selling this item.
    /// </summary>
    public ulong SellingRetainerId { get; init; }

    /// <summary>
    /// Gets the ID of the item that is being sold.
    /// </summary>
    public uint ItemId { get; init; }

    /// <summary>
    /// Gets the unit price for this marketboard listing.
    /// </summary>
    public uint UnitPrice { get; init; }

    /// <summary>
    /// Gets the total tax that would need to be paid on this marketboard listing.
    /// </summary>
    public uint TotalTax { get; init; }

    /// <summary>
    /// Gets the quantity of items in this listing.
    /// </summary>
    public uint Quantity { get; init; }

    /// <summary>
    /// Gets the town that this listing belongs to.
    /// </summary>
    public byte TownId { get; init; }

    /// <summary>
    /// Gets a value indicating whether this listing is for a High Quality item.
    /// </summary>
    public bool IsHighQuality { get; init; }

    /// <summary>
    /// Gets the calculated total price of this listing.
    /// </summary>
    public int StackPrice => (int)((this.UnitPrice * this.Quantity) + this.TotalTax);

    /// <summary>
    /// Gets the Lumina item being sold as part of this listing.
    /// </summary>
    public Item? Item => Service<DataManager>.Get().GetExcelSheet<Item>()!.GetRow(this.ItemId);

    /// <summary>
    /// Gets the Lumina town that this listing belongs to.
    /// </summary>
    public Town? Town => Service<DataManager>.Get().GetExcelSheet<Town>()!.GetRow(this.TownId);

    /// <summary>
    /// Create a wrapped MarketBoardListing from the internal struct, as determined by ClientStructs.
    /// </summary>
    /// <param name="listing">A ClientStructs listing to reference.</param>
    /// <returns>Returns a wrapped MarketBoardListing for consumption.</returns>
    public static MarketBoardListing FromInfoProxyEntry(
        FFXIVClientStructs.FFXIV.Client.UI.Info.MarketBoardListing listing)
    {
        return new MarketBoardListing
        {
            ListingId = listing.GlobalItemId,
            ItemId = listing.ItemId,
            Quantity = listing.Quantity,
            UnitPrice = listing.UnitPrice,
            TotalTax = listing.TotalTax,
            IsHighQuality = listing.IsHqItem,
            SellingRetainerId = listing.SellingRetainerContentId,
            SellingPlayerId = listing.SellingPlayerContentId,
            TownId = listing.Town,
        };
    }

    /// <summary>
    /// Create a wrapped MarketBoardListing from a LastPurchasedMarketBoardItem struct.
    /// </summary>
    /// <param name="listing">The last marketboard item to generate a listing for.</param>
    /// <returns>Returns a wrapped MarketBoardListing for consumption.</returns>
    public static MarketBoardListing FromLastPurchasedEntry(LastPurchasedMarketboardItem listing)
    {
        return new MarketBoardListing
        {
            ListingId = listing.ListingId,
            ItemId = listing.ItemId,
            Quantity = listing.Quantity,
            UnitPrice = listing.UnitPrice,
            TotalTax = listing.TotalTax,
            IsHighQuality = listing.IsHqItem,
            SellingRetainerId = listing.SellingRetainerContentId,
            TownId = listing.TownId,
        };
    }
}
