using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.Unicode;

namespace Dalamud.Interface.Fonts;

/// <summary>A fluent builder of codepoint ranges for <see cref="ChainFamilyId.Element.CodepointRanges"/>.</summary>
/// <example><code>
/// var element = new ChainFamilyId.Element(new SystemFontFamilyId("Malgun Gothic"))
/// {
///     CodepointRanges = default(FluentGlyphRangeBuilder).WithLanguage("ko-KR").Build(),
/// };
/// </code></example>
[SuppressMessage(
    "StyleCop.CSharp.SpacingRules",
    "SA1010:Opening square brackets should be spaced correctly",
    Justification = "No")]
public struct FluentGlyphRangeBuilder
{
    private const int MaxCodepoint = 0x10FFFF;

    // Characters the engine draws for missing characters, in order of preference.
    private const string FallbackCodepoints = "〓-";

    // Characters the engine draws as an ellipsis, in order of preference.
    private const string EllipsisCodepoints = "….";

    // The ranges added, in the order they were added, as inclusive bounds.
    private List<(int Low, int High)>? ranges;

    /// <summary>Clears the builder.</summary>
    /// <returns><c>this</c> for method chaining.</returns>
    /// <remarks>A builder is in cleared state on first use.</remarks>
    public FluentGlyphRangeBuilder Clear()
    {
        this.ranges?.Clear();
        return this;
    }

    /// <summary>Adds a single codepoint to the builder.</summary>
    /// <param name="codepoint">The codepoint to add.</param>
    /// <returns><c>this</c> for method chaining.</returns>
    /// <remarks>Codepoints outside U+0001 to U+10FFFF are ignored.</remarks>
    public FluentGlyphRangeBuilder With(char codepoint) => this.With((int)codepoint);

    /// <inheritdoc cref="With(char)"/>
    public FluentGlyphRangeBuilder With(uint codepoint) =>
        codepoint <= MaxCodepoint ? this.With((int)codepoint) : this;

    /// <inheritdoc cref="With(char)"/>
    public FluentGlyphRangeBuilder With(int codepoint)
    {
        if (codepoint is >= 1 and <= MaxCodepoint)
            this.Add(codepoint, codepoint);
        return this;
    }

    /// <summary>Adds unicode ranges to the builder.</summary>
    /// <param name="ranges">Unicode ranges to add.</param>
    /// <returns><c>this</c> for method chaining.</returns>
    /// <remarks>Codepoints outside U+0001 to U+10FFFF are ignored.</remarks>
    public FluentGlyphRangeBuilder With(params ReadOnlySpan<UnicodeRange> ranges)
    {
        foreach (var range in ranges)
            this.With(range.FirstCodePoint, (range.FirstCodePoint + range.Length) - 1);
        return this;
    }

    /// <inheritdoc cref="With(ReadOnlySpan{UnicodeRange})"/>
    public FluentGlyphRangeBuilder With(IEnumerable<UnicodeRange> ranges)
    {
        foreach (var range in ranges)
            this.With(range.FirstCodePoint, (range.FirstCodePoint + range.Length) - 1);
        return this;
    }

    /// <summary>Adds a range of characters to the builder.</summary>
    /// <param name="from">The first codepoint, inclusive.</param>
    /// <param name="to">The last codepoint, inclusive.</param>
    /// <returns><c>this</c> for method chaining.</returns>
    /// <remarks>
    /// <para>Codepoints outside U+0001 to U+10FFFF are ignored.</para>
    /// <para>If <paramref name="from"/> is more than <paramref name="to"/>, then they will be swapped.</para>
    /// </remarks>
    public FluentGlyphRangeBuilder With(char from, char to) => this.With(from, (int)to);

    /// <inheritdoc cref="With(char,char)"/>
    public FluentGlyphRangeBuilder With(uint from, uint to) =>
        this.With((int)Math.Min(from, int.MaxValue), (int)Math.Min(to, int.MaxValue));

