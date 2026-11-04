using System.Runtime.InteropServices;

namespace Dalamud.Interface.Fonts.Internal.GameFonts.Formats;

/// <summary>Header of the glyph table.</summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct FdtGlyphTableHeader
{
    /// <summary>The signature, "fthd".</summary>
    public fixed byte Signature[4];

    /// <summary>Number of the glyphs defined in this file.</summary>
    public int GlyphCount;

    /// <summary>Number of the kerning pairs defined in this file.</summary>
    public int KerningPairCount;

    /// <summary>Unused/unknown.</summary>
    public fixed byte Padding[0x04];

    /// <summary>Width of the backing texture.</summary>
    public ushort TextureWidth;

    /// <summary>Height of the backing texture.</summary>
    public ushort TextureHeight;

    /// <summary>Size of the font defined from this file, in point unit.</summary>
    public float Size;

    /// <summary>Line height of the font defined from this file, in pixel unit.</summary>
    public int LineHeight;

    /// <summary>Ascent of the font defined from this file, in pixel unit.</summary>
    public int Ascent;

    /// <summary>Validates that <see cref="Signature"/> has the expected value.</summary>
    /// <returns>Whether the validation passes.</returns>
    public readonly bool ValidateSignature()
    {
        fixed (byte* p = this.Signature)
            return new ReadOnlySpan<byte>(p, 4).SequenceEqual("fthd"u8);
    }
}
