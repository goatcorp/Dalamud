using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.IO;

namespace Dalamud.Game.Chat.SeStringHandling.Payloads
{
    public class UIForegroundPayload : Payload
    {
        public static UIForegroundPayload UIForegroundOff => new UIForegroundPayload(0);

        public override PayloadType Type => PayloadType.UIForeground;

        public bool IsEnabled => ColorKey != 0;

        private UIColor color;
        public UIColor UIColor
        {
            get
            {
                this.color ??= this.dataResolver.GetExcelSheet<UIColor>().GetRow(this.colorKey);
                return this.color;
            }
        }

        public ushort ColorKey
        {
            get { return this.colorKey; }
            set
            {
                this.colorKey = value;
                this.color = null;
                Dirty = true;
            }
        }

        public uint RGB
        {
            get
            {
                return (UIColor.UIForeground & 0xFFFFFF);
            }
        }

        private ushort colorKey;

        internal UIForegroundPayload() { }

        public UIForegroundPayload(ushort colorKey)
        {
            this.colorKey = colorKey;
        }

        public override string ToString()
        {
            return $"{Type} - UIColor: {colorKey}";
        }

        protected override byte[] EncodeImpl()
        {
            var colorBytes = MakeInteger(this.colorKey);
            var chunkLen = colorBytes.Length + 1;

            var bytes = new List<byte>(new byte[]
            {
                START_BYTE, (byte)SeStringChunkType.UIForeground, (byte)chunkLen
            });

            bytes.AddRange(colorBytes);
            bytes.Add(END_BYTE);

            return bytes.ToArray();
        }

        protected override void DecodeImpl(BinaryReader reader, long endOfStream)
        {
            this.colorKey = (ushort)GetInteger(reader);
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
