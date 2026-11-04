using System.Runtime.InteropServices;

using Dalamud.Interface.Fonts.Internal.Glyphs;
using Dalamud.Utility;

namespace Dalamud.Interface.Fonts.Internal.Sources;

/// <summary>A font that takes each grapheme from one of its source fonts.</summary>
/// <remarks>A grapheme goes to the source of its first codepoint, unless that source lacks another codepoint of it
/// that shows. Then the first source that has all of them takes it. Graphemes of one source are shaped together, and
/// pairs across sources are kerned with <see cref="Font.KernAcross"/>. Glyph ids are (source index &lt;&lt; shift) |
/// the source's glyph id, and glyph 0 is the .notdef glyph of <see cref="NotdefSource"/>.</remarks>
internal abstract class MultiSourceFont : Font
{
    // Longer graphemes keep the source of their first codepoint.
    private const int MaxPickedLength = 32;

    private readonly int sourceShift;
    private readonly uint maxLeafGlyph;

    private uint[] firstBuffer = new uint[64];
    private int[] sourceBuffer = new int[64];
    private uint[] pickBuffer = new uint[64];
    private uint[] leafBuffer = new uint[64];

    /// <summary>Initializes a new instance of the <see cref="MultiSourceFont"/> class.</summary>
    /// <param name="name">Name for debugging.</param>
    /// <param name="sourceShift">The bit of glyph ids that the source index starts at.</param>
    protected MultiSourceFont(string name, int sourceShift)
        : base(name)
    {
        this.sourceShift = sourceShift;
        this.maxLeafGlyph = 1u << sourceShift;
    }

    /// <summary>Gets the source of glyph 0.</summary>
    /// <value>The source, or -1 for none.</value>
    protected abstract int NotdefSource { get; }

    /// <summary>Tests whether a codepoint is a default ignorable one, which shows nothing even without a glyph.
    /// </summary>
    /// <param name="c">The codepoint.</param>
    /// <returns>Whether it has the Default_Ignorable_Code_Point property.</returns>
    public static bool IsDefaultIgnorable(uint c) =>
        c switch
        {
            < 0xAD => false,
            0xAD or 0x34F or 0x61C or 0x115F or 0x1160 or 0x17B4 or 0x17B5 or 0x3164 or 0xFEFF or 0xFFA0 => true,
            >= 0x180B and <= 0x180F => true,
            >= 0x200B and <= 0x200F => true,
            >= 0x202A and <= 0x202E => true,
            >= 0x2060 and <= 0x206F => true,
            >= 0xFE00 and <= 0xFE0F => true,
            >= 0xFFF0 and <= 0xFFF8 => true,
            >= 0x1BCA0 and <= 0x1BCA3 => true,
            >= 0x1D173 and <= 0x1D17A => true,
            >= 0xE0000 and <= 0xE0FFF => true,
            _ => false,
        };

    /// <inheritdoc/>
    public override void GetGlyphMetrics(
        ReadOnlySpan<uint> glyphs, float size, float density, Span<GlyphMetrics> metrics)
    {
        var pooledLeaf = PooledSpan<uint>.FromBuffer(ref this.leafBuffer, glyphs.Length);
        var leaf = pooledLeaf.Span;
        for (var begin = 0; begin < glyphs.Length;)
        {
            var end = this.RunEnd(glyphs, begin, out var source);
            if (source < 0 || source >= this.SourceCount())
            {
                metrics[begin..end].Clear();
            }
            else
            {
                this.GetSourceGlyphMetrics(
                    source, this.ToLeaf(glyphs, begin, end, leaf), size, density, metrics[begin..end]);
            }

            begin = end;
        }

        pooledLeaf.Recycle();
    }

    /// <inheritdoc/>
    /// <remarks>Each source shapes its graphemes.</remarks>
    public override void Shape(ShapeRun run, ShapedGlyphList output)
    {
        if (run.End <= run.Begin)
            return;
        var graphemeList = this.FindGraphemes(run);
        var graphemes = CollectionsMarshal.AsSpan(graphemeList);
        var count = graphemes.Length;
        var pooledFirsts = PooledSpan<uint>.FromBuffer(ref this.firstBuffer, count);
        var pooledSources = PooledSpan<int>.FromBuffer(ref this.sourceBuffer, count);
        var firsts = pooledFirsts.Span;
        var sources = pooledSources.Span;
        for (var k = 0; k < count; k++)
            firsts[k] = run.Text[graphemes[k]];
        this.PickSources(firsts, sources);
        for (var k = 0; k < count; k++)
        {
            var end = k + 1 < count ? graphemes[k + 1] : run.End;
            if (end - graphemes[k] > 1)
                sources[k] = this.PickSource(run.Text[graphemes[k]..end], sources[k]);
        }

        var previous = -1;
        var previousFirst = -1;
        var previousLast = 0u;
        for (var k = 0; k < count;)
        {
            var source = sources[k];
            var kb = k++;
            while (k < count && sources[k] == source)
                k++;

            var begin = graphemes[kb];
            var end = k < count ? graphemes[k] : run.End;
            var first = output.Count;
            this.ShapeWith(source, run.Slice(begin, end), graphemes[kb..k], output);
            if (source >= 0)
            {
                foreach (ref var g in output.Span[first..])
                    g.Glyph = this.Tag(source, g.Glyph);
            }

            if (previous >= 0 && source >= 0 && first > previousFirst)
                this.KernAt(previous, source, previousLast, run.Text[begin], run, output, first);

            previous = source;
            previousFirst = first;
            previousLast = run.Text[graphemes[k - 1]];
        }

        pooledFirsts.Recycle();
        pooledSources.Recycle();
    }

