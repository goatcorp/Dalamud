using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

using Dalamud.Interface.Fonts.Internal.Glyphs;

namespace Dalamud.Interface.Fonts.Internal.Sources;

/// <summary>A font for <see cref="Layout.TextLayouter"/>.</summary>
/// <remarks>All calls come from the main thread.</remarks>
/// <seealso cref="FontImpl"/>
internal abstract class Font
{
    /// <summary>The base-2 logarithm of <see cref="SubpixelStepCount"/>.</summary>
    public const int SubpixelStepShift = 2;

    /// <summary>The steps a device pixel is divided into for placing glyphs right of whole pixels.</summary>
    /// <seealso cref="GetGlyphImages"/>
    public const int SubpixelStepCount = 1 << SubpixelStepShift;

    /// <summary>The steps a device pixel is divided into for the sizes glyph images are made at.</summary>
    /// <remarks>Fonts make images at sizes rounded to a multiple of 1 / this. <see cref="CompositeFont"/> relies on it
    /// to fit glyphs.</remarks>
    public const int SizeStepsPerPixel = 4;

    // The number of pairs whose kerning is kept, from which all are forgotten.
    private const int MaxPairKernings = 4096;

    private static long lastId;

    private readonly Dictionary<TextStyle, Font> styled = [];
    private ShapedGlyphList? pairOutput;
    private List<int>? graphemesBuffer;

    // Kerning of pairs, or null if the font does not have both characters.
    private Dictionary<(uint First, uint Second, float Size, float Density), float?>? pairKernings;

    /// <summary>Initializes a new instance of the <see cref="Font"/> class.</summary>
    /// <param name="name">Name for debugging.</param>
    protected Font(string name) => this.Name = name;

    /// <summary>Gets the name for debugging.</summary>
    public string Name { get; }

    /// <summary>Gets a number no other font has.</summary>
    public long Id { get; } = Interlocked.Increment(ref lastId);

    /// <summary>Gets a value indicating whether the size of this face is its em, rather than its natural line box.
    /// </summary>
    public virtual bool SizeIsEm => false;

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
        return AverageKerning(l, r);
    }

    /// <summary>Gets the kerning across fonts from what each font says.</summary>
    /// <param name="left">The kerning by the font of the first character, or null if it has no say.</param>
    /// <param name="right">The kerning by the font of the second character, or null if it has no say.</param>
    /// <returns>The average of the ones that have a say, or 0.</returns>
    public static float AverageKerning(float? left, float? right) =>
        (left, right) switch
        {
            ({ } l, { } r) => (l + r) / 2,
            ({ } l, null) => l,
            (null, { } r) => r,
            _ => 0,
        };

    /// <inheritdoc/>
    public override string ToString() => $"{this.GetType().Name}({this.Name})";

    /// <summary>Gets the font that does the work.</summary>
    /// <returns>This font, or the font it currently stands for.</returns>
    /// <seealso cref="SlotFont"/>
    public virtual Font Resolve() => this;

    /// <summary>Gets the face that gives the metrics, such as the primary element of a composite.</summary>
    /// <param name="sizeRatio">Multiplied by the em of the face relative to the em of this font.</param>
    /// <returns>The face, or this font.</returns>
    public virtual Font GetPrimary(ref float sizeRatio) => this;

    /// <summary>Gets the natural line box of this face as a multiple of its em.</summary>
    /// <param name="size">The em or the natural line box in device pixels.</param>
    /// <param name="isEm">Whether the size is the em rather than the natural line box.</param>
    /// <returns>The ratio, 1 if not known.</returns>
    public virtual float NaturalRatio(float size, bool isEm) => 1;

    /// <summary>Gets the size, the em and the natural line box of this font from one of them.</summary>
    /// <param name="value">The value in logical pixels.</param>
    /// <param name="isEm">Whether the value is the em or the natural line box. Null means the size.</param>
    /// <param name="density">Device pixels per logical pixel.</param>
    /// <returns>The size, the em and the natural line box in logical pixels.</returns>
    /// <remarks>The primary face gives the ratios.</remarks>
    /// <seealso cref="GetPrimary"/>
    public (float Size, float Em, float Line) Measure(float value, bool? isEm, float density)
    {
        var sizeRatio = 1f;
        var face = this.GetPrimary(ref sizeRatio);
        var faceIsEm = isEm ?? face.SizeIsEm;
        var faceValue = isEm == true ? value * sizeRatio : value;
        var ratio = face.NaturalRatio(faceValue * density, faceIsEm);
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
    /// <returns>Whether the font has both characters and shapes them to one glyph each, and so has a say.</returns>
    /// <remarks>The pair is shaped. Pairs are kept, as text switches fonts at the same pairs, such as at spaces between
    /// scripts.</remarks>
    public bool TryGetPairKerning(uint first, uint second, float size, float density, out float kerning)
    {
        // The font stood for may change, so the font that does the work keeps what it says.
        var resolved = this.Resolve();
        if (!ReferenceEquals(resolved, this))
            return resolved.TryGetPairKerning(first, second, size, density, out kerning);

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

    /// <summary>Makes the font in a style, for <see cref="WithStyle"/>.</summary>
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

    // TryGetPairKerning, without the pairs kept.
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
        // A ligature or a cluster has no kerning between its parts. A zero here would halve the other font's say.
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
