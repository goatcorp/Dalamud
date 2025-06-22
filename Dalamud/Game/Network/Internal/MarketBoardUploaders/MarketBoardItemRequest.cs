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
    /// Gets the request status. Nonzero statuses are errors.
    /// Known values: default=0; rate limited=0x70000003.
    /// </summary>
    public uint Status { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this request was successful.
    /// </summary>
    public bool Ok => this.Status == 0;

    /// <summary>
    /// Gets the amount to arrive.
    /// </summary>
    public uint AmountToArrive { get; private set; }

    /// <summary>
    /// Gets or sets the offered catalog id used in this listing.
    /// </summary>
    public uint CatalogId { get; internal set; }

    /// <summary>
    /// Gets the offered item listings.
    /// </summary>
    public List<MarketBoardCurrentOfferings.MarketBoardItemListing> Listings { get; } = [];

    /// <summary>
    /// Gets the historical item listings.
    /// </summary>
    public List<MarketBoardHistory.MarketBoardHistoryListing> History { get; } = [];

    /// <summary>
    /// Read a packet off the wire.
    /// </summary>
    /// <param name="dataPtr">Packet data.</param>
    /// <returns>An object representing the data read.</returns>
    public static unsafe MarketBoardItemRequest Read(nint dataPtr)
    {
        using var stream = new UnmanagedMemoryStream((byte*)dataPtr.ToPointer(), 1544);
        using var reader = new BinaryReader(stream);

        var output = new MarketBoardItemRequest
        {
            Status = reader.ReadUInt32(),
            AmountToArrive = reader.ReadUInt32(),
        };

        return output;
    }
}
