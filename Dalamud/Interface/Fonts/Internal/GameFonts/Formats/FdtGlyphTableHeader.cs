using System.Runtime.InteropServices;

namespace Dalamud.Interface.Fonts.Internal.GameFonts.Formats;

/// <summary>Header of the glyph table.</summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct FdtGlyphTableHeader
{
    /// <summary>The signature, <c>"fthd"</c>.</summary>
    public fixed byte Signature[4];

    /// <summary>Number of glyph entries in this file.</summary>
    public int GlyphCount;

    /// <summary>Number of kerning pairs in this file.</summary>
    public int KerningPairCount;

    /// <summary>Unused padding.</summary>
    public fixed byte Padding[0x04];

    /// <summary>Width of the backing texture.</summary>
    public ushort TextureWidth;

    /// <summary>Height of the backing texture.</summary>
    public ushort TextureHeight;

    /// <summary>Font size in points.</summary>
    public float Size;

    /// <summary>Line height in pixels.</summary>
    public int LineHeight;

    /// <summary>Ascent in pixels.</summary>
    public int Ascent;

    /// <summary>Checks whether <see cref="Signature"/> equals <c>"fthd"</c>.</summary>
    /// <returns><see langword="true"/> if valid.</returns>
    public readonly bool ValidateSignature()
    {
        fixed (byte* p = this.Signature)
            return new ReadOnlySpan<byte>(p, 4).SequenceEqual("fthd"u8);
    }
}
