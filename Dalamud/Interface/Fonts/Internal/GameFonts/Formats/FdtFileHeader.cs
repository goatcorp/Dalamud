using System.Runtime.InteropServices;

namespace Dalamud.Interface.Fonts.Internal.GameFonts.Formats;

/// <summary>Header of game font file format.</summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct FdtFileHeader
{
    /// <summary>The signature, "fcsv0100".</summary>
    public fixed byte Signature[8];

    /// <summary>Offset to <see cref="FdtGlyphTableHeader"/>.</summary>
    public int GlyphTableHeaderOffset;

    /// <summary>Offset to <see cref="FdtKerningPairTableHeader"/>.</summary>
    public int KerningTableHeaderOffset;

    /// <summary>Unused/unknown.</summary>
    public fixed byte Padding[0x10];

    /// <summary>Validates that <see cref="Signature"/> has the expected value.</summary>
    /// <returns>Whether the validation passes.</returns>
    public readonly bool ValidateSignature()
    {
        fixed (byte* p = this.Signature)
            return new ReadOnlySpan<byte>(p, 8).SequenceEqual("fcsv0100"u8);
    }
}
