using System.Runtime.InteropServices;

namespace Dalamud.Interface.Fonts.Internal.GameFonts.Formats;

/// <summary>A single kerning pair.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct FdtKerningPair
{
    /// <summary>The left rune.</summary>
    public FdtRune Left;

    /// <summary>The right rune.</summary>
    public FdtRune Right;

    /// <summary>Shift_JIS encoding of <see cref="Left"/>.</summary>
    public ushort LeftSjis;

    /// <summary>Shift_JIS encoding of <see cref="Right"/>.</summary>
    public ushort RightSjis;

    /// <summary>Horizontal advance adjustment for the right character, in pixels.</summary>
    public int RightOffset;
}
