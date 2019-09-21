using System.Collections.Generic;
using Newtonsoft.Json;

namespace Dalamud.Game.Network.MarketBoardUploaders.Universalis {
    internal class UniversalisItemListingsEntry {
        [JsonProperty("listingID")]
        public ulong ListingId { get; set; }

        [JsonProperty("hq")]
        public bool Hq { get; set; }

        [JsonProperty("pricePerUnit")]
        public uint PricePerUnit { get; set; }

        [JsonProperty("quantity")]
        public uint Quantity { get; set; }

        [JsonProperty("retainerName")]
        public string RetainerName { get; set; }

        [JsonProperty("retainerID")]
        public ulong RetainerId { get; set; }

        [JsonProperty("creatorName")]
        public string CreatorName { get; set; }

        [JsonProperty("onMannequin")]
        public bool OnMannequin { get; set; }

        [JsonProperty("sellerID")]
        public ulong SellerId { get; set; }

        [JsonProperty("creatorID")]
        public ulong CreatorId { get; set; }

        [JsonProperty("stainID")]
        public int StainId { get; set; }

        [JsonProperty("retainerCity")]
        public int RetainerCity { get; set; }

        [JsonProperty("lastReviewTime")]
        public long LastReviewTime { get; set; }

        [JsonProperty("materia")]
        public List<UniversalisItemMateria> Materia { get; set; }
    }
}
