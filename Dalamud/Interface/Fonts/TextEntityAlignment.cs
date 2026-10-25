namespace Dalamud.Interface.Fonts;

/// <summary>Where an entity is placed in its line.</summary>
/// <seealso cref="TextEntity.Alignment"/>
public enum TextEntityAlignment
{
    /// <summary>Centered in the line, as tall as its text makes it.</summary>
    Center,

    /// <summary>Its top is at the top of the font, the ascent above the baseline.</summary>
    TextTop,

    /// <summary>Its bottom is on the baseline.</summary>
    Baseline,

    /// <summary>Its bottom is at the bottom of the font, the descent below the baseline.</summary>
    TextBottom,
}
