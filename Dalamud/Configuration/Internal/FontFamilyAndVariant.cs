using System.Collections.Immutable;
using System.Linq;

using CheapLoc;
using Newtonsoft.Json;
using SharpDX.DirectWrite;

namespace Dalamud.Configuration.Internal;

/// <summary>
/// Indicates font family and variant.
/// </summary>
[Serializable]
internal class FontFamilyAndVariant : IEquatable<FontFamilyAndVariant>
{
    /// <summary>
    /// An instance of <see cref="FontFamilyAndVariant"/> with empty values.
    /// </summary>
    public static readonly FontFamilyAndVariant Empty = new(string.Empty);

    /// <summary>
    /// An instance of <see cref="ImmutableList{T}"/> containing one <see cref="Empty"/>.
    /// </summary>
    public static readonly ImmutableList<FontFamilyAndVariant> EmptySingleItem = new[] { Empty }.ToImmutableList();

    /// <summary>
    /// Initializes a new instance of the <see cref="FontFamilyAndVariant"/> class.
    /// </summary>
    /// <param name="name">Name of the font family. It cannot be empty.</param>
    /// <param name="weight">Weight of the font.</param>
    /// <param name="stretch">Stretch of the font.</param>
    /// <param name="style">Style of the font.</param>
    [JsonConstructor]
    public FontFamilyAndVariant(
        string name,
        FontWeight weight = FontWeight.Normal,
        FontStretch stretch = FontStretch.Normal,
        FontStyle style = FontStyle.Normal)
    {
        this.Name = name;
        this.Weight = weight;
        this.Stretch = stretch;
        this.Style = style;
    }

    /// <summary>
    /// Gets the weight of default font from the system.
    /// </summary>
    public FontWeight Weight { get; }

    /// <summary>
    /// Gets the stretch of default font from the system.
    /// </summary>
    public FontStretch Stretch { get; }

    /// <summary>
    /// Gets the style of default font from the system.
    /// </summary>
    public FontStyle Style { get; }

    /// <summary>
    /// Gets the name of default font from the system. Set to "" to use the default font shipped with Dalamud.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Compare left and right for equality.
    /// </summary>
    /// <param name="left">Left value.</param>
    /// <param name="right">Right value.</param>
    /// <returns>Equality.</returns>
    public static bool operator ==(FontFamilyAndVariant? left, FontFamilyAndVariant? right) => Equals(left, right);

    /// <summary>
    /// Compare left and right for inequality.
    /// </summary>
    /// <param name="left">Left value.</param>
    /// <param name="right">Right value.</param>
    /// <returns>Inequality.</returns>
    public static bool operator !=(FontFamilyAndVariant? left, FontFamilyAndVariant? right) => !Equals(left, right);

    /// <summary>
    /// Checks for equality in variant information.
    /// </summary>
    /// <param name="other">Other instance.</param>
    /// <returns>Equality.</returns>
    public bool VariantEquals(FontFamilyAndVariant? other) =>
        other != null &&
        this.Weight == other.Weight &&
        this.Stretch == other.Stretch &&
        this.Style == other.Style;

    /// <inheritdoc/>
    public bool Equals(FontFamilyAndVariant? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return this.Weight == other.Weight &&
               this.Stretch == other.Stretch &&
               this.Style == other.Style &&
               this.Name == other.Name;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return this.Equals((FontFamilyAndVariant)obj);
    }

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(
        (int)this.Weight,
        (int)this.Stretch,
        (int)this.Style,
        this.Name);

    /// <inheritdoc/>
    public override string ToString() => $"{this.Name}: {this.Weight}, {this.Stretch}, {this.Style}";

    /// <summary>
    /// Gets the localized description of font variant.
    /// </summary>
    /// <returns>Localized string.</returns>
    public string GetLocalizedVariantDescription() =>
        $"{Localize(this.Weight)}, {Localize(this.Stretch)}, {Localize(this.Style)}";

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
