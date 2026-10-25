using System.Collections.Generic;
using System.Runtime.InteropServices;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Fonts.Internal.Sources;

namespace Dalamud.Interface.Fonts.Internal.Layout;

/// <content>Line breaking and horizontal alignment.</content>
internal sealed partial class TextLayout
{
    private void BreakLines(List<Item> src)
    {
        var maxWidth = this.desc.MaxWidth > 0 ? this.desc.MaxWidth : float.MaxValue;
        var useEllipsis = (this.desc.Flags & ImTextLayoutFlags.Ellipsis) != 0 && maxWidth < float.MaxValue;
        var stopWidth = useEllipsis ? float.MaxValue : maxWidth;
        var maxLines = this.desc.MaxLines;
        var ellipsisCount = 0;
        var ellipsisItem = useEllipsis || maxLines > 0 ? this.MakeEllipsis(out ellipsisCount) : default;
        this.TextLength = this.runeOffsets[this.runeCount];
        var blockPos = 0f;
        var i = 0;
        while (true)
        {
            var line = new Line
                { ItemBegin = this.items.Count, TextBegin = i < src.Count ? this.GetTextBegin(src[i]) : this.TextLength };
            line.EndsWithNewline = this.FillLine(src, ref i, line.ItemBegin, stopWidth, out var stopped);

            // Kerning towards the next line does not apply.
            this.DropLastKern(line.ItemBegin);
            line.ItemEnd = this.items.Count;
            this.MeasureLine(ref line, src, i);
            // Only text that goes on is cut. The empty line after a trailing newline is left out without an ellipsis.
            var atMaxLines = !stopped && maxLines > 0 && this.lines.Count + 1 >= maxLines &&
                             (this.desc.Flags & ImTextLayoutFlags.StopOnNewLine) == 0;
            var cutOff = atMaxLines && i < src.Count;
            if (cutOff)
                this.TruncateLast(ref line, ellipsisItem, ellipsisCount, maxWidth);
            else if (useEllipsis && line.Width > maxWidth)
                this.Truncate(ref line, ellipsisItem, ellipsisCount, maxWidth);
            this.PlaceLine(ref line, ref blockPos);

            if (cutOff)
            {
                this.TextLength = line.TextEnd;
                break;
            }

            if (stopped || (this.desc.Flags & ImTextLayoutFlags.StopOnNewLine) != 0)
            {
                if (!stopped)
                    this.TextLength = line.TextNext;
                break;
            }

            if (i >= src.Count && (!line.EndsWithNewline || atMaxLines))
                break;
        }

        var inlineMax = 0f;
        foreach (ref readonly var l in CollectionsMarshal.AsSpan(this.lines))
            inlineMax = Math.Max(inlineMax, l.Extent);
        this.Size = new(this.AlignLines(inlineMax), blockPos);
    }

    // Shifts lines horizontally for alignment within WrapWidth (or max line extent if unwrapped) using Extent so overhangs stay inside the box.
    private float AlignLines(float inlineMax)
    {
        var align = this.desc.Align;
        if (align == 0)
            return inlineMax;
        var box = Math.Max(this.desc.WrapWidth, inlineMax);
        foreach (ref var line in CollectionsMarshal.AsSpan(this.lines))
        {
            var dx = MathF.Round((box - line.Extent) * align);
            if (dx <= 0)
                continue;
            for (var k = line.ItemBegin; k < line.ItemEnd; k++)
                this.ItemAt(k).X += dx;
            line.Start = dx;
            line.Width += dx;
            line.FullWidth += dx;
            line.Extent += dx;
        }

        return box;
    }

    // Strips trailing blanks and line breaks on the MaxLines cutoff line, then truncates with an ellipsis within maxWidth.
    private void TruncateLast(ref Line line, in Item ellipsisItem, int ellipsisCount, float maxWidth)
    {
        var end = line.ItemEnd;
        while (end > line.ItemBegin && this.ItemAt(end - 1).Kind is ItemKind.Blank or ItemKind.Newline or
                   ItemKind.Control or ItemKind.SoftHyphen)
        {
            end--;
        }

        this.items.RemoveRange(end, this.items.Count - end);
        line.ItemEnd = end;
        line.TextEnd = end > line.ItemBegin ? this.GetTextEnd(this.ItemAt(end - 1)) : line.TextBegin;
        line.EndsWithNewline = false;
        var limit = this.desc.WrapWidth > 0 ? Math.Min(this.desc.WrapWidth, maxWidth) : maxWidth;
        this.Truncate(ref line, ellipsisItem, ellipsisCount, limit);
        line.TextNext = line.TextEnd;
    }

