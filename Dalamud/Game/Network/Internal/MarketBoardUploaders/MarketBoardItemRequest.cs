using System.Collections.Generic;
using System.IO;

using Dalamud.Game.Network.Structures;

namespace Dalamud.Game.Network.Internal.MarketBoardUploaders;

/// <summary>
/// This represents a submission to a marketboard aggregation website.
/// </summary>
internal class MarketBoardItemRequest
{
    private MarketBoardItemRequest()
    {
    }

    /// <summary>
    /// Gets the catalog ID.
    /// </summary>
    public uint CatalogId { get; private set; }

    /// <summary>
    /// Gets the request status. Nonzero statuses are errors.
    /// </summary>
    public uint Status { get; private set; }

    /// <summary>
    /// Gets a value indicating whether or not this request was successful.
    /// </summary>
    public bool Ok => this.Status == 0;

    /// <summary>
    /// Gets the amount to arrive.
    /// </summary>
    public byte AmountToArrive { get; private set; }

    /// <summary>
    /// Gets the offered item listings.
    /// </summary>
    public List<MarketBoardCurrentOfferings.MarketBoardItemListing> Listings { get; } = new();

    /// <summary>
    /// Gets the historical item listings.
    /// </summary>
    public List<MarketBoardHistory.MarketBoardHistoryListing> History { get; } = new();

    /// <summary>
    /// Gets or sets the listing request ID.
    /// </summary>
    public int ListingsRequestId { get; set; } = -1;

    /// <summary>
    /// Gets a value indicating whether the upload is complete.
    /// </summary>
    public bool IsDone => this.Listings.Count == this.AmountToArrive && this.History.Count != 0;

    /// <summary>
    /// Read a packet off the wire.
    /// </summary>
    /// <param name="dataPtr">Packet data.</param>
    /// <returns>An object representing the data read.</returns>
    public static unsafe MarketBoardItemRequest Read(IntPtr dataPtr)
    {
        using var stream = new UnmanagedMemoryStream((byte*)dataPtr.ToPointer(), 1544);
        using var reader = new BinaryReader(stream);

        var output = new MarketBoardItemRequest();

        output.CatalogId = reader.ReadUInt32();
        output.Status = reader.ReadUInt32();
        stream.Position += 0x3;
        output.AmountToArrive = reader.ReadByte();

        return output;
    }
}
