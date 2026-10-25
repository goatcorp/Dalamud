using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading.Tasks;

using Dalamud.Interface.Fonts.Internal.Sources;
using Dalamud.Utility;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Dalamud.Interface.Fonts.Internal.DirectWrite;

/// <summary>Fallback font backed by DirectWrite's <see cref="IDWriteFontFallback"/>.</summary>
/// <remarks><ul>
/// <li>Faces are picked per grapheme for a locale. Glyph IDs pack <c>(faceIndex &lt;&lt; 16) | glyphId</c>, up to 256
/// resolved faces.</li>
/// <li>Metrics and glyph <c>0</c> come from the face chosen for a probe character of the locale's main script.</li>
/// <li>Newly installed fonts are picked up after refreshing installed families and calling
/// <see cref="DirectWriteFontSources.DropSystemFallbacks"/>.</li>
/// </ul></remarks>
/// <seealso cref="DirectWriteUtilities.ListFamilies"/>
internal sealed unsafe class SystemFallbackFont : MultiSourceFont
{
    private const int MaxFaces = 256;
    private const int FaceShift = 16;
    private const uint Unmapped = uint.MaxValue;

    private readonly string locale;
    private readonly DirectWriteFontSources sources;
    private readonly FontStyle style;
    private readonly ImmutableArray<FontFeature> features;
    private readonly bool noHinting;
    private readonly Dictionary<uint, uint> glyphsByCodepoint = [];
    private readonly Dictionary<string, int> faceIndices = [];

    // Resolved fallback fonts paired with the scale factor returned by MapCharacters.
    private readonly List<(DirectWriteFont Font, float Scale)> faces = [];
    private IDWriteFontFallback* fallback;
    private char[] textBuffer = [];
    private int[] ownerBuffer = [];

    // Index into faces for metrics and .notdef, -1 if none was found, or int.MinValue while uninitialized.
    private int metricsFace = int.MinValue;

    /// <summary>Initializes a new instance of the <see cref="SystemFallbackFont"/> class.</summary>
    /// <param name="locale">Locale name for the fallback choices and for shaping, such as "ja-JP".</param>
    /// <param name="sources">The sources whose faces to share.</param>
    /// <param name="style">The style of the faces to pick.</param>
    /// <param name="features">The OpenType features to shape text with, or empty for the default features.</param>
    /// <param name="noHinting">Whether to rasterize without fitting the outlines to the pixel grid.</param>
    /// <remarks><ul>
    /// <li>The locale decides between fonts, and between glyph forms of a font, such as Han variants.</li>
    /// <li>Windows simulates the styles of fonts without faces for them.</li>
    /// </ul></remarks>
    public SystemFallbackFont(
        string locale, DirectWriteFontSources sources, FontStyle style = default,
        ImmutableArray<FontFeature> features = default, bool noHinting = false)
        : base($"System fallback ({locale}) {style}", FaceShift)
    {
        this.locale = locale;
        this.sources = sources;
        this.style = style;
        this.features = features;
        this.noHinting = noHinting;
        fixed (IDWriteFontFallback** p = &this.fallback)
            DirectWriteUtilities.Factory->GetSystemFontFallback(p).ThrowOnError();
    }

    /// <summary>Finalizes an instance of the <see cref="SystemFallbackFont"/> class.</summary>
    ~SystemFallbackFont()
    {
        if (this.fallback is not null)
            this.fallback->Release();
        this.fallback = null;
    }

    /// <inheritdoc/>
    /// <remarks>The face Windows picks for a probe character of the locale's main script.</remarks>
    protected override int NotdefSource =>
        this.metricsFace == int.MinValue ? this.metricsFace = this.FindMetricsFace() : this.metricsFace;

    private DirectWriteFont? MetricsFace => this.NotdefSource >= 0 ? this.faces[this.NotdefSource].Font : null;

    private float MetricsScale => this.NotdefSource >= 0 ? this.faces[this.NotdefSource].Scale : 1f;

    /// <summary>Triggers Windows font enumeration on a background thread to avoid a first-use stall.</summary>
    /// <param name="sources">The sources whose faces to share.</param>
    /// <remarks>Uses a throwaway instance so live fonts are not touched off the main thread.</remarks>
    public static void Preload(DirectWriteFontSources sources) =>
        _ = Task.Run(() =>
        {
            try
            {
                var font = new SystemFallbackFont("en-US", sources);
                _ = font.GetGlyphIndex(0x3042);
            }
            catch
            {
                // Best-effort preload.
            }
        });

    /// <inheritdoc/>
    public override float LineHeightRatioAtEm(float em) =>
        this.MetricsFace?.LineHeightRatioAtEm(em * this.MetricsScale) ?? 1;

    /// <inheritdoc/>
    public override float LineHeightRatioAtLineHeight(float lineHeight) =>
        this.MetricsFace?.LineHeightRatioAtLineHeight(lineHeight * this.MetricsScale) ?? 1;

    /// <inheritdoc/>
    public override FontMetrics GetMetrics(float size, float density) =>
        this.MetricsFace is { } face ? face.GetMetrics(size * this.MetricsScale, density) : FontMetrics.Fallback(size);

