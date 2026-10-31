using System.Buffers.Binary;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

using Dalamud.Interface.Fonts.Internal.Sources;
using Dalamud.Utility;
using Dalamud.Utility.Unicode;

using Lumina.Text;

namespace Dalamud.Interface.Fonts.Internal.Layout;

/// <summary>Layout construction.</summary>
internal sealed partial class TextLayout
{
    private const uint SoftHyphen = 0xAD;
    private const uint ObjectReplacement = 0xFFFC;

    // Layouts given back are kept for making others, up to this many, of up to this many codepoints each and this many
    // in total, so that text that changes allocates little.
    private const int MaxPooled = 256;
    private const int MaxPooledCodepoints = 4096;
    private const int MaxPooledTotalCodepoints = 256 * 1024;

    private static readonly Stack<TextLayout> Pool = new();

    // The codepoint capacity of the layouts in the pool.
    private static int pooledCodepoints;

    private BuildScratch? scratch;

    private TextLayout()
    {
    }

    private BuildScratch Scratch => this.scratch ?? throw new InvalidOperationException("Not building.");

    /// <summary>Makes a layout, reusing one given back if there is one.</summary>
    /// <param name="font">The font of text outside runs, and of runs without one.</param>
    /// <param name="desc">Layout parameters.</param>
    /// <param name="text">The text, in bytes.</param>
    /// <param name="codeUnitSize">1 for UTF-8, 2 for UTF-16, 4 for UTF-32 (little endian).</param>
    /// <param name="runs">Attributed ranges.</param>
    /// <param name="objects">Inline objects, one per U+FFFC in the text.</param>
    /// <param name="sourceOffsets">The offset in another text of each byte of <paramref name="text"/> and of its
    /// end, ascending, if the layout reports offsets into that text. Runs are given in offsets into
    /// <paramref name="text"/>.</param>
    /// <returns>The layout.</returns>
    /// <remarks>Every offset of the layout is a byte offset into <paramref name="text"/>, or into the source text.
    /// </remarks>
    /// <seealso cref="Recycle"/>
    public static TextLayout Rent(
        FontImpl font, in TextLayoutDesc desc, ReadOnlySpan<byte> text, int codeUnitSize,
        ReadOnlySpan<TextRun> runs, ReadOnlySpan<InlineObject> objects, ReadOnlySpan<int> sourceOffsets = default)
    {
        if (Pool.TryPop(out var layout))
            pooledCodepoints -= layout.runes.Length;
        else
            layout = new();
        layout.Init(font, desc, text, codeUnitSize, runs, objects, sourceOffsets);
        return layout;
    }

    /// <summary>Gives the layout back, to make another from, as nothing uses it anymore.</summary>
    public void Recycle()
    {
        var size = this.runes.Length;
        if (size > MaxPooledCodepoints || Pool.Count >= MaxPooled || pooledCodepoints + size > MaxPooledTotalCodepoints)
            return;
        this.Reset();
        Pool.Push(this);
        pooledCodepoints += size;
    }

    private static bool IsHardBreak(uint c, bool mask) =>
        c is '\n' or '\r' or ObjectReplacement || (c is '\t' or SoftHyphen && !mask);

    // The start of the grapheme after the one at a codepoint, within a limit.
    private static int NextGraphemeIn(bool[] graphemeStart, int codepoint, int limit)
    {
        do
            codepoint++;
        while (codepoint < limit && !graphemeStart[codepoint]);
        return Math.Min(codepoint, limit);
    }

    // Whether shaping left a glyph out. Rare, so FixMissing does the rest.
    private static bool HasMissing(ReadOnlySpan<ShapedGlyph> shaped)
    {
        foreach (ref readonly var g in shaped)
        {
            if (g.Glyph == 0)
                return true;
        }

        return false;
    }

    // Arrays at least as long, keeping the first codepoints. The offsets have room for one more, for the end.
    private static void GrowKeeping(ref uint[] runes, ref int[] offsets, int count, int length)
    {
        if (runes.Length < length)
        {
            var grown = runes.Grow(length);
            runes.AsSpan(0, count).CopyTo(grown);
            runes = grown;
        }

        if (offsets.Length <= length)
        {
            var grown = offsets.Grow(length + 1);
            offsets.AsSpan(0, count).CopyTo(grown);
            offsets = grown;
        }
    }

