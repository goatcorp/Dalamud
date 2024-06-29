namespace Dalamud.Game.Network.Structures;

/// <summary>
/// An interface that represents market board purchase information. This message is sent from the
/// client when a purchase is made at a market board.
/// </summary>
public interface IMarketBoardPurchaseHandler
{
    /// <summary>
    /// Gets the object ID of the retainer associated with the sale.
    /// </summary>
    public ulong RetainerId { get; }

    /// <summary>
    /// Gets the object ID of the item listing.
    /// </summary>
    public ulong ListingId { get; }

    /// <summary>
    /// Gets the item ID of the item that was purchased.
    /// </summary>
    public uint CatalogId { get; }

    /// <summary>
    /// Gets the quantity of the item that was purchased.
    /// </summary>
    public uint ItemQuantity { get; }

    /// <summary>
    /// Gets the unit price of the item.
    /// </summary>
    public uint PricePerUnit { get; }

    /// <summary>
    /// Gets a value indicating whether the item is HQ.
    /// </summary>
    public bool IsHq { get; }

    /// <summary>
    /// Gets the total tax.
    /// </summary>
    public uint TotalTax { get; }

    /// <summary>
    /// Gets the city ID of the retainer selling the item.
    /// </summary>
    public int RetainerCityId { get; }
}