    /// <inheritdoc/>
    public override void GetGlyphImages(
        ReadOnlySpan<uint> glyphs, ReadOnlySpan<byte> subpixelSteps, float size, float density, float embolden,
        bool subpixel, Span<GlyphImage> images)
    {
        var pooledLeaf = PooledSpan<uint>.FromBuffer(ref this.leafBuffer, glyphs.Length);
        var leaf = pooledLeaf.Span;
        for (var begin = 0; begin < glyphs.Length;)
        {
            var end = this.RunEnd(glyphs, begin, out var source);
            if (source < 0 || source >= this.SourceCount())
            {
                images[begin..end].Fill(new() { IsFinal = true });
            }
            else
            {
                this.GetSourceGlyphImages(
                    source,
                    this.ToLeaf(glyphs, begin, end, leaf),
                    subpixelSteps[begin..end],
                    size,
                    density,
                    embolden,
                    subpixel,
                    images[begin..end]);
            }

            begin = end;
        }

        pooledLeaf.Recycle();
    }

    /// <summary>Gets the font of a source.</summary>
    /// <param name="source">The source.</param>
    /// <returns>The font.</returns>
    protected abstract Font SourceFont(int source);

    /// <summary>Gets the number of sources.</summary>
    /// <returns>The count.</returns>
    protected abstract int SourceCount();

    /// <summary>Tests whether a source may take a codepoint, before its font is asked.</summary>
    /// <param name="source">The source.</param>
    /// <param name="codepoint">The codepoint.</param>
    /// <returns>Whether it may. The default takes all.</returns>
    protected virtual bool Takes(int source, uint codepoint) => true;

    /// <summary>Gets the metrics of glyphs of one source.</summary>
    /// <param name="source">The source.</param>
    /// <param name="leaf">The glyph ids of the source's font.</param>
    /// <param name="size">The size of this font.</param>
    /// <param name="density">Device pixels per logical pixel.</param>
    /// <param name="metrics">Receives the metrics.</param>
    protected virtual void GetSourceGlyphMetrics(
        int source, ReadOnlySpan<uint> leaf, float size, float density, Span<GlyphMetrics> metrics) =>
        this.SourceFont(source).GetGlyphMetrics(leaf, size, density, metrics);

    /// <summary>Gets the images of glyphs of one source.</summary>
    /// <param name="source">The source.</param>
    /// <param name="leaf">The glyph ids of the source's font.</param>
    /// <param name="subpixelSteps">The subpixel steps.</param>
    /// <param name="size">The size of this font.</param>
    /// <param name="density">Device pixels per logical pixel.</param>
    /// <param name="embolden">Synthetic bold in device pixels.</param>
    /// <param name="subpixel">Whether subpixel coverage is wanted.</param>
    /// <param name="images">Receives the images.</param>
    protected virtual void GetSourceGlyphImages(
        int source, ReadOnlySpan<uint> leaf, ReadOnlySpan<byte> subpixelSteps, float size, float density,
        float embolden, bool subpixel, Span<GlyphImage> images) =>
        this.SourceFont(source).GetGlyphImages(leaf, subpixelSteps, size, density, embolden, subpixel, images);

    /// <summary>Adds the source to a glyph id of its font.</summary>
    /// <param name="source">The source.</param>
    /// <param name="leaf">The glyph id of the source's font.</param>
    /// <returns>The glyph id, or 0 if the source's glyph is 0 or does not fit.</returns>
    protected uint Tag(int source, uint leaf) =>
        leaf is not 0 && leaf < this.maxLeafGlyph ? ((uint)source << this.sourceShift) | leaf : 0;

    /// <summary>Gets the source of a glyph that a codepoint maps to.</summary>
    /// <param name="glyph">The glyph.</param>
    /// <returns>The source, or -1 for glyph 0.</returns>
    protected int PickedSource(uint glyph) => glyph == 0 ? -1 : (int)(glyph >> this.sourceShift);

    /// <summary>Finds a source for a grapheme whose first codepoint's source lacks another of its codepoints, before
    /// the other sources are tried in order.</summary>
    /// <param name="codepoints">The codepoints, other than default ignorable ones.</param>
    /// <returns>The source, which may be new, or -1. The default has none.</returns>
    protected virtual int AddSourceFor(ReadOnlySpan<uint> codepoints) => -1;

