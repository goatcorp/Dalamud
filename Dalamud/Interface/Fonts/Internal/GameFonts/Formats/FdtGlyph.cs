using System.Runtime.InteropServices;

namespace Dalamud.Interface.Fonts.Internal.GameFonts.Formats;

/// <summary>Glyph table entry.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct FdtGlyph
{
    /// <summary>The rune contained within.</summary>
    public FdtRune Rune;

    /// <summary><see cref="Rune"/> but in Shift_JIS.</summary>
    public ushort CharSjis;

    /// <summary>Index of the backing texture, as the file index times 4 plus the channel index.</summary>
    public ushort TextureIndex;

    /// <summary>Horizontal offset of the glyph image in the backing texture.</summary>
    public ushort TextureOffsetX;

    /// <summary>Vertical offset of the glyph image in the backing texture.</summary>
    public ushort TextureOffsetY;

    /// <summary>Bounding width of this glyph.</summary>
    public byte BoundingWidth;

    /// <summary>Bounding height of this glyph.</summary>
    public byte BoundingHeight;

    /// <summary>Distance adjustment for drawing the next character.</summary>
    public sbyte NextOffsetX;

    /// <summary>Distance adjustment for drawing the current character.</summary>
    public sbyte CurrentOffsetY;

    /// <summary>Gets the index of the file among all the backing texture files.</summary>
    public readonly int TextureFileIndex => this.TextureIndex / 4;

    /// <summary>Gets the channel index in the backing texture file.</summary>
    public readonly int TextureChannelIndex => this.TextureIndex % 4;

    /// <summary>Gets the byte index of the channel in a B8G8R8A8 pixel.</summary>
    public readonly int TextureChannelByteIndex => ((ReadOnlySpan<byte>)[2, 1, 0, 3])[this.TextureChannelIndex];

    /// <summary>Gets the advance width of this character.</summary>
    public readonly int AdvanceWidth => this.BoundingWidth + this.NextOffsetX;
}
