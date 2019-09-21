using Newtonsoft.Json;

namespace Dalamud.Game.Network.MarketBoardUploaders.Universalis {
    internal class UniversalisItemMateria {
        [JsonProperty("slotID")]
        public int SlotId { get; set; }

        [JsonProperty("materiaID")]
        public int MateriaId { get; set; }
    }
}