    /// <summary>Shapes graphemes of one source, and appends their glyphs.</summary>
    /// <param name="source">The source, or -1 for graphemes no source has.</param>
    /// <param name="run">The graphemes.</param>
    /// <param name="graphemes">The grapheme starts of <paramref name="run"/>.</param>
    /// <param name="output">Receives the glyphs, with the glyph ids of the source's font. The source is added to them
    /// after.</param>
    protected abstract void ShapeWith(int source, ShapeRun run, ReadOnlySpan<int> graphemes, ShapedGlyphList output);

    /// <summary>Gets the font and size to kern a source with across sources.</summary>
    /// <param name="source">The source.</param>
    /// <param name="size">The size of the run.</param>
    /// <param name="density">Device pixels per logical pixel.</param>
    /// <param name="sourceSize">Receives the size of the source's glyphs.</param>
    /// <returns>The font, or null to not kern pairs with it. The default is the source's font at the run's size.
    /// </returns>
    protected virtual Font? KerningFontOf(int source, float size, float density, out float sourceSize)
    {
        sourceSize = size;
        return this.SourceFont(source);
    }

    // The end of the glyphs from one source, and that source. Glyph 0 is a run of its own.
    private int RunEnd(ReadOnlySpan<uint> glyphs, int begin, out int source)
    {
        var end = begin + 1;
        if (glyphs[begin] == 0)
        {
            source = this.NotdefSource;
            while (end < glyphs.Length && glyphs[end] == 0)
                end++;
            return end;
        }

        var high = glyphs[begin] >> this.sourceShift;
        source = (int)high;
        while (end < glyphs.Length && glyphs[end] != 0 && glyphs[end] >> this.sourceShift == high)
            end++;
        return end;
    }

    // The glyph ids of a run's source font. Those of source 0, and glyph 0, are their own.
    private ReadOnlySpan<uint> ToLeaf(ReadOnlySpan<uint> glyphs, int begin, int end, Span<uint> leaf)
    {
        if (glyphs[begin] < this.maxLeafGlyph)
            return glyphs[begin..end];
        for (var i = begin; i < end; i++)
            leaf[i] = glyphs[i] & (this.maxLeafGlyph - 1);
        return leaf[begin..end];
    }

    // Picks the source of each grapheme from its first codepoint, or -1 if no source has it.
    private void PickSources(ReadOnlySpan<uint> firsts, Span<int> sources)
    {
        var pooledGlyphs = PooledSpan<uint>.FromBuffer(ref this.pickBuffer, firsts.Length);
        var glyphs = pooledGlyphs.Span;
        this.GetGlyphIndices(firsts, glyphs);
        for (var k = 0; k < firsts.Length; k++)
            sources[k] = this.PickedSource(glyphs[k]);
        pooledGlyphs.Recycle();
    }

    // Whether a source has every codepoint of a grapheme. Glyphs is scratch as long as the codepoints.
    private bool HasAll(int source, ReadOnlySpan<uint> codepoints, Span<uint> glyphs)
    {
        foreach (var c in codepoints)
        {
            if (!this.Takes(source, c))
                return false;
        }

        this.SourceFont(source).GetGlyphIndices(codepoints, glyphs);
        foreach (var g in glyphs[..codepoints.Length])
        {
            if (g is 0 || g >= this.maxLeafGlyph)
                return false;
        }

        return true;
    }

    // Kerns the last glyph before a group with the first of it, if both sources kern.
    private void KernAt(
        int left, int right, uint first, uint second, ShapeRun run, ShapedGlyphList output, int at)
    {
        if (this.KerningFontOf(left, run.Size, run.Density, out var leftSize) is not { } leftFont ||
            this.KerningFontOf(right, run.Size, run.Density, out var rightSize) is not { } rightFont)
        {
            return;
        }

        output.Span[at - 1].Advance += KernAcross(leftFont, leftSize, rightFont, rightSize, first, second, run.Density);
    }

    // The source for a grapheme of several codepoints. Rarely differs from the one of its first codepoint.
    private int PickSource(ReadOnlySpan<uint> grapheme, int source)
    {
        if (grapheme.Length > MaxPickedLength)
            return source;
        Span<uint> needed = stackalloc uint[MaxPickedLength];
        Span<uint> glyphs = stackalloc uint[MaxPickedLength];
        var n = 0;
        foreach (var c in grapheme)
        {
            if (n == 0 || !IsDefaultIgnorable(c))
                needed[n++] = c;
        }

        if (n == 1 || (source >= 0 && this.HasAll(source, needed[..n], glyphs)))
            return source;
        var added = this.AddSourceFor(needed[..n]);
        if (added >= 0 && added != source && this.HasAll(added, needed[..n], glyphs))
            return added;
        for (var s = 0; s < this.SourceCount(); s++)
        {
            if (s != source && this.HasAll(s, needed[..n], glyphs))
                return s;
        }

        return source;
    }
}