    // Places items from src[i] until a newline (returned true), the wrap width, or the end. Stops the text where an
    // item would pass stopWidth.
    private bool FillLine(List<Item> src, ref int i, int lineItemBegin, float stopWidth, out bool stopped)
    {
        var wrap = this.desc.WrapWidth;
        var breakBefore = this.Scratch.BreakBefore;
        var x = 0f;
        var breakAt = -1; // index into src where the next line may start
        var lineFirst = i;
        stopped = false;
        for (; i < src.Count; i++)
        {
            var item = src[i];
            if (item.Kind == ItemKind.Newline)
            {
                item.X = x;
                item.Advance = 0;
                this.items.Add(item);
                i++;
                return true;
            }

            // Wrapping after a soft hyphen shows it, so it must fit.
            if (i > lineFirst && breakBefore[item.CodepointBegin] &&
                (src[i - 1].Kind != ItemKind.SoftHyphen || wrap <= 0 || x + src[i - 1].HyphenAdvance <= wrap))
                breakAt = i;

            if (this.runes[item.CodepointBegin] == '\t' && !this.Masked)
                item.Advance = this.TabAdvance(x, item.Advance);

            // What takes no room fits wherever trailing blanks have taken x, such as the CR of a CRLF.
            var reach = item.Advance - item.Kern;
            if (reach > 0 && x + reach > stopWidth && item.Kind != ItemKind.Blank)
            {
                this.TextLength = this.GetTextBegin(item);
                stopped = true;
                return false;
            }

            if (wrap > 0 && reach > 0 && item.Kind != ItemKind.Blank && x + reach > wrap && i > lineFirst)
            {
                var cut = breakAt > lineFirst ? breakAt : i;

                // Undo items already placed past the cut, one per item of src. Items may share an offset, so they
                // are counted.
                var keep = lineItemBegin + (cut - lineFirst);
                this.items.RemoveRange(keep, this.items.Count - keep);
                this.ReshapeAtBreak(src, cut, lineItemBegin);

                // A soft hyphen the line wraps after shows as a hyphen. It stays hidden where the line has no room
                // for it, as when no other break fits.
                if (this.items.Count > lineItemBegin && this.items[^1] is
                        { Kind: ItemKind.SoftHyphen } last && last.X + last.HyphenAdvance <= wrap)
                {
                    ref var hyphen = ref this.ItemAt(this.items.Count - 1);
                    hyphen.Kind = ItemKind.Glyph;
                    hyphen.Advance = hyphen.HyphenAdvance;
                    if (this.items.Count - lineItemBegin >= 2)
                    {
                        ref var prev = ref this.ItemAt(this.items.Count - 2);
                        var prevKern = prev.Kern;
                        var prevAdvance = prev.Advance;
                        this.KernBoundary(ref prev, hyphen);
                        if (prev.X + prev.Advance + hyphen.Advance <= wrap)
                        {
                            hyphen.X = prev.X + prev.Advance;
                        }
                        else
                        {
                            prev.Kern = prevKern;
                            prev.Advance = prevAdvance;
                        }
                    }
                }

                i = cut;
                return false;
            }

            item.X = x;
            x += item.Advance;
            this.items.Add(item);
        }

        return false;
    }

    private void DropLastKern(int lineItemBegin)
    {
        var k = this.items.Count - 1;
        while (k > lineItemBegin && this.ItemAt(k).Kind == ItemKind.SoftHyphen)
            k--;
        if (k < lineItemBegin)
            return;
        ref var last = ref this.ItemAt(k);
        var kern = last.Kern;
        last.Advance -= kern;
        last.Kern = 0;
        for (k++; k < this.items.Count; k++)
            this.ItemAt(k).X -= kern;
    }

