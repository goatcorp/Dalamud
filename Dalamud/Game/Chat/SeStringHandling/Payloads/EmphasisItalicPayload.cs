using System;
using System.Collections.Generic;
using System.IO;

namespace Dalamud.Game.Chat.SeStringHandling.Payloads
{
    /// <summary>
    /// An SeString Payload containing information about enabling or disabling italics formatting on following text.
    /// </summary>
    /// <remarks>
    /// As with other formatting payloads, this is only useful in a payload block, where it affects any subsequent
    /// text payloads.
    /// </remarks>
    public class EmphasisItalicPayload : Payload
    {
        /// <summary>
        /// Payload representing enabling italics on following text.
        /// </summary>
        public static EmphasisItalicPayload ItalicsOn => new EmphasisItalicPayload(true);
        /// <summary>
        /// Payload representing disabling italics on following text.
        /// </summary>
        public static EmphasisItalicPayload ItalicsOff => new EmphasisItalicPayload(false);

        public override PayloadType Type => PayloadType.EmphasisItalic;

        /// <summary>
        /// Whether this payload enables italics formatting for following text.
        /// </summary>
        public bool IsEnabled { get; private set; }

        internal EmphasisItalicPayload() { }

        /// <summary>
        /// Creates an EmphasisItalicPayload.
        /// </summary>
        /// <param name="enabled">Whether italics formatting should be enabled or disabled for following text.</param>
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