    private void Init(
        FontImpl font, in TextLayoutDesc layoutDesc, ReadOnlySpan<byte> text, int codeUnitSize,
        ReadOnlySpan<TextRun> textRuns, ReadOnlySpan<InlineObject> inlineObjects, ReadOnlySpan<int> sourceOffsets)
    {
        if (codeUnitSize is not (1 or 2 or 4))
            throw new ArgumentOutOfRangeException(nameof(codeUnitSize), codeUnitSize, "Must be 1, 2 or 4.");
        this.fonts.Add(new() { Font = font.Font, Size = layoutDesc.EmSize });
        this.desc = layoutDesc;
        this.defaultFont = font;
        this.runs = this.runs.Grow(textRuns.Length);
        textRuns.CopyTo(this.runs);
        this.RunCount = textRuns.Length;
        this.objects = this.objects.Grow(inlineObjects.Length);
        inlineObjects.CopyTo(this.objects);
        this.objectCount = inlineObjects.Length;
        this.textBytes = text.Length;

        // Not given back if building throws. The next layout then makes new scratch.
        this.scratch = BuildScratch.Rent();
        var start = FontProfiler.Begin(FontProfiler.Section.LayoutAnalyze);
        this.Analyze(text, codeUnitSize);
        FontProfiler.End(FontProfiler.Section.LayoutAnalyze, start, text.Length);
        if (!sourceOffsets.IsEmpty)
            this.MapToSource(sourceOffsets);
        this.Build();
        BuildScratch.Return(this.scratch);
        this.scratch = null;
    }

    // Back to how a new layout is, keeping what was allocated.
    private void Reset()
    {
        this.fonts.Clear();
        this.lines.Clear();
        this.items.Clear();
        this.glyphs.Clear();
        this.decorationSpans.Clear();
        this.visibleRanges.Clear();
        this.cachedSlices.Clear();
        this.cachedAtlas = null;
        this.cachedShadows = false;
        this.pass = default;
        Array.Clear(this.renderStyles);
        Array.Clear(this.runs, 0, this.RunCount);
        Array.Clear(this.objects, 0, this.objectCount);
        this.RunCount = this.objectCount = this.runeCount = 0;
        this.desc = default;
        this.defaultFont = null!;
        this.textBytes = 0;
        this.segmentCount = 0;
        this.Size = default;
        this.Ink = default;
        this.TextLength = 0;
        this.EllipsisOffset = -1;
        this.RenderHook = null;
        this.Cached = false;
    }

    // Codepoints, graphemes (UAX #29), line break opportunities (UAX #14), and the run of each codepoint.
    private void Analyze(ReadOnlySpan<byte> text, int unit)
    {
        // One set of flags for every walk, so that boundaries fall on the codepoints decoded. Byte order marks are
        // text. U+FFFE is yielded rather than skipped, so that every byte is in a codepoint.
        var flags = UtfEnumeratorFlags.DisrespectByteOrderMask | UtfEnumeratorFlags.YieldByteOrderMask | unit switch
        {
            1 => UtfEnumeratorFlags.Utf8,
            2 => UtfEnumeratorFlags.Utf16 | UtfEnumeratorFlags.LittleEndian,
            _ => UtfEnumeratorFlags.Utf32 | UtfEnumeratorFlags.LittleEndian,
        };
        var s = this.Scratch;
        this.Decode(text, unit, flags);
        var n = this.runeCount;
        var offsets = this.runeOffsets;
        s.Prepare(n);

        // Boundaries come in ascending order. Those inside a codepoint are dropped.
        var graphemeStarts = this.graphemeStart = this.graphemeStart.Grow(n);
        Array.Clear(graphemeStarts, 0, n);
        if (n > 0)
            graphemeStarts[0] = true;
        var cursor = 0;
        foreach (var boundary in new UnicodeGraphemeClusterEnumerator(text, flags))
        {
            while (cursor < n && offsets[cursor] < boundary)
                cursor++;
            if (cursor < n && offsets[cursor] == boundary)
                graphemeStarts[cursor] = true;
        }

        cursor = 0;
        foreach (var item in new UnicodeLineBreakEnumerator(text, flags))
        {
            while (cursor < n && offsets[cursor] < item.ByteOffset)
                cursor++;
            if (cursor == n || offsets[cursor] == item.ByteOffset)
                s.BreakBefore[cursor] = true;
        }

        // Each grapheme takes the run of its first codepoint, which is the last run to have its first byte.
        var codepointRun = s.CodepointRun;
        codepointRun.AsSpan(0, n).Fill(-1);
        var offsetSpan = offsets.AsSpan(0, n + 1);
        for (var r = 0; r < this.RunCount; r++)
        {
            var b = offsetSpan.BinarySearch(this.runs[r].TextBegin);
            var e = offsetSpan.BinarySearch(this.runs[r].TextEnd);
            b = Math.Min(b >= 0 ? b : ~b, n);
            e = Math.Min(e >= 0 ? e : ~e, n);
            if (b < e)
                codepointRun.AsSpan(b, e - b).Fill(r);
        }

        for (var codepoint = 1; codepoint < n; codepoint++)
        {
            if (!graphemeStarts[codepoint])
                codepointRun[codepoint] = codepointRun[codepoint - 1];
        }

        // Tabs are shaped as spaces, soft hyphens as the hyphens they are where lines wrap after them, and masked
        // graphemes as the mask character.
        var mask = this.Masked;
        var maskChar = this.desc.MaskChar != 0 ? this.desc.MaskChar : '*';
        for (var codepoint = 0; codepoint < n; codepoint++)
        {
            var c = this.runes[codepoint];
            s.ShapeText[codepoint] = c switch
            {
                _ when mask && this.IsGraphemeStart(codepoint) && c is not ('\n' or '\r' or ObjectReplacement) =>
                    maskChar,
                '\t' => ' ',
                SoftHyphen => '-',
                _ => c,
            };
        }
    }

