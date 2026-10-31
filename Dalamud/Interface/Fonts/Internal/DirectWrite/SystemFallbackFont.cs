using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading.Tasks;

using Dalamud.Interface.Fonts.Internal.Sources;
using Dalamud.Utility;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Dalamud.Interface.Fonts.Internal.DirectWrite;

/// <summary>Any installed font that has a codepoint, as Windows' system font fallback picks it.</summary>
/// <remarks>Faces are picked per grapheme, for a locale. Glyph ids are (face index &lt;&lt; 16) | the face's glyph id,
/// for at most 256 faces. The metrics and glyph 0 come from the face of a character of the locale's main script.
/// </remarks>
internal sealed unsafe class SystemFallbackFont : MultiSourceFont
{
    private const int MaxFaces = 256;
    private const int FaceShift = 16;

    private readonly string locale;
    private readonly DirectWriteFontSources? sources;
    private readonly FontStyle style;
    private readonly ImmutableArray<FontFeature> features;
    private readonly bool noHinting;
    private readonly Dictionary<uint, uint> glyphsByCodepoint = [];
    private readonly Dictionary<string, int> faceIndices = [];
    private readonly List<DirectWriteFont> faces = [];
    private IDWriteFontFallback* fallback;

    // The face of the metrics and .notdef, -1 if none was found, or int.MinValue until looked for.
    private int metricsFace = int.MinValue;

