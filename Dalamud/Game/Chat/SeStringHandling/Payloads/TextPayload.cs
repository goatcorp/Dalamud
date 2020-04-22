using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Dalamud.Game.Chat.SeStringHandling.Payloads
{
    public class TextPayload : Payload
    {
        public override PayloadType Type => PayloadType.RawText;

        public string Text { get; private set; }

        public override string ToString()
        {
            return $"{Type} - Text: {Text}";
        }

        protected override byte[] EncodeImpl()
        {
            return Encoding.UTF8.GetBytes(Text);
        }

        protected override void DecodeImpl(BinaryReader reader, long endOfStream)
        {
            var text = new List<byte>();

            while (reader.BaseStream.Position < endOfStream)
            {
                if ((byte)reader.PeekChar() == START_BYTE)
                    break;

                // not the most efficient, but the easiest
                text.Add(reader.ReadByte());
            }

            if (text.Count > 0)
            {
                // TODO: handling of the game's assorted special unicode characters
                Text = Encoding.UTF8.GetString(text.ToArray());
            }
        }
    }
}
