using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Unicode;

using Dalamud.Interface.ManagedFontAtlas.Internals;

namespace Dalamud.Interface.ManagedFontAtlas;

/// <summary>A fluent ImGui glyph range builder.</summary>
[SuppressMessage(
    "StyleCop.CSharp.SpacingRules",
    "SA1010:Opening square brackets should be spaced correctly",
    Justification = "No")]
public struct FluentGlyphRangeBuilder
{
    private const int ImUnicodeCodepointMax = char.MaxValue;

    private BitArray? characters;

    /// <summary>Clears the builder.</summary>
    /// <returns><c>this</c> for method chaining.</returns>
    /// <remarks>A builder is in cleared state on first use.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FluentGlyphRangeBuilder Clear()
    {
        this.characters?.SetAll(false);
        return this;
    }

    /// <summary>Adds a single codepoint to the builder.</summary>
    /// <param name="codepoint">The codepoint to add.</param>
    /// <returns><c>this</c> for method chaining.</returns>
    /// <remarks>Unsupported codepoints will be ignored.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FluentGlyphRangeBuilder With(char codepoint) => this.With((int)codepoint);

    /// <inheritdoc cref="With(char)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FluentGlyphRangeBuilder With(uint codepoint) =>
        codepoint <= char.MaxValue ? this.With((int)codepoint) : this;

    /// <inheritdoc cref="With(char)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FluentGlyphRangeBuilder With(int codepoint)
    {
        if (codepoint <= ImUnicodeCodepointMax)
            this.EnsureCharacters().Set(codepoint, true);
        return this;
    }

    /// <summary>Adds a unicode range to the builder.</summary>
    /// <param name="range">The unicode range to add.</param>
    /// <returns><c>this</c> for method chaining.</returns>
    /// <remarks>Unsupported codepoints will be ignored.</remarks>
    public FluentGlyphRangeBuilder With(UnicodeRange range) =>
        this.With(range.FirstCodePoint, (range.FirstCodePoint + range.Length) - 1);

    /// <summary>Adds unicode ranges to the builder.</summary>
    /// <param name="range1">The 1st unicode range to add.</param>
    /// <param name="range2">The 2st unicode range to add.</param>
    /// <returns><c>this</c> for method chaining.</returns>
    /// <remarks>Unsupported codepoints will be ignored.</remarks>
    public FluentGlyphRangeBuilder With(UnicodeRange range1, UnicodeRange range2) =>
        this.With(range1.FirstCodePoint, (range1.FirstCodePoint + range1.Length) - 1)
            .With(range2.FirstCodePoint, (range2.FirstCodePoint + range2.Length) - 1);

    /// <summary>Adds unicode ranges to the builder.</summary>
    /// <param name="range1">The 1st unicode range to add.</param>
    /// <param name="range2">The 2st unicode range to add.</param>
    /// <param name="range3">The 3rd unicode range to add.</param>
    /// <returns><c>this</c> for method chaining.</returns>
    /// <remarks>Unsupported codepoints will be ignored.</remarks>
    public FluentGlyphRangeBuilder With(UnicodeRange range1, UnicodeRange range2, UnicodeRange range3) =>
        this.With(range1.FirstCodePoint, (range1.FirstCodePoint + range1.Length) - 1)
            .With(range2.FirstCodePoint, (range2.FirstCodePoint + range2.Length) - 1)
            .With(range3.FirstCodePoint, (range3.FirstCodePoint + range3.Length) - 1);

    /// <summary>Adds unicode ranges to the builder.</summary>
    /// <param name="range1">The 1st unicode range to add.</param>
    /// <param name="range2">The 2st unicode range to add.</param>
    /// <param name="range3">The 3rd unicode range to add.</param>
    /// <param name="range4">The 4th unicode range to add.</param>
    /// <param name="evenMoreRanges">Even more unicode ranges to add.</param>
    /// <returns><c>this</c> for method chaining.</returns>
    /// <remarks>Unsupported codepoints will be ignored.</remarks>
    public FluentGlyphRangeBuilder With(
        UnicodeRange range1,
        UnicodeRange range2,
        UnicodeRange range3,
        UnicodeRange range4,
        params UnicodeRange[] evenMoreRanges) =>
        this.With(range1.FirstCodePoint, (range1.FirstCodePoint + range1.Length) - 1)
            .With(range2.FirstCodePoint, (range2.FirstCodePoint + range2.Length) - 1)
            .With(range3.FirstCodePoint, (range3.FirstCodePoint + range3.Length) - 1)
            .With(range4.FirstCodePoint, (range4.FirstCodePoint + range4.Length) - 1)
            .With(evenMoreRanges);

    /// <summary>Adds unicode ranges to the builder.</summary>
    /// <param name="ranges">Unicode ranges to add.</param>
    /// <returns><c>this</c> for method chaining.</returns>
    /// <remarks>Unsupported codepoints will be ignored.</remarks>
    public FluentGlyphRangeBuilder With(IEnumerable<UnicodeRange> ranges)
    {
        foreach (var range in ranges)
            this.With(range);
        return this;
    }

