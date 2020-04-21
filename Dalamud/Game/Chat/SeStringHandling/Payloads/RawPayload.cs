using System;
using System.Collections.Generic;
using System.IO;

namespace Dalamud.Game.Chat.SeStringHandling.Payloads
{
    public class RawPayload : Payload
    {
        public override PayloadType Type => PayloadType.Unknown;

        public byte ChunkType { get; set; }
        public byte[] Data { get; set; }

        public RawPayload(byte chunkType = 0)
        {
            ChunkType = chunkType;
        }

        public override void Resolve()
        {
            // nothing to do
        }

        public override byte[] Encode()
        {
            var chunkLen = Data.Length + 1;

            var bytes = new List<byte>()
            {
                START_BYTE,
                ChunkType,
                (byte)chunkLen
            };
            bytes.AddRange(Data);

            bytes.Add(END_BYTE);

            return bytes.ToArray();
        }

        public override string ToString()
        {
            return $"{Type} - Chunk type: {ChunkType:X}, Data: {BitConverter.ToString(Data).Replace("-", " ")}";
        }

        protected override void ProcessChunkImpl(BinaryReader reader, long endOfStream)
        {
            Data = reader.ReadBytes((int)(endOfStream - reader.BaseStream.Position + 1));
        }
    }
}
