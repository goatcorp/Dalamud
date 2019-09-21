using System.Collections.Generic;
using Newtonsoft.Json;

namespace Dalamud.Game.Network.MarketBoardUploaders.Universalis {
    internal class UniversalisHistoryUploadRequest {
        [JsonProperty("worldID")]
        public int WorldId { get; set; }

        [JsonProperty("itemID")]
        public uint ItemId { get; set; }

        [JsonProperty("entries")]
        public List<UniversalisHistoryEntry> Entries { get; set; }

        [JsonProperty("uploaderID")]
        public ulong UploaderId { get; set; }
    }
}
