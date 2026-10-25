using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Numerics;

using Dalamud.Interface.Fonts.Internal;

namespace Dalamud.Interface.Fonts;

/// <summary>Changes to the glyphs of a wrapped font.</summary>
/// <remarks><ul>
/// <li>The default value changes nothing.</li>
/// <li>For lengths, <see cref="StyleLength.Px"/> is in pixels before the global scale, and <see cref="StyleLength.Em"/>
/// is in the em of the wrapped font, which is the em of the text times <see cref="SizeScale"/>.</li>
/// <li>Adjustments of a wrapped font inside another wrapped font are combined with those of the outer one.</li>
/// </ul></remarks>
/// <seealso cref="IUiBuilder.RegisterWrappedFont"/>
public readonly record struct FontAdjustments
{
    private readonly EquatableArray<uint> ranges;
    private readonly EquatableArray<FontFeature> features;

    // 0 represents scale 1 so default(FontAdjustments) leaves size unchanged.
    private readonly float sizeScale;

    /// <summary>Gets the ranges of codepoints to take from the font.</summary>
    /// <value>Pairs of inclusive bounds, or empty for all.</value>
    /// <seealso cref="FluentGlyphRangeBuilder"/>
    public ImmutableArray<uint> CodepointRanges
    {
        get => this.ranges.Array;
        init => this.ranges = new(value);
    }

    /// <summary>Gets the scale factor applied to the text's em size for glyphs from this font.</summary>
    /// <remarks>A value that is not positive and finite is stored as 1.</remarks>
    public float SizeScale
    {
        get => this.sizeScale == 0 ? 1 : this.sizeScale;
        init => this.sizeScale = value is > 0 and < float.PositiveInfinity and not 1 ? value : 0;
    }

    /// <summary>Gets the offset of the glyphs.</summary>
    public Vector2 GlyphOffset { get; init; }

    /// <summary>Gets the unit of <see cref="GlyphOffset"/>. <see cref="StyleUnit.Default"/> is ems.</summary>
    public StyleUnit GlyphOffsetUnit { get; init; }

    /// <summary>Gets extra spacing added after each character on top of <see cref="TextStyle.LetterSpacing"/>.</summary>
    public StyleLength LetterSpacing { get; init; }

    /// <summary>Gets the minimum horizontal glyph advance. Narrower glyphs are centered within it.</summary>
    /// <value>The advance, or <see cref="StyleUnit.Default"/> for no minimum.</value>
    public StyleLength GlyphMinAdvanceX { get; init; }

    /// <summary>Gets the maximum horizontal glyph advance. Wider glyphs are centered within it.</summary>
    /// <value>The advance, or <see cref="StyleUnit.Default"/> for no maximum.</value>
    public StyleLength GlyphMaxAdvanceX { get; init; }

    /// <summary>Gets a value indicating whether to scale glyphs down to fit a square of 1 em.</summary>
    public bool FitToSquare { get; init; }

    /// <summary>Gets the OpenType features to shape text with, unless the style has its own.</summary>
    public ImmutableArray<FontFeature> Features
    {
        get => this.features.Array;
        init => this.features = new(value);
    }

    /// <summary>Gets adjustments that give every glyph the same advance.</summary>
    /// <param name="advance">The advance, such as <see cref="StyleLength.Em"/> of 1 for icons.</param>
    /// <returns>The adjustments, with <see cref="GlyphMinAdvanceX"/> and <see cref="GlyphMaxAdvanceX"/> set to
    /// <paramref name="advance"/>.</returns>
    public static FontAdjustments FixedAdvance(StyleLength advance) =>
        new() { GlyphMinAdvanceX = advance, GlyphMaxAdvanceX = advance };

    /// <inheritdoc/>
    /// <remarks>The adjustments that are not at their defaults, as in <c>U+E000-U+F8FF, advance 1em</c>.</remarks>
    public override string ToString()
    {
        var parts = new List<string>();
        var ranges = this.CodepointRanges;
        for (var i = 0; i + 1 < ranges.Length && i < 4; i += 2)
            parts.Add(ranges[i] == ranges[i + 1] ? $"U+{ranges[i]:X4}" : $"U+{ranges[i]:X4}-U+{ranges[i + 1]:X4}");
        if (ranges.Length > 4)
            parts.Add($"+{(ranges.Length - 4) / 2} ranges");

        if (this.GlyphMinAdvanceX.Unit != StyleUnit.Default && this.GlyphMinAdvanceX == this.GlyphMaxAdvanceX)
        {
            parts.Add($"advance {this.GlyphMinAdvanceX}");
        }
        else
        {
            if (this.GlyphMinAdvanceX.Unit != StyleUnit.Default)
                parts.Add($"min {this.GlyphMinAdvanceX}");
            if (this.GlyphMaxAdvanceX.Unit != StyleUnit.Default)
                parts.Add($"max {this.GlyphMaxAdvanceX}");
        }

        if (this.FitToSquare)
            parts.Add("fit");
        if (this.SizeScale != 1)
            parts.Add(string.Create(CultureInfo.InvariantCulture, $"scale {this.SizeScale:G6}"));
        if (this.GlyphOffset != Vector2.Zero)
        {
            var unit = this.GlyphOffsetUnit == StyleUnit.Px ? "px" : "em";
            parts.Add(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"offset ({this.GlyphOffset.X:G6},{this.GlyphOffset.Y:G6}){unit}"));
        }

        if (this.LetterSpacing.Unit != StyleUnit.Default)
            parts.Add($"spacing {this.LetterSpacing}");
        if (!this.Features.IsEmpty)
        {
            parts.Add(
                string.Join(
                    ' ',
                    this.Features.Select(
                        static f => f.Value switch
                        {
                            0 => $"-{f.TagName}",
                            1 => $"+{f.TagName}",
                            _ => $"{f.TagName}={f.Value}",
                        })));
        }

        return string.Join(", ", parts);
    }
}
