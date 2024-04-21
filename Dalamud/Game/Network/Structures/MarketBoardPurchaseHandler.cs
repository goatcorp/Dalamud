using System.IO;

namespace Dalamud.Game.Network.Structures;

/// <summary>
/// Represents market board purchase information. This message is sent from the
/// client when a purchase is made at a market board.
/// </summary>
public class MarketBoardPurchaseHandler
{
    private MarketBoardPurchaseHandler()
    {
    }

    /// <summary>
    /// Gets the object ID of the retainer associated with the sale.
    /// </summary>
    public ulong RetainerId { get; private set; }

    /// <summary>
    /// Gets the object ID of the item listing.
    /// </summary>
    public ulong ListingId { get; private set; }

    /// <summary>
    /// Gets the item ID of the item that was purchased.
    /// </summary>
    public uint CatalogId { get; private set; }

    /// <summary>
    /// Gets the quantity of the item that was purchased.
    /// </summary>
    public uint ItemQuantity { get; private set; }

    /// <summary>
    /// Gets the unit price of the item.
    /// </summary>
    public uint PricePerUnit { get; private set; }

    /// <summary>
    /// Reads market board purchase information from the struct at the provided pointer.
    /// </summary>
    /// <param name="dataPtr">A pointer to a struct containing market board purchase information from the client.</param>
    /// <returns>An object representing the data read.</returns>
    public static unsafe MarketBoardPurchaseHandler Read(IntPtr dataPtr)
    {
        using var stream = new UnmanagedMemoryStream((byte*)dataPtr.ToPointer(), 1544);
        using var reader = new BinaryReader(stream);

        var output = new MarketBoardPurchaseHandler
        {
            RetainerId = reader.ReadUInt64(),
            ListingId = reader.ReadUInt64(),
            CatalogId = reader.ReadUInt32(),
            ItemQuantity = reader.ReadUInt32(),
            PricePerUnit = reader.ReadUInt32(),
        };

        return output;
    }
}