    /// <inheritdoc cref="With(char,char)"/>
    public FluentGlyphRangeBuilder With(int from, int to)
    {
        if (from > to)
            (from, to) = (to, from);
        from = Math.Max(from, 1);
        to = Math.Min(to, MaxCodepoint);
        if (from <= to)
            this.Add(from, to);
        return this;
    }

    /// <summary>Adds characters from a UTF-8 character sequence.</summary>
    /// <param name="utf8Sequence">The sequence.</param>
    /// <returns><c>this</c> for method chaining.</returns>
    /// <remarks>Invalid sequences are skipped.</remarks>
    public FluentGlyphRangeBuilder With(ReadOnlySpan<byte> utf8Sequence)
    {
        while (!utf8Sequence.IsEmpty)
        {
            if (Rune.DecodeFromUtf8(utf8Sequence, out var rune, out var len) == OperationStatus.Done)
                this.With(rune.Value);
            utf8Sequence = utf8Sequence[len..];
        }

        return this;
    }

    /// <inheritdoc cref="With(ReadOnlySpan{byte})"/>
    public FluentGlyphRangeBuilder With(IEnumerable<byte> utf8Sequence)
    {
        Span<byte> buf = stackalloc byte[4];
        var count = 0;
        foreach (var b in utf8Sequence)
        {
            buf[count++] = b;
            while (count > 0
                   && Rune.DecodeFromUtf8(buf[..count], out var rune, out var len) is var status
                   && status != OperationStatus.NeedMoreData)
            {
                if (status == OperationStatus.Done)
                    this.With(rune.Value);

                // Invalid data may be followed by bytes that start the next sequence.
                buf[len..count].CopyTo(buf);
                count -= len;
            }
        }

        return this;
    }

    /// <summary>Adds characters from a UTF-16 character sequence.</summary>
    /// <param name="utf16Sequence">The sequence.</param>
    /// <returns><c>this</c> for method chaining.</returns>
    /// <remarks>Unpaired surrogates are skipped.</remarks>
    public FluentGlyphRangeBuilder With(ReadOnlySpan<char> utf16Sequence)
    {
        while (!utf16Sequence.IsEmpty)
        {
            if (Rune.DecodeFromUtf16(utf16Sequence, out var rune, out var len) == OperationStatus.Done)
                this.With(rune.Value);
            utf16Sequence = utf16Sequence[len..];
        }

        return this;
    }

    /// <inheritdoc cref="With(ReadOnlySpan{char})"/>
    public FluentGlyphRangeBuilder With(IEnumerable<char> utf16Sequence)
    {
        var high = '\0';
        foreach (var c in utf16Sequence)
        {
            if (char.IsHighSurrogate(c))
            {
                high = c;
                continue;
            }

            if (!char.IsLowSurrogate(c))
                this.With(c);
            else if (high != '\0')
                this.With(char.ConvertToUtf32(high, c));
            high = '\0';
        }

        return this;
    }

    /// <summary>Adds characters from a string.</summary>
    /// <param name="string">The string.</param>
    /// <returns><c>this</c> for method chaining.</returns>
    /// <remarks>Unpaired surrogates are skipped.</remarks>
    public FluentGlyphRangeBuilder With(string @string) => this.With(@string.AsSpan());

