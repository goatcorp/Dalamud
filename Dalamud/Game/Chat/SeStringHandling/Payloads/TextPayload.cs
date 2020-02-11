using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dalamud.Game.Chat.SeStringHandling.Payloads
{
    public class TextPayload : Payload
    {
        public override PayloadType Type => PayloadType.RawText;

        public string Text { get; set; }

        public TextPayload() { }

        public TextPayload(string text)
        {
            Text = text;
        }

        public override void Resolve()
        {
            // nothing to do
        }

        public override byte[] Encode()
        {
            return Encoding.UTF8.GetBytes(Text);
        }

        public override string ToString()
        {
            return $"{Type} - Text: {Text}";
        }

        protected override void ProcessChunkImpl(BinaryReader reader, long endOfStream)
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