    // The codepoints and their byte offsets, and the end. Invalid sequences become U+FFFD.
    private void Decode(ReadOnlySpan<byte> text, int unit, UtfEnumeratorFlags flags)
    {
        // Exact for valid text in the BMP, so that the arrays rarely grow while decoding.
        var expected = unit switch
        {
            1 => Encoding.UTF8.GetCharCount(text),
            2 => (text.Length + 1) / 2,
            _ => (text.Length + 3) / 4,
        };
        var (runes, offsets, n) = (this.runes, this.runeOffsets, 0);
        GrowKeeping(ref runes, ref offsets, 0, expected);
        foreach (var c in UtfEnumerator.From(text, flags))
        {
            // At most 3 codepoints at once, for a pair and 2 low surrogates.
            if (n + 3 > runes.Length || n + 3 >= offsets.Length)
                GrowKeeping(ref runes, ref offsets, n, n + 3);
            var offset = c.ByteOffset;
            if (unit == 2 && c.ByteLength > 4)
            {
                // UtfEnumerator reads low surrogates after a pair as one invalid codepoint. The pair is kept, and each
                // surrogate after it is a U+FFFD of its own.
                var high = BinaryPrimitives.ReadUInt16LittleEndian(text[offset..]);
                var low = BinaryPrimitives.ReadUInt16LittleEndian(text[(offset + 2)..]);
                runes[n] = 0x10000 + ((high - 0xD800u) << 10) + (low - 0xDC00u);
                offsets[n++] = offset;
                for (var k = offset + 4; k < offset + c.ByteLength; k += 2)
                {
                    runes[n] = 0xFFFD;
                    offsets[n++] = k;
                }

                continue;
            }

            runes[n] = (uint)c.EffectiveInt;
            offsets[n++] = offset;
        }

        offsets[n] = text.Length;
        (this.runes, this.runeOffsets, this.runeCount) = (runes, offsets, n);
    }

    // As the game does, characters no font has are shaped as U+3013 GETA MARK, or a hyphen if the font lacks it too, or
    // left as nothing if it lacks both. Characters that show nothing of their own, such as marks and format characters,
    // are left as they are, and format characters, controls and line or paragraph separators take no room. Only
    // clusters shaped as glyph 0 are looked at, after the segment is shaped.
    private void FixMissing(int fontIndex, int codepointBegin, int codepointEnd, bool perGrapheme)
    {
        var shapeText = this.Scratch.ShapeText;
        var font = this.SlotOf(fontIndex).Font;
        Span<uint> one = stackalloc uint[1];
        Span<uint> glyph = stackalloc uint[1];
        var changed = false;
        var shaped = this.Scratch.ShapeOutput.Span;
        for (var k = 0; k < shaped.Length; k++)
        {
            if (shaped[k].Glyph != 0 || (k > 0 && shaped[k - 1].Cluster == shaped[k].Cluster))
                continue;
            var next = k + 1;
            while (next < shaped.Length && shaped[next].Cluster == shaped[k].Cluster)
                next++;
            var clusterEnd = next < shaped.Length ? shaped[next].Cluster : codepointEnd;
            for (var codepoint = shaped[k].Cluster; codepoint < clusterEnd; codepoint++)
            {
                if (!ShowsItself(shapeText[codepoint]))
                    continue;
                one[0] = shapeText[codepoint];
                font.GetGlyphIndices(one, glyph);
                var replacement = glyph[0] == 0 ? this.ReplacementOf(fontIndex) : 0;
                if (replacement == 0)
                    continue;
                shapeText[codepoint] = replacement;
                changed = true;
            }
        }

        if (changed)
            this.ShapeGlyphs(fontIndex, codepointBegin, codepointEnd, perGrapheme);

        foreach (ref var g in this.Scratch.ShapeOutput.Span)
        {
            if (g.Glyph == 0 && TakesNoRoom(shapeText[g.Cluster]))
                g.Advance = 0;
        }

        return;

        static bool ShowsItself(uint c) =>
            c <= 0x10FFFF && UnicodeData.GeneralCategory[(int)c] is not (
                UnicodeGeneralCategory.Mn or UnicodeGeneralCategory.Mc or UnicodeGeneralCategory.Me or
                UnicodeGeneralCategory.Zs or UnicodeGeneralCategory.Zl or UnicodeGeneralCategory.Zp or
                UnicodeGeneralCategory.Cc or UnicodeGeneralCategory.Cf or UnicodeGeneralCategory.Cs);

        static bool TakesNoRoom(uint c) =>
            MultiSourceFont.IsDefaultIgnorable(c) || (c <= 0x10FFFF && UnicodeData.GeneralCategory[(int)c] is
                                                          UnicodeGeneralCategory.Cc or UnicodeGeneralCategory.Cf or
                                                          UnicodeGeneralCategory.Zl or UnicodeGeneralCategory.Zp);
    }

