using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Dalamud.Game.Chat.SeStringHandling.Payloads
{
    /// <summary>
    /// An SeString Payload representing a plain text string.
    /// </summary>
    public class TextPayload : Payload, ITextProvider
    {
        public override PayloadType Type => PayloadType.RawText;

        // allow modifying the text of existing payloads on the fly
        [JsonProperty]
        private string text;
        /// <summary>
        /// The text contained in this payload.
        /// This may contain SE's special unicode characters.
        /// </summary>
        [JsonIgnore]
        public string Text
        {
            get { return this.text; }
            set
            {
                this.text = value;
                Dirty = true;
            }
        }

        public override string ToString()
        {
            return $"{Type} - Text: {Text}";
        }

        internal TextPayload() { }

        /// <summary>
        /// Creates a new TextPayload for the given text.
        /// </summary>
        /// <param name="text">The text to include for this payload.</param>
        public TextPayload(string text)
        {
            this.text = text;
        }

        protected override byte[] EncodeImpl()
        {
            // special case to allow for empty text payloads, so users don't have to check
            // this may change or go away
            if (string.IsNullOrEmpty(this.text))
            {
                return new byte[] { };
            }

            return Encoding.UTF8.GetBytes(this.text);
        }

        protected override void DecodeImpl(BinaryReader reader, long endOfStream)
        {
            var textBytes = new List<byte>();

            while (reader.BaseStream.Position < endOfStream)
            {
                var nextByte = reader.ReadByte();
                if (nextByte == START_BYTE)
                {
                    // rewind since this byte isn't part of this payload
                    reader.BaseStream.Position--;
                    break;
                }

                textBytes.Add(nextByte);
            }

            if (textBytes.Count > 0)
            {
                // TODO: handling of the game's assorted special unicode characters
                this.text = Encoding.UTF8.GetString(textBytes.ToArray());
            }
        }
    }
}
