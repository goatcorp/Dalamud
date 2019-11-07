using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Dalamud.Game.Network.MarketBoardUploaders.Universalis
{
    class UniversalisTaxUploadRequest
    {
        [JsonProperty("uploaderID")]
        public ulong UploaderId { get; set; }

        [JsonProperty("worldID")]
        public int WorldId { get; set; }

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
