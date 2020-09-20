using System.Collections.Generic;
using System.IO;
using Serilog;

namespace Dalamud.Game.Chat.SeStringHandling.Payloads {

    /// <summary>
    /// SeString payload representing a bitmap icon from fontIcon
    /// </summary>
    public class IconPayload : Payload {

        /// <summary>
        /// Index of the icon
        /// </summary>
        public uint IconIndex { get; private set; }

        internal IconPayload() { }

        /// <summary>
        /// Create a Icon payload for the specified icon.
        /// </summary>
        /// <param name="iconIndex">Index of the icon</param>
        public IconPayload(uint iconIndex) {
            this.IconIndex = iconIndex;
        }

        public override PayloadType Type => PayloadType.Icon;

        protected override byte[] EncodeImpl() {
            var indexBytes = MakeInteger(this.IconIndex);
            var chunkLen = indexBytes.Length + 1;
            var bytes = new List<byte>(new byte[] {
                START_BYTE, (byte)SeStringChunkType.Icon, (byte)chunkLen
            });
            bytes.AddRange(indexBytes);
            bytes.Add(END_BYTE);
            return bytes.ToArray();
        }

        protected override void DecodeImpl(BinaryReader reader, long endOfStream) {
            this.IconIndex = GetInteger(reader);
        }

        public override string ToString() {
            return $"{Type} - IconIndex: {this.IconIndex}";
        }

    }
}
