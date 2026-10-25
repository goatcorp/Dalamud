using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;

using Dalamud.Bindings.ImGui;
using Dalamud.Utility.Unicode;

namespace Dalamud.Interface.Fonts.Internal.Layout;

/// <content>Carets, hit testing, and navigation.</content>
internal sealed partial class TextLayout
{
    /// <summary>Gets the caret at an offset.</summary>
    /// <param name="textOffset">The offset, in code units.</param>
    /// <param name="preferPreviousLine">At a soft wrap boundary, place the caret at the end of the preceding line
    /// instead of the start of the next.</param>
    /// <returns>The caret.</returns>
    public ImTextCaret GetCaret(int textOffset, bool preferPreviousLine = false) =>
        this.MakeCaret(textOffset, this.LineOf(textOffset, preferPreviousLine));

    /// <summary>Gets the caret nearest to a point relative to the layout origin, with the run or object under it.
    /// </summary>
    /// <param name="point">Point relative to the layout origin.</param>
    /// <returns>The caret.</returns>
    public ImTextCaret HitTest(Vector2 point)
    {
        var under = this.ItemUnder(point, out var lineIndex, out var inside);
        var line = this.lines[lineIndex];
        var offset = this.OffsetInLine(line, point.X);
        if (offset < 0)
            offset = Math.Max(line.TextEnd, line.TextNext == line.TextBegin ? line.TextEnd : this.LineEndOffset(line));

        var caret = this.MakeCaret(offset, lineIndex);
        caret.IsInside = inside;
        caret.UserData = 0;
        if (under >= 0)
        {
            ref readonly var item = ref this.ItemAt(under);
            caret.RunIndex = item.Run;
            caret.ObjectIndex = item.Object;
            caret.UserData = this.UserDataOf(item.Run, item.Object);
        }

        return caret;
    }

    /// <summary>Moves a caret.</summary>
    /// <remarks>Moves along the block axis first (<paramref name="dy"/> lines, or the first or last line), then to a
    /// place on that line, then <paramref name="dx"/> units along the text.</remarks>
    /// <param name="from">The caret to move from.</param>
    /// <param name="flags">Where to move, and the unit of <paramref name="dx"/>.</param>
    /// <param name="dx">Units along the text, after the axes.</param>
    /// <param name="dy">Lines, for <see cref="ImTextMoveFlags.BlockRelative"/>.</param>
    /// <param name="preferredInlinePos">Position along the line to keep, or &lt; 0 for the caret's.</param>
    /// <returns>The caret.</returns>
    public ImTextCaret Move(in ImTextCaret from, ImTextMoveFlags flags, int dx, int dy, float preferredInlinePos = -1)
    {
        var block = flags & ImTextMoveFlags.BlockMask;
        var inline = flags & ImTextMoveFlags.InlineMask;
        int lineIndex;
        int target;
        if (block == ImTextMoveFlags.BlockRelative && dy == 0 && inline == ImTextMoveFlags.InlineKeep)
        {
            // Not moving along the axes, so start from the caret itself, keeping its side of a wrap point.
            lineIndex = Math.Clamp(from.LineIndex, 0, this.lines.Count - 1);
            target = from.TextOffset;
        }
        else
        {
            lineIndex = block switch
            {
                ImTextMoveFlags.BlockFirst => 0,
                ImTextMoveFlags.BlockLast => this.lines.Count - 1,
                _ => from.LineIndex + dy,
            };
            if (lineIndex < 0 || lineIndex >= this.lines.Count)
            {
                var past = lineIndex >= this.lines.Count;
                lineIndex = past ? this.lines.Count - 1 : 0;
                if ((flags & ImTextMoveFlags.BlockClampKeepInline) == 0)
                    inline = past ? ImTextMoveFlags.InlineEnd : ImTextMoveFlags.InlineBegin;
            }

            var line = this.lines[lineIndex];
            switch (inline)
            {
                case ImTextMoveFlags.InlineBegin:
                    target = line.TextBegin;
                    break;
                case ImTextMoveFlags.InlineEnd:
                    target = this.LineEndOffset(line);
                    break;
                case ImTextMoveFlags.InlineKeep:
                default:
                    target = this.OffsetInLine(line, preferredInlinePos >= 0 ? preferredInlinePos : from.InlinePos);
                    if (target < 0)
                        target = this.LineEndOffset(line);
                    break;
            }
        }

        if (dx == 0)
            return this.MakeCaret(target, lineIndex);

        var offset = this.StepUnits(target, flags & ImTextMoveFlags.UnitMask, dx);
        if ((flags & ImTextMoveFlags.DxWithinLine) != 0)
        {
            var line = this.lines[lineIndex];
            var end = this.LineEndOffset(line);
            return this.MakeCaret(Math.Clamp(offset, line.TextBegin, end), lineIndex);
        }

        return this.MakeCaret(offset, this.LineOf(offset, false));
    }

