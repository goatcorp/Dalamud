using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Interface.Fonts.Internal.Glyphs;
using Dalamud.Utility;

namespace Dalamud.Interface.Fonts.Internal.Sources;

/// <summary>A font made of other fonts, such as the font of a <see cref="ChainFamilyId"/>.</summary>
/// <remarks><ul>
/// <li>Each codepoint comes from the first element whose ranges contain it and whose font has a glyph for it.</li>
/// <li>Glyph ids are (element index &lt;&lt; 24) | the element font's glyph id. Glyph 0 and the font metrics come
/// from the first element that is not a <see cref="Element.Supplement"/>.</li>
/// <li>Elements that are composite fonts themselves are flattened.</li>
/// <li>It does not change once made.</li>
/// </ul></remarks>
internal sealed partial class CompositeFont : MultiSourceFont
{
    private const int MaxElements = 256;

    // Glyph ids hold the element index above this bit, and the glyph id of its font below.
    private const int ElementShift = 24;

    private readonly Element[] elements;

    // The index of the first element that is not a supplement, or 0.
    private readonly int primary;

    private uint[] codepointBuffer = new uint[64];
    private uint[] glyphBuffer = new uint[64];
    private int[] whereBuffer = new int[64];
    private GlyphMetrics[] metricsBuffer = new GlyphMetrics[64];
    private Vector2[] shiftBuffer = new Vector2[64];
    private float[] sizeBuffer = new float[64];
    private uint[] shapeLeafBuffer = new uint[64];
    private GlyphMetrics[] shapeMetricsBuffer = new GlyphMetrics[64];
    private GlyphMetrics[] shapeAdjustedBuffer = new GlyphMetrics[64];
    private Vector2[] shapeShiftBuffer = new Vector2[64];

    /// <summary>Initializes a new instance of the <see cref="CompositeFont"/> class.</summary>
    /// <param name="name">Name for debugging.</param>
    /// <param name="elements">The elements, in priority order. Those marked <see cref="Element.Terminal"/> go last.
    /// </param>
    public CompositeFont(string name, ReadOnlySpan<Element> elements)
        : base(name, ElementShift)
    {
        this.elements = this.Flatten(elements);
        this.primary = Math.Max(Array.FindIndex(this.elements, static e => !e.Supplement), 0);
    }

    // A copy with other element fonts, for CreateStyled.
    private CompositeFont(CompositeFont source, string name, Element[] elements)
        : base(name, ElementShift)
    {
        this.elements = elements;
        this.primary = source.primary;
    }

    /// <summary>Gets the elements.</summary>
    public IReadOnlyList<Element> Elements => this.elements;

    /// <inheritdoc/>
    protected override int NotdefSource => this.primary;

    /// <summary>Converts <see cref="ChainFamilyId.Element.CodepointRanges"/> to sorted ranges.</summary>
    /// <param name="ranges">Pairs of inclusive bounds, which may end with a zero. A value left without a pair is not
    /// used.</param>
    /// <returns>The ranges. Empty for none.</returns>
    public static (uint Low, uint High)[] ToRanges(ReadOnlySpan<uint> ranges)
    {
        var list = new List<(uint, uint)>();
        for (var i = 0; i + 1 < ranges.Length; i += 2)
            list.Add((ranges[i], ranges[i + 1]));
        list.Sort();
        return [.. list];
    }

    /// <inheritdoc/>
    public override Font GetPrimary(ref float sizeRatio)
    {
        if (this.elements.Length == 0)
            return this;
        var e = this.elements[this.primary];
        sizeRatio *= e.SizeRatio;
        return e.Font.GetPrimary(ref sizeRatio);
    }

    /// <inheritdoc/>
    public override FontMetrics GetMetrics(float size, float density) => this.GlyphFontMetrics(size, density);

    /// <inheritdoc/>
    public override void GetGlyphIndices(ReadOnlySpan<uint> codepoints, Span<uint> glyphs)
    {
        glyphs[..codepoints.Length].Clear();
        var pooledMissing = PooledSpan<uint>.FromBuffer(ref this.codepointBuffer, codepoints.Length);
        var pooledWhere = PooledSpan<int>.FromBuffer(ref this.whereBuffer, codepoints.Length);
        var pooledLeaf = PooledSpan<uint>.FromBuffer(ref this.glyphBuffer, codepoints.Length);
        var missing = pooledMissing.Span;
        var where = pooledWhere.Span;
        var leaf = pooledLeaf.Span;
        for (var ei = 0; ei < this.elements.Length; ei++)
        {
            var e = this.elements[ei];
            var n = 0;
            for (var i = 0; i < codepoints.Length; i++)
            {
                if (glyphs[i] == 0 && e.Contains(codepoints[i]))
                {
                    missing[n] = codepoints[i];
                    where[n++] = i;
                }
            }

            if (n == 0)
                continue;
            e.Font.GetGlyphIndices(missing[..n], leaf[..n]);
            for (var i = 0; i < n; i++)
                glyphs[where[i]] = this.Tag(ei, leaf[i]);
        }

        pooledMissing.Recycle();
        pooledWhere.Recycle();
        pooledLeaf.Recycle();
    }

