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

        private string textConverted = null;

        /// <summary>
        /// The Text of this text payload as an UTF-8 converted string.
        /// Don't rely on this for accurate representation of SE payload data, please check RawData instead.
        /// </summary>
        public string Text {
            get { return this.textConverted ??= Encoding.UTF8.GetString(RawData); }
            set {
                this.textConverted = value;
                RawData = Encoding.UTF8.GetBytes(value);
            }
        }

        /// <summary>
        /// The raw unconverted data of this text payload.
        /// </summary>
        public byte[] RawData { get; set; }

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
                var nextByte = reader.ReadByte();
                if (nextByte == START_BYTE)
                {
                    // rewind since this byte isn't part of this payload
                    reader.BaseStream.Position--;
                    break;
                }

                text.Add(nextByte);
            }

            if (text.Count > 0)
            {
                // TODO: handling of the game's assorted special unicode characters
                Text = Encoding.UTF8.GetString(text.ToArray());
            }
        }
    }
}
