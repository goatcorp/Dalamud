using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Dalamud.Game.Chat.SeStringHandling.Payloads
{
    /// <summary>
    /// An SeString Payload representing unhandled raw payload data.
    /// Mainly useful for constructing unhandled hardcoded payloads, or forwarding any unknown
    /// payloads without modification.
    /// </summary>
    public class RawPayload : Payload
    {
        // this and others could be an actual static member somewhere and avoid construction costs, but that probably isn't a real concern
        /// <summary>
        /// A fixed Payload representing a common link-termination sequence, found in many payload chains.
        /// </summary>
        public static RawPayload LinkTerminator => new RawPayload(new byte[] { 0x02, 0x27, 0x07, 0xCF, 0x01, 0x01, 0x01, 0xFF, 0x01, 0x03 });

        public override PayloadType Type => PayloadType.Unknown;

        [JsonProperty]
        private byte[] data;
        // this is a bit different from the underlying data
        // We need to store just the chunk data for decode to behave nicely, but when reading data out
        // it makes more sense to get the entire payload
        /// <summary>
        /// The entire payload byte sequence for this payload.
        /// The returned data is a clone and modifications will not be persisted.
        /// </summary>
        [JsonIgnore]
        public byte[] Data
        {
            get
            {
                // for now don't allow modifying the contents
                // because we don't really have a way to track Dirty
                return (byte[])Encode().Clone();
            }
        }

        [JsonProperty]
        private byte chunkType;

        [JsonConstructor]
        internal RawPayload(byte chunkType)
        {
            this.chunkType = chunkType;
        }

        public RawPayload(byte[] data)
        {
            // this payload is 'special' in that we require the entire chunk to be passed in
            // and not just the data after the header
            // This sets data to hold the chunk data fter the header, excluding the END_BYTE
            this.chunkType = data[1];
            this.data = data.Skip(3).Take(data.Length-4).ToArray();
        }

        public override string ToString()
        {
            return $"{Type} - Data: {BitConverter.ToString(Data).Replace("-", " ")}";
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