    // The character missing ones are shaped as with a font. 0 if the font has none.
    private uint ReplacementOf(int fontIndex)
    {
        ref var slot = ref this.SlotOf(fontIndex);
        if (slot.Replacement is { } replacement)
            return replacement;
        Span<uint> marks = stackalloc uint[2];
        Span<uint> ids = stackalloc uint[2];
        marks[0] = 0x3013;
        marks[1] = '-';
        slot.Font.GetGlyphIndices(marks, ids);
        replacement = ids[0] != 0 ? 0x3013u : ids[1] != 0 ? '-' : 0u;
        slot.Replacement = replacement;
        return replacement;
    }

    // From here on, offsets are into the source text. Bytes of it that show nothing belong to what precedes them.
    private void MapToSource(ReadOnlySpan<int> sourceOffsets)
    {
        if (sourceOffsets.Length != this.textBytes + 1)
            throw new ArgumentException("Must have one offset per byte, and one for the end.", nameof(sourceOffsets));
        foreach (ref var offset in this.runeOffsets.AsSpan(0, this.runeCount + 1))
            offset = sourceOffsets[offset];
        foreach (ref var run in this.runs.AsSpan(0, this.RunCount))
        {
            run = run with
            {
                TextBegin = sourceOffsets[Math.Clamp(run.TextBegin, 0, this.textBytes)],
                TextEnd = sourceOffsets[Math.Clamp(run.TextEnd, 0, this.textBytes)],
            };
        }

        this.textBytes = sourceOffsets[^1];
    }

    private void Build()
    {
        var codepointCount = this.runeCount;
        var mask = this.Masked;

        // Controls and objects are items as they are, and text is shaped in segments.
        var src = this.Scratch.Src;
        var codepointRun = this.Scratch.CodepointRun;
        var objectIndex = 0;
        var start = FontProfiler.Begin(FontProfiler.Section.LayoutShape);
        for (var codepoint = 0; codepoint < codepointCount;)
        {
            var c = this.runes[codepoint];
            var gEnd = this.NextGrapheme(codepoint, codepointCount);
            if (c == '\n' || (c == '\r' && codepoint + 1 < codepointCount && this.runes[codepoint + 1] == '\n') ||
                (c == ObjectReplacement && objectIndex < this.objectCount))
            {
                // CR LF is one grapheme of two items.
                for (var k = codepoint; k < (c == ObjectReplacement ? codepoint + 1 : gEnd); k++)
                {
                    var item = new Item
                    {
                        CodepointBegin = k,
                        CodepointEnd = c == ObjectReplacement ? gEnd : k + 1,
                        Run = codepointRun[k],
                        Object = -1,
                        Segment = -1,
                        GlyphBegin = this.glyphs.Count,
                        GlyphEnd = this.glyphs.Count,
                    };
                    item.FontIndex = (byte)this.FontIndexOfRun(item.Run);
                    if (this.runes[k] == ObjectReplacement)
                    {
                        item.Kind = ItemKind.Object;
                        item.Object = objectIndex++;
                        var b = this.objects[item.Object].Bounds;
                        item.Advance = b.Z;
                    }
                    else
                    {
                        // CR before LF is part of the line break.
                        item.Kind = this.runes[k] == '\r' ? ItemKind.Control : ItemKind.Newline;
                    }

                    src.Add(item);
                }

                codepoint = gEnd;
                continue;
            }

            if (IsHardBreak(c, mask))
            {
                // A tab, a soft hyphen, a lone CR, or U+FFFC without an object, is shaped alone.
                this.ShapeSegment(codepoint, gEnd, src);
                codepoint = gEnd;
                continue;
            }

            var end = gEnd;
            while (end < codepointCount && !IsHardBreak(this.runes[end], mask) &&
                   codepointRun[end] == codepointRun[codepoint])
                end = this.NextGrapheme(end, codepointCount);
            this.ShapeSegment(codepoint, end, src);
            codepoint = end;
        }

        var srcSpan = CollectionsMarshal.AsSpan(src);
        for (var k = 0; k + 1 < srcSpan.Length; k++)
            this.KernBoundary(ref srcSpan[k], srcSpan[k + 1]);
        FontProfiler.End(FontProfiler.Section.LayoutShape, start, codepointCount);

        start = FontProfiler.Begin(FontProfiler.Section.LayoutFinish);
        this.BreakLines(src);
        this.Finish();
        FontProfiler.End(FontProfiler.Section.LayoutFinish, start, codepointCount);
    }

