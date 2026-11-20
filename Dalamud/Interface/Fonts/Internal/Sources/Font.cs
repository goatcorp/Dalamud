using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

using Dalamud.Interface.Fonts.Internal.Glyphs;

namespace Dalamud.Interface.Fonts.Internal.Sources;

/// <summary>A font for <see cref="Layout.TextLayouter"/>.</summary>
/// <remarks>Shaping, metric, kerning, and glyph-image calls are main-thread only and reuse unsynchronized scratch
/// buffers and caches. <see cref="WithStyle"/> and <see cref="CreateStyled"/> may also be called from background
/// load threads.</remarks>
/// <seealso cref="FontImpl"/>
internal abstract class Font
{
    /// <summary>The base-2 logarithm of <see cref="SubpixelStepCount"/>.</summary>
    public const int SubpixelStepShift = 2;

    /// <summary>The steps a device pixel is divided into for placing glyphs right of whole pixels.</summary>
    /// <seealso cref="GetGlyphImages"/>
    public const int SubpixelStepCount = 1 << SubpixelStepShift;

    /// <summary>The steps a device pixel is divided into for the sizes glyph images are made at.</summary>
    /// <remarks>Fonts rasterize at sizes quantized to a multiple of 1 over <see cref="SizeStepsPerPixel"/> device
    /// pixels. <see cref="CompositeFont"/> relies on this when fitting glyphs.</remarks>
    public const int SizeStepsPerPixel = 4;

    /// <summary>The variation selector that asks for text presentation.</summary>
    public const char TextPresentation = '\uFE0E';

    /// <summary>The variation selector that asks for emoji presentation.</summary>
    public const char EmojiPresentation = '\uFE0F';

    // Maximum cached pair kernings before clearing the cache.
    private const int MaxPairKernings = 4096;

    private static long lastId;

    private readonly Dictionary<TextStyle, Font> styled = [];
    private ShapedGlyphList? pairOutput;
    private List<int>? graphemesBuffer;

    // Cached kerning per pair, or null when unkernable due to a missing glyph, ligature, or multi-glyph cluster.
    private Dictionary<(uint First, uint Second, float Size, float Density), float?>? pairKernings;

    /// <summary>Initializes a new instance of the <see cref="Font"/> class.</summary>
    /// <param name="name">Name for debugging.</param>
    protected Font(string name) => this.Name = name;

    /// <summary>Which font dimension an input to <see cref="Measure"/> represents.</summary>
    public enum Measured
    {
        /// <summary>The size, as the primary face measures it.</summary>
        Size,

        /// <summary>The em.</summary>
        Em,

        /// <summary>The line height.</summary>
        Line,
    }

    /// <summary>Gets the name for debugging.</summary>
    public string Name { get; }

    /// <summary>Gets a unique identifier for this font instance.</summary>
    public long Id { get; } = Interlocked.Increment(ref lastId);

    /// <summary>Gets a value indicating whether the size of this face is its em, rather than its line height.
    /// </summary>
    public virtual bool SizeIsEm => false;

    /// <summary>Gets a value indicating whether this face has color glyphs, such as emoji.</summary>
    /// <remarks>The default has none.</remarks>
    public virtual bool HasColorGlyphs => false;

    /// <summary>Gets the kerning between the last character shaped with one font and the first shaped with another.
    /// </summary>
    /// <param name="left">The font of the first character.</param>
    /// <param name="leftSize">The size of the first character.</param>
    /// <param name="right">The font of the second character.</param>
    /// <param name="rightSize">The size of the second character.</param>
    /// <param name="first">The first character.</param>
    /// <param name="second">The second character.</param>
    /// <param name="density">Device pixels per logical pixel.</param>
    /// <returns>The adjustment in logical pixels.</returns>
    /// <remarks>The average of what the fonts that have both characters say.</remarks>
    public static float KernAcross(
        Font left, float leftSize, Font right, float rightSize, uint first, uint second, float density)
    {
        var l = left.TryGetPairKerning(first, second, leftSize, density, out var k) ? k : (float?)null;

        // ReSharper disable once CompareOfFloatsByEqualityOperator
        var r = (left != right || leftSize != rightSize) &&
                right.TryGetPairKerning(first, second, rightSize, density, out k)
                    ? k
                    : (float?)null;
        return (l, r) switch
        {
            ({ } a, { } b) => (a + b) / 2,
            ({ } a, null) => a,
            (null, { } b) => b,
            _ => 0,
        };
    }

    /// <inheritdoc/>
    public override string ToString() => $"{this.GetType().Name}({this.Name})";