    /// <inheritdoc/>
    /// <remarks>Each element takes the variant of its own font.</remarks>
    protected override Font CreateStyled(in TextStyle key)
    {
        var styled = new Element[this.elements.Length];
        for (var i = 0; i < styled.Length; i++)
            styled[i] = this.elements[i] with { Font = this.elements[i].Font.WithStyle(key) };
        return new CompositeFont(this, $"{this.Name} {key.On(default)}", styled);
    }

    /// <inheritdoc/>
    protected override Font SourceFont(int source) => this.elements[source].Font;

    /// <inheritdoc/>
    protected override int SourceCount() => this.elements.Length;

    /// <inheritdoc/>
    protected override bool Takes(int source, uint codepoint) => this.elements[source].Contains(codepoint);

    /// <inheritdoc/>
    protected override void GetSourceGlyphMetrics(
        int source, ReadOnlySpan<uint> leaf, float size, float density, Span<GlyphMetrics> metrics)
    {
        var e = this.elements[source];
        var pooledShift = PooledSpan<Vector2>.FromBuffer(ref this.shiftBuffer, leaf.Length);
        e.Font.GetGlyphMetrics(leaf, this.SizeOf(e, size, density), density, metrics);
        this.Adjust(e, leaf, size, density, metrics, pooledShift.Span, default);
        pooledShift.Recycle();
    }

    /// <inheritdoc/>
    protected override void GetSourceGlyphImages(
        int source, ReadOnlySpan<uint> leaf, ReadOnlySpan<byte> subpixelSteps, float size, float density,
        float embolden, bool subpixel, Span<GlyphImage> images)
    {
        // Without metrics, the run's images are of one size and moved by the offset alone, if any.
        var e = this.elements[source];
        if (!e.PlacesImagesByMetrics)
        {
            e.Font.GetGlyphImages(
                leaf, subpixelSteps, this.SizeOf(e, size, density), density, embolden, subpixel, images);
            var offset = Snap(e.OffsetAt(this.EmOf(e, size, density)), density) * density;
            if (offset != Vector2.Zero)
            {
                foreach (ref var image in images)
                    image.Rect += new Vector4(offset.X, offset.Y, offset.X, offset.Y);
            }

            return;
        }

        var pooledMetrics = PooledSpan<GlyphMetrics>.FromBuffer(ref this.metricsBuffer, leaf.Length);
        var pooledShift = PooledSpan<Vector2>.FromBuffer(ref this.shiftBuffer, leaf.Length);
        var pooledSizes = PooledSpan<float>.FromBuffer(ref this.sizeBuffer, leaf.Length);
        var metrics = pooledMetrics.Span;
        var shift = pooledShift.Span;
        var sizes = pooledSizes.Span;
        sizes.Fill(this.SizeOf(e, size, density));
        e.Font.GetGlyphMetrics(leaf, sizes[0], density, metrics);
        this.Adjust(e, leaf, size, density, metrics, shift, sizes);

        // One call per image size, as fit-to-square glyphs may each have their own. Sizes are compared exactly, as
        // any difference is another image size.
        for (var b = 0; b < leaf.Length;)
        {
            var c = b + 1;
            while (c < leaf.Length && sizes[c].Equals(sizes[b]))
                c++;
            e.Font.GetGlyphImages(
                leaf[b..c], subpixelSteps[b..c], sizes[b], density, embolden, subpixel, images[b..c]);
            b = c;
        }

        for (var i = 0; i < leaf.Length; i++)
        {
            var d = shift[i] * density;
            images[i].Rect += new Vector4(d.X, d.Y, d.X, d.Y);
        }

        pooledMetrics.Recycle();
        pooledShift.Recycle();
        pooledSizes.Recycle();
    }

    /// <inheritdoc/>
    /// <remarks>Missing graphemes are glyph 0, with the advance the composite gives it.</remarks>
    protected override void ShapeWith(int source, ShapeRun run, ReadOnlySpan<int> graphemes, ShapedGlyphList output)
    {
        if (source >= 0)
        {
            var e = this.elements[source];
            var first = output.Count;
            e.Font.Shape(run.WithSize(this.SizeOf(e, run.Size, run.Density)), output);
            this.AdjustShaped(e, run.Size, run.Density, output.Span[first..]);
            return;
        }

        Span<uint> notdef = stackalloc uint[1];
        Span<GlyphMetrics> metrics = stackalloc GlyphMetrics[1];
        this.GetGlyphMetrics(notdef, run.Size, run.Density, metrics);
        foreach (var g in graphemes)
            output.Add(0, g, metrics[0].Advance);
    }