    /// <summary>Finds the item under <paramref name="point"/> if its start offset falls within
    /// <paramref name="textBegin"/> to <paramref name="textEnd"/>.</summary>
    /// <param name="point">The point, relative to the layout origin.</param>
    /// <param name="textBegin">Start offset of the range, in code units.</param>
    /// <param name="textEnd">End offset of the range, in code units.</param>
    /// <param name="runIndex">The run of the item, or <c>-1</c>.</param>
    /// <param name="userData">The user data of the item's object or run, or <c>0</c>.</param>
    /// <returns>Whether the point is inside an item in the range.</returns>
    /// <remarks>Uses the same hit-test bounds as <see cref="HitTest"/> without constructing a caret.</remarks>
    internal bool TryGetItemAt(Vector2 point, int textBegin, int textEnd, out int runIndex, out ulong userData)
    {
        (runIndex, userData) = (-1, 0);
        var under = this.ItemUnder(point, out _, out var inside);
        if (!inside)
            return false;

        ref readonly var item = ref this.ItemAt(under);
        if (!this.InRange(item, textBegin, textEnd))
            return false;

        (runIndex, userData) = (item.Run, this.UserDataOf(item.Run, item.Object));
        return true;
    }

    // Letters, marks, numbers and connectors such as '_'. Punctuation, symbols and spaces of all scripts are not.
    private static bool IsWordChar(uint c) =>
        c < 0x80
            ? c is >= '0' and <= '9' or >= 'a' and <= 'z' or >= 'A' and <= 'Z' or '_'
            : c <= 0x10FFFF && UnicodeData.GeneralCategory[(int)c] is UnicodeGeneralCategory.Lu or
                  UnicodeGeneralCategory.Ll or UnicodeGeneralCategory.Lt or UnicodeGeneralCategory.Lm or
                  UnicodeGeneralCategory.Lo or UnicodeGeneralCategory.Mn or UnicodeGeneralCategory.Mc or
                  UnicodeGeneralCategory.Me or UnicodeGeneralCategory.Nd or UnicodeGeneralCategory.Nl or
                  UnicodeGeneralCategory.No or UnicodeGeneralCategory.Pc;

    // Inline position of the grapheme boundary at or before 'offset', which is within the item.
    private float InlinePosInItem(in Item item, int offset)
    {
        var n = this.GraphemeCount(item);
        if (n <= 1)
            return item.X;
        var k = -1;
        for (var codepoint = item.CodepointBegin; codepoint < item.CodepointEnd; codepoint++)
        {
            if (!this.IsGraphemeStart(codepoint))
                continue;
            if (this.runeOffsets[codepoint] > offset)
                break;
            k++;
        }

        return item.X + (((item.Advance - item.Kern) * Math.Max(k, 0)) / n);
    }

    // Offset of the grapheme boundary within the item nearest to 'u', or -1 if 'u' is past the middle of its last
    // grapheme.
    private int OffsetInItem(in Item item, float u)
    {
        var n = item.Kind == ItemKind.Ellipsis ? 1 : this.GraphemeCount(item);
        if (n <= 1)
            return u < item.X + (item.Advance * 0.5f) ? this.OffsetOf(item) : -1;
        var w = (item.Advance - item.Kern) / n;
        var k = 0;
        for (var codepoint = item.CodepointBegin; codepoint < item.CodepointEnd; codepoint++)
        {
            if (!this.IsGraphemeStart(codepoint))
                continue;
            if (u < item.X + (w * (k + 0.5f)))
                return this.runeOffsets[codepoint];
            k++;
        }

        return -1;
    }

