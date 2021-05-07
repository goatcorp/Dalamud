using Newtonsoft.Json;

namespace Dalamud.Game.Network.MarketBoardUploaders.Universalis
{
    class UniversalisTaxUploadRequest
    {
        [JsonProperty("uploaderID")]
        public string UploaderId { get; set; }

        [JsonProperty("worldID")]
        public uint WorldId { get; set; }

        [JsonProperty("marketTaxRates")]
        public UniversalisTaxData TaxData { get; set; }
    }

    class UniversalisTaxData {
        [JsonProperty("limsaLominsa")]
        public uint LimsaLominsa { get; set; }

        [JsonProperty("gridania")]
        public uint Gridania { get; set; }

        [JsonProperty("uldah")]
        public uint Uldah { get; set; }

        [JsonProperty("ishgard")]
        public uint Ishgard { get; set; }

        [JsonProperty("kugane")]
        public uint Kugane { get; set; }

        [JsonProperty("crystarium")]
        public uint Crystarium { get; set; }
    }
}