    /// <summary>Adds a range of characters to the builder.</summary>
    /// <param name="from">The first codepoint, inclusive.</param>
    /// <param name="to">The last codepoint, inclusive.</param>
    /// <returns><c>this</c> for method chaining.</returns>
    /// <remarks>
    /// <para>Unsupported codepoints will be ignored.</para>
    /// <para>If <paramref name="from"/> is more than <paramref name="to"/>, then they will be swapped.</para>
    /// </remarks>
    public FluentGlyphRangeBuilder With(char from, char to) =>
        this.With(Math.Clamp(from, int.MinValue, int.MaxValue), Math.Clamp(to, int.MinValue, int.MaxValue));

    /// <inheritdoc cref="With(char,char)"/>
    public FluentGlyphRangeBuilder With(uint from, uint to) =>
        this.With((int)Math.Min(from, int.MaxValue), (int)Math.Min(to, int.MaxValue));

    /// <inheritdoc cref="With(char,char)"/>
    public FluentGlyphRangeBuilder With(int from, int to)
    {
        from = Math.Clamp(from, 1, ImUnicodeCodepointMax);
        to = Math.Clamp(to, 1, ImUnicodeCodepointMax);
        if (from > to)
            (from, to) = (to, from);

        var bits = this.EnsureCharacters();
        for (; from <= to; from++)
            bits.Set(from, true);
        return this;
    }

    /// <summary>Adds characters from a UTF-8 character sequence.</summary>
    /// <param name="utf8Sequence">The sequence.</param>
    /// <returns><c>this</c> for method chaining.</returns>
    /// <remarks>Unsupported codepoints will be ignored.</remarks>
    public FluentGlyphRangeBuilder With(ReadOnlySpan<byte> utf8Sequence)
    {
        var bits = this.EnsureCharacters();
        while (!utf8Sequence.IsEmpty)
        {
            if (Rune.DecodeFromUtf8(utf8Sequence, out var rune, out var len) == OperationStatus.Done
                && rune.Value < ImUnicodeCodepointMax)
                bits.Set(rune.Value, true);
            utf8Sequence = utf8Sequence[len..];
        }

        return this;
    }

    /// <summary>Adds characters from a UTF-8 character sequence.</summary>
    /// <param name="utf8Sequence">The sequence.</param>
    /// <returns><c>this</c> for method chaining.</returns>
    /// <remarks>Unsupported codepoints will be ignored.</remarks>
    public FluentGlyphRangeBuilder With(IEnumerable<byte> utf8Sequence)
    {
        Span<byte> buf = stackalloc byte[4];
        var bufp = 0;
        var bits = this.EnsureCharacters();
        foreach (var b in utf8Sequence)
        {
            buf[bufp++] = b;

            while (Rune.DecodeFromUtf8(buf[..bufp], out var rune, out var len) is var state
                   && state != OperationStatus.NeedMoreData)
            {
                switch (state)
                {
                    case OperationStatus.Done when rune.Value <= ImUnicodeCodepointMax:
                        bits.Set(rune.Value, true);
                        goto case OperationStatus.InvalidData;

                    case OperationStatus.InvalidData:
                        bufp -= len;
                        break;

                    case OperationStatus.NeedMoreData:
                    case OperationStatus.DestinationTooSmall:
                    default:
                        throw new InvalidOperationException($"Unexpected return from {Rune.DecodeFromUtf8}.");
                }
            }
        }

        return this;
    }

    /// <summary>Adds characters from a UTF-16 character sequence.</summary>
    /// <param name="utf16Sequence">The sequence.</param>
    /// <returns><c>this</c> for method chaining.</returns>
    /// <remarks>Unsupported codepoints will be ignored.</remarks>
    public FluentGlyphRangeBuilder With(ReadOnlySpan<char> utf16Sequence)
    {
        var bits = this.EnsureCharacters();
        while (!utf16Sequence.IsEmpty)
        {
            if (Rune.DecodeFromUtf16(utf16Sequence, out var rune, out var len) == OperationStatus.Done
                && rune.Value <= ImUnicodeCodepointMax)
                bits.Set(rune.Value, true);
            utf16Sequence = utf16Sequence[len..];
        }

        return this;
    }

    /// <summary>Adds characters from a UTF-16 character sequence.</summary>
    /// <param name="utf16Sequence">The sequence.</param>
    /// <returns><c>this</c> for method chaining.</returns>
    /// <remarks>Unsupported codepoints will be ignored.</remarks>
    public FluentGlyphRangeBuilder With(IEnumerable<char> utf16Sequence)
    {
        var bits = this.EnsureCharacters();
        foreach (var c in utf16Sequence)
        {
            if (!char.IsSurrogate(c))
                bits.Set(c, true);
        }

        return this;
    }

    /// <summary>Adds characters from a string.</summary>
    /// <param name="string">The string.</param>
    /// <returns><c>this</c> for method chaining.</returns>
    /// <remarks>Unsupported codepoints will be ignored.</remarks>
    public FluentGlyphRangeBuilder With(string @string) => this.With(@string.AsSpan());

