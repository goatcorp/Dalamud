namespace Dalamud.Interface.Fonts;

/// <summary>The unit of a <see cref="StyleLength"/>.</summary>
public enum StyleUnit : byte
{
    /// <summary>Uses the property's default or inherited value.</summary>
    Default,

    /// <summary>Pixels before global scaling.</summary>
    /// <remarks>A line height in pixels is fixed, whatever the glyph size.</remarks>
    Px,

    /// <summary>Multiples of the em of the font.</summary>
    /// <remarks>For sizes and line heights, multiplies the font size as measured by <see cref="TextStyle.SizeBasis"/>,
    /// like a unitless <c>line-height</c> in CSS.</remarks>
    Em,
}