    /// <inheritdoc/>
    /// <remarks>Glyphs fit to squares are not kerned.</remarks>
    protected override Font? KerningFontOf(int source, float size, float density, out float sourceSize)
    {
        var e = this.elements[source];
        sourceSize = this.SizeOf(e, size, density);
        return e.FitToSquare ? null : e.Font;
    }

    private static StyleLength Scale(StyleLength v, float k) => v.Unit == StyleUnit.Em ? StyleLength.Em(v.Value * k) : v;

    private static Vector2 Snap(Vector2 logical, float density) =>
        new(MathF.Round(logical.X * density) / density, MathF.Round(logical.Y * density) / density);

    private static (uint Low, uint High)[]? Intersect((uint Low, uint High)[]? a, (uint Low, uint High)[]? b)
    {
        if (a is null)
            return b;
        if (b is null)
            return a;
        var result = new List<(uint, uint)>();
        foreach (var x in a)
        {
            foreach (var y in b)
            {
                var lo = Math.Max(x.Low, y.Low);
                var hi = Math.Min(x.High, y.High);
                if (lo <= hi)
                    result.Add((lo, hi));
            }
        }

        result.Sort();
        return [.. result];
    }

    // Flattens the elements into elements of this font, in its units, with the terminal ones last. A composite
    // gives its own and its terminal elements. The terminal elements come from the first element that has any, and
    // later ones are left out.
    private Element[] Flatten(ReadOnlySpan<Element> elems)
    {
        var normal = new List<Element>();
        List<Element>? terminal = null;
        var parts = new List<Element>();
        foreach (var element in elems)
        {
            parts.Clear();
            this.Flatten(element, parts);
            if (element.Terminal)
            {
                terminal ??= [.. parts];
                continue;
            }

            normal.AddRange(parts.Where(static e => !e.Terminal));
            if (terminal is null && parts.Any(static e => e.Terminal))
                terminal = [.. parts.Where(static e => e.Terminal)];
        }

        return normal.Count + (terminal?.Count ?? 0) > MaxElements
                   ? throw new InvalidOperationException($"A font may have at most {MaxElements} elements.")
                   : [.. normal, .. terminal ?? []];
    }

    // Splits an element into elements of this font. Parts of a terminal element are terminal.
    private void Flatten(Element element, List<Element> output)
    {
        if (element.Font is not CompositeFont inner)
        {
            output.Add(element);
            return;
        }

        foreach (var e in inner.elements)
        {
            var k = 1 / e.SizeRatio;
            output.Add(
                e with
                {
                    Ranges = Intersect(element.Ranges, e.Ranges),
                    SizeRatio = element.SizeRatio * e.SizeRatio,
                    Offset = (element.Offset * k) + e.Offset,
                    OffsetPx = element.OffsetPx + e.OffsetPx,
                    ExtraAdvanceX = (element.ExtraAdvanceX * k) + e.ExtraAdvanceX,
                    ExtraAdvanceXPx = element.ExtraAdvanceXPx + e.ExtraAdvanceXPx,
                    MinAdvanceX = element.MinAdvanceX.Unit != StyleUnit.Default ? Scale(element.MinAdvanceX, k)
                                  : e.MinAdvanceX,
                    MaxAdvanceX = element.MaxAdvanceX.Unit != StyleUnit.Default ? Scale(element.MaxAdvanceX, k)
                                  : e.MaxAdvanceX,
                    FitToSquare = element.FitToSquare || e.FitToSquare,
                    Supplement = element.Supplement || e.Supplement,
                    Terminal = element.Terminal || e.Terminal,
                });
        }
    }

    // The metrics of the font at the size its glyphs are drawn at.
    private FontMetrics GlyphFontMetrics(float size, float density)
    {
        if (this.elements.Length == 0)
            return FontMetrics.Fallback(size);
        return this.elements[this.primary].Font.GetMetrics(size, density);
    }

    // Gets the em of an element's font, which is the em of the composite times Element.SizeRatio.
    private float EmOf(Element e, float size, float density) =>
        this.Measure(size, null, density).Em * e.SizeRatio;

    // Gets the size an element's font is drawn at, at its em. The primary element is drawn at the size of the
    // composite.
    private float SizeOf(Element e, float size, float density) =>
        ReferenceEquals(e, this.elements[this.primary])
            ? size
            : e.Font.Measure(this.EmOf(e, size, density), true, density).Size;
}