    /// <summary>Adds glyphs that are likely to be used in the given culture to the builder.</summary>
    /// <param name="cultureInfo">A culture info.</param>
    /// <returns><c>this</c> for method chaining.</returns>
    /// <remarks>Unsupported codepoints will be ignored. Unsupported culture will do nothing.
    /// Do make a PR if you need more.</remarks>
    public FluentGlyphRangeBuilder WithLanguage(CultureInfo cultureInfo)
    {
        // Call in chunks of three to avoid allocating arrays.
        // Avoid adding ranges that goes over BMP; that is, ranges that goes over ImUnicodeCodepointMax.
        switch (cultureInfo.TwoLetterISOLanguageName)
        {
            case "ja":
                // http://www.rikai.com/library/kanjitables/kanji_codes.unicode.shtml
                return
                    this
                        .With(
                            UnicodeRanges.CjkSymbolsandPunctuation,
                            UnicodeRanges.Hiragana,
                            UnicodeRanges.Katakana)
                        .With(
                            UnicodeRanges.HalfwidthandFullwidthForms,
                            UnicodeRanges.CjkUnifiedIdeographs,
                            UnicodeRanges.CjkUnifiedIdeographsExtensionA)
                        // Blame Japanese cell carriers for the below.
                        .With(
                            UnicodeRanges.EnclosedCjkLettersandMonths);
            case "zh":
                return
                    this
                        .With(
                            UnicodeRanges.CjkUnifiedIdeographs,
                            UnicodeRanges.CjkUnifiedIdeographsExtensionA);
            case "ko":
                return
                    this
                        .With(
                            UnicodeRanges.HangulJamo,
                            UnicodeRanges.HangulCompatibilityJamo,
                            UnicodeRanges.HangulSyllables)
                        .With(
                            UnicodeRanges.HangulJamoExtendedA,
                            UnicodeRanges.HangulJamoExtendedB);
            default:
                return this;
        }
    }

    /// <summary>Adds glyphs that are likely to be used in the given culture to the builder.</summary>
    /// <param name="languageTag">A language tag that will be used to locate the culture info.</param>
    /// <returns><c>this</c> for method chaining.</returns>
    /// <remarks>See <see cref="CultureInfo.GetCultureInfo(string)"/> documentation for supported language tags.
    /// </remarks>
    public FluentGlyphRangeBuilder WithLanguage(string languageTag) =>
        this.WithLanguage(CultureInfo.GetCultureInfo(languageTag));

    /// <summary>Builds the accumulated data into an ImGui glyph range.</summary>
    /// <param name="addFallbackCodepoints">Whether to add the default fallback codepoints to the range.</param>
    /// <param name="addEllipsisCodepoints">Whether to add the default ellipsis codepoints to the range.</param>
    /// <returns>The built ImGui glyph ranges.</returns>
    public ushort[] Build(bool addFallbackCodepoints = true, bool addEllipsisCodepoints = true)
    {
        if (addFallbackCodepoints)
            this.With(FontAtlasFactory.FallbackCodepoints);
        if (addEllipsisCodepoints)
            this.With(FontAtlasFactory.EllipsisCodepoints).With('.');
        return this.BuildExact();
    }

    /// <summary>Builds the accumulated data into an ImGui glyph range, exactly as specified.</summary>
    /// <returns>The built ImGui glyph ranges.</returns>
    public ushort[] BuildExact()
    {
        if (this.characters is null)
            return [0];
        var bits = this.characters;

        // Count the number of ranges first.
        var numRanges = 0;
        var lastCodepoint = -1;
        for (var i = 1; i <= ImUnicodeCodepointMax; i++)
        {
            if (bits.Get(i))
            {
                if (lastCodepoint == -1)
                    lastCodepoint = i;
            }
            else
            {
                if (lastCodepoint != -1)
                {
                    numRanges++;
                    lastCodepoint = -1;
                }
            }
        }

        // Handle the final range that terminates on the ending boundary.
        if (lastCodepoint != -1)
            numRanges++;

        // Allocate the array and build the range.
        var res = GC.AllocateUninitializedArray<ushort>((numRanges * 2) + 1);
        var resp = 0;
        for (var i = 1; i <= ImUnicodeCodepointMax; i++)
        {
            if (bits.Get(i) == ((resp & 1) == 0))
                res[resp++] = unchecked((ushort)i);
        }

        // Handle the final range that terminates on the ending boundary.
        if ((resp & 1) == 1)
            res[resp++] = ImUnicodeCodepointMax;

        // Add the zero terminator.
        res[resp] = 0;

        return res;
    }

    /// <summary>Ensures that <see cref="characters"/> is not null, by creating one as necessary.</summary>
    /// <returns>An instance of <see cref="BitArray"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private BitArray EnsureCharacters() => this.characters ??= new(ImUnicodeCodepointMax + 1);
}
