namespace Dalamud.Interface.Fonts;

/// <summary>The parts of a <see cref="TextStyle"/> that a <see cref="FontChooserDialog"/> lets the user edit, besides
/// the family and the face.</summary>
[Flags]
public enum FontChooserFields
{
    /// <summary>Only the family and the face.</summary>
    None = 0,

    /// <summary>The size, in a column next to the faces.</summary>
    Size = 1,

    /// <summary>The letter spacing, in the details.</summary>
    LetterSpacing = 2,

    /// <summary>The line height, in the details.</summary>
    LineHeight = 4,

    /// <summary>The locale, in the details.</summary>
    Locale = 8,

    /// <summary>All of them.</summary>
    Default = Size | LetterSpacing | LineHeight | Locale,
}
