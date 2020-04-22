using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Dalamud.Game.Chat.SeStringHandling.Payloads
{
    public class TextPayload : Payload, ITextProvider
    {
        public override PayloadType Type => PayloadType.RawText;

        // allow modifying the text of existing payloads on the fly
        private string text;
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
                if ((byte)reader.PeekChar() == START_BYTE)
                    break;

                // not the most efficient, but the easiest
                textBytes.Add(reader.ReadByte());
            }

            if (textBytes.Count > 0)
            {
                // TODO: handling of the game's assorted special unicode characters
                this.text = Encoding.UTF8.GetString(textBytes.ToArray());
            }
        }
    }
}
