using System.Collections.Generic;
using System.IO;

namespace Dalamud.Game.Text.SeStringHandling.Payloads;

/// <summary>
/// SeString payload representing a bitmap icon from fontIcon.
/// </summary>
public class IconPayload : Payload
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IconPayload"/> class.
    /// Create a Icon payload for the specified icon.
    /// </summary>
    /// <param name="icon">The Icon.</param>
    public IconPayload(BitmapFontIcon icon)
    {
        this.Icon = icon;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IconPayload"/> class.
    /// Create a Icon payload for the specified icon.
    /// </summary>
    internal IconPayload()
    {
    }

    /// <inheritdoc/>
    public override PayloadType Type => PayloadType.Icon;

    /// <summary>
    /// Gets or sets the icon the payload represents.
    /// </summary>
    public BitmapFontIcon Icon { get; set; } = BitmapFontIcon.None;

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{this.Type} - {this.Icon}";
    }

    /// <inheritdoc />
    protected override byte[] EncodeImpl()
    {
        var indexBytes = MakeInteger((uint)this.Icon);
        var chunkLen = indexBytes.Length + 1;
        var bytes = new List<byte>(new byte[]
        {
            START_BYTE, (byte)SeStringChunkType.Icon, (byte)chunkLen,
        });
        bytes.AddRange(indexBytes);
        bytes.Add(END_BYTE);
        return bytes.ToArray();
    }

    /// <inheritdoc />
    protected override void DecodeImpl(BinaryReader reader, long endOfStream)
    {
        this.Icon = (BitmapFontIcon)GetInteger(reader);
    }
}
