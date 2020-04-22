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

        private string playerName;
        public string PlayerName
        {
            get { return this.playerName; }
            set
            {
                this.playerName = value;
                Dirty = true;
            }
        }

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

        internal PlayerPayload() { }

        public PlayerPayload(string playerName, uint serverId)
        {
            this.playerName = playerName;
            this.serverId = serverId;
        }

        public override string ToString()
        {
            return $"{Type} - PlayerName: {PlayerName}, ServerId: {serverId}";
        }

        protected override byte[] EncodeImpl()
        {
            var chunkLen = this.playerName.Length + 7;
            var bytes = new List<byte>()
            {
                START_BYTE,
                (byte)SeStringChunkType.Interactable, (byte)chunkLen, (byte)EmbeddedInfoType.PlayerName,
                /* unk */ 0x01,
                (byte)(this.serverId+1),                 // I didn't want to deal with single-byte values in MakeInteger, so we have to do the +1 manually
                /* unk */0x01, /* unk */0xFF,       // these sometimes vary but are frequently this
                (byte)(this.playerName.Length+1)
            };

            bytes.AddRange(Encoding.UTF8.GetBytes(this.playerName));
            bytes.Add(END_BYTE);

            // TODO: should these really be here? additional payloads should come in separately already...

            // encoded names are followed by the name in plain text again
            // use the payload parsing for consistency, as this is technically a new chunk
            bytes.AddRange(new TextPayload(playerName).Encode());

            // unsure about this entire packet, but it seems to always follow a name
            bytes.AddRange(new byte[]
            {
                START_BYTE, (byte)SeStringChunkType.Interactable, 0x07, (byte)EmbeddedInfoType.LinkTerminator,
                0x01, 0x01, 0x01, 0xFF, 0x01,
                END_BYTE
            });

            return bytes.ToArray();
        }

        protected override void DecodeImpl(BinaryReader reader, long endOfStream)
        {
            // unk
            reader.ReadByte();

            this.serverId = GetInteger(reader);

            // unk
            reader.ReadBytes(2);

            var nameLen = (int)GetInteger(reader);
            this.playerName = Encoding.UTF8.GetString(reader.ReadBytes(nameLen));
        }
    }
}
