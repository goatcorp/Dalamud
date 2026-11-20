using System.Runtime.InteropServices;

namespace Dalamud.Interface.Fonts.Internal.SeStrings.Formats;

/// <summary>Header of a game bitmap font icon table file (.gfd).</summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct GfdHeader
{
    /// <summary>The signature, "gftd0100".</summary>
    public fixed byte Signature[8];

    /// <summary>Number of entries.</summary>
    public int Count;

    /// <summary>Unused/unknown.</summary>
    public fixed byte Padding[4];

    /// <summary>Tests whether the signature is valid.</summary>
    /// <returns>Whether it is.</returns>
    public readonly bool ValidateSignature()
    {
        fixed (byte* p = this.Signature)
            return new ReadOnlySpan<byte>(p, 8).SequenceEqual("gftd0100"u8);
    }
}
