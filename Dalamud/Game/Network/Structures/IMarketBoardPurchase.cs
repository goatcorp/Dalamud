namespace Dalamud.Game.Network.Structures;

/// <summary>
/// An interface that represents market board purchase information. This message is received from the
/// server when a purchase is made at a market board.
/// </summary>
public interface IMarketBoardPurchase
{
    /// <summary>
    /// Gets the item ID of the item that was purchased.
    /// </summary>
    uint CatalogId { get; }

    /// <summary>
    /// Gets the quantity of the item that was purchased.
    /// </summary>
    uint ItemQuantity { get; }
}