    // Offset of the grapheme boundary on the line nearest to 'u', or -1 if 'u' is past the middle of its last grapheme.
    private int OffsetInLine(in Line line, float u)
    {
        for (var k = line.ItemBegin; k < line.ItemEnd; k++)
        {
            ref readonly var item = ref this.ItemAt(k);
            if (item.Kind == ItemKind.Newline)
                break;
            var offset = this.OffsetInItem(item, u);
            if (offset >= 0)
                return offset;
        }

        return -1;
    }

    // The item under a point, or -1. The line is the one the point is in along the block axis, or the first or last
    // line. Only the inline axis is tested against the item. 'inside' also requires the point to be within the line.
    private int ItemUnder(Vector2 point, out int lineIndex, out bool inside)
    {
        var lineSpan = CollectionsMarshal.AsSpan(this.lines);
        lineIndex = 0;
        while (lineIndex < lineSpan.Length - 1 &&
               point.Y >= lineSpan[lineIndex].BlockPos + lineSpan[lineIndex].BlockSize)
        {
            lineIndex++;
        }

        ref readonly var line = ref lineSpan[lineIndex];
        var under = -1;
        for (var k = line.ItemBegin; k < line.ItemEnd; k++)
        {
            ref readonly var item = ref this.ItemAt(k);
            if (item.Kind == ItemKind.Newline)
                break;
            if (point.X >= item.X && point.X < item.X + item.Advance)
                under = k;
        }

        inside = under >= 0 && point.X >= 0 && point.Y >= line.BlockPos && point.Y < line.BlockPos + line.BlockSize;
        return under;
    }

    // End offset for carets on a line, before the newline on newline-terminated lines or after the last item otherwise.
    private int LineEndOffset(in Line line)
    {
        if (line.ItemEnd <= line.ItemBegin)
            return line.TextBegin;
        ref readonly var last = ref this.ItemAt(line.ItemEnd - 1);
        return Math.Max(line.TextEnd, last.Kind == ItemKind.Newline ? line.TextEnd : this.EndOf(last));
    }

    private float InlinePosAt(in Line line, int offset)
    {
        for (var i = line.ItemBegin; i < line.ItemEnd; i++)
        {
            ref readonly var item = ref this.ItemAt(i);
            if (item.Kind == ItemKind.Ellipsis || offset <= this.OffsetOf(item))
                return item.X;
            if (offset < this.EndOf(item))
                return this.InlinePosInItem(item, offset);
        }

        return line.FullWidth;
    }

    private int LineOf(int offset, bool preferPreviousLine)
    {
        for (var n = 0; n + 1 < this.lines.Count; n++)
        {
            var next = this.lines[n + 1].TextBegin;
            if (offset < next || (preferPreviousLine && !this.lines[n].EndsWithNewline && offset == next))
                return n;
        }

        return this.lines.Count - 1;
    }

    private ImTextCaret MakeCaret(int offset, int lineIndex)
    {
        var line = this.lines[lineIndex];
        var lineEnd = this.LineEndOffset(line);
        offset = Math.Clamp(offset, line.TextBegin, Math.Max(line.TextNext, lineEnd));
        // Wrapped lines place the end-of-line caret at FullWidth after trailing blanks, while newline lines stop at Width.
        var inlinePos = offset >= lineEnd && !line.EndsWithNewline ? line.FullWidth : this.InlinePosAt(line, offset);
        if (line.EndsWithNewline && offset >= line.TextEnd)
            inlinePos = line.Width;
        var caret = new ImTextCaret
        {
            Pos = new(inlinePos, line.BlockPos),
            Length = line.BlockSize,
            InlinePos = inlinePos,
            TextOffset = offset,
            LineIndex = lineIndex,
            IsPreviousLine = lineIndex + 1 < this.lines.Count && !line.EndsWithNewline &&
                             offset == this.lines[lineIndex + 1].TextBegin,
            RunIndex = -1,
            ObjectIndex = -1,
        };
        for (var i = line.ItemBegin; i < line.ItemEnd; i++)
        {
            if (this.OffsetOf(this.ItemAt(i)) >= offset || i + 1 == line.ItemEnd)
            {
                ref readonly var item =
                    ref this.ItemAt(this.OffsetOf(this.ItemAt(i)) > offset && i > line.ItemBegin ? i - 1 : i);
                caret.RunIndex = item.Run;
                caret.ObjectIndex = item.Object;
                break;
            }
        }

        caret.UserData = this.UserDataOf(caret.RunIndex, caret.ObjectIndex);
        return caret;
    }

