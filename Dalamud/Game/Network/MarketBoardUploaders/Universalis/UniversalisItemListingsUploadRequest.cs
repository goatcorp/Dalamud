using System.Collections.Generic;

using Newtonsoft.Json;

namespace Dalamud.Game.Network.MarketBoardUploaders.Universalis
{
    internal class UniversalisItemListingsUploadRequest
    {
        [JsonProperty("worldID")]
        public uint WorldId { get; set; }

        [JsonProperty("itemID")]
        public uint ItemId { get; set; }

        [JsonProperty("listings")]
        public List<UniversalisItemListingsEntry> Listings { get; set; }

        [JsonProperty("uploaderID")]
        public string UploaderId { get; set; }
    }
}