    // U+2026 from the default font, else three periods.
    private Item MakeEllipsis(out int count)
    {
        var font = this.SlotOf(0).Font;
        Span<uint> chars = [0x2026, '.'];
        Span<uint> ids = stackalloc uint[2];
        font.GetGlyphIndices(chars, ids);
        var glyph = new Glyph { Id = ids[0] != 0 ? ids[0] : ids[1] };
        count = ids[0] != 0 ? 1 : 3;
        Span<GlyphMetrics> m = stackalloc GlyphMetrics[1];
        font.GetGlyphMetrics([glyph.Id], this.desc.EmSize, this.Density, m);
        var item = new Item
        {
            Kind = ItemKind.Ellipsis,
            Run = -1,
            Object = -1,
            Segment = -1,
            Advance = m[0].Advance,
            Bounds = m[0].Bounds,
            GlyphBegin = this.glyphs.Count,
            GlyphEnd = this.glyphs.Count + 1,
        };
        this.glyphs.Add(glyph);
        return item;
    }

    // Ink, line records and run rectangles.
    private void Finish()
    {
        // Line by line, and in locals, as this runs per item in unoptimized builds too.
        var layoutSize = this.Size;
        var (inkLeft, inkTop, inkRight, inkBottom) = (0f, 0f, layoutSize.X, layoutSize.Y);
        var itemSpan = CollectionsMarshal.AsSpan(this.items);
        var lineSpan = CollectionsMarshal.AsSpan(this.lines);
        foreach (ref readonly var line in lineSpan)
        {
            var y = line.BlockPos + line.Baseline;
            foreach (ref readonly var item in itemSpan[line.ItemBegin..line.ItemEnd])
            {
                if (item.Kind is not (ItemKind.Glyph or ItemKind.Ellipsis) || item.Bounds.Z <= item.Bounds.X)
                    continue;
                inkLeft = Math.Min(inkLeft, item.X + item.Bounds.X);
                inkTop = Math.Min(inkTop, y + item.Bounds.Y);
                inkRight = Math.Max(inkRight, item.X + item.Bounds.Z);
                inkBottom = Math.Max(inkBottom, y + item.Bounds.W);
            }
        }

        var ink = new Vector4(inkLeft, inkTop, inkRight, inkBottom);

        // By the most any style draws past the glyphs.
        var grow = this.GrowOf(this.defaultFont);
        foreach (ref readonly var run in this.runs.AsSpan(0, this.RunCount))
        {
            if (run.Impl is { } runFont)
                grow = Math.Max(grow, this.GrowOf(runFont));
        }

        this.Ink = new(ink.X - grow, ink.Y - grow, ink.Z + grow, ink.W + grow);

        // Pinned, as ImGui reads them in place. Only an empty one is not.
        this.lineArray = this.lineArray.Grow(lineSpan.Length, true);
        for (var n = 0; n < lineSpan.Length; n++)
        {
            ref readonly var line = ref lineSpan[n];
            var pos = new Vector2(0, line.BlockPos);
            var size = new Vector2(line.Extent, line.BlockSize);
            this.lineArray[n] = new(line.TextBegin, line.TextEnd, line.TextNext, line.Baseline, pos, size);
        }

        // One rectangle per run and line it is on, grouped by run. Runs may overlap, so items are not in run order.
        var s = this.Scratch;
        s.PrepareRuns(this.RunCount);
        var pairs = s.RunRects;
        var lineRuns = s.LineRuns;
        for (var n = 0; n < lineSpan.Length; n++)
        {
            ref readonly var line = ref lineSpan[n];
            lineRuns.Clear();
            foreach (ref readonly var item in itemSpan[line.ItemBegin..line.ItemEnd])
            {
                var r = item.Run;
                if (r < 0 || item.Kind == ItemKind.Newline)
                    continue;
                if (s.RunLine[r] != n + 1)
                {
                    s.RunLine[r] = n + 1;
                    (s.RunU0[r], s.RunU1[r]) = (item.X, item.X + item.Advance);
                    lineRuns.Add(r);
                }
                else
                {
                    s.RunU0[r] = Math.Min(s.RunU0[r], item.X);
                    s.RunU1[r] = Math.Max(s.RunU1[r], item.X + item.Advance);
                }
            }

            foreach (var r in CollectionsMarshal.AsSpan(lineRuns))
            {
                if (s.RunU0[r] >= s.RunU1[r])
                    continue;
                var rect = new Vector4(s.RunU0[r], line.BlockPos, s.RunU1[r], line.BlockPos + line.BlockSize);
                pairs.Add((r, rect));
                s.RunRectCount[r]++;
            }
        }

        this.runRectRange = this.runRectRange.Grow(this.RunCount);
        var first = 0;
        for (var r = 0; r < this.RunCount; r++)
        {
            this.runRectRange[r] = (first, 0);
            first += s.RunRectCount[r];
        }

        this.runRects = this.runRects.Grow(pairs.Count, true);
        foreach (var (r, rect) in CollectionsMarshal.AsSpan(pairs))
        {
            ref var range = ref this.runRectRange[r];
            this.runRects[range.First + range.Count++] = rect;
        }
    }

