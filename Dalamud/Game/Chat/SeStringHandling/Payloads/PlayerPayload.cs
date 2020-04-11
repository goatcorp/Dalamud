using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dalamud.Game.Chat.SeStringHandling.Payloads
{
    public class PlayerPayload : Payload
    {
        public override PayloadType Type => PayloadType.Player;

        public string PlayerName { get; private set; }
        public uint ServerId { get; private set; }
        public string ServerName { get; private set; } = String.Empty;

        public PlayerPayload() { }

        public PlayerPayload(string playerName, uint serverId)
        {
            PlayerName = playerName;
            ServerId = serverId;
        }

        public override void Resolve()
        {
            if (string.IsNullOrEmpty(ServerName))
            {
                dynamic server = XivApi.Get($"World/{ServerId}").GetAwaiter().GetResult();
                ServerName = server.Name;
            }
        }

        public override byte[] Encode()
        {
            var chunkLen = PlayerName.Length + 7;
            var bytes = new List<byte>()
            {
                START_BYTE,
                (byte)SeStringChunkType.Interactable, (byte)chunkLen, (byte)EmbeddedInfoType.PlayerName,
                /* unk */ 0x01,
                (byte)(ServerId+1),     // I didn't want to deal with single-byte values in MakeInteger, so we have to do the +1 manually
                /* unk */0x01, /* unk */0xFF,       // these sometimes vary but are frequently this
                (byte)(PlayerName.Length+1)
            };

            bytes.AddRange(Encoding.UTF8.GetBytes(PlayerName));
            bytes.Add(END_BYTE);

            // encoded names are followed by the name in plain text again
            // use the payload parsing for consistency, as this is technically a new chunk
            bytes.AddRange(new TextPayload(PlayerName).Encode());

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
            return $"{Type} - PlayerName: {PlayerName}, ServerId: {ServerId}, ServerName: {ServerName}";
        }

        protected override void ProcessChunkImpl(BinaryReader reader, long endOfStream)
        {
            // unk
            reader.ReadByte();

            ServerId = GetInteger(reader);

            // unk
            reader.ReadBytes(2);

            var nameLen = (int)GetInteger(reader);
            PlayerName = Encoding.UTF8.GetString(reader.ReadBytes(nameLen));
        }
    }
}
