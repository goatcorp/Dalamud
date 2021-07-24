using System.Collections.Generic;

using Dalamud.Game.Network.Structures;

namespace Dalamud.Game.Network.Internal.MarketBoardUploaders
{
    /// <summary>
    /// This represents a submission to a marketboard aggregation website.
    /// </summary>
    internal class MarketBoardItemRequest
    {
        /// <summary>
        /// Gets or sets the catalog ID.
        /// </summary>
        public uint CatalogId { get; set; }

        /// <summary>
        /// Gets or sets the amount to arrive.
        /// </summary>
        public byte AmountToArrive { get; set; }

        /// <summary>
        /// Gets or sets the offered item listings.
        /// </summary>
        public List<MarketBoardCurrentOfferings.MarketBoardItemListing> Listings { get; set; }

        /// <summary>
        /// Gets or sets the historical item listings.
        /// </summary>
        public List<MarketBoardHistory.MarketBoardHistoryListing> History { get; set; }

        /// <summary>
        /// Gets or sets the listing request ID.
        /// </summary>
        public int ListingsRequestId { get; set; } = -1;

        /// <summary>
        /// Gets a value indicating whether the upload is complete.
        /// </summary>
        public bool IsDone => this.Listings.Count == this.AmountToArrive && this.History.Count != 0;
    }
}