    /// <summary>Gets whether this font can present a grapheme in the requested form.</summary>
    /// <param name="grapheme">The codepoints of the grapheme.</param>
    /// <param name="presentation">The presentation selector, <see cref="TextPresentation"/> or
    /// <see cref="EmojiPresentation"/>.</param>
    /// <returns>Whether it can present <paramref name="grapheme"/> in that form.</returns>
    public virtual bool CanPresentAs(ReadOnlySpan<uint> grapheme, char presentation)
    {
        Debug.Assert(presentation is TextPresentation or EmojiPresentation, "Presentation must be a selector.");
        return this.HasColorGlyphs == (presentation == EmojiPresentation);
    }

    /// <summary>Gets the face that gives the metrics, such as the primary element of a composite.</summary>
    /// <param name="sizeRatio">Multiplied by the ratio of the returned face's em to this font's em.</param>
    /// <returns>The face, or this font.</returns>
    public virtual Font GetPrimary(ref float sizeRatio) => this;

    /// <summary>Gets the line height of this face as a multiple of its em, at an em.</summary>
    /// <param name="em">The em in device pixels.</param>
    /// <returns>The line height divided by the em, 1 if not known.</returns>
    public virtual float LineHeightRatioAtEm(float em) => 1;

    /// <summary>Gets the line height of this face as a multiple of its em, at a line height.</summary>
    /// <param name="lineHeight">The line height in device pixels.</param>
    /// <returns>The line height divided by the em, 1 if not known.</returns>
    public virtual float LineHeightRatioAtLineHeight(float lineHeight) => 1;

    /// <summary>Gets the size, the em and the line height of this font from one of them.</summary>
    /// <param name="value">The value in logical pixels.</param>
    /// <param name="measured">What the value is.</param>
    /// <param name="density">Device pixels per logical pixel.</param>
    /// <returns>The size, the em and the line height in logical pixels.</returns>
    /// <remarks>The primary face gives the ratios.</remarks>
    /// <seealso cref="GetPrimary"/>
    public (float Size, float Em, float Line) Measure(float value, Measured measured, float density)
    {
        var sizeRatio = 1f;
        var face = this.GetPrimary(ref sizeRatio);
        var faceIsEm = measured switch
        {
            Measured.Em => true,
            Measured.Line => false,
            _ => face.SizeIsEm,
        };
        var faceValue = measured == Measured.Em ? value * sizeRatio : value;
        var ratio = faceIsEm
                        ? face.LineHeightRatioAtEm(faceValue * density)
                        : face.LineHeightRatioAtLineHeight(faceValue * density);
        var (em, line) = faceIsEm ? (faceValue, faceValue * ratio) : (faceValue / ratio, faceValue);
        return (face.SizeIsEm ? em : line, em / sizeRatio, line);
    }

    /// <summary>Gets the font in a style.</summary>
    /// <param name="key">The <see cref="TextStyle.FaceKey"/> of the style.</param>
    /// <returns>The font with the faces for it where it has them, emboldened or slanted where not.</returns>
    public Font WithStyle(in TextStyle key)
    {
        if (key == default)
            return this;
        lock (this.styled)
        {
            if (!this.styled.TryGetValue(key, out var font))
                this.styled.Add(key, font = this.CreateStyled(key));
            return font;
        }
    }

    /// <summary>Gets the metrics at a size, as designed.</summary>
    /// <param name="size">Size in logical pixels.</param>
    /// <param name="density">Device pixels per logical pixel.</param>
    /// <returns>The metrics.</returns>
    /// <remarks>The line gap is not split into the ascent and the descent.</remarks>
    public abstract FontMetrics GetMetrics(float size, float density);

    /// <summary>Maps codepoints to glyphs.</summary>
    /// <remarks>0 means missing. Glyph ids must stay below 1 &lt;&lt; 24, so that the font can be part of a
    /// <see cref="CompositeFont"/>.</remarks>
    /// <param name="codepoints">The codepoints.</param>
    /// <param name="glyphs">Receives one glyph per codepoint.</param>
    public abstract void GetGlyphIndices(ReadOnlySpan<uint> codepoints, Span<uint> glyphs);

    /// <summary>Maps a codepoint to a glyph.</summary>
    /// <param name="codepoint">The codepoint.</param>
    /// <returns>The glyph, or 0 if missing.</returns>
    public uint GetGlyphIndex(uint codepoint)
    {
        var glyph = 0u;
        this.GetGlyphIndices(new(in codepoint), new(ref glyph));
        return glyph;
    }

    /// <summary>Tests whether the font has a glyph for a codepoint.</summary>
    /// <param name="codepoint">The codepoint.</param>
    /// <returns>Whether the glyph exists.</returns>
    public bool HasGlyph(uint codepoint) => this.GetGlyphIndex(codepoint) != 0;