    // The widths and text ends of a filled line. Trailing blanks count with WrapKeepBlanks only.
    private void MeasureLine(ref Line line, List<Item> src, int next)
    {
        var keepBlanks = (this.desc.Flags & ImTextLayoutFlags.WrapKeepBlanks) != 0;
        var endItem = line.ItemEnd - (line.EndsWithNewline ? 1 : 0);
        line.FullWidth = endItem > line.ItemBegin ? this.ItemAt(endItem - 1).X + this.ItemAt(endItem - 1).Advance : 0;

        // Blanks before the CR of a CRLF trail as before a lone LF.
        var crlf = line.EndsWithNewline && endItem > line.ItemBegin &&
                   this.ItemAt(endItem - 1).Kind == ItemKind.Control;
        var trailing = crlf ? endItem - 1 : endItem;
        if (!keepBlanks)
        {
            while (trailing > line.ItemBegin && this.ItemAt(trailing - 1).Kind == ItemKind.Blank)
                trailing--;
        }

        line.TextEnd = line.TextBegin;
        line.Width = 0;
        if (trailing > line.ItemBegin)
        {
            ref readonly var last = ref this.ItemAt(trailing - 1);
            line.TextEnd = this.GetTextEnd(last);
            line.Width = (last.X + last.Advance) - (trailing < endItem ? last.Kern : 0);
        }

        // With WrapKeepBlanks, end a CRLF line at LF rather than CR so ImGui InputText line loops step past '\n'.
        if (line.EndsWithNewline && keepBlanks)
            line.TextEnd = this.GetTextBegin(this.ItemAt(line.ItemEnd - 1));
        line.Extent = this.GetInkExtent(line.ItemBegin, trailing);
        line.TextNext = next < src.Count ? this.GetTextBegin(src[next]) : this.runeOffsets[this.runeCount];
        if (line.EndsWithNewline)
            line.TextNext = this.GetTextEnd(this.ItemAt(line.ItemEnd - 1));
    }

    // Truncates the line to maxWidth, including the ellipsis after what is kept.
    private void Truncate(ref Line line, in Item ellipsisItem, int ellipsisCount, float maxWidth)
    {
        var ellipsisAdvance = ellipsisItem.Advance;
        var endItem = line.ItemEnd - (line.EndsWithNewline ? 1 : 0);
        var keep = line.ItemBegin;
        while (keep < endItem &&
               ((this.ItemAt(keep).X + this.ItemAt(keep).Advance) - this.ItemAt(keep).Kern) +
               (ellipsisAdvance * ellipsisCount) <= maxWidth)
        {
            keep++;
        }

        if ((this.desc.Flags & ImTextLayoutFlags.WrapKeepBlanks) == 0)
        {
            while (keep > line.ItemBegin &&
                   this.ItemAt(keep - 1).Kind is ItemKind.Blank or ItemKind.Control or ItemKind.SoftHyphen)
            {
                keep--;
            }
        }

        var cutCodepoint = keep < endItem ? this.ItemAt(keep).CodepointBegin : this.CodepointIndex(line.TextEnd);
        var cutOffset = this.runeOffsets[cutCodepoint];
        if (this.EllipsisOffset < 0)
            this.EllipsisOffset = cutOffset;

        // The ellipsis is drawn in the style of the text it replaces.
        var run = keep < endItem ? this.ItemAt(keep).Run : keep > line.ItemBegin ? this.ItemAt(keep - 1).Run : -1;
        this.items.RemoveRange(keep, this.items.Count - keep);
        this.DropLastKern(line.ItemBegin);
        var ex = keep > line.ItemBegin ? this.ItemAt(keep - 1).X + this.ItemAt(keep - 1).Advance : 0;
        for (var e = 0; e < ellipsisCount; e++)
        {
            this.items.Add(
                ellipsisItem with { CodepointBegin = cutCodepoint, CodepointEnd = cutCodepoint, X = ex, Run = run });
            ex += ellipsisAdvance;
        }

        line.ItemEnd = this.items.Count;
        line.TextEnd = cutOffset;
        line.Width = line.FullWidth = ex;
        line.Extent = this.GetInkExtent(line.ItemBegin, line.ItemEnd);
    }

    // How far items reach from the line start, with the ink they draw past their advances. Game fonts often draw past
    // the advance, and ImGui clips to the size it is told.
    private float GetInkExtent(int itemBegin, int itemEnd)
    {
        var extent = 0f;
        for (var k = itemBegin; k < itemEnd; k++)
        {
            ref readonly var item = ref this.ItemAt(k);
            var reach = item.Advance - item.Kern;
            if (item.Kind is ItemKind.Glyph or ItemKind.Ellipsis)
                reach = Math.Max(reach, item.Bounds.Z);
            extent = Math.Max(extent, item.X + reach);
        }

        return extent;
    }

