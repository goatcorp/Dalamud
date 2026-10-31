namespace Dalamud.Interface.Fonts;

/// <summary>A field of <see cref="TextStyle"/> that a <see cref="TextStyleRange"/> sets.</summary>
public enum TextStyleField : byte
{
    /// <summary><see cref="TextStyle.FamilyId"/>.</summary>
    FamilyId,

    /// <summary><see cref="TextStyle.Color"/>.</summary>
    Color,

    /// <summary><see cref="TextStyle.Shadow"/>.</summary>
    Shadow,

    /// <summary><see cref="TextStyle.Edge"/>.</summary>
    Edge,

    /// <summary><see cref="TextStyle.Background"/>.</summary>
    Background,

    /// <summary><see cref="TextStyle.Lines"/>.</summary>
    Lines,

    /// <summary><see cref="TextStyle.ExtraDecoration"/>.</summary>
    ExtraDecoration,

    /// <summary><see cref="TextStyle.Bold"/>.</summary>
    Bold,

    /// <summary><see cref="TextStyle.Italic"/>.</summary>
    Italic,

    /// <summary><see cref="TextStyle.NoSubpixel"/>.</summary>
    NoSubpixel,

    /// <summary><see cref="TextStyle.NoHinting"/>.</summary>
    NoHinting,

    /// <summary><see cref="TextStyle.Weight"/>.</summary>
    Weight,

    /// <summary><see cref="TextStyle.Stretch"/>.</summary>
    Stretch,

    /// <summary><see cref="TextStyle.LetterSpacing"/>.</summary>
    LetterSpacing,

    /// <summary><see cref="TextStyle.Locale"/>.</summary>
    Locale,

    /// <summary><see cref="TextStyle.Features"/>.</summary>
    Features,
}
