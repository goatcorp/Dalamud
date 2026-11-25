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

/// <content>Layout construction and shaping.</content>
internal sealed partial class TextLayout
{
    private const uint SoftHyphen = 0xAD;
    private const uint ObjectReplacement = 0xFFFC;

    // Pool limits for recycled TextLayout instances.
    private const int MaxPooled = 256;
    private const int MaxPooledCodepoints = 4096;
    private const int MaxPooledTotalCodepoints = 256 * 1024;

    private static readonly Stack<TextLayout> Pool = new();
    private static int pooledCodepoints;

    private BuildScratch? scratch;

    private TextLayout()
    {
    }

    private BuildScratch Scratch => this.scratch ?? throw new InvalidOperationException("Not building.");

    /// <summary>Builds a layout, reusing a pooled instance when available.</summary>
    /// <param name="font">The font of text outside runs, and of runs without one.</param>
    /// <param name="desc">Layout parameters.</param>
    /// <param name="text">The text, in bytes.</param>
    /// <param name="codeUnitSize">1 for UTF-8, 2 for UTF-16 (little endian).</param>
    /// <param name="runs">Attributed ranges, in byte offsets.</param>
    /// <param name="objects">Inline objects, one per <c>U+FFFC</c> in the text.</param>
    /// <param name="sourceOffsets">Optional ascending map from each code unit of <paramref name="text"/> (plus one
    /// entry for its end) to offsets in an outer source text. <paramref name="runs"/> remain in byte offsets into
    /// <paramref name="text"/>.</param>
    /// <returns>The layout.</returns>
    /// <remarks>Every offset the layout reports and takes is in code units of <paramref name="text"/>, or into the
    /// source text. For UTF-8, those are bytes.</remarks>
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

    /// <summary>Resets and returns this layout to the reuse pool once unreferenced.</summary>
    public void Recycle()
    {
        var size = this.runes.Length;
        if (size > MaxPooledCodepoints || Pool.Count >= MaxPooled || pooledCodepoints + size > MaxPooledTotalCodepoints)
            return;
        this.Reset();
        Pool.Push(this);
        pooledCodepoints += size;
    }

    private static bool IsHardBreak(uint c, bool masked) =>
        IsMandatoryBreak(c) || c is '\r' or ObjectReplacement || (c is '\t' or SoftHyphen && !masked);

    // Only LF ends a line, unlike the mandatory breaks of UAX #14, so that lines match the rows of InputText.
    private static bool IsMandatoryBreak(uint c) => c == '\n';

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

    // Grows runes to length and offsets to length + 1 (for the end offset), preserving the first count entries.
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
        if (codeUnitSize is not (1 or 2))
            throw new ArgumentOutOfRangeException(nameof(codeUnitSize), codeUnitSize, "Must be 1 or 2.");
        if (!sourceOffsets.IsEmpty && sourceOffsets.Length != (text.Length / codeUnitSize) + 1)
            throw new ArgumentException("Must have one offset per byte, and one for the end.", nameof(sourceOffsets));
        this.fonts.Add(new() { Font = font.Font, Size = layoutDesc.EmSize });
        this.desc = layoutDesc.Canonical;
        this.defaultFont = font;
        this.runs = this.runs.Grow(textRuns.Length);
        textRuns.CopyTo(this.runs);
        this.RunCount = textRuns.Length;
        this.objects = this.objects.Grow(inlineObjects.Length);
        inlineObjects.CopyTo(this.objects);
        this.objectCount = inlineObjects.Length;

