namespace Dalamud.Interface.Fonts;

/// <summary>The markup format used to parse text.</summary>
/// <seealso cref="TextStyle.Markup"/>
public enum TextMarkup : byte
{
    /// <summary>UTF-8.</summary>
    Plain,

    /// <summary>A SeString.</summary>
    SeString,

    /// <summary>The macro representation of a SeString.</summary>
    MacroString,
}