    // Steps by one codepoint or grapheme, skipping consecutive codepoints that share a source offset.
    private int Step(int offset, CaretDirection direction, ImTextMoveFlags unit)
    {
        Debug.Assert(unit is ImTextMoveFlags.Codepoint or ImTextMoveFlags.Grapheme, "Unit must be a codepoint or a grapheme.");
        var grapheme = unit == ImTextMoveFlags.Grapheme;
        var index = this.CodepointIndex(offset);
        if (direction == CaretDirection.Forward)
        {
            do
                index++;
            while (index < this.runeCount &&
                   (this.runeOffsets[index] <= offset || (grapheme && !this.IsGraphemeStart(index))));
            index = Math.Min(index, this.runeCount);
        }
        else
        {
            while (index > 0 && (this.runeOffsets[index] >= offset || (grapheme && !this.IsGraphemeStart(index))))
                index--;
        }

        return this.runeOffsets[index];
    }

    // 'count' units along the text from 'offset' (sign = direction), within the laid out text. Stops where a step does
    // not move.
    private int StepUnits(int offset, ImTextMoveFlags unit, int count)
    {
        var textLength = this.TextLength;
        var direction = count < 0 ? CaretDirection.Backward : CaretDirection.Forward;
        for (var n = Math.Abs((long)count); n > 0; n--)
        {
            var before = offset;
            switch (unit)
            {
                case ImTextMoveFlags.Codepoint:
                case ImTextMoveFlags.Grapheme:
                    offset = this.Step(offset, direction, unit);
                    break;
                case ImTextMoveFlags.WordStart:
                case ImTextMoveFlags.WordEnd:
                default:
                    // WordStart stops at the start of a word and WordEnd at the end of one, in either direction.
                    var forward = direction == CaretDirection.Forward;
                    var firstWord = (unit != ImTextMoveFlags.WordStart) == forward;
                    if (forward)
                    {
                        offset = this.SkipForward(offset, textLength, !firstWord);
                        offset = this.SkipForward(offset, textLength, firstWord);
                    }
                    else
                    {
                        offset = this.SkipBackward(offset, !firstWord);
                        offset = this.SkipBackward(offset, firstWord);
                    }

                    break;
            }

            if (offset == before)
                break;
        }

        return Math.Clamp(offset, 0, textLength);
    }

    private int SkipForward(int offset, int textLength, bool skipWord)
    {
        while (offset < textLength && IsWordChar(this.CharAt(offset)) == skipWord)
        {
            var next = this.Step(offset, CaretDirection.Forward, ImTextMoveFlags.Grapheme);
            if (next <= offset)
                break;
            offset = next;
        }

        return offset;
    }

    private int SkipBackward(int offset, bool skipWord)
    {
        while (offset > 0)
        {
            var previous = this.Step(offset, CaretDirection.Backward, ImTextMoveFlags.Grapheme);
            if (previous >= offset || IsWordChar(this.CharAt(previous)) != skipWord)
                break;
            offset = previous;
        }

        return offset;
    }

    // First codepoint mapped to offset, since multiple codepoints may share a source offset.
    private uint CharAt(int offset)
    {
        var index = this.CodepointIndex(offset);
        while (index > 0 && this.runeOffsets[index - 1] == this.runeOffsets[index])
            index--;
        return index < this.runeCount ? this.runes[index] : 0;
    }
}