    /// <inheritdoc/>
    public override void GetGlyphIndices(ReadOnlySpan<uint> codepoints, Span<uint> glyphs)
    {
        var missing = false;
        for (var i = 0; i < codepoints.Length; i++)
        {
            if (!this.glyphsByCodepoint.TryGetValue(codepoints[i], out glyphs[i]))
            {
                glyphs[i] = Unmapped;
                missing = true;
            }
        }

        if (missing)
            this.MapMissing(codepoints, glyphs);
    }

    /// <inheritdoc/>
    protected override Font CreateStyled(in TextStyle key) =>
        new SystemFallbackFont(
            key.Locale ?? this.locale,
            this.sources,
            key.On(this.style),
            key.Features.IsEmpty ? this.features : key.Features,
            key.NoHinting || this.noHinting);

    /// <inheritdoc/>
    protected override Font SourceFont(int source) => this.faces[source].Font;

    /// <inheritdoc/>
    protected override int SourceCount() => this.faces.Count;

    /// <inheritdoc/>
    protected override int AddSourceFor(ReadOnlySpan<uint> codepoints) => this.FaceFor(codepoints);

    /// <inheritdoc/>
    /// <remarks>Applies the scale factor returned by DirectWrite fallback for <paramref name="source"/>.</remarks>
    protected override float SourceSize(int source, float size) => size * this.faces[source].Scale;

    /// <inheritdoc/>
    /// <remarks>Missing graphemes emit glyph <c>0</c> with zero advance.</remarks>
    protected override void ShapeWith(int source, ShapeRun run, ReadOnlySpan<int> graphemes, ShapedGlyphList output)
    {
        if (source < 0)
        {
            foreach (var g in graphemes)
                output.Add(0, g, 0);
            return;
        }

        var (font, scale) = this.faces[source];
        font.Shape(scale == 1f ? run : run.WithSize(run.Size * scale), output);
    }

    // Face index chosen by DirectWrite for a probe character of the locale's main script, or -1.
    private int FindMetricsFace()
    {
        var probe = this.locale.Length >= 2
                        ? (char.ToLowerInvariant(this.locale[0]), char.ToLowerInvariant(this.locale[1])) switch
                        {
                            ('j', 'a') => 0x3042u,
                            ('z', 'h') => 0x4E2Du,
                            ('k', 'o') => 0xAC00u,
                            _ => 'A',
                        }
                        : 'A';
        return this.PickedSource(this.GetGlyphIndex(probe));
    }

    // Batches unmapped codepoints into one UTF-16 buffer for MapCharacters and caches the resulting glyph IDs.
    private void MapMissing(ReadOnlySpan<uint> codepoints, Span<uint> glyphs)
    {
        // The UTF-16 text of the codepoints, and per code unit, the index of its codepoint.
        var pooledText = PooledSpan<char>.FromBuffer(ref this.textBuffer, codepoints.Length * 2);
        var pooledOwners = PooledSpan<int>.FromBuffer(ref this.ownerBuffer, codepoints.Length * 2);
        var text = pooledText.Span;
        var owners = pooledOwners.Span;
        var length = 0;
        for (var i = 0; i < codepoints.Length; i++)
        {
            if (glyphs[i] != Unmapped)
                continue;
            if (!Rune.TryCreate(codepoints[i], out var rune))
            {
                glyphs[i] = this.glyphsByCodepoint[codepoints[i]] = 0;
                continue;
            }

            var n = rune.EncodeToUtf16(text[length..]);
            owners.Slice(length, n).Fill(i);
            length += n;
        }

        var position = 0;
        if (length > 0)
        {
            fixed (char* pText = text)
                position = this.MapText(pText, length, owners, codepoints, glyphs);
        }

        for (var k = position; k < length; k++)
            glyphs[owners[k]] = this.glyphsByCodepoint[codepoints[owners[k]]] = 0;

        pooledText.Recycle();
        pooledOwners.Recycle();
    }

    // Runs MapCharacters across text in runs, caching mapped glyphs.
    // Returns the number of UTF-16 code units mapped, which is less than length if fallback fails partway.
    private int MapText(
        char* text, int length, ReadOnlySpan<int> owners, ReadOnlySpan<uint> codepoints, Span<uint> glyphs)
    {
        var position = 0;
        var (weight, fontStyle, stretch) = this.DirectWriteStyle();
        fixed (char* pLocale = this.locale)
        {
            var source = new DirectWriteTextSource(text, (uint)length, pLocale);
            while (position < length)
            {
                using var font = default(ComPtr<IDWriteFont>);
                uint mapped;
                float scale;
                if (this.fallback->MapCharacters(
                        (IDWriteTextAnalysisSource*)&source,
                        (uint)position,
                        (uint)(length - position),
                        null,
                        null,
                        weight,
                        fontStyle,
                        stretch,
                        &mapped,
                        font.GetAddressOf(),
                        &scale).FAILED)
                {
                    break;
                }

                // Keep surrogate pairs intact and ensure forward progress even if mapped is 0.
                var end = Math.Min(length, position + (int)Math.Max(mapped, 1));
                while (end < length && owners[end] == owners[end - 1])
                    end++;
                var faceIndex = font.Get() is null ? -1 : this.FaceIndexOf(font.Get(), scale);
                for (var k = position; k < end; k++)
                {
                    if (k > position && owners[k] == owners[k - 1])
                        continue;
                    var c = codepoints[owners[k]];
                    glyphs[owners[k]] = this.glyphsByCodepoint[c] =
                        faceIndex < 0 ? 0 : this.Tag(faceIndex, this.faces[faceIndex].Font.GetGlyphIndex(c));
                }

                position = end;
            }
        }

        return position;
    }

