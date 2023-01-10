using System.Collections.Generic;
using System.IO;

namespace Dalamud.Game.Text.SeStringHandling.Payloads;

/// <summary>
/// An SeString Payload containing information about enabling or disabling italics formatting on following text.
/// </summary>
/// <remarks>
/// As with other formatting payloads, this is only useful in a payload block, where it affects any subsequent
/// text payloads.
/// </remarks>
public class EmphasisItalicPayload : Payload
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EmphasisItalicPayload"/> class.
    /// Creates an EmphasisItalicPayload.
    /// </summary>
    /// <param name="enabled">Whether italics formatting should be enabled or disabled for following text.</param>
    public EmphasisItalicPayload(bool enabled)
    {
        this.IsEnabled = enabled;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EmphasisItalicPayload"/> class.
    /// Creates an EmphasisItalicPayload.
    /// </summary>
    internal EmphasisItalicPayload()
    {
    }

    /// <summary>
    /// Gets a payload representing enabling italics on following text.
    /// </summary>
    public static EmphasisItalicPayload ItalicsOn => new(true);

    /// <summary>
    /// Gets a payload representing disabling italics on following text.
    /// </summary>
    public static EmphasisItalicPayload ItalicsOff => new(false);

    /// <summary>
    /// Gets a value indicating whether this payload enables italics formatting for following text.
    /// </summary>
    public bool IsEnabled { get; private set; }

    /// <inheritdoc/>
    public override PayloadType Type => PayloadType.EmphasisItalic;

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{this.Type} - Enabled: {this.IsEnabled}";
    }

    /// <inheritdoc/>
    protected override byte[] EncodeImpl()
    {
        // realistically this will always be a single byte of value 1 or 2
        // but we'll treat it normally anyway
        var enabledBytes = MakeInteger(this.IsEnabled ? 1u : 0);

        var chunkLen = enabledBytes.Length + 1;
        var bytes = new List<byte>()
        {
            START_BYTE, (byte)SeStringChunkType.EmphasisItalic, (byte)chunkLen,
        };
        bytes.AddRange(enabledBytes);
        bytes.Add(END_BYTE);

        return bytes.ToArray();
    }

    /// <inheritdoc/>
    protected override void DecodeImpl(BinaryReader reader, long endOfStream)
    {
        this.IsEnabled = GetInteger(reader) == 1;
    }
}
