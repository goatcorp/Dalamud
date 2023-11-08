using System.Linq;

using CheapLoc;
using Newtonsoft.Json;
using SharpDX.DirectWrite;

namespace Dalamud.Interface.EasyFonts;

/// <summary>
/// Indicates a set of font variant information.
/// </summary>
[JsonObject(MemberSerialization.OptIn)]
public record struct FontVariant
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FontVariant"/> class.
    /// </summary>
    public FontVariant()
    {
        this.Weight = FontWeight.Normal;
        this.Stretch = FontStretch.Normal;
        this.Style = FontStyle.Normal;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FontVariant"/> class.
    /// </summary>
    /// <param name="weight">Weight of the font.</param>
    /// <param name="stretch">Stretch of the font.</param>
    /// <param name="style">Style of the font.</param>
    public FontVariant(
        FontWeight weight,
        FontStretch stretch = FontStretch.Normal,
        FontStyle style = FontStyle.Normal)
    {
        this.Weight = weight;
        this.Stretch = stretch;
        this.Style = style;
    }

    /// <summary>
    /// Gets or sets the weight of font.
    /// </summary>
    public FontWeight Weight { get; set; } = FontWeight.Normal;

    /// <summary>
    /// Gets or sets the stretch of font.
    /// </summary>
    public FontStretch Stretch { get; set; } = FontStretch.Normal;

    /// <summary>
    /// Gets or sets the style of font.
    /// </summary>
    public FontStyle Style { get; set; } = FontStyle.Normal;

    /// <inheritdoc/>
    public override string ToString() => $"{this.Weight}, {this.Stretch}, {this.Style}";

    /// <summary>
    /// Gets the localized description of font variant.
    /// </summary>
    /// <returns>Localized string.</returns>
    public string ToStringLocalized() => $"{Localize(this.Weight)}, {Localize(this.Stretch)}, {Localize(this.Style)}";

    private static string Localize(FontWeight v)
    {
        var weightName = Enum.GetValues<FontWeight>().MinBy(x => Math.Abs((int)v - (int)x)) switch
        {
            FontWeight.Thin => Loc.Localize("FontWeightThin", "Thin"),
            FontWeight.ExtraLight => Loc.Localize("FontWeightExtraLight", "Extra Light"),
            FontWeight.Light => Loc.Localize("FontWeightLight", "Light"),
            FontWeight.SemiLight => Loc.Localize("FontWeightSemiLight", "Semi Light"),
            FontWeight.Normal => Loc.Localize("FontWeightNormal", "Normal"),
            FontWeight.Medium => Loc.Localize("FontWeightMedium", "Medium"),
            FontWeight.DemiBold => Loc.Localize("FontWeightDemiBold", "Demi Bold"),
            FontWeight.Bold => Loc.Localize("FontWeightBold", "Bold"),
            FontWeight.ExtraBold => Loc.Localize("FontWeightExtraBold", "Extra Bold"),
            FontWeight.Black => Loc.Localize("FontWeightBlack", "Black"),
            FontWeight.ExtraBlack => Loc.Localize("FontWeightExtraBlack", "Extra Black"),
            _ => throw new ArgumentOutOfRangeException(nameof(v), v, null),
        };

        return weightName + $" ({(int)v})";
    }

    private static string Localize(FontStretch v) => v switch
    {
        FontStretch.Undefined => Loc.Localize("FontStretchUndefined", "Undefined"),
        FontStretch.UltraCondensed => Loc.Localize("FontStretchUltraCondensed", "Ultra Condensed"),
        FontStretch.ExtraCondensed => Loc.Localize("FontStretchExtraCondensed", "Extra Condensed"),
        FontStretch.Condensed => Loc.Localize("FontStretchCondensed", "Condensed"),
        FontStretch.SemiCondensed => Loc.Localize("FontStretchSemiCondensed", "Semi Condensed"),
        FontStretch.Normal => Loc.Localize("FontStretchNormal", "Normal"),
        FontStretch.SemiExpanded => Loc.Localize("FontStretchSemiExpanded", "Semi Expanded"),
        FontStretch.Expanded => Loc.Localize("FontStretchExpanded", "Expanded"),
        FontStretch.ExtraExpanded => Loc.Localize("FontStretchExtraExpanded", "Extra Expanded"),
        FontStretch.UltraExpanded => Loc.Localize("FontStretchUltraExpanded", "Ultra Expanded"),
        _ => throw new ArgumentOutOfRangeException(nameof(v), v, null),
    };

    private static string Localize(FontStyle v) => v switch
    {
        FontStyle.Normal => Loc.Localize("FontStyleNormal", "Normal"),
        FontStyle.Oblique => Loc.Localize("FontStyleOblique", "Oblique"),
        FontStyle.Italic => Loc.Localize("FontStyleItalic", "Italic"),
        _ => throw new ArgumentOutOfRangeException(nameof(v), v, null),
    };
}
