using System.Collections.Generic;
using System.Runtime.InteropServices;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Fonts.Internal.Sources;

namespace Dalamud.Interface.Fonts.Internal.Layout;

/// <summary>Line breaking.</summary>
internal sealed partial class TextLayout
{
    // Lines from the items, each filled, measured, truncated if it must be, and placed below the one before.
    private void BreakLines(List<Item> src)
    {
        var maxWidth = this.desc.MaxWidth > 0 ? this.desc.MaxWidth : float.MaxValue;
        var useEllipsis = (this.desc.Flags & ImTextLayoutFlags.Ellipsis) != 0 && maxWidth < float.MaxValue;
        var stopWidth = useEllipsis ? float.MaxValue : maxWidth;
        var ellipsisCount = 0;
        var ellipsisItem = useEllipsis ? this.MakeEllipsis(out ellipsisCount) : default;
        this.TextLength = this.textBytes;
        var blockPos = 0f;
        var i = 0;
        while (true)
        {
            var line = new Line
                { ItemBegin = this.items.Count, TextBegin = i < src.Count ? this.OffsetOf(src[i]) : this.textBytes };
            line.EndsWithNewline = this.FillLine(src, ref i, line.ItemBegin, stopWidth, out var stopped);

            // Kerning towards the next line does not apply.
            this.DropLastKern(line.ItemBegin);
            line.ItemEnd = this.items.Count;
            this.MeasureLine(ref line, src, i);
            if (useEllipsis && line.Width > maxWidth)
                this.Truncate(ref line, ellipsisItem, ellipsisCount, maxWidth);
            this.PlaceLine(ref line, ref blockPos);

            if (stopped || ((this.desc.Flags & ImTextLayoutFlags.StopOnNewLine) != 0 &&
                            (line.EndsWithNewline || i < src.Count)))
            {
                if (!stopped)
                    this.TextLength = line.TextNext;
                break;
            }

            if (i >= src.Count && !line.EndsWithNewline)
                break;
        }

        var inlineMax = 0f;
        foreach (ref readonly var l in CollectionsMarshal.AsSpan(this.lines))
            inlineMax = Math.Max(inlineMax, l.Extent);
        this.Size = new(inlineMax, blockPos);
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
                this.TextLength = this.OffsetOf(item);
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

    // Takes the kerning towards what follows off the last item of the line, if any.
    private void DropLastKern(int lineItemBegin)
    {
        if (this.items.Count <= lineItemBegin)
            return;
        ref var last = ref this.ItemAt(this.items.Count - 1);
        last.Advance -= last.Kern;
        last.Kern = 0;
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
            line.TextEnd = this.EndOf(last);
            line.Width = (last.X + last.Advance) - (trailing < endItem ? last.Kern : 0);
        }

        // The text of a CRLF line ends at the LF, as the line loops of InputText step past a '\n' only.
        if (line.EndsWithNewline && (keepBlanks || crlf))
            line.TextEnd = this.OffsetOf(this.ItemAt(line.ItemEnd - 1));
        line.Extent = this.ExtentOf(line.ItemBegin, trailing);
        line.TextNext = next < src.Count ? this.OffsetOf(src[next]) : this.textBytes;
        if (line.EndsWithNewline)
            line.TextNext = this.EndOf(this.ItemAt(line.ItemEnd - 1));
    }

    // Truncates the line to maxWidth, including the ellipsis after what is kept.
    private void Truncate(ref Line line, in Item ellipsisItem, int ellipsisCount, float maxWidth)
    {
        var ellipsisAdvance = ellipsisItem.Advance;
        var endItem = line.ItemEnd - (line.EndsWithNewline ? 1 : 0);
        var keep = line.ItemBegin;
        while (keep < endItem &&
               this.ItemAt(keep).X + this.ItemAt(keep).Advance + (ellipsisAdvance * ellipsisCount) <= maxWidth)
            keep++;
        var cutCodepoint = keep < endItem ? this.ItemAt(keep).CodepointBegin : this.CodepointIndex(line.TextEnd);
        var cutOffset = this.runeOffsets[cutCodepoint];
        if (this.EllipsisOffset < 0)
            this.EllipsisOffset = cutOffset;
        var ex = keep > line.ItemBegin
                     ? (this.ItemAt(keep - 1).X + this.ItemAt(keep - 1).Advance) - this.ItemAt(keep - 1).Kern
                     : 0;

        // The ellipsis is drawn in the style of the text it replaces.
        var run = keep < endItem ? this.ItemAt(keep).Run : keep > line.ItemBegin ? this.ItemAt(keep - 1).Run : -1;
        this.items.RemoveRange(keep, this.items.Count - keep);
        this.DropLastKern(line.ItemBegin);
        for (var e = 0; e < ellipsisCount; e++)
        {
            this.items.Add(
                ellipsisItem with { CodepointBegin = cutCodepoint, CodepointEnd = cutCodepoint, X = ex, Run = run });
            ex += ellipsisAdvance;
        }

        line.ItemEnd = this.items.Count;
        line.TextEnd = cutOffset;
        line.Width = line.FullWidth = ex;
        line.Extent = this.ExtentOf(line.ItemBegin, line.ItemEnd);
    }

    // How far items reach from the line start, with the ink they draw past their advances. Game fonts often draw past
    // the advance, and ImGui clips to the size it is told.
    private float ExtentOf(int itemBegin, int itemEnd)
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
        var m = this.MetricsOf(line.ItemEnd > line.ItemBegin ? this.ItemAt(line.ItemBegin).FontIndex : 0);
        line.BlockSize = FontMetrics.BlockSize(this.desc.LineHeight, this.desc.Size);
        // Objects grow the room above and below the baseline apart, as one may go above the text and another below.
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

    // A line ends before src[cut]. Where shaping across the cut is unsafe, shapes the segment's part at the end of
    // items, and from src[cut] up to the next item that is safe to break before, again, each without the other. Shaping
    // stops there, as the rest shapes the same either way. The part on the line may come out a little wider,
    // such as where kerning across the break no longer applies. It is not checked against the wrap width again. It
    // passes it by little, and only in fonts that shape across breaks.
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
        fresh.Clear();
        this.ShapeSegment(cutCodepoint, src[re - 1].CodepointEnd, fresh);
        if (fresh.Count > 0 && re < src.Count && src[re].Segment != segment)
        {
            var lastFresh = fresh[^1];
            this.KernBoundary(ref lastFresh, src[re]);
            fresh[^1] = lastFresh;
        }

        src.RemoveRange(cut, re - cut);
        src.InsertRange(cut, fresh);
    }
}