    /// <summary>Initializes a new instance of the <see cref="SystemFallbackFont"/> class.</summary>
    /// <param name="locale">Locale name for the fallback choices and for shaping, such as "ja-JP".</param>
    /// <param name="sources">The sources whose faces to share, or null for faces of its own. Those draw the outlines
    /// of color glyphs, and rasterize all glyphs at once.</param>
    /// <param name="style">The style of the faces to pick.</param>
    /// <param name="features">The OpenType features to shape text with, or empty for the default features.</param>
    /// <param name="noHinting">Whether to rasterize without fitting the outlines to the pixel grid.</param>
    /// <remarks>The locale decides between fonts, and between glyph forms of a font, such as Han variants. Windows
    /// simulates the styles of fonts without faces for them.</remarks>
    public SystemFallbackFont(
        string locale, DirectWriteFontSources? sources = null, FontStyle style = default,
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
    /// <remarks>The face Windows picks for a character of the locale's main script.</remarks>
    protected override int NotdefSource =>
        this.metricsFace == int.MinValue ? this.metricsFace = this.FindMetricsFace() : this.metricsFace;

    // The face of the metrics, or null if there is none.
    private DirectWriteFont? MetricsFace => this.NotdefSource >= 0 ? this.faces[this.NotdefSource] : null;

    /// <summary>Makes Windows enumerate the installed fonts now in the background, instead of on first use.</summary>
    public static void Preload() =>
        _ = Task.Run(() =>
        {
            try
            {
                var font = new SystemFallbackFont("en-US");
                Span<uint> glyph = stackalloc uint[1];
                font.GetGlyphIndices([0x3042], glyph);
            }
            catch
            {
                // nothing to warm
            }
        });

    /// <inheritdoc/>
    /// <remarks>That of the face of the metrics.</remarks>
    public override float NaturalRatio(float size, bool isEm) => this.MetricsFace?.NaturalRatio(size, isEm) ?? 1;

    /// <inheritdoc/>
    public override FontMetrics GetMetrics(float size, float density) =>
        this.MetricsFace is { } face ? face.GetMetrics(size, density) : FontMetrics.Fallback(size);

    /// <inheritdoc/>
    public override void GetGlyphIndices(ReadOnlySpan<uint> codepoints, Span<uint> glyphs)
    {
        for (var i = 0; i < codepoints.Length; i++)
        {
            if (!this.glyphsByCodepoint.TryGetValue(codepoints[i], out var g))
                this.glyphsByCodepoint.Add(codepoints[i], g = this.Map(codepoints[i]));
            glyphs[i] = g;
        }
    }

    /// <summary>Gets the English name of localized names, or the first.</summary>
    /// <param name="names">The names.</param>
    /// <returns>The name.</returns>
    internal static string EnglishNameOf(IDWriteLocalizedStrings* names)
    {
        uint index;
        fixed (char* en = "en-us")
        {
            BOOL exists;
            if (names->FindLocaleName(en, &index, &exists).FAILED || !exists)
                index = 0;
        }

        uint length;
        if (names->GetStringLength(index, &length).FAILED)
            return "?";
        var buf = new char[length + 1];
        fixed (char* p = buf)
        {
            if (names->GetString(index, p, length + 1).FAILED)
                return "?";
        }

        return new(buf, 0, (int)length);
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
    protected override Font SourceFont(int source) => this.faces[source];

    /// <inheritdoc/>
    protected override int SourceCount() => this.faces.Count;

    /// <inheritdoc/>
    /// <remarks>The face Windows picks for the whole grapheme.</remarks>
    protected override int AddSourceFor(ReadOnlySpan<uint> codepoints) => this.FaceFor(codepoints);

    /// <inheritdoc/>
    /// <remarks>Missing graphemes are glyph 0, with no advance.</remarks>
    protected override void ShapeWith(int source, ShapeRun run, ReadOnlySpan<int> graphemes, ShapedGlyphList output)
    {
        if (source < 0)
        {
            foreach (var g in graphemes)
                output.Add(0, g, 0);
            return;
        }

        this.faces[source].Shape(run, output);
    }

    // The face Windows picks for a character of the locale's main script, or -1.
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
        Span<uint> codepoint = stackalloc uint[1];
        Span<uint> glyph = stackalloc uint[1];
        codepoint[0] = probe;
        this.GetGlyphIndices(codepoint, glyph);
        return this.PickedSource(glyph[0]);
    }

    private uint Map(uint codepoint)
    {
        Span<uint> one = stackalloc uint[1];
        one[0] = codepoint;
        var faceIndex = this.FaceFor(one);
        if (faceIndex < 0)
            return 0;
        Span<uint> glyph = stackalloc uint[1];
        this.faces[faceIndex].GetGlyphIndices(one, glyph);
        return this.Tag(faceIndex, glyph[0]);
    }

    // The face Windows picks for text that has its first codepoint, or -1.
    private int FaceFor(ReadOnlySpan<uint> codepoints)
    {
        if (this.fallback is null || codepoints.IsEmpty || codepoints.Length > 64)
            return -1;
        var text = stackalloc char[codepoints.Length * 2];
        var length = 0;
        foreach (var c in codepoints)
        {
            if (!Rune.TryCreate(c, out var rune))
                return -1;
            length += rune.EncodeToUtf16(new(text + length, 2));
        }

        using var font = default(ComPtr<IDWriteFont>);
        fixed (char* pLocale = this.locale)
        {
            var source = new DirectWriteTextSource(text, (uint)length, pLocale);
            uint mappedLength;
            float scale;
            if (this.fallback->MapCharacters(
                    (IDWriteTextAnalysisSource*)&source,
                    0,
                    (uint)length,
                    null,
                    null,
                    (DWRITE_FONT_WEIGHT)Math.Min(this.style.EffectiveWeight, 999),
                    this.style.Italicized
                        ? DWRITE_FONT_STYLE.DWRITE_FONT_STYLE_ITALIC
                        : DWRITE_FONT_STYLE.DWRITE_FONT_STYLE_NORMAL,
                    (DWRITE_FONT_STRETCH)this.style.EffectiveStretch,
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
        return this.FaceIndexOf(font.Get());
    }

    private int FaceIndexOf(IDWriteFont* font)
    {
        using var family = default(ComPtr<IDWriteFontFamily>);
        using var names = default(ComPtr<IDWriteLocalizedStrings>);
        var familyName = font->GetFontFamily(family.GetAddressOf()).FAILED ||
                         family.Get()->GetFamilyNames(names.GetAddressOf()).FAILED
                             ? "?"
                             : EnglishNameOf(names.Get());

        var key =
            $"{familyName}/{(int)font->GetWeight()}/{(int)font->GetStretch()}/{(int)font->GetStyle()}/" +
            $"{(int)font->GetSimulations()}";
        if (this.faceIndices.TryGetValue(key, out var index))
            return index;
        if (this.faces.Count >= MaxFaces)
            return -1;

        using var face = default(ComPtr<IDWriteFontFace>);
        if (font->CreateFontFace(face.GetAddressOf()).FAILED)
            return -1;
        var sims = face.Get()->GetSimulations();
        var shared = this.sources?.GetFace(key, face.Get(), sims) ?? new DirectWriteFontFace(key, face.Get(), sims);
        index = this.faces.Count;
        this.faces.Add(new(key, shared, this.locale, features: this.features, noHinting: this.noHinting));
        this.faceIndices.Add(key, index);
        return index;
    }
}
