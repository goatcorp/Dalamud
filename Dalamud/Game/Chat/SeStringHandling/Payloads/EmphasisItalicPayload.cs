using System;
using System.Collections.Generic;
using System.IO;

namespace Dalamud.Game.Chat.SeStringHandling.Payloads
{
    class EmphasisItalicPayload : Payload
    {
        public static EmphasisItalicPayload ItalicsOn => new EmphasisItalicPayload(true);
        public static EmphasisItalicPayload ItalicsOff => new EmphasisItalicPayload(false);

        public override PayloadType Type => PayloadType.EmphasisItalic;

        public bool IsEnabled { get; private set; }

        internal EmphasisItalicPayload() { }

        public EmphasisItalicPayload(bool enabled)
        {
            IsEnabled = enabled;
        }

        public override string ToString()
        {
            return $"{Type} - Enabled: {IsEnabled}";
        }

        protected override byte[] EncodeImpl()
        {
            // realistically this will always be a single byte of value 1 or 2
            // but we'll treat it normally anyway
            var enabledBytes = MakeInteger(IsEnabled ? (uint)1 : 0);

            var chunkLen = enabledBytes.Length + 1;
            var bytes = new List<byte>()
            {
                START_BYTE, (byte)SeStringChunkType.EmphasisItalic, (byte)chunkLen
            };
            bytes.AddRange(enabledBytes);
            bytes.Add(END_BYTE);

            return bytes.ToArray();
        }

        protected override void DecodeImpl(BinaryReader reader, long endOfStream)
        {
            IsEnabled = (GetInteger(reader) == 1);
        }
    }
}
