using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.IO;
using Dalamud.Data;
using Newtonsoft.Json;

namespace Dalamud.Game.Chat.SeStringHandling.Payloads
{
    /// <summary>
    /// An SeString Payload representing an interactable status link.
    /// </summary>
    public class StatusPayload : Payload
    {
        public override PayloadType Type => PayloadType.Status;

        private Status status;
        /// <summary>
        /// The Lumina Status object represented by this payload.
        /// </summary>
        /// <remarks>
        /// Value is evaluated lazily and cached.
        /// </remarks>
        [JsonIgnore]
        public Status Status
        {
            get
            {
                status ??= this.DataResolver.GetExcelSheet<Status>().GetRow(this.statusId);
                return status;
            }
        }

        [JsonProperty]
        private uint statusId;

        internal StatusPayload() { }

        /// <summary>
        /// Creates a new StatusPayload for the given status id.
        /// </summary>
        /// <param name="data">DataManager instance needed to resolve game data.</param>
        /// <param name="statusId">The id of the Status for this link.</param>
        public StatusPayload(DataManager data, uint statusId) {
            this.DataResolver = data;
            this.statusId = statusId;
        }

        public override string ToString()
        {
            return $"{Type} - StatusId: {statusId}, Name: {Status.Name}";
        }

        protected override byte[] EncodeImpl()
        {
            var idBytes = MakeInteger(this.statusId);

            var chunkLen = idBytes.Length + 7;
            var bytes = new List<byte>()
            {
                START_BYTE, (byte)SeStringChunkType.Interactable, (byte)chunkLen, (byte)EmbeddedInfoType.Status
            };

            bytes.AddRange(idBytes);
            // unk
            bytes.AddRange(new byte[] { 0x01, 0x01, 0xFF, 0x02, 0x20, END_BYTE });

            return bytes.ToArray();
        }

        protected override void DecodeImpl(BinaryReader reader, long endOfStream)
        {
            this.statusId = GetInteger(reader);
        }
    }
}
