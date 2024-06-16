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
    ulong RetainerId { get; }

    /// <summary>
    /// Gets the object ID of the item listing.
    /// </summary>
    ulong ListingId { get; }

    /// <summary>
    /// Gets the item ID of the item that was purchased.
    /// </summary>
    uint CatalogId { get; }

    /// <summary>
    /// Gets the quantity of the item that was purchased.
    /// </summary>
    uint ItemQuantity { get; }

    /// <summary>
    /// Gets the unit price of the item.
    /// </summary>
    uint PricePerUnit { get; }
}
