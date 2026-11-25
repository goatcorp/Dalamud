namespace Dalamud.Interface.Fonts;

/// <summary>A rendering layer for a text decoration relative to text shadow, edge, and fill.</summary>
/// <seealso cref="TextDecorationDrawer"/>
/// <seealso cref="TextDecorationSpan.Layer"/>
/// <seealso cref="IUiBuilder.RegisterTextDecoration"/>
public enum TextDecorationLayer
{
    /// <summary>Under the text, before its shadow.</summary>
    /// <example>Backgrounds and underlines.</example>
    BelowText,

    /// <summary>Over the shadow of the text, under its edge.</summary>
    BelowEdge,

    /// <summary>Over the edge of the text, under its fill.</summary>
    BelowFill,

    /// <summary>Over the text and its inline objects.</summary>
    /// <example>Strikethrough lines.</example>
    OverText,
}
