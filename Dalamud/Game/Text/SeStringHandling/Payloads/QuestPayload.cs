using System.Collections.Generic;
using System.IO;

using Dalamud.Data;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;

namespace Dalamud.Game.Text.SeStringHandling.Payloads {
    /// <summary>
    /// An SeString Payload representing an interactable quest link.
    /// </summary>
    public class QuestPayload : Payload {
        public override PayloadType Type => PayloadType.Quest;

        private Quest quest;
        /// <summary>
        /// The underlying Lumina Quest represented by this payload.
        /// </summary>
        /// <remarks>
        /// Value is evaluated lazily and cached.
        /// </remarks>
        
        [JsonIgnore]
        public Quest Quest {
            get {
                this.quest ??= this.DataResolver.GetExcelSheet<Quest>().GetRow(this.questId);
                return this.quest;
            }
        }

        [JsonProperty]
        private uint questId;

        internal QuestPayload() { }

        /// <summary>
        /// Creates a payload representing an interactable quest link for the specified quest.
        /// </summary>
        /// <param name="data">DataManager instance needed to resolve game data.</param>
        /// <param name="questId">The id of the quest.</param>
        public QuestPayload(DataManager data, uint questId) {
            this.DataResolver = data;
            this.questId = questId;
        }

        /// <inheritdoc />
        public override string ToString() {
            return $"{Type} - QuestId: {this.questId}, Name: {Quest?.Name ?? "QUEST NOT FOUND"}";
        }

        protected override byte[] EncodeImpl() {
            var idBytes = MakeInteger((ushort) this.questId);
            var chunkLen = idBytes.Length + 4;

            var bytes = new List<byte>() {
                START_BYTE, (byte) SeStringChunkType.Interactable, (byte) chunkLen, (byte) EmbeddedInfoType.QuestLink,
            };

            bytes.AddRange(idBytes);
            bytes.AddRange(new byte[] {0x01, 0x01, END_BYTE});
            return bytes.ToArray();

        }

        protected override void DecodeImpl(BinaryReader reader, long endOfStream) {
            // Game uses int16, Luimina uses int32
            this.questId = GetInteger(reader) + 65536;
        }
    }
}