    /// <summary>Gets glyph metrics.</summary>
    /// <param name="glyphs">The glyphs.</param>
    /// <param name="size">Size in logical pixels.</param>
    /// <param name="density">Device pixels per logical pixel.</param>
    /// <param name="metrics">Receives one entry per glyph.</param>
    public abstract void GetGlyphMetrics(
        ReadOnlySpan<uint> glyphs, float size, float density, Span<GlyphMetrics> metrics);

    /// <summary>Shapes a run and appends its glyphs, left to right.</summary>
    /// <param name="run">The run.</param>
    /// <param name="output">Receives the glyphs.</param>
    /// <remarks>Clusters are indices into <see cref="ShapeRun.Text"/>, ascending and on grapheme boundaries.</remarks>
    public abstract void Shape(ShapeRun run, ShapedGlyphList output);

    /// <summary>Gets the kerning of a pair of characters with this font alone.</summary>
    /// <param name="first">The first character.</param>
    /// <param name="second">The second character.</param>
    /// <param name="size">Size in logical pixels.</param>
    /// <param name="density">Device pixels per logical pixel.</param>
    /// <param name="kerning">The adjustment in logical pixels.</param>
    /// <returns><see langword="true"/> if the font contains both characters and shapes each to a single glyph.</returns>
    /// <remarks>The pair is shaped. Pairs are kept, as text switches fonts at the same pairs, such as at spaces between
    /// scripts.</remarks>
    public bool TryGetPairKerning(uint first, uint second, float size, float density, out float kerning)
    {
        var pairs = this.pairKernings ??= [];
        ref var kept = ref CollectionsMarshal.GetValueRefOrNullRef(pairs, (first, second, size, density));
        if (!Unsafe.IsNullRef(ref kept))
        {
            kerning = kept.GetValueOrDefault();
            return kept.HasValue;
        }

        var has = this.TryGetPairKerningUncached(first, second, size, density, out kerning);
        if (pairs.Count >= MaxPairKernings)
            pairs.Clear();
        pairs[(first, second, size, density)] = has ? kerning : null;
        return has;
    }

    /// <summary>Produces glyph images.</summary>
    /// <param name="glyphs">The glyphs.</param>
    /// <param name="subpixelSteps">Per glyph, how far the pen is right of a whole device pixel, in
    /// 1/<see cref="SubpixelStepCount"/> pixel steps. The font may ignore it.</param>
    /// <param name="size">Size in logical pixels.</param>
    /// <param name="density">Device pixels per logical pixel.</param>
    /// <param name="embolden">Synthetic bold, in device pixels to widen the strokes by horizontally.</param>
    /// <param name="subpixel">Whether subpixel coverage is wanted. The font may ignore it.</param>
    /// <param name="images">Receives one entry per glyph.</param>
    public abstract void GetGlyphImages(
        ReadOnlySpan<uint> glyphs, ReadOnlySpan<byte> subpixelSteps, float size, float density, float embolden,
        bool subpixel, Span<GlyphImage> images);

    /// <summary>Creates a styled variant of this font for <see cref="WithStyle"/>.</summary>
    /// <param name="key">The face key.</param>
    /// <returns>The font. The default emboldens and slants this one, and keeps its locale and features.</returns>
    protected virtual Font CreateStyled(in TextStyle key) => new SyntheticStyleFont(this, key.On(default));

    /// <summary>Finds the grapheme starts of a run.</summary>
    /// <param name="run">The run.</param>
    /// <returns>The starts, in a buffer reused by the next call.</returns>
    protected List<int> FindGraphemes(ShapeRun run)
    {
        var list = this.graphemesBuffer ??= [];
        run.FindGraphemes(list);
        return list;
    }

    private bool TryGetPairKerningUncached(uint first, uint second, float size, float density, out float kerning)
    {
        kerning = 0;
        Span<uint> pair = [first, second];
        Span<uint> glyphs = stackalloc uint[2];
        this.GetGlyphIndices(pair, glyphs);
        if (glyphs[0] == 0 || glyphs[1] == 0)
            return false;

        var output = this.pairOutput ??= new();
        output.Clear();
        this.Shape(new(pair, 0, 2, size, density), output);

        // Ligatures and multi-codepoint clusters have no inter-glyph kerning. Returning 0 here would halve the other
        // font's kerning in KernAcross.
        if (output.Count != 2 || output.Span[1].Cluster != 1)
            return false;
        var together = output.Span[0].Advance;
        output.Clear();
        this.Shape(new(pair, 0, 1, size, density), output);
        if (output.Count != 1)
            return false;
        kerning = together - output.Span[0].Advance;
        return true;
    }
}
