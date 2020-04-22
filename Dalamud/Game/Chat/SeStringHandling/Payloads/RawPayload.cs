using System;
using System.Collections.Generic;
using System.IO;

namespace Dalamud.Game.Chat.SeStringHandling.Payloads
{
    public class RawPayload : Payload
    {
        public override PayloadType Type => PayloadType.Unknown;

        public byte[] Data { get; private set; }

        private byte chunkType;

        public RawPayload(byte chunkType)
        {
            this.chunkType = chunkType;
        }

        public override string ToString()
        {
            return $"{Type} - Chunk type: {chunkType:X}, Data: {BitConverter.ToString(Data).Replace("-", " ")}";
        }

        protected override byte[] EncodeImpl()
        {
            var chunkLen = Data.Length + 1;

            var bytes = new List<byte>()
            {
                START_BYTE,
                this.chunkType,
                (byte)chunkLen
            };
            bytes.AddRange(Data);

            bytes.Add(END_BYTE);

            return bytes.ToArray();
        }

        protected override void DecodeImpl(BinaryReader reader, long endOfStream)
        {
            Data = reader.ReadBytes((int)(endOfStream - reader.BaseStream.Position + 1));
        }
    }
}