    private (DWRITE_FONT_WEIGHT Weight, DWRITE_FONT_STYLE Style, DWRITE_FONT_STRETCH Stretch) DirectWriteStyle() =>
        ((DWRITE_FONT_WEIGHT)Math.Min(this.style.EffectiveWeight, 999),
         this.style.Italicized
             ? DWRITE_FONT_STYLE.DWRITE_FONT_STYLE_ITALIC
             : DWRITE_FONT_STYLE.DWRITE_FONT_STYLE_NORMAL,
         (DWRITE_FONT_STRETCH)this.style.EffectiveStretch);

    // Resolves the fallback face index for a grapheme (verifying it supports codepoints[0]), or -1.
    private int FaceFor(ReadOnlySpan<uint> codepoints)
    {
        if (codepoints.IsEmpty || codepoints.Length > 64)
            return -1;
        var text = stackalloc char[codepoints.Length * 2];
        var length = 0;
        foreach (var c in codepoints)
        {
            if (!Rune.TryCreate(c, out var rune))
                return -1;
            length += rune.EncodeToUtf16(new(text + length, 2));
        }

        // MapCharacters ignores U+FE0E and U+FE0F, so steer it with Segoe UI Emoji or Segoe UI Symbol when present.
        // Without a base family, MultiSourceFont would reject the pick and scan whichever faces were loaded earlier.
        using var collection = default(ComPtr<IDWriteFontCollection>);
        // MultiSourceFont.PickSourceOnce places any presentation selector at codepoints[^1].
        var baseFamily = codepoints[^1] switch
        {
            EmojiPresentation => "Segoe UI Emoji",
            TextPresentation => "Segoe UI Symbol",
            _ => null,
        };
        if (baseFamily is not null &&
            DirectWriteUtilities.Factory->GetSystemFontCollection(collection.GetAddressOf(), false).FAILED)
        {
            baseFamily = null;
        }

        using var font = default(ComPtr<IDWriteFont>);
        float scale;
        var (weight, fontStyle, stretch) = this.DirectWriteStyle();
        fixed (char* pLocale = this.locale, pBaseFamily = baseFamily)
        {
            var source = new DirectWriteTextSource(text, (uint)length, pLocale);
            uint mappedLength;
            if (this.fallback->MapCharacters(
                    (IDWriteTextAnalysisSource*)&source,
                    0,
                    (uint)length,
                    baseFamily is null ? null : collection.Get(),
                    pBaseFamily,
                    weight,
                    fontStyle,
                    stretch,
                    &mappedLength,
                    font.GetAddressOf(),
                    &scale).FAILED || font.Get() is null)
            {
                return -1;
            }
        }

        BOOL exists;
        if (font.Get()->HasCharacter(codepoints[0], &exists).FAILED || !exists)
            return -1;
        return this.FaceIndexOf(font.Get(), scale);
    }

    // Returns or registers the face index for a fallback font and scale, or -1 if MaxFaces is reached.
    private int FaceIndexOf(IDWriteFont* font, float scale)
    {
        // Keyed by name, as DirectWrite hands out a new font object for each mapping.
        using var family = default(ComPtr<IDWriteFontFamily>);
        using var names = default(ComPtr<IDWriteLocalizedStrings>);
        var familyName = font->GetFontFamily(family.GetAddressOf()).FAILED ||
                         family.Get()->GetFamilyNames(names.GetAddressOf()).FAILED
                             ? "?"
                             : DirectWriteUtilities.EnglishNameOf(names.Get());

        var key =
            $"{familyName}/{(int)font->GetWeight()}/{(int)font->GetStretch()}/{(int)font->GetStyle()}/" +
            $"{(int)font->GetSimulations()}";
        var scaledKey = scale == 1f ? key : $"{key}/{scale}";
        if (!this.faceIndices.TryGetValue(scaledKey, out var index))
        {
            if (this.faces.Count >= MaxFaces)
                return -1;

            using var face = default(ComPtr<IDWriteFontFace>);
            if (font->CreateFontFace(face.GetAddressOf()).FAILED)
                return -1;
            var sims = face.Get()->GetSimulations();
            var slant = DirectWriteFontFace.TakeOblique(ref sims) ? SyntheticStyleFont.Slant : 0;
            var shared = this.sources.GetFace(key, face.Get(), sims);
            index = this.faces.Count;
            this.faces.Add(
                (new(key, shared, this.locale, slant, features: this.features, noHinting: this.noHinting), scale));
            this.faceIndices.Add(scaledKey, index);
        }

        return index;
    }
}
