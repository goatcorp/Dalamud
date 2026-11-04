namespace Dalamud.Interface.Fonts;

/// <summary>When a text decoration is drawn.</summary>
/// <seealso cref="TextDecorationDrawer"/>
/// <seealso cref="IUiBuilder.RegisterTextDecoration"/>
[Flags]
public enum TextDecorationFlags
{
    /// <summary>Drawn over the text, as <see cref="OverText"/>.</summary>
    None = 0,

    /// <summary>Drawn in <see cref="TextDecorationLayer.BelowText"/>.</summary>
    BelowText = 1 << TextDecorationLayer.BelowText,

    /// <summary>Drawn in <see cref="TextDecorationLayer.BelowEdge"/>.</summary>
    BelowEdge = 1 << TextDecorationLayer.BelowEdge,

    /// <summary>Drawn in <see cref="TextDecorationLayer.BelowFill"/>.</summary>
    BelowFill = 1 << TextDecorationLayer.BelowFill,

    /// <summary>Drawn in <see cref="TextDecorationLayer.OverText"/>.</summary>
    OverText = 1 << TextDecorationLayer.OverText,
}
