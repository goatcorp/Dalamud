using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Dalamud.Game.Chat.SeStringHandling.Payloads
{
    public class RawPayload : Payload
    {
        public override PayloadType Type => PayloadType.Unknown;

        private byte[] data;
        public byte[] Data
        {
            get
            {
                // for now don't allow modifying the contents
                // because we don't really have a way to track Dirty
                return (byte[])data.Clone();
            }
        }

        private byte chunkType;

        internal RawPayload(byte chunkType)
        {
            this.chunkType = chunkType;
        }

        public RawPayload(byte[] data)
        {
            this.chunkType = data[0];
            this.data = data.Skip(1).ToArray();
        }

        public override string ToString()
        {
            return $"{Type} - Chunk type: {chunkType:X}, Data: {BitConverter.ToString(data).Replace("-", " ")}";
        }

        protected override byte[] EncodeImpl()
        {
            var chunkLen = this.data.Length + 1;

            var bytes = new List<byte>()
            {
                START_BYTE,
                this.chunkType,
                (byte)chunkLen
            };
            bytes.AddRange(this.data);

            bytes.Add(END_BYTE);

            return bytes.ToArray();
        }

        protected override void DecodeImpl(BinaryReader reader, long endOfStream)
        {
            this.data = reader.ReadBytes((int)(endOfStream - reader.BaseStream.Position + 1));
        }
    }
}
