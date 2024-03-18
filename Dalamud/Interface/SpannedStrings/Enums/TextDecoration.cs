using Dalamud.Interface.SpannedStrings.Internal;

namespace Dalamud.Interface.SpannedStrings.Enums;

/// <summary>The kind of decoration that is used on text in an element, such as an underline or overline.</summary>
/// <remarks>See <a href="https://developer.mozilla.org/en-US/docs/Web/CSS/text-decoration-line">MDN</a>.</remarks>
[Flags]
public enum TextDecoration : byte
{
    /// <summary>Produces no text decoration.</summary>
    [SpannedParseShortName("none", "disable", "off", "false", "null", "empty")]
    None = 0,

    /// <summary>Each line of text has a decorative line beneath it.</summary>
    [SpannedParseShortName("under", "down", "below", "_")]
    Underline = 1 << 1,

    /// <summary>Each line of text has a decorative line above it.</summary>
    [SpannedParseShortName("over", "up", "above", "^")]
    Overline = 1 << 2,

    /// <summary>Each line of text has a decorative line going through its middle.</summary>
    [SpannedParseShortName("line-through", "strike-through", "strikethrough", "strike", "middle", "mid", "center", "-")]
    LineThrough = 1 << 3,
}
