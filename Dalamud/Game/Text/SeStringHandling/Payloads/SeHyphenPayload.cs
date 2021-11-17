using System.IO;

namespace Dalamud.Game.Text.SeStringHandling.Payloads;

/// <summary>
/// A wrapped '–'.
/// </summary>
public class SeHyphenPayload : Payload, ITextProvider
{
    private readonly byte[] bytes = { START_BYTE, (byte)SeStringChunkType.SeHyphen, 0x01, END_BYTE };

    /// <summary>
    /// Gets an instance of SeHyphenPayload.
    /// </summary>
    public static SeHyphenPayload Payload => new();

    /// <summary>
    /// Gets the text, just a '–'.
    /// </summary>
    public string Text => "–";

    /// <inheritdoc/>
    public override PayloadType Type => PayloadType.SeHyphen;

    /// <inheritdoc />
    protected override byte[] EncodeImpl() => this.bytes;

    /// <inheritdoc />
    protected override void DecodeImpl(BinaryReader reader, long endOfStream)
    {
    }
}
