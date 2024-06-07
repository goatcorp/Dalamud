using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Dalamud.Game.Network.Structures;

/// <summary>
/// This class represents the market board history from a game network packet.
/// </summary>
public class MarketBoardHistory
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MarketBoardHistory"/> class.
    /// </summary>
    internal MarketBoardHistory()
    {
    }

    /// <summary>
    /// Gets the catalog ID.
    /// </summary>
    public uint CatalogId { get; private set; }

    /// <summary>
    /// Gets the second catalog ID.
    /// </summary>
    public uint CatalogId2 { get; private set; }

    /// <summary>
    /// Gets the list of individual item history listings.
    /// </summary>
    public List<MarketBoardHistoryListing> HistoryListings { get; } = new();

    /// <summary>
    /// Read a <see cref="MarketBoardHistory"/> object from memory.
    /// </summary>
    /// <param name="dataPtr">Address to read.</param>
    /// <returns>A new <see cref="MarketBoardHistory"/> object.</returns>
    public static unsafe MarketBoardHistory Read(IntPtr dataPtr)
    {
        using var stream = new UnmanagedMemoryStream((byte*)dataPtr.ToPointer(), 1544);
        using var reader = new BinaryReader(stream);

        var output = new MarketBoardHistory
        {
            CatalogId = reader.ReadUInt32(),
            CatalogId2 = reader.ReadUInt32(),
        };

        if (output.CatalogId2 == 0)
        {
            // No items found in the resulting packet - just return the empty history.
            return output;
        }

        for (var i = 0; i < 20; i++)
        {
            var listingEntry = new MarketBoardHistoryListing
            {
                SalePrice = reader.ReadUInt32(),
                PurchaseTime = DateTimeOffset.FromUnixTimeSeconds(reader.ReadUInt32()).UtcDateTime,
                Quantity = reader.ReadUInt32(),
                IsHq = reader.ReadBoolean(),
            };

            reader.ReadBoolean();

            listingEntry.OnMannequin = reader.ReadBoolean();
            listingEntry.BuyerName = Encoding.UTF8.GetString(reader.ReadBytes(33)).TrimEnd('\u0000');
            listingEntry.NextCatalogId = reader.ReadUInt32();

            output.HistoryListings.Add(listingEntry);

            if (listingEntry.NextCatalogId == 0)
                break;
        }

        return output;
    }

    /// <summary>
    /// This class represents the market board history of a single item from the <see cref="MarketBoardHistory"/> network packet.
    /// </summary>
    public class MarketBoardHistoryListing
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MarketBoardHistoryListing"/> class.
        /// </summary>
        internal MarketBoardHistoryListing()
        {
        }

        /// <summary>
        /// Gets the buyer's name.
        /// </summary>
        public string BuyerName { get; internal set; }

        /// <summary>
        /// Gets the next entry's catalog ID.
        /// </summary>
        public uint NextCatalogId { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether the item is HQ.
        /// </summary>
        public bool IsHq { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether the item is on a mannequin.
        /// </summary>
        public bool OnMannequin { get; internal set; }

        /// <summary>
        /// Gets the time of purchase.
        /// </summary>
        public DateTime PurchaseTime { get; internal set; }

        /// <summary>
        /// Gets the quantity.
        /// </summary>
        public uint Quantity { get; internal set; }

        /// <summary>
        /// Gets the sale price.
        /// </summary>
        public uint SalePrice { get; internal set; }
    }
}