    private float GrowOf(FontImpl font)
    {
        var (style, em, scale) = (font.Style, this.EmOf(this.FontIndexOf(font.Font)), this.desc.GlobalScale);
        var shadow = style.Shadow.OffsetAt(em, scale);
        return style.Edge.WidthAt(em, scale) + Math.Max(Math.Abs(shadow.X), Math.Abs(shadow.Y));
    }

    // Glyphs of whole graphemes of one run, shaped by the font alone. For password masks and tabs, one glyph per
    // grapheme from its first codepoint, whose advance ShapeSegment sets.
    private void ShapeGlyphs(int fontIndex, int codepointBegin, int codepointEnd, bool perGrapheme)
    {
        var output = this.Scratch.ShapeOutput;
        output.Clear();
        ref readonly var slot = ref this.SlotOf(fontIndex);
        var (font, size) = (slot.Font, slot.Size);
        if (!perGrapheme)
        {
            var start = FontProfiler.Begin(FontProfiler.Section.LayoutShapeByFonts);
            var length = codepointEnd - codepointBegin;
            font.Shape(
                new(
                    this.Scratch.ShapeText.AsSpan(codepointBegin, length),
                    0,
                    length,
                    size,
                    this.Density,
                    this.graphemeStart.AsSpan(codepointBegin, length)),
                output);
            FontProfiler.End(FontProfiler.Section.LayoutShapeByFonts, start, length);

            // Clusters as layout codepoints, within the run and ascending.
            var last = codepointBegin;
            foreach (ref var g in output.Span)
            {
                g.Cluster = Math.Clamp(g.Cluster + codepointBegin, last, codepointEnd - 1);
                last = g.Cluster;
            }

            return;
        }

        var count = 0;
        for (var codepoint = codepointBegin;
             codepoint < codepointEnd;
             codepoint = this.NextGrapheme(codepoint, codepointEnd))
        {
            count++;
        }

        var pooledChars = new PooledSpan<uint>(count <= 64 ? stackalloc uint[count] : default, count);
        var pooledIds = new PooledSpan<uint>(count <= 64 ? stackalloc uint[count] : default, count);
        var chars = pooledChars.Span;
        var ids = pooledIds.Span;
        var k = 0;
        for (var codepoint = codepointBegin;
             codepoint < codepointEnd;
             codepoint = this.NextGrapheme(codepoint, codepointEnd))
        {
            chars[k++] = this.Scratch.ShapeText[codepoint];
        }

        font.GetGlyphIndices(chars, ids);
        k = 0;
        for (var codepoint = codepointBegin;
             codepoint < codepointEnd;
             codepoint = this.NextGrapheme(codepoint, codepointEnd), k++)
        {
            output.Add(ids[k], codepoint, 0);
        }

        pooledChars.Recycle();
        pooledIds.Recycle();
    }

