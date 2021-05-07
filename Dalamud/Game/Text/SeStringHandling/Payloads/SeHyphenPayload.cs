using System.IO;

namespace Dalamud.Game.Text.SeStringHandling.Payloads
{
    /// <summary>
    /// A wrapped '–'.
    /// </summary>
    public class SeHyphenPayload : Payload, ITextProvider
    {
        /// <summary>
        /// Instance of SeHyphenPayload.
        /// </summary>
        public static SeHyphenPayload Payload => new SeHyphenPayload();

        /// <inheritdoc />
        public override PayloadType Type => PayloadType.SeHyphen;

        private readonly byte[] bytes = { START_BYTE, (byte)SeStringChunkType.SeHyphen, 0x01, END_BYTE };

        /// <inheritdoc />
        protected override byte[] EncodeImpl() => this.bytes;

        /// <inheritdoc />
        protected override void DecodeImpl(BinaryReader reader, long endOfStream)
        {
        }

        /// <summary>
        /// Just a '–'.
        /// </summary>
        public string Text => "–";
    }
}