        // Not given back if building throws. The next layout then makes new scratch.
        this.scratch = BuildScratch.Rent();
        var start = FontProfiler.Begin(FontProfiler.Section.LayoutAnalyze);
        this.Analyze(text, codeUnitSize);
        FontProfiler.End(FontProfiler.Section.LayoutAnalyze, start, text.Length);
        this.ReportOffsets(codeUnitSize, sourceOffsets);
        this.Build();
        BuildScratch.Return(this.scratch);
        this.scratch = null;
    }

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
        this.segmentCount = 0;
        this.Size = default;
        this.Ink = default;
        this.HasDrawnContent = false;
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
        const UtfEnumeratorFlags textFlags =
            UtfEnumeratorFlags.DisrespectByteOrderMask | UtfEnumeratorFlags.YieldByteOrderMask;
        var flags = textFlags |
                    (unit == 1 ? UtfEnumeratorFlags.Utf8 : UtfEnumeratorFlags.Utf16 | UtfEnumeratorFlags.LittleEndian);
        var s = this.Scratch;
        this.Decode(text, unit, flags);
        var n = this.runeCount;
        var offsets = this.runeOffsets;
        s.Prepare(n);

        // Boundaries come in ascending order, as byte offsets into the codepoints, 4 bytes each. The one at the end is
        // dropped.
        var decoded = MemoryMarshal.AsBytes(this.runes.AsSpan(0, n));
        const UtfEnumeratorFlags decodedFlags =
            textFlags | UtfEnumeratorFlags.Utf32 | UtfEnumeratorFlags.LittleEndian;
        var graphemeStarts = this.graphemeStart = this.graphemeStart.Grow(n);
        Array.Clear(graphemeStarts, 0, n);
        if (n > 0)
            graphemeStarts[0] = true;
        foreach (var boundary in new UnicodeGraphemeClusterEnumerator(decoded, decodedFlags))
        {
            if (boundary / 4 < n)
                graphemeStarts[boundary / 4] = true;
        }

        foreach (var item in new UnicodeLineBreakEnumerator(decoded, decodedFlags))
            s.BreakBefore[Math.Min(item.ByteOffset / 4, n)] = true;

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

        var mask = this.Masked;
        var maskChar = this.desc.MaskChar;
        for (var codepoint = 0; codepoint < n; codepoint++)
        {
            var c = this.runes[codepoint];
            s.ShapeText[codepoint] = c switch
            {
                _ when mask && this.IsGraphemeStart(codepoint) && !IsMandatoryBreak(c) &&
                       c is not ('\r' or ObjectReplacement) =>
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
        var expected = unit == 1 ? Encoding.UTF8.GetCharCount(text) : (text.Length + 1) / 2;
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

    // Replaces unmapped visible characters (glyph 0) with U+3013 GETA MARK or '-' to match game fonts, then reshapes.
    // Non-visible marks, spaces, and controls are left unreplaced, and default-ignorable or control glyphs get zero advance.
    private void FixMissing(int fontIndex, int codepointBegin, int codepointEnd)
    {
        var shapeText = this.Scratch.ShapeText;
        var font = this.GetSlot(fontIndex).Font;
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
                var replacement = font.HasGlyph(shapeText[codepoint]) ? 0 : this.GetReplacementCodepoint(fontIndex);
                if (replacement == 0)
                    continue;
                shapeText[codepoint] = replacement;
                changed = true;
            }
        }

        if (changed)
            this.ShapeGlyphs(fontIndex, codepointBegin, codepointEnd);

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

    // Caches the fallback replacement codepoint (U+3013, '-', or 0) supported by the font.
    private uint GetReplacementCodepoint(int fontIndex)
    {
        ref var slot = ref this.GetSlot(fontIndex);
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

    // Converts byte offsets from Analyze into reported code-unit or sourceOffsets coordinates.
    private void ReportOffsets(int unit, ReadOnlySpan<int> sourceOffsets)
    {
        foreach (ref var offset in this.runeOffsets.AsSpan(0, this.runeCount + 1))
        {
            offset /= unit;
            if (!sourceOffsets.IsEmpty)
                offset = sourceOffsets[offset];
        }
    }

    private void Build()
    {
        var codepointCount = this.runeCount;
        var masked = this.Masked;

        var src = this.Scratch.Src;
        var codepointRun = this.Scratch.CodepointRun;
        var objectIndex = 0;
        var start = FontProfiler.Begin(FontProfiler.Section.LayoutShape);
        for (var codepoint = 0; codepoint < codepointCount;)
        {
            var c = this.runes[codepoint];
            var gEnd = this.NextGrapheme(codepoint, codepointCount);
            if (c == ObjectReplacement && objectIndex < this.objectCount)
            {
                var item = this.MakeBareItem(codepoint, gEnd, codepointRun[codepoint], ItemKind.Object);
                item.Object = objectIndex++;
                item.Advance = this.objects[item.Object].Bounds.Z;
                src.Add(item);
                codepoint = gEnd;
                continue;
            }

            if (IsMandatoryBreak(c) ||
                (c == '\r' && codepoint + 1 < codepointCount && this.runes[codepoint + 1] == '\n'))
            {
                // UAX #29 groups CR+LF into one grapheme, so emit CR as Control and LF as Newline.
                for (var k = codepoint; k < gEnd; k++)
                {
                    var kind = this.runes[k] == '\r' ? ItemKind.Control : ItemKind.Newline;
                    src.Add(this.MakeBareItem(k, k + 1, codepointRun[k], kind));
                }

                codepoint = gEnd;
                continue;
            }

            if (IsHardBreak(c, masked))
            {
                this.ShapeSegment(codepoint, gEnd, src);
                codepoint = gEnd;
                continue;
            }

            var end = gEnd;
            while (end < codepointCount && !IsHardBreak(this.runes[end], masked) &&
                   codepointRun[end] == codepointRun[codepoint])
                end = this.NextGrapheme(end, codepointCount);
            this.ShapeSegment(codepoint, end, src);
            codepoint = end;
        }

        var srcSpan = CollectionsMarshal.AsSpan(src);
        for (var k = 0; k + 1 < srcSpan.Length; k++)
        {
            var r = k + 1;
            while (r + 1 < srcSpan.Length && srcSpan[r].Kind == ItemKind.SoftHyphen)
                r++;
            this.KernBoundary(ref srcSpan[k], srcSpan[r]);
        }

        FontProfiler.End(FontProfiler.Section.LayoutShape, start, codepointCount);

        start = FontProfiler.Begin(FontProfiler.Section.LayoutFinish);
        this.BreakLines(src);
        this.Finish();
        FontProfiler.End(FontProfiler.Section.LayoutFinish, start, codepointCount);
    }

    private Item MakeBareItem(int codepointBegin, int codepointEnd, int run, ItemKind kind) => new()
    {
        Kind = kind,
        CodepointBegin = codepointBegin,
        CodepointEnd = codepointEnd,
        Run = run,
        Object = -1,
        Segment = -1,
        GlyphBegin = this.glyphs.Count,
        GlyphEnd = this.glyphs.Count,
        FontIndex = checked((ushort)this.GetRunFontIndex(run)),
    };

    // U+2026 from the default font, else three periods.
    private Item MakeEllipsis(out int count)
    {
        var font = this.GetSlot(0).Font;
        Span<uint> chars = [0x2026, '.'];
        Span<uint> ids = stackalloc uint[2];
        font.GetGlyphIndices(chars, ids);
        var glyph = new Glyph { Id = ids[0] != 0 ? ids[0] : ids[1] };
        count = ids[0] != 0 ? 1 : 3;
        Span<GlyphMetrics> m = stackalloc GlyphMetrics[1];
        font.GetGlyphMetrics([glyph.Id], this.desc.EmSize, this.Density, m);
        var item = this.MakeBareItem(0, 0, -1, ItemKind.Ellipsis);
        item.GlyphEnd++;
        item.Advance = m[0].Advance;
        item.Bounds = m[0].Bounds;
        this.glyphs.Add(glyph);
        return item;
    }

    private void Finish()
    {
        var layoutSize = this.Size;
        var (inkLeft, inkTop, inkRight, inkBottom) = (0f, 0f, layoutSize.X, layoutSize.Y);
        var hasDrawn = false;
        var itemSpan = CollectionsMarshal.AsSpan(this.items);
        var lineSpan = CollectionsMarshal.AsSpan(this.lines);
        foreach (ref readonly var line in lineSpan)
        {
            var y = line.BlockPos + line.Baseline;
            foreach (ref readonly var item in itemSpan[line.ItemBegin..line.ItemEnd])
            {
                if (item.Kind is ItemKind.Glyph or ItemKind.Ellipsis &&
                    item.Bounds.Z > item.Bounds.X && item.Bounds.W > item.Bounds.Y)
                {
                    hasDrawn = true;
                    inkLeft = Math.Min(inkLeft, item.X + item.Bounds.X);
                    inkTop = Math.Min(inkTop, y + item.Bounds.Y);
                    inkRight = Math.Max(inkRight, item.X + item.Bounds.Z);
                    inkBottom = Math.Max(inkBottom, y + item.Bounds.W);
                }
                else if (!hasDrawn && item.Kind == ItemKind.Object)
                {
                    hasDrawn = true;
                }
                else if (!hasDrawn &&
                         item.Kind is not (ItemKind.Newline or ItemKind.Control or ItemKind.SoftHyphen) &&
                         item.Advance > 0)
                {
                    ref readonly var st = ref this.GetRunFont(item.Run).Style;
                    if (st.Background.Alpha != 0 || st.Lines.IsOn || !st.ExtraDecoration.IsNone)
                        hasDrawn = true;
                }
            }
        }

        this.HasDrawnContent = hasDrawn;

        var ink = new Vector4(inkLeft, inkTop, inkRight, inkBottom);

        // Expand ink bounds by the maximum edge and shadow overhang across styles.
        var grow = this.GetOverhang(this.defaultFont);
        foreach (ref readonly var run in this.runs.AsSpan(0, this.RunCount))
        {
            if (run.Impl is { } runFont)
                grow = Math.Max(grow, this.GetOverhang(runFont));
        }

        this.Ink = new(ink.X - grow, ink.Y - grow, ink.Z + grow, ink.W + grow);

        this.lineArray = this.lineArray.Grow(lineSpan.Length, true);

        // One rectangle per run and line it is on, grouped by run. Runs may overlap, so items are not in run order.
        var s = this.Scratch;
        s.PrepareRuns(this.RunCount);
        var pairs = s.RunRects;
        var lineRuns = s.LineRuns;
        for (var n = 0; n < lineSpan.Length; n++)
        {
            ref readonly var line = ref lineSpan[n];
            var pos = new Vector2(line.Start, line.BlockPos);
            var size = new Vector2(line.Extent - line.Start, line.BlockSize);
            this.lineArray[n] = new(line.TextBegin, line.TextEnd, line.TextNext, line.Baseline, pos, size);

            lineRuns.Clear();
            foreach (ref readonly var item in itemSpan[line.ItemBegin..line.ItemEnd])
            {
                var r = item.Run;
                if (r < 0 || item.Kind is ItemKind.Newline or ItemKind.Control or ItemKind.SoftHyphen)
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

    // Maximum edge and shadow overhang, returning early before GetOrAddFontIndex so unstyled fonts do not allocate a FontSlot.
    private float GetOverhang(FontImpl font)
    {
        ref readonly var style = ref font.Style;
        var shadowOn = style.Shadow.IsOn;
        if (!style.Edge.IsOn && !shadowOn)
            return 0;

        // OffsetAt ignores IsOn, and a shadow that is off is not drawn.
        var (em, scale) = (this.GetEmSize(this.GetOrAddFontIndex(font.Font)), this.desc.GlobalScale);
        var shadow = shadowOn ? style.Shadow.OffsetAt(em, scale) : Vector2.Zero;
        return style.Edge.WidthAt(em, scale) + Math.Max(Math.Abs(shadow.X), Math.Abs(shadow.Y));
    }

    // Glyphs of whole graphemes of one run, shaped by the font alone.
    private void ShapeGlyphs(int fontIndex, int codepointBegin, int codepointEnd)
    {
        var output = this.Scratch.ShapeOutput;
        output.Clear();
        ref readonly var slot = ref this.GetSlot(fontIndex);
        var start = FontProfiler.Begin(FontProfiler.Section.LayoutShapeByFonts);
        var length = codepointEnd - codepointBegin;
        var text = this.Scratch.ShapeText.AsSpan(codepointBegin, length);
        var starts = this.graphemeStart.AsSpan(codepointBegin, length);
        var composedLength = this.Scratch.ComposeHangul(text, starts);
        if (composedLength < length)
        {
            text = this.Scratch.ComposedText.AsSpan(0, composedLength);
            starts = this.Scratch.ComposedStarts.AsSpan(0, composedLength);
        }

        slot.Font.Shape(new(text, 0, text.Length, slot.Size, this.Density, starts), output);
        FontProfiler.End(FontProfiler.Section.LayoutShapeByFonts, start, length);

        // Clusters as layout codepoints, within the run and ascending. A syllable spans the jamo it was composed of.
        var composedSource = this.Scratch.ComposedSource;
        var last = codepointBegin;
        foreach (ref var g in output.Span)
        {
            var cluster = composedLength < length
                              ? composedSource[Math.Clamp(g.Cluster, 0, composedLength - 1)]
                              : g.Cluster;
            g.Cluster = Math.Clamp(cluster + codepointBegin, last, codepointEnd - 1);
            last = g.Cluster;
        }
    }

    // Maps one glyph per grapheme for password masks and tabs, leaving ShapeSegment to assign advances.
    private void ShapePerGrapheme(int fontIndex, int codepointBegin, int codepointEnd)
    {
        var output = this.Scratch.ShapeOutput;
        output.Clear();

        var max = codepointEnd - codepointBegin;
        var pooledChars = new PooledSpan<uint>(max <= 64 ? stackalloc uint[max] : default, max);
        var pooledIds = new PooledSpan<uint>(max <= 64 ? stackalloc uint[max] : default, max);
        var chars = pooledChars.Span;
        var ids = pooledIds.Span;
        var count = 0;
        for (var codepoint = codepointBegin;
             codepoint < codepointEnd;
             codepoint = this.NextGrapheme(codepoint, codepointEnd))
        {
            chars[count++] = this.Scratch.ShapeText[codepoint];
            output.Add(0, codepoint, 0);
        }

        this.GetSlot(fontIndex).Font.GetGlyphIndices(chars[..count], ids[..count]);
        var shaped = output.Span;
        for (var k = 0; k < count; k++)
            shaped[k].Glyph = ids[k];

        pooledChars.Recycle();
        pooledIds.Recycle();
    }

    // Shapes whole graphemes of one run between hard breaks into items.
    private void ShapeSegment(int codepointBegin, int codepointEnd, List<Item> output)
    {
        var segment = this.segmentCount++;
        var shapeText = this.Scratch.ShapeText;
        var run = this.Scratch.CodepointRun[codepointBegin];
        var fontIndex = this.GetRunFontIndex(run);
        ref readonly var slot = ref this.GetSlot(fontIndex);
        var (font, size) = (slot.Font, slot.Size);
        var spacing = StyleLength.Resolve(
            this.GetRunFont(run).Style.LetterSpacing, this.GetEmSize(fontIndex), this.desc.GlobalScale, 0);
        var tab = !this.Masked && this.runes[codepointBegin] == '\t';
        var perGrapheme = this.Masked || tab;

        if (perGrapheme)
            this.ShapePerGrapheme(fontIndex, codepointBegin, codepointEnd);
        else
            this.ShapeGlyphs(fontIndex, codepointBegin, codepointEnd);
        if (!perGrapheme && HasMissing(this.Scratch.ShapeOutput.Span))
            this.FixMissing(fontIndex, codepointBegin, codepointEnd);
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

        var glyphBase = this.glyphs.Count;
        CollectionsMarshal.SetCount(this.glyphs, glyphBase + shaped.Length);
        var newGlyphs = CollectionsMarshal.AsSpan(this.glyphs)[glyphBase..];
        var codepoints = this.runes;
        var graphemeStarts = this.graphemeStart;
        var softHyphens = !this.Masked;
        var g = 0;
        for (var codepoint = codepointBegin; codepoint < codepointEnd;)
        {
            var end = NextGraphemeIn(graphemeStarts, codepoint, codepointEnd);
            var gEnd = g;
            // Extend end to whole-grapheme boundaries if a ligature cluster spans past it.
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
            item.FontIndex = checked((ushort)fontIndex);
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

            // A tab is shaped as a space. BreakLines widens it to the next tab stop. Zero width format characters take
            // no letter spacing. A negative spacing stops at zero, so that the pen never goes back.
            var zeroWidth = shapeText[codepoint] is 0x200B or 0x200C or 0x2060 or 0xFEFF;
            item.Advance = zeroWidth ? pen : Math.Max(pen + spacing, 0);

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

    // Kerns adjacent items of different segments, as Font.KernAcross does.
    private void KernBoundary(ref Item left, in Item right)
    {
        left.Advance -= left.Kern;
        left.Kern = 0;
        if (!IsText(left) || !IsText(right) || left.Segment == right.Segment ||
            left.GlyphBegin == left.GlyphEnd || right.GlyphBegin == right.GlyphEnd)
        {
            return;
        }

        var between = left.CodepointEnd;
        while (between < right.CodepointBegin && !this.Masked && this.runes[between] == SoftHyphen)
            between++;
        if (between != right.CodepointBegin)
            return;

        var aCodepoint = left.CodepointEnd - 1;
        while (aCodepoint > left.CodepointBegin && !this.IsGraphemeStart(aCodepoint))
            aCodepoint--;
        if (!this.Masked && (this.runes[aCodepoint] == '\t' || this.runes[right.CodepointBegin] == '\t'))
            return;
        var a = this.Scratch.ShapeText[aCodepoint];
        var b = this.Scratch.ShapeText[right.CodepointBegin];
        ref readonly var l = ref this.GetSlot(left.FontIndex);
        ref readonly var r = ref this.GetSlot(right.FontIndex);
        left.Kern = Font.KernAcross(l.Font, l.Size, r.Font, r.Size, a, b, this.Density);
        left.Advance += left.Kern;
    }

    // Thread-local scratch buffers reused across layout builds.
    private sealed class BuildScratch
    {
        // Oversized buffers are left for GC instead of staying retained on the thread.
        private const int MaxKeptLength = 1 << 16;

        // Null while checked out so re-entrant builds allocate their own scratch.
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

        // Text with conjoining jamo composed, by ComposeHangul. Per codepoint, its first index in the text it was
        // composed from, and whether a grapheme starts there.
        public uint[] ComposedText { get; private set; } = [];

        public int[] ComposedSource { get; private set; } = [];

        public bool[] ComposedStarts { get; private set; } = [];

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

        // Ensures scratch arrays cover codepointCount and clears reused buffers.
        public void Prepare(int codepointCount)
        {
            this.BreakBefore = Take(this.BreakBefore, codepointCount + 1);
            this.CodepointRun = Take(this.CodepointRun, codepointCount);
            this.ShapeText = Take(this.ShapeText, codepointCount);
        }

        // Composes modern Hangul jamo into precomposed NFC syllables so DirectWrite does not render jamo separately,
        // leaving Old Hangul unchanged. Returns the composed length, or text.Length if unchanged.
        public int ComposeHangul(ReadOnlySpan<uint> text, ReadOnlySpan<bool> graphemeStarts)
        {
            const uint syllableBase = 0xAC00, leadBase = 0x1100, vowelBase = 0x1161, trailBase = 0x11A7;
            const uint leadCount = 19, vowelCount = 21, trailCount = 28;
            const uint syllableCount = leadCount * vowelCount * trailCount;

            // Every composition takes a vowel or a trailing consonant.
            if (text.IndexOfAnyInRange(vowelBase, trailBase + trailCount - 1) < 0)
                return text.Length;

            this.ComposedText = this.ComposedText.Grow(text.Length);
            this.ComposedSource = this.ComposedSource.Grow(text.Length);
            this.ComposedStarts = this.ComposedStarts.Grow(text.Length);
            var w = 0;
            for (var i = 0; i < text.Length;)
            {
                var c = text[i];
                var n = 1;
                if (c - leadBase < leadCount && i + 1 < text.Length && text[i + 1] - vowelBase < vowelCount)
                {
                    c = syllableBase + ((((c - leadBase) * vowelCount) + (text[i + 1] - vowelBase)) * trailCount);
                    n = 2;
                }

                if ((n == 2 || (c - syllableBase < syllableCount && (c - syllableBase) % trailCount == 0)) &&
                    i + n < text.Length && text[i + n] - (trailBase + 1) < trailCount - 1)
                {
                    c += text[i + n] - trailBase;
                    n++;
                }

                this.ComposedText[w] = c;
                this.ComposedSource[w] = i;
                this.ComposedStarts[w++] = graphemeStarts[i];
                i += n;
            }

            return w;
        }

        public void PrepareRuns(int runCount)
        {
            this.RunLine = Take(this.RunLine, runCount);
            this.RunU0 = Take(this.RunU0, runCount);
            this.RunU1 = Take(this.RunU1, runCount);
            this.RunRectCount = Take(this.RunRectCount, runCount);
        }

        private static T[] Take<T>(T[] array, int length)
        {
            var result = array.Grow(length);
            if (result == array)
                Array.Clear(array, 0, length);
            return result;
        }
    }
}
