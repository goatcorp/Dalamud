using System.Runtime.InteropServices;

namespace Dalamud.Interface.Fonts.Internal.GameFonts.Formats;

/// <summary>Header of the kerning pairs table.</summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct FdtKerningPairTableHeader
{
    /// <summary>The signature, <c>"knhd"</c>.</summary>
    public fixed byte Signature[4];

    /// <summary>Number of kerning entries in this table.</summary>
    public int Count;

    /// <summary>Unused padding.</summary>
    public fixed byte Padding[0x08];

    /// <summary>Checks whether <see cref="Signature"/> equals <c>"knhd"</c>.</summary>
    /// <returns><see langword="true"/> if valid.</returns>
    public readonly bool ValidateSignature()
    {
        fixed (byte* p = this.Signature)
            return new ReadOnlySpan<byte>(p, 4).SequenceEqual("knhd"u8);
    }
}
