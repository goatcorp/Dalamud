using System;
using System.Collections.Generic;
using System.IO;

namespace Dalamud.Game.Chat.SeStringHandling.Payloads
{
    public class UIGlowPayload : Payload
    {
        public override PayloadType Type => PayloadType.UIGlow;

        public ushort RawColor { get; private set; }

        //public int Red { get; private set; }
        //public int Green { get; private set; }
        //public int Blue { get; private set; }

        public override byte[] Encode()
        {
            var colorBytes = MakeInteger(RawColor);
            var chunkLen = colorBytes.Length + 1;

            var bytes = new List<byte>(new byte[]
            {
                START_BYTE, (byte)SeStringChunkType.UIGlow, (byte)chunkLen
            });

            bytes.AddRange(colorBytes);
            bytes.Add(END_BYTE);

            return bytes.ToArray();
        }

        public override void Resolve()
        {
            // TODO: resolve color keys to hex colors via UIColor table
        }

        public override string ToString()
        {
            return $"{Type} - RawColor: {RawColor}";
        }

        protected override void ProcessChunkImpl(BinaryReader reader, long endOfStream)
        {
            RawColor = (ushort)GetInteger(reader);
        }

        protected override byte GetMarkerForIntegerBytes(byte[] bytes)
        {
            return bytes.Length switch
            {
                // a single byte of 0x01 is used to 'disable' color, and has no marker
                1 => (byte)IntegerType.None,
                _ => base.GetMarkerForIntegerBytes(bytes)
            };
        }
    }
}
