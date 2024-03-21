namespace Dalamud.Interface.Spannables.Internal;

/// <summary>Possible types for a span.</summary>
internal enum SpannedRecordType : byte
{
    /// <summary>This span entry is empty.</summary>
    None,

    /// <summary>Link data offset has been changed.</summary>
    Link,

    /// <summary>Font has been changed.</summary>
    FontHandleSetIndex,

    /// <summary>Font size has been changed.</summary>
    FontSize,

    /// <summary>Line height has been changed.</summary>
    LineHeight,

    /// <summary>Horizontal offset for glyphs has been changed.</summary>
    HorizontalOffset,

    /// <summary>Horizontal alignment for the line has been changed.</summary>
    HorizontalAlignment,

    /// <summary>Vertical offset for glyphs has been changed.</summary>
    VerticalOffset,

    /// <summary>Vertical alignment w.r.t. the line has been changed.</summary>
    VerticalAlignment,

    /// <summary>Whether to use italic fonts has been changed.</summary>
    Italic,

    /// <summary>Whether to use bold fonts has been changed.</summary>
    Bold,

    /// <summary>Text decoration has been changed.</summary>
    TextDecoration,

    /// <summary>Text decoration style has been changed.</summary>
    TextDecorationStyle,

    /// <summary>Background color has been changed.</summary>
    BackColor,

    /// <summary>Shadow color has been changed.</summary>
    ShadowColor,

    /// <summary>Border color has been changed.</summary>
    EdgeColor,

    /// <summary>Text decoration color has been changed.</summary>
    TextDecorationColor,

    /// <summary>Foreground color has been changed.</summary>
    ForeColor,

    /// <summary>Border width has been changed.</summary>
    BorderWidth,

    /// <summary>Shadow offset has been changed.</summary>
    ShadowOffset,

    /// <summary>Thickness of text decoration has been changed.</summary>
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
