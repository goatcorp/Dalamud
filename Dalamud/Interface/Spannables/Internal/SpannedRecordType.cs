namespace Dalamud.Interface.Spannables.Internal;

/// <summary>Possible types for a span.</summary>
internal enum SpannedRecordType : byte
{
    /// <summary>This span entry is empty.</summary>
    None,

    /// <summary>Link data offset is changing.</summary>
    Link,

    /// <summary>Font is changing.</summary>
    FontHandleSetIndex,

    /// <summary>Font size is changing.</summary>
    FontSize,

    /// <summary>Line height is changing.</summary>
    LineHeight,

    /// <summary>Horizontal offset for glyphs is changing.</summary>
    HorizontalOffset,

    /// <summary>Horizontal alignment for the line is changing.</summary>
    HorizontalAlignment,

    /// <summary>Vertical offset for glyphs is changing.</summary>
    VerticalOffset,

    /// <summary>Vertical alignment w.r.t. the line is changing.</summary>
    VerticalAlignment,

    /// <summary>Whether to use italic fonts is changing.</summary>
    Italic,

    /// <summary>Whether to use bold fonts is changing.</summary>
    Bold,

    /// <summary>Text decoration is changing.</summary>
    TextDecoration,

    /// <summary>Text decoration style is changing.</summary>
    TextDecorationStyle,

    /// <summary>Background color is changing.</summary>
    BackColor,

    /// <summary>Shadow color is changing.</summary>
    ShadowColor,

    /// <summary>Border color is changing.</summary>
    EdgeColor,

    /// <summary>Text decoration color is changing.</summary>
    TextDecorationColor,

    /// <summary>Foreground color is changing.</summary>
    ForeColor,

    /// <summary>Border width is changing.</summary>
    EdgeWidth,

    /// <summary>Shadow offset is changing.</summary>
    ShadowOffset,

    /// <summary>Thickness of text decoration is changing.</summary>
    TextDecorationThickness,

    /// <summary>An icon entity should be drawn from gfdata.gfd file.</summary>
    ObjectIcon,

    /// <summary>An icon entity should be drawn from a texture.</summary>
    ObjectTexture,

    /// <summary>A manual newline should be processed.</summary>
    ObjectNewLine,

    /// <summary>A callback should be called upon processing this insertion entry.</summary>
    ObjectSpannable,
}