    /// <summary>Adds glyphs that are likely to be used in the given culture to the builder.</summary>
    /// <param name="cultureInfo">A culture info.</param>
    /// <returns><c>this</c> for method chaining.</returns>
    /// <remarks>Unsupported culture will do nothing. Do make a PR if you need more.</remarks>
    public FluentGlyphRangeBuilder WithLanguage(CultureInfo cultureInfo) =>
        cultureInfo.TwoLetterISOLanguageName switch
        {
            // http://www.rikai.com/library/kanjitables/kanji_codes.unicode.shtml
            // Enclosed CJK letters are for Japanese cell carriers.
            "ja" => this.With(
                UnicodeRanges.CjkSymbolsandPunctuation,
                UnicodeRanges.Hiragana,
                UnicodeRanges.Katakana,
                UnicodeRanges.HalfwidthandFullwidthForms,
                UnicodeRanges.CjkUnifiedIdeographs,
                UnicodeRanges.CjkUnifiedIdeographsExtensionA,
                UnicodeRanges.EnclosedCjkLettersandMonths),
            "zh" => this.With(UnicodeRanges.CjkUnifiedIdeographs, UnicodeRanges.CjkUnifiedIdeographsExtensionA),
            "ko" => this.With(
                UnicodeRanges.HangulJamo,
                UnicodeRanges.HangulCompatibilityJamo,
                UnicodeRanges.HangulSyllables,
                UnicodeRanges.HangulJamoExtendedA,
                UnicodeRanges.HangulJamoExtendedB),
            _ => this,
        };

    /// <summary>Adds glyphs that are likely to be used in the given culture to the builder.</summary>
    /// <param name="languageTag">A language tag that will be used to locate the culture info.</param>
    /// <returns><c>this</c> for method chaining.</returns>
    /// <remarks>See <see cref="CultureInfo.GetCultureInfo(string)"/> documentation for supported language tags.
    /// </remarks>
    public FluentGlyphRangeBuilder WithLanguage(string languageTag) =>
        this.WithLanguage(CultureInfo.GetCultureInfo(languageTag));

    /// <summary>Builds the ranges, with the characters the engine draws for missing characters and ellipses.
    /// </summary>
    /// <param name="addFallbackCodepoints">Whether to add U+3013 and '-', drawn for missing characters.</param>
    /// <param name="addEllipsisCodepoints">Whether to add U+2026 and '.', drawn as an ellipsis.</param>
    /// <returns>Sorted pairs of inclusive bounds, for <see cref="ChainFamilyId.Element.CodepointRanges"/>.</returns>
    /// <remarks>The builder itself does not change.</remarks>
    public readonly ImmutableArray<uint> Build(bool addFallbackCodepoints = true, bool addEllipsisCodepoints = true)
    {
        var copy = new FluentGlyphRangeBuilder { ranges = this.ranges is null ? [] : [.. this.ranges] };
        if (addFallbackCodepoints)
            copy = copy.With(FallbackCodepoints);
        if (addEllipsisCodepoints)
            copy = copy.With(EllipsisCodepoints);
        return ToCodepointRanges(copy.ranges!);
    }

    /// <summary>Builds the ranges, exactly as added.</summary>
    /// <returns>Sorted pairs of inclusive bounds, for <see cref="ChainFamilyId.Element.CodepointRanges"/>. Empty if
    /// nothing was added, which a chain element takes as all codepoints.</returns>
    public readonly ImmutableArray<uint> BuildExact() =>
        ToCodepointRanges(this.ranges is null ? [] : [.. this.ranges]);

    private static ImmutableArray<uint> ToCodepointRanges(List<(int Low, int High)> ranges)
    {
        ranges.Sort();
        var result = ImmutableArray.CreateBuilder<uint>();
        for (var i = 0; i < ranges.Count;)
        {
            var (low, high) = ranges[i++];
            while (i < ranges.Count && ranges[i].Low <= high + 1)
                high = Math.Max(high, ranges[i++].High);
            result.Add((uint)low);
            result.Add((uint)high);
        }

        return result.DrainToImmutable();
    }

    // Adds a range of valid codepoints.
    private void Add(int low, int high)
    {
        var list = this.ranges ??= [];

        // Characters added one by one, as from a string, mostly extend the last range.
        if (list.Count > 0 && list[^1] is var last && low >= last.Low && low <= last.High + 1)
        {
            if (high > last.High)
                list[^1] = (last.Low, high);
            return;
        }

        list.Add((low, high));
    }
}
