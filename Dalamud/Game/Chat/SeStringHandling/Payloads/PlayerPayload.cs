using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Dalamud.Game.Chat.SeStringHandling.Payloads
{
    public class PlayerPayload : Payload
    {
        public override PayloadType Type => PayloadType.Player;

        public string PlayerName { get; private set; }

        private World world;
        public World World
        {
            get
            {
                this.world ??= this.dataResolver.GetExcelSheet<World>().GetRow((int)this.serverId);
                return this.world;
            }
        }

        private uint serverId;

        public override byte[] Encode()
        {
            var chunkLen = PlayerName.Length + 7;
            var bytes = new List<byte>()
            {
                START_BYTE,
                (byte)SeStringChunkType.Interactable, (byte)chunkLen, (byte)EmbeddedInfoType.PlayerName,
                /* unk */ 0x01,
                (byte)(this.serverId+1),                 // I didn't want to deal with single-byte values in MakeInteger, so we have to do the +1 manually
                /* unk */0x01, /* unk */0xFF,       // these sometimes vary but are frequently this
                (byte)(PlayerName.Length+1)
            };

            bytes.AddRange(Encoding.UTF8.GetBytes(PlayerName));
            bytes.Add(END_BYTE);

            // encoded names are followed by the name in plain text again
            // use the payload parsing for consistency, as this is technically a new chunk
            // bytes.AddRange(new TextPayload(PlayerName).Encode());

            // FIXME
            bytes.AddRange(Encoding.UTF8.GetBytes(PlayerName));

            // unsure about this entire packet, but it seems to always follow a name
            bytes.AddRange(new byte[]
            {
                START_BYTE, (byte)SeStringChunkType.Interactable, 0x07, (byte)EmbeddedInfoType.LinkTerminator,
                0x01, 0x01, 0x01, 0xFF, 0x01,
                END_BYTE
            });

            return bytes.ToArray();
        }

        public override string ToString()
        {
            return $"{Type} - PlayerName: {PlayerName}, ServerId: {serverId}";
        }

        protected override void ProcessChunkImpl(BinaryReader reader, long endOfStream)
        {
            // unk
            reader.ReadByte();

            this.serverId = GetInteger(reader);

            // unk
            reader.ReadBytes(2);

            var nameLen = (int)GetInteger(reader);
            PlayerName = Encoding.UTF8.GetString(reader.ReadBytes(nameLen));
        }
    }
}
