using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.IO;
using Dalamud.Data;
using Newtonsoft.Json;

namespace Dalamud.Game.Chat.SeStringHandling.Payloads
{
    /// <summary>
    /// An SeString Payload representing a UI glow color applied to following text payloads.
    /// </summary>
    public class UIGlowPayload : Payload
    {
        /// <summary>
        /// Payload representing disabling glow color on following text.
        /// </summary>
        // TODO Make this work with DI
        public static UIGlowPayload UIGlowOff => new UIGlowPayload(null, 0);

        public override PayloadType Type => PayloadType.UIGlow;

        /// <summary>
        /// Whether or not this payload represents applying a glow color, or disabling one.
        /// </summary>
        public bool IsEnabled => ColorKey != 0;

        private UIColor color;
        /// <summary>
        /// A Lumina UIColor object representing this payload.  The actual color data is at UIColor.UIGlow
        /// </summary>
        /// <remarks>
        /// Value is evaluated lazily and cached.
        /// </remarks>
        [JsonIgnore]
        public UIColor UIColor
        {
            get
            {
                this.color ??= this.DataResolver.GetExcelSheet<UIColor>().GetRow(this.colorKey);
                return this.color;
            }
        }

        /// <summary>
        /// The color key used as a lookup in the UIColor table for this glow color.
        /// </summary>
        [JsonIgnore]
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

        /// <summary>
        /// The Red/Green/Blue values for this glow color, encoded as a typical hex color.
        /// </summary>
        [JsonIgnore]
        public uint RGB
        {
            get
            {
                return (UIColor.UIGlow & 0xFFFFFF);
            }
        }

        [JsonProperty]
        private ushort colorKey;

        internal UIGlowPayload() { }

        /// <summary>
        /// Creates a new UIForegroundPayload for the given UIColor key.
        /// </summary>
        /// <param name="data">DataManager instance needed to resolve game data.</param>
        /// <param name="colorKey"></param>
        public UIGlowPayload(DataManager data, ushort colorKey) {
            this.DataResolver = data;
            this.colorKey = colorKey;
        }

        public override string ToString()
        {
            return $"{Type} - UIColor: {colorKey} color: {(IsEnabled ? RGB : 0)}";
        }

        protected override byte[] EncodeImpl()
        {
            var colorBytes = MakeInteger(this.colorKey);
            var chunkLen = colorBytes.Length + 1;

            var bytes = new List<byte>(new byte[]
            {
                START_BYTE, (byte)SeStringChunkType.UIGlow, (byte)chunkLen
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
