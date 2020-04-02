using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dalamud.Game.Chat.SeStringHandling.Payloads
{
    public class StatusPayload : Payload
    {
        public override PayloadType Type => PayloadType.Status;

        public int StatusId { get; private set; }

        public string StatusName { get; private set; } = string.Empty;

        public StatusPayload() { }

        public StatusPayload(int statusId)
        {
            StatusId = statusId;
        }

        public override void Resolve()
        {
            if (string.IsNullOrEmpty(StatusName))
            {
                dynamic status = XivApi.Get($"Status/{StatusId}").GetAwaiter().GetResult();
                //Console.WriteLine($"Resolved status {StatusId} to {status.Name}");
                StatusName = status.Name;
            }
        }

        public override byte[] Encode()
        {
            var idBytes = MakeInteger(StatusId);

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

        public override string ToString()
        {
            return $"{Type} - StatusId: {StatusId}, StatusName: {StatusName}";
        }

        protected override void ProcessChunkImpl(BinaryReader reader, long endOfStream)
        {
            StatusId = GetInteger(reader);
        }
    }
}
