using System.Runtime.InteropServices;

namespace Dalamud.Interface.Fonts.Internal.GameFonts.Formats;

/// <summary>File header of a game font <c>.fdt</c> file.</summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct FdtFileHeader
{
    /// <summary>The signature, <c>"fcsv0100"</c>.</summary>
    public fixed byte Signature[8];

    /// <summary>Offset to <see cref="FdtGlyphTableHeader"/>.</summary>
    public int GlyphTableHeaderOffset;

    /// <summary>Offset to <see cref="FdtKerningPairTableHeader"/>.</summary>
    public int KerningTableHeaderOffset;

    /// <summary>Unused padding.</summary>
    public fixed byte Padding[0x10];

    /// <summary>Checks whether <see cref="Signature"/> equals <c>"fcsv0100"</c>.</summary>
    /// <returns><see langword="true"/> if valid.</returns>
    public readonly bool ValidateSignature()
    {
        fixed (byte* p = this.Signature)
            return new ReadOnlySpan<byte>(p, 8).SequenceEqual("fcsv0100"u8);
    }
}
