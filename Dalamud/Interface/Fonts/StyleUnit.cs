namespace Dalamud.Interface.Fonts;

/// <summary>The unit of a <see cref="StyleLength"/>.</summary>
public enum StyleUnit : byte
{
    /// <summary>No value of its own. What it means depends on where the length is.</summary>
    Default,

    /// <summary>Pixels before global scaling.</summary>
    /// <remarks>A line height in pixels is fixed, whatever the glyph size.</remarks>
    Px,

    /// <summary>Multiples of the em of the font.</summary>
    /// <remarks>A size or a line height in it is a multiple of the size of the font, as
    /// <see cref="TextStyle.SizeBasis"/> measures it. A line height in it is like a unitless number in CSS.</remarks>
    Em,
}
