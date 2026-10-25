namespace Dalamud.Interface.Fonts;

/// <summary>Where lines of a text are.</summary>
/// <seealso cref="TextLineDecoration"/>
[Flags]
public enum TextLinePosition : byte
{
    /// <summary>No lines.</summary>
    None = 0,

    /// <summary>Under the text, at the font's underline position. Multiple underlines are placed below.
    /// Layered under the text.</summary>
    Under = 1 << 0,

    /// <summary>Over the text, at the top of the font's em box. Multiple overlines are placed above.
    /// Layered under the text.</summary>
    Over = 1 << 1,

    /// <summary>Through the text, at the font's strikethrough position. Multiple strikethroughs are spread around it.
    /// Layered over the text.</summary>
    Through = 1 << 2,
}
