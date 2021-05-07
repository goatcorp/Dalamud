using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using JetBrains.Annotations;

namespace Dalamud.Game.Text.SeStringHandling.Payloads {

    /// <summary>
    /// 
    /// </summary>
    public class DalamudLinkPayload : Payload {
        public override PayloadType Type => PayloadType.DalamudLink;

        public uint CommandId { get; internal set; } = 0;

        [NotNull]
        public string Plugin { get; internal set; } = string.Empty;
        
        protected override byte[] EncodeImpl() {
            var pluginBytes = Encoding.UTF8.GetBytes(Plugin);
            var commandBytes = MakeInteger(CommandId);
            var chunkLen = 3 + pluginBytes.Length + commandBytes.Length;

            if (chunkLen > 255) {
                throw new Exception("Chunk is too long. Plugin name exceeds limits for DalamudLinkPayload");
            }

            var bytes = new List<byte> {START_BYTE, (byte) SeStringChunkType.Interactable, (byte) chunkLen, (byte) EmbeddedInfoType.DalamudLink};
            bytes.Add((byte) pluginBytes.Length);
            bytes.AddRange(pluginBytes);
            bytes.AddRange(commandBytes);
            bytes.Add(END_BYTE);
            return bytes.ToArray();
        }

        protected override void DecodeImpl(BinaryReader reader, long endOfStream) {
            Plugin = Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadByte()));
            CommandId = GetInteger(reader);
        }

        public override string ToString() {
            return $"{Type} -  Plugin: {Plugin}, Command: {CommandId}";
        }
    }
}
