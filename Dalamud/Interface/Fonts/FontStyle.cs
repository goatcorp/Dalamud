using System.Text;

namespace Dalamud.Interface.Fonts;

/// <summary>The weight, italic and stretch of a face, to choose among the faces of a font family.</summary>
/// <remarks>Combine with <c>|</c>, as in <c>FontStyle.Bold | FontStyle.Italic</c>. Does nothing if the font used is
/// already bold or italic.</remarks>
public readonly record struct FontStyle
{
    /// <summary>Initializes a new instance of the <see cref="FontStyle"/> struct.</summary>
    /// <param name="weight">The weight, from 1 (thinnest) to 1000 (heaviest), or 0 for unspecified.</param>
    /// <param name="italicized">Whether italic.</param>
    /// <param name="stretch">The stretch, from 1 (narrowest) to 9 (widest), or 0 for unspecified.</param>
    public FontStyle(int weight = 0, bool italicized = false, int stretch = 0)
    {
        this.Weight = weight;
        this.Italicized = italicized;
        this.Stretch = stretch;
    }

    /// <summary>Gets the unspecified style.</summary>
    public static FontStyle Default => default;

    /// <summary>Gets the weight 100.</summary>
    public static FontStyle Thin => new(100);

    /// <summary>Gets the weight 200.</summary>
    public static FontStyle ExtraLight => new(200);

    /// <summary>Gets the weight 300.</summary>
    public static FontStyle Light => new(300);

    /// <summary>Gets the weight 350.</summary>
    public static FontStyle SemiLight => new(350);

    /// <summary>Gets the weight 400.</summary>
    public static FontStyle Regular => new(400);

    /// <summary>Gets the weight 500.</summary>
    public static FontStyle Medium => new(500);

    /// <summary>Gets the weight 600.</summary>
    public static FontStyle SemiBold => new(600);

    /// <summary>Gets the weight 700.</summary>
    public static FontStyle Bold => new(700);

    /// <summary>Gets the weight 800.</summary>
    public static FontStyle ExtraBold => new(800);

    /// <summary>Gets the weight 900.</summary>
    public static FontStyle Black => new(900);

    /// <summary>Gets the weight 950.</summary>
    public static FontStyle ExtraBlack => new(950);

    /// <summary>Gets the italic style.</summary>
    public static FontStyle Italic => new(italicized: true);

    /// <summary>Gets the stretch 1.</summary>
    public static FontStyle UltraCondensed => new(stretch: 1);

    /// <summary>Gets the stretch 2.</summary>
    public static FontStyle ExtraCondensed => new(stretch: 2);

    /// <summary>Gets the stretch 3.</summary>
    public static FontStyle Condensed => new(stretch: 3);

    /// <summary>Gets the stretch 4.</summary>
    public static FontStyle SemiCondensed => new(stretch: 4);

    /// <summary>Gets the stretch 5.</summary>
    public static FontStyle NormalStretch => new(stretch: 5);

    /// <summary>Gets the stretch 6.</summary>
    public static FontStyle SemiExpanded => new(stretch: 6);

    /// <summary>Gets the stretch 7.</summary>
    public static FontStyle Expanded => new(stretch: 7);

    /// <summary>Gets the stretch 8.</summary>
    public static FontStyle ExtraExpanded => new(stretch: 8);

    /// <summary>Gets the stretch 9.</summary>
    public static FontStyle UltraExpanded => new(stretch: 9);

    /// <summary>Gets the weight, from 1 (thinnest) to 1000 (heaviest), or 0 for unspecified.</summary>
    public int Weight
    {
        get;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 0);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 1000);
            field = value;
        }
    }

    /// <summary>Gets a value indicating whether italic.</summary>
    /// <remarks>Uses either a real italic or an oblique(faux italic) font.</remarks>
    public bool Italicized { get; init; }

    /// <summary>Gets the stretch, from 1 (narrowest) to 9 (widest), or 0 for unspecified.</summary>
    public int Stretch
    {
        get;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 0);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 9);
            field = value;
        }
    }

    /// <summary>Gets <see cref="Weight"/>, or 400 if unspecified.</summary>
    internal int EffectiveWeight => this.Weight == 0 ? 400 : this.Weight;

    /// <summary>Gets <see cref="Stretch"/>, or 5 if unspecified.</summary>
    internal int EffectiveStretch => this.Stretch == 0 ? 5 : this.Stretch;

    /// <summary>Combines two styles. <paramref name="right"/> overwrites <paramref name="left"/> where specified.
    /// </summary>
    /// <param name="left">The style to start from.</param>
    /// <param name="right">The style to apply.</param>
    /// <returns>The combined style.</returns>
    public static FontStyle operator |(FontStyle left, FontStyle right) =>
        new(
            right.Weight != 0 ? right.Weight : left.Weight,
            right.Italicized || left.Italicized,
            right.Stretch != 0 ? right.Stretch : left.Stretch);

    /// <inheritdoc/>
    public override string ToString()
    {
        var sb = new StringBuilder();
        _ = this.Stretch switch
        {
            1 => sb.Append("Ultra Condensed, "),
            2 => sb.Append("Extra Condensed, "),
            3 => sb.Append("Condensed, "),
            4 => sb.Append("Semi Condensed, "),
            6 => sb.Append("Semi Expanded, "),
            7 => sb.Append("Expanded, "),
            8 => sb.Append("Extra Expanded, "),
            9 => sb.Append("Ultra Expanded, "),
            _ => sb,
        };
        _ = this.Weight switch
        {
            0 or 400 => null,
            100 => sb.Append("Thin, "),
            200 => sb.Append("Extra Light, "),
            300 => sb.Append("Light, "),
            350 => sb.Append("Semi Light, "),
            500 => sb.Append("Medium, "),
            600 => sb.Append("Semi Bold, "),
            700 => sb.Append("Bold, "),
            800 => sb.Append("Extra Bold, "),
            900 => sb.Append("Black, "),
            950 => sb.Append("Extra Black, "),
            var w => sb.Append($"Weight {w}, "),
        };
        if (this.Italicized)
            sb.Append("Italic, ");
        return sb.Length == 0 ? "Regular" : sb.Remove(sb.Length - 2, 2).ToString();
    }
}