    // Shapes whole graphemes of one run between hard breaks into items.
    private void ShapeSegment(int codepointBegin, int codepointEnd, List<Item> output)
    {
        var segment = this.segmentCount++;
        var shapeText = this.Scratch.ShapeText;
        var run = this.Scratch.CodepointRun[codepointBegin];
        var fontIndex = this.FontIndexOfRun(run);
        ref readonly var slot = ref this.SlotOf(fontIndex);
        var (font, size) = (slot.Font, slot.Size);
        var spacing = StyleLength.Resolve(
            this.FontOfRun(run).Style.LetterSpacing, this.EmOf(fontIndex), this.desc.GlobalScale, 0);
        var tab = !this.Masked && this.runes[codepointBegin] == '\t';
        var perGrapheme = this.Masked || tab;

        this.ShapeGlyphs(fontIndex, codepointBegin, codepointEnd, perGrapheme);
        if (!this.Masked && HasMissing(this.Scratch.ShapeOutput.Span))
            this.FixMissing(fontIndex, codepointBegin, codepointEnd, perGrapheme);
        var shaped = this.Scratch.ShapeOutput.Span;
        var n = shaped.Length;
        var pooledIds = new PooledSpan<uint>(n <= 64 ? stackalloc uint[n] : default, n);
        var pooledMetrics = new PooledSpan<GlyphMetrics>(n <= 64 ? stackalloc GlyphMetrics[n] : default, n);
        var ids = pooledIds.Span;
        var metrics = pooledMetrics.Span;
        for (var k = 0; k < shaped.Length; k++)
            ids[k] = shaped[k].Glyph;
        if (!ids.IsEmpty)
            font.GetGlyphMetrics(ids, size, this.Density, metrics);
        if (perGrapheme)
        {
            for (var k = 0; k < shaped.Length; k++)
                shaped[k].Advance = metrics[k].Advance;
        }

        // Arrays are read into locals, and items filled in place, as this runs per grapheme in unoptimized builds too.
        var glyphBase = this.glyphs.Count;
        CollectionsMarshal.SetCount(this.glyphs, glyphBase + shaped.Length);
        var newGlyphs = CollectionsMarshal.AsSpan(this.glyphs)[glyphBase..];
        var codepoints = this.runes;
        var graphemeStarts = this.graphemeStart;
        var softHyphens = !this.Masked;
        var g = 0;
        for (var codepoint = codepointBegin; codepoint < codepointEnd;)
        {
            // The item ends where both a cluster and a grapheme do.
            var end = NextGraphemeIn(graphemeStarts, codepoint, codepointEnd);
            var gEnd = g;
            while (true)
            {
                while (gEnd < shaped.Length && shaped[gEnd].Cluster < end)
                    gEnd++;
                var next = gEnd < shaped.Length ? shaped[gEnd].Cluster : codepointEnd;
                if (next <= end || gEnd == g)
                    break;
                end = NextGraphemeIn(graphemeStarts, next - 1, codepointEnd);
            }

            output.Add(default);
            ref var item = ref CollectionsMarshal.AsSpan(output)[^1];
            item.CodepointBegin = codepoint;
            item.CodepointEnd = end;
            item.Run = run;
            item.Object = -1;
            item.FontIndex = (byte)fontIndex;
            item.Segment = segment;
            item.Kind = IsBlank(shapeText[codepoint]) ? ItemKind.Blank : ItemKind.Glyph;
            item.UnsafeBefore = gEnd > g && shaped[g].UnsafeToBreak;
            item.GlyphBegin = glyphBase + g;
            item.GlyphEnd = glyphBase + gEnd;

            var pen = 0f;
            var (inkLeft, inkTop) = (float.MaxValue, float.MaxValue);
            var (inkRight, inkBottom) = (float.MinValue, float.MinValue);
            for (var k = g; k < gEnd; k++)
            {
                ref readonly var s = ref shaped[k];
                ref var glyph = ref newGlyphs[k];
                var b = metrics[k].Bounds;
                var x = pen + s.Offset.X;
                glyph.Id = s.Glyph;
                glyph.Pos = new(x, s.Offset.Y);
                pen += s.Advance;
                if (s.Glyph != 0 && b.Z > b.X && b.W > b.Y)
                {
                    inkLeft = Math.Min(inkLeft, b.X + x);
                    inkTop = Math.Min(inkTop, b.Y + s.Offset.Y);
                    inkRight = Math.Max(inkRight, b.Z + x);
                    inkBottom = Math.Max(inkBottom, b.W + s.Offset.Y);
                }
            }

            if (item.Kind == ItemKind.Glyph && inkRight > inkLeft)
                item.Bounds = new(inkLeft, inkTop, inkRight, inkBottom);

            // A tab is shaped as a space. BreakLines widens it to the next tab stop.
            item.Advance = pen + spacing;

            // A soft hyphen takes no space, unless BreakLines shows it.
            if (softHyphens && codepoints[codepoint] == SoftHyphen)
            {
                item.Kind = ItemKind.SoftHyphen;
                item.HyphenAdvance = item.Advance;
                item.Advance = 0;
            }

            g = gEnd;
            codepoint = end;
        }

        // Glyphs past the last cluster, which shaping should not give, are dropped.
        CollectionsMarshal.SetCount(this.glyphs, glyphBase + g);
        pooledIds.Recycle();
        pooledMetrics.Recycle();
    }

