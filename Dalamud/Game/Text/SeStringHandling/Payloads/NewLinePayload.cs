using System.IO;

namespace Dalamud.Game.Text.SeStringHandling.Payloads;

/// <summary>
/// A wrapped newline character.
/// </summary>
public class NewLinePayload : Payload, ITextProvider
{
    private readonly byte[] bytes = { START_BYTE, (byte)SeStringChunkType.NewLine, 0x01, END_BYTE };

    /// <summary>
    /// Gets an instance of NewLinePayload.
    /// </summary>
    public static NewLinePayload Payload => new();

    /// <summary>
    /// Gets the text of this payload, evaluates to <c>Environment.NewLine</c>.
    /// </summary>
    public string Text => Environment.NewLine;

    /// <inheritdoc/>
    public override PayloadType Type => PayloadType.NewLine;

    /// <inheritdoc/>
    protected override byte[] EncodeImpl() => this.bytes;

    /// <inheritdoc/>
    protected override void DecodeImpl(BinaryReader reader, long endOfStream)
    {
    }
}
