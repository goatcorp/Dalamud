using System.Collections.Generic;
using System.IO;
using System;

namespace Dalamud.Game.Text.SeStringHandling.Payloads {

    /// <summary>
    /// SeString payload representing a bitmap icon from fontIcon
    /// </summary>
    public class IconPayload : Payload {

        /// <summary>
        /// Index of the icon
        /// </summary>
        [Obsolete("Use IconPayload.Icon")]
        public uint IconIndex => (uint) Icon;

        /// <summary>
        /// Icon the payload represents.
        /// </summary>
        public BitmapFontIcon Icon { get; set; } = BitmapFontIcon.None;

        internal IconPayload() { }

        /// <summary>
        /// Create a Icon payload for the specified icon.
        /// </summary>
        /// <param name="iconIndex">Index of the icon</param>
        [Obsolete("IconPayload(uint) is deprecated, please use IconPayload(BitmapFontIcon).")]
        public IconPayload(uint iconIndex) : this((BitmapFontIcon) iconIndex) { }

        /// <summary>
        /// Create a Icon payload for the specified icon.
        /// </summary>
        /// <param name="icon">The Icon</param>
        public IconPayload(BitmapFontIcon icon) {
            Icon = icon;
        }

        /// <inheritdoc />
        public override PayloadType Type => PayloadType.Icon;

        /// <inheritdoc />
        protected override byte[] EncodeImpl() {
            var indexBytes = MakeInteger((uint) this.Icon);
            var chunkLen = indexBytes.Length + 1;
            var bytes = new List<byte>(new byte[] {
                START_BYTE, (byte)SeStringChunkType.Icon, (byte)chunkLen
            });
            bytes.AddRange(indexBytes);
            bytes.Add(END_BYTE);
            return bytes.ToArray();
        }

        /// <inheritdoc />
        protected override void DecodeImpl(BinaryReader reader, long endOfStream) {
            Icon = (BitmapFontIcon) GetInteger(reader);
        }

        /// <inheritdoc />
        public override string ToString() {
            return $"{Type} - {Icon}";
        }

    }
}