    // The kerning of the pair (a, b) alone with a font, or null if the font lacks either character. The font keeps
    // the pairs.
    private float? PairKern(int fontIndex, uint a, uint b)
    {
        ref readonly var slot = ref this.SlotOf(fontIndex);
        return slot.Font.TryGetPairKerning(a, b, slot.Size, this.Density, out var k) ? k : null;
    }

    // Kerns adjacent items of different segments, as Font.KernAcross does.
    private void KernBoundary(ref Item left, in Item right)
    {
        left.Advance -= left.Kern;
        left.Kern = 0;
        if (!IsText(left) || !IsText(right) || left.Segment == right.Segment ||
            left.CodepointEnd != right.CodepointBegin ||
            left.GlyphBegin == left.GlyphEnd || right.GlyphBegin == right.GlyphEnd)
        {
            return;
        }

        var aCodepoint = left.CodepointEnd - 1;
        while (aCodepoint > left.CodepointBegin && !this.IsGraphemeStart(aCodepoint))
            aCodepoint--;
        if (!this.Masked && (this.runes[aCodepoint] == '\t' || this.runes[right.CodepointBegin] == '\t'))
            return;
        var a = this.Scratch.ShapeText[aCodepoint];
        var b = this.Scratch.ShapeText[right.CodepointBegin];
        var l = this.PairKern(left.FontIndex, a, b);
        var r = right.FontIndex != left.FontIndex ? this.PairKern(right.FontIndex, a, b) : null;
        left.Kern = Font.AverageKerning(l, r);
        left.Advance += left.Kern;
    }

    // What only building a layout needs, reused by the next layout built on the thread.
    private sealed class BuildScratch
    {
        // Larger ones are left to the GC, rather than kept for the thread's lifetime.
        private const int MaxKeptLength = 1 << 16;

        // Null while in use. A layout made while making another then gets scratch of its own.
        [ThreadStatic]
        private static BuildScratch? free;

        public List<Item> Src { get; } = [];

        public List<Item> Fresh { get; } = [];

        public List<(int Run, Vector4 Rect)> RunRects { get; } = [];

        public List<int> LineRuns { get; } = [];

        public ShapedGlyphList ShapeOutput { get; } = new();

        // At least as long as Prepare was given. Only that much is meaningful.
        public bool[] BreakBefore { get; private set; } = [];

        public int[] CodepointRun { get; private set; } = [];

        public uint[] ShapeText { get; private set; } = [];

        // Per run, for finding run rectangles. RunLine is the line index plus 1 where the run was last met.
        public int[] RunLine { get; private set; } = [];

        public float[] RunU0 { get; private set; } = [];

        public float[] RunU1 { get; private set; } = [];

        public int[] RunRectCount { get; private set; } = [];

        public static BuildScratch Rent()
        {
            var scratch = free ?? new();
            free = null;
            return scratch;
        }

        public static void Return(BuildScratch scratch)
        {
            if (scratch.ShapeText.Length > MaxKeptLength || scratch.Src.Capacity > MaxKeptLength)
                return;
            scratch.Src.Clear();
            scratch.Fresh.Clear();
            scratch.RunRects.Clear();
            scratch.LineRuns.Clear();
            scratch.ShapeOutput.Clear();
            free = scratch;
        }

        // Makes the arrays long enough for the text, with BreakBefore cleared.
        public void Prepare(int codepointCount)
        {
            this.BreakBefore = Take(this.BreakBefore, codepointCount + 1);
            this.CodepointRun = Take(this.CodepointRun, codepointCount);
            this.ShapeText = Take(this.ShapeText, codepointCount);
        }

        // Makes the per run arrays long enough, cleared.
        public void PrepareRuns(int runCount)
        {
            this.RunLine = Take(this.RunLine, runCount);
            this.RunU0 = Take(this.RunU0, runCount);
            this.RunU1 = Take(this.RunU1, runCount);
            this.RunRectCount = Take(this.RunRectCount, runCount);
        }

        // An array at least as long, with as much of it cleared.
        private static T[] Take<T>(T[] array, int length)
        {
            var result = array.Grow(length);
            if (result == array)
                Array.Clear(array, 0, length);
            return result;
        }
    }
}
