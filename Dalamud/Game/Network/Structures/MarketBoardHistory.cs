using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Dalamud.Game.Network.Structures;

/// <summary>
/// This class represents the market board history from a game network packet.
/// </summary>
public class MarketBoardHistory : IMarketBoardHistory
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
    /// Gets the ID (for EXD) for the item being sold.
    /// </summary>
    public uint ItemId => this.CatalogId;

    /// <summary>
    /// Gets the list of individual item listings.
    /// </summary>
    public IReadOnlyList<IMarketBoardHistoryListing> HistoryListings => this.InternalHistoryListings;

    /// <summary>
    /// Gets or sets a list of individual item listings.
    /// </summary>
    internal List<MarketBoardHistoryListing> InternalHistoryListings { get; set; } = [];

    /// <summary>
    /// Read a <see cref="MarketBoardHistory"/> object from memory.
    /// </summary>
    /// <param name="dataPtr">Address to read.</param>
    /// <returns>A new <see cref="MarketBoardHistory"/> object.</returns>
    public static unsafe MarketBoardHistory Read(nint dataPtr)
    {
        using var stream = new UnmanagedMemoryStream((byte*)dataPtr.ToPointer(), 1544);
        using var reader = new BinaryReader(stream);

        var output = new MarketBoardHistory { CatalogId = reader.ReadUInt32() };

        var historyListings = new List<MarketBoardHistoryListing>();
        for (var i = 0; i < 20; i++)
        {
            var price = reader.ReadUInt32();
            if (price == 0)
            {
                // no price means we reached the end of available listings
                break;
            }

            var listingEntry = new MarketBoardHistoryListing
            {
                SalePrice = price,
                PurchaseTime = DateTimeOffset.FromUnixTimeSeconds(reader.ReadUInt32()).UtcDateTime,
                Quantity = reader.ReadUInt32(),
                IsHq = reader.ReadBoolean(),
                OnMannequin = reader.ReadBoolean(),
                BuyerName = Encoding.UTF8.GetString(reader.ReadBytes(0x20)).TrimEnd('\u0000'),
            };

            // Skip padding
            reader.ReadBytes(0x2);

            historyListings.Add(listingEntry);
        }

        output.InternalHistoryListings = historyListings;

        return output;
    }

    /// <summary>
    /// This class represents the market board history of a single item from the <see cref="MarketBoardHistory"/> network packet.
    /// </summary>
    public class MarketBoardHistoryListing : IMarketBoardHistoryListing
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
