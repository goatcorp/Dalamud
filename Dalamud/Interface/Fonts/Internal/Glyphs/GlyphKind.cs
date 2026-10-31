namespace Dalamud.Interface.Fonts.Internal.Glyphs;

/// <summary>How a glyph image stores its coverage.</summary>
internal enum GlyphKind
{
    /// <summary>One channel of coverage.</summary>
    Grayscale,

    /// <summary>Coverage per subpixel, in red, green and blue.</summary>
    Subpixel,

    /// <summary>Premultiplied colors, and optionally a mask of the text color.</summary>
    Color,
}
