using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Numerics;

using Dalamud.Interface.Fonts.Internal;

using Newtonsoft.Json;

namespace Dalamud.Interface.Fonts;

/// <summary>A family made of other families, each drawing the characters that the ones before it lack.</summary>
/// <param name="elements">The families, in order of priority.</param>
/// <example><code>
/// // Comic Sans MS, then Gungsuh at 90% for Hangul, then the default font.
/// var family = new ChainFamilyId(
///     [
///         new(new SystemFontFamilyId("Comic Sans MS")),
///         new(new SystemFontFamilyId("Gungsuh")) { SizeScale = 0.9f, CodepointRanges = [0xAC00, 0xD7A3] },
///         new(DalamudDefaultFontId.Instance),
///     ]);
/// </code></example>
[method: JsonConstructor]
public sealed class ChainFamilyId(ImmutableArray<ChainFamilyId.Element> elements) : IFontFamilyId
{
    private readonly EquatableArray<Element> elements = new(elements);
    private int hashCode;

    /// <summary>Gets the families, in order of priority.</summary>
    [JsonProperty]
    public ImmutableArray<Element> Elements => this.elements.Array;

    /// <inheritdoc/>
    [JsonIgnore]
    public LocalizedName Name => field ??= new(string.Join(" + ", this.Elements.Select(e => e.Family.Name.English)));

    /// <inheritdoc/>
    /// <remarks>The faces of the first family.</remarks>
    [JsonIgnore]
    public IReadOnlyList<IFontFaceId> Fonts => this.Elements.IsEmpty ? [] : this.Elements[0].Family.Fonts;

    /// <inheritdoc/>
    public int FindBestMatch(FontStyle style) =>
        this.Elements.IsEmpty ? 0 : this.Elements[0].Family.FindBestMatch(style);

    /// <inheritdoc/>
    public override bool Equals(object? obj) =>
        ReferenceEquals(this, obj) || (obj is ChainFamilyId other && this.elements.Equals(other.elements));

    /// <inheritdoc/>
    public override int GetHashCode() =>
        this.hashCode != 0 ? this.hashCode : this.hashCode = this.elements.GetHashCode();

    /// <inheritdoc/>
    /// <remarks>Each element shows its family and the adjustments that are not at their defaults, as in
    /// <c>Chain[FontAwesomeFreeSolid(U+E000-U+F8FF, advance 1em), Default]</c>.</remarks>
    public override string ToString() => $"Chain[{string.Join(", ", this.Elements)}]";

    /// <summary>A family in a chain, with adjustments to its faces.</summary>
    /// <param name="Family">The family. It may be a chain itself.</param>
    public sealed record Element(IFontFamilyId Family)
    {
        private readonly EquatableArray<uint> ranges;
        private readonly EquatableArray<FontFeature> features;
        private readonly float sizeScale = 1;

        /// <summary>Gets the ranges of codepoints to take from <see cref="Family"/>.</summary>
        /// <value>Pairs of inclusive bounds, or empty for all.</value>
        public ImmutableArray<uint> CodepointRanges
        {
            get => this.ranges.Array;
            init => this.ranges = new(value);
        }

        /// <summary>Gets the offset of the glyphs.</summary>
        public Vector2 GlyphOffset { get; init; }

        /// <summary>Gets the unit of <see cref="GlyphOffset"/>. <see cref="StyleUnit.Default"/> is ems.</summary>
        public StyleUnit GlyphOffsetUnit { get; init; }

        /// <summary>Gets the space added after each character, to that of the style.</summary>
        public StyleLength LetterSpacing { get; init; }

        /// <summary>Gets the least advance of the glyphs, which are centered in it.</summary>
        public StyleLength GlyphMinAdvanceX { get; init; }

        /// <summary>Gets the most advance of the glyphs, or <see cref="StyleUnit.Default"/> for no limit.</summary>
        public StyleLength GlyphMaxAdvanceX { get; init; }

        /// <summary>Gets the advance of every glyph, such as <c>StyleLength.Em(1)</c> for icons, or
        /// <see cref="StyleUnit.Default"/> for their own.</summary>
        public StyleLength Advance { get; init; }

        /// <summary>Gets a value indicating whether to scale glyphs down to fit a square of 1 em.</summary>
        public bool FitToSquare { get; init; }

        /// <summary>Gets the em of the glyphs, relative to the em of the text.</summary>
        /// <remarks>Em lengths of this element are in its own em. A value that is not positive and finite is stored as
        /// 1.</remarks>
        public float SizeScale
        {
            get => this.sizeScale;
            init => this.sizeScale = value is > 0 and < float.PositiveInfinity ? value : 1;
        }

        /// <summary>Gets the OpenType features to shape text with, unless the style has its own.</summary>
        public ImmutableArray<FontFeature> Features
        {
            get => this.features.Array;
            init => this.features = new(value);
        }

        /// <inheritdoc/>
        /// <remarks>The family, then the adjustments that are not at their defaults.</remarks>
        public override string ToString()
        {
            var parts = new List<string>();
            var ranges = this.CodepointRanges;
            if (!ranges.IsDefaultOrEmpty)
            {
                for (var i = 0; i + 1 < ranges.Length && i < 4; i += 2)
                {
                    parts.Add(
                        ranges[i] == ranges[i + 1]
                            ? $"U+{ranges[i]:X4}"
                            : $"U+{ranges[i]:X4}-U+{ranges[i + 1]:X4}");
                }

                if (ranges.Length > 4)
                    parts.Add($"+{(ranges.Length - 4) / 2} ranges");
            }

            if (this.Advance.Unit != StyleUnit.Default)
                parts.Add($"advance {this.Advance}");
            if (this.GlyphMinAdvanceX.Unit != StyleUnit.Default)
                parts.Add($"min {this.GlyphMinAdvanceX}");
            if (this.GlyphMaxAdvanceX.Unit != StyleUnit.Default)
                parts.Add($"max {this.GlyphMaxAdvanceX}");
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
            if (!this.Features.IsDefaultOrEmpty)
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

            var name = this.Family.ShortName;
            return parts.Count == 0 ? name : $"{name}({string.Join(", ", parts)})";
        }
    }
}