    // Block metrics come from the font of the first item on the line, and grow to fit inline objects. The line goes at
    // blockPos, which moves past it.
    private void PlaceLine(ref Line line, ref float blockPos)
    {
        var m = this.GetMetrics(line.ItemEnd > line.ItemBegin ? this.ItemAt(line.ItemBegin).FontIndex : 0);
        line.BlockSize = FontMetrics.BlockSize(this.desc.LineHeight, this.desc.Size);
        // Track above-baseline and below-baseline heights separately so tall objects on opposite sides do not collapse.
        var above = m.BaselineIn(line.BlockSize);
        var below = line.BlockSize - above;
        for (var k = line.ItemBegin; k < line.ItemEnd; k++)
        {
            ref readonly var item = ref this.ItemAt(k);
            if (item.Kind != ItemKind.Object)
                continue;
            var b = this.objects[item.Object].Bounds;
            above = Math.Max(above, -b.Y);
            below = Math.Max(below, b.W);
        }

        line.Baseline = above;
        line.BlockSize = above + below;
        line.BlockPos = blockPos;
        blockPos += line.BlockSize;
        for (var k = line.ItemBegin; k < line.ItemEnd; k++)
            this.ItemAt(k).LineIndex = this.lines.Count;
        this.lines.Add(line);
    }

    // The advance of a tab at x from the line start to the next tab stop, given the advance of a space. A stop closer
    // than half a space is skipped, as editors do.
    private float TabAdvance(float x, float space)
    {
        var stop = space * (this.desc.TabSize > 0 ? this.desc.TabSize : 4);
        if (stop <= 0)
            return space;
        var next = (MathF.Floor(x / stop) + 1) * stop;
        if (next - x < space * 0.5f)
            next += stop;
        return next - x;
    }

    // When breaking at an unsafe shaping boundary, reshapes the broken segment's tail on this line and its head up to
    // the next safe break independently. Slight width growth on the current line is not re-wrapped.
    private void ReshapeAtBreak(List<Item> src, int cut, int lineItemBegin)
    {
        if (cut <= 0 || cut >= src.Count || !src[cut].UnsafeBefore || src[cut - 1].Segment != src[cut].Segment)
            return;
        var segment = src[cut].Segment;
        var cutCodepoint = src[cut].CodepointBegin;
        var fresh = this.Scratch.Fresh;
        fresh.Clear();

        var lb = this.items.Count;
        while (lb > lineItemBegin && this.items[lb - 1].Segment == segment)
            lb--;
        if (lb < this.items.Count)
        {
            this.ShapeSegment(this.items[lb].CodepointBegin, cutCodepoint, fresh);
            var x = this.items[lb].X;
            this.items.RemoveRange(lb, this.items.Count - lb);
            foreach (var item in fresh)
            {
                this.items.Add(item with { X = x });
                x += item.Advance;
            }
        }

        var re = cut + 1;
        while (re < src.Count && src[re].Segment == segment && src[re].UnsafeBefore)
            re++;
        var original = src[re - 1];
        fresh.Clear();
        this.ShapeSegment(cutCodepoint, original.CodepointEnd, fresh);
        if (fresh.Count > 0 && re < src.Count)
        {
            var lastFresh = fresh[^1];
            if (src[re].Segment != segment)
            {
                this.KernBoundary(ref lastFresh, src[re]);
            }
            else if (this.SameGlyphs(lastFresh, original))
            {
                // Preserve the intra-segment kerning toward src[re] that was baked into original.Advance before splitting.
                lastFresh.Kern = (original.Advance - original.Kern) - lastFresh.Advance;
                lastFresh.Advance += lastFresh.Kern;
            }

            fresh[^1] = lastFresh;
        }

        src.RemoveRange(cut, re - cut);
        src.InsertRange(cut, fresh);
    }

    private bool SameGlyphs(in Item a, in Item b)
    {
        var count = a.GlyphEnd - a.GlyphBegin;
        if (a.CodepointBegin != b.CodepointBegin || a.CodepointEnd != b.CodepointEnd ||
            count != b.GlyphEnd - b.GlyphBegin)
        {
            return false;
        }

        var glyphSpan = CollectionsMarshal.AsSpan(this.glyphs);
        for (var k = 0; k < count; k++)
        {
            if (glyphSpan[a.GlyphBegin + k].Id != glyphSpan[b.GlyphBegin + k].Id)
                return false;
        }

        return true;
    }
}
