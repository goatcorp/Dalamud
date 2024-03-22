using System.Collections;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Unicode;

using Dalamud.Interface.Spannables.EventHandlerArgs;
using Dalamud.Interface.Spannables.Internal;
using Dalamud.Interface.Spannables.Rendering;
using Dalamud.Interface.Spannables.Styles;
using Dalamud.Utility;
using Dalamud.Utility.Numerics;
using Dalamud.Utility.Text;

using ImGuiNET;

namespace Dalamud.Interface.Spannables.Strings;

/// <summary>Base class for <see cref="SpannedString"/> and <see cref="SpannedStringBuilder"/>.</summary>
public abstract partial class SpannedStringBase : ISpannable
{
    private static readonly BitArray WordBreakNormalBreakChars;

    static SpannedStringBase()
    {
        // Initialize which characters will make a valid word break point.

        WordBreakNormalBreakChars = new(char.MaxValue + 1);

        // https://en.wikipedia.org/wiki/Whitespace_character
        foreach (var c in
                 "\t\n\v\f\r\x20\u0085\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2008\u2009\u200A\u2028\u2029\u205F\u3000\u180E\u200B\u200C\u200D")
            WordBreakNormalBreakChars[c] = true;

        foreach (var range in new[]
                 {
                     UnicodeRanges.HangulJamo,
                     UnicodeRanges.HangulSyllables,
                     UnicodeRanges.HangulCompatibilityJamo,
                     UnicodeRanges.HangulJamoExtendedA,
                     UnicodeRanges.HangulJamoExtendedB,
                     UnicodeRanges.CjkCompatibility,
                     UnicodeRanges.CjkCompatibilityForms,
                     UnicodeRanges.CjkCompatibilityIdeographs,
                     UnicodeRanges.CjkRadicalsSupplement,
                     UnicodeRanges.CjkSymbolsandPunctuation,
                     UnicodeRanges.CjkStrokes,
                     UnicodeRanges.CjkUnifiedIdeographs,
                     UnicodeRanges.CjkUnifiedIdeographsExtensionA,
                     UnicodeRanges.Hiragana,
                     UnicodeRanges.Katakana,
                     UnicodeRanges.KatakanaPhoneticExtensions,
                 })
        {
            for (var i = 0; i < range.Length; i++)
                WordBreakNormalBreakChars[range.FirstCodePoint + i] = true;
        }
    }

    /// <inheritdoc/>
    public ISpannableState RentState(scoped in SpannableRentStateArgs args) =>
        State.Rent(args, this.GetData());

    /// <inheritdoc/>
    public void ReturnState(ISpannableState? state)
    {
        if (state is State s)
            State.Return(s, this.GetData());
    }

    /// <inheritdoc/>
    public void MeasureSpannable(scoped in SpannableMeasureArgs args)
    {
        var state = args.State as State ?? new();
        state.Offset = Vector2.Zero;
        state.TextState.LastStyle = state.TextState.InitialStyle;
        state.MaxSize = args.MaxSize;
        state.ClearBoundary();

        var data = this.GetData();
        var segment = new DataRef.Segment(data, 0, 0);
        var linkRecordIndex = -1;

        var drawArgs = new SpannableDrawArgs(state, default, default);
        var charRenderer = new CharRenderer(drawArgs, data, state, true);
        var skipNextLine = false;
        while (true)
        {
            state.FindFirstWordWrapByteOffset(args, segment, new(segment), out var line);
            line.FirstOffset = new(segment);
            var lineSegment = new DataRef.Segment(data, line.Offset.Text, line.Offset.Record);

            // If wrapped, then omit the ending whitespaces.
            if (line.IsWrapped)
            {
                var searchTarget = data.TextStream[segment.Offset.Text..line.Offset.Text];
                var nbTrim = 0;
                var minOffset = line.Offset.Text;
                var prevSegment = lineSegment;
                while (prevSegment.TryGetPrevious(out prevSegment))
                {
                    if (prevSegment.TryGetRawText(out var text))
                        minOffset -= text.Length;
                    else if (!prevSegment.TryGetRecord(out var prevRec, out _) || prevRec.Type.IsObject())
                        break;
                }

                while (!searchTarget.IsEmpty)
                {
                    if (!UtfValue.TryDecode8(searchTarget[^1..], out var v, out var len)
                        && !UtfValue.TryDecode8(searchTarget[^2..], out v, out len)
                        && !UtfValue.TryDecode8(searchTarget[^3..], out v, out len)
                        && !UtfValue.TryDecode8(searchTarget[^4..], out v, out len))
                        break;
                    if (!v.TryGetRune(out var rune))
                        break;
                    if (!Rune.IsWhiteSpace(rune))
                        break;
                    if (line.Offset.Text - nbTrim - len < minOffset)
                        break;
                    nbTrim += len;
                    searchTarget = searchTarget[..^len];
                }

                line.OmitOffset = line.Offset.AddTextOffset(-nbTrim);
            }
            else
            {
                line.OmitOffset = line.Offset;
            }

            if (!skipNextLine)
            {
                state.AddLine(line);
                charRenderer.SetLine(line);

                var accumulatedBoundary = RectVector4.InvertedExtrema;

                for (var seg = segment; seg.Offset < line.Offset;)
                {
                    if (seg.TryGetRawText(out var rawText))
                    {
                        foreach (var c in rawText.EnumerateUtf(UtfEnumeratorFlags.Utf8))
                        {
                            var absOffset = new CompositeOffset(seg, c.ByteOffset);
                            if (absOffset.Text < seg.Offset.Text)
                                continue;
                            if (absOffset.Text >= line.OmitOffset.Text)
                                break;

                            if (state.TextState.UseControlCharacter)
                            {
                                var name = c.Value.ShortName;
                                if (!name.IsEmpty)
                                {
                                    var offset = charRenderer.StyleTranslation;
                                    state.Offset += offset;
                                    var old = charRenderer.UpdateSpanParams(
                                        state.TextState.ControlCharactersStyle);
                                    charRenderer.LastRendered.Clear();
                                    foreach (var c2 in name)
                                        charRenderer.RenderOne(c2);
                                    charRenderer.LastRendered.Clear();
                                    _ = charRenderer.UpdateSpanParams(old);
                                    state.Offset -= offset;
                                }
                            }

                            accumulatedBoundary = RectVector4.Union(
                                accumulatedBoundary,
                                charRenderer.RenderOne(c.EffectiveChar));
                            charRenderer.LastRendered.SetCodepoint(c.Value);
                        }
                    }
                    else if (seg.TryGetRecord(out var record, out var recordData))
                    {
                        switch (record.Type)
                        {
                            case SpannedRecordType.Link when record.IsRevert:
                                state.UpdateAndResetBoundary(ref accumulatedBoundary, linkRecordIndex);
                                linkRecordIndex = -1;
                                break;

                            case SpannedRecordType.Link when SpannedRecordCodec.TryDecodeLink(recordData, out var link):
                                state.UpdateAndResetBoundary(ref accumulatedBoundary, linkRecordIndex);
                                linkRecordIndex = record.IsRevert || link.IsEmpty ? -1 : seg.Offset.Record;
                                break;
                        }

                        accumulatedBoundary = RectVector4.Union(
                            accumulatedBoundary,
                            charRenderer.HandleSpan(record, recordData));
                    }

                    if (!seg.TryGetNext(out seg))
                        break;
                }

                accumulatedBoundary = RectVector4.Union(
                    accumulatedBoundary,
                    state.ProcessPostLine(line, ref charRenderer, default));

                state.UpdateAndResetBoundary(ref accumulatedBoundary, linkRecordIndex);
                state.ExtendBoundaryBottom(state.Offset.Y + charRenderer.MostRecentLineHeight);
            }
            else
            {
                for (var seg = segment; seg.Offset < line.OmitOffset;)
                {
                    if (seg.TryGetRecord(out var record, out var recordData))
                    {
                        charRenderer.HandleSpan(record, recordData);
                    }

                    if (!seg.TryGetNext(out seg))
                        break;
                }

                if (line.HasNewLineAtEnd || (line.IsWrapped && state.TextState.WordBreak != WordBreakType.KeepAll))
                    state.BreakLineImmediate(line);
            }

            if (lineSegment.Offset == data.EndOffset)
                break;
            segment = lineSegment;
            if (state.TextState.WordBreak == WordBreakType.KeepAll)
            {
                if (skipNextLine && !line.IsWrapped)
                    state.MeasuredLines[^1].HasNewLineAtEnd = true;
                skipNextLine = line.IsWrapped;
            }
        }

        state.TextState.LineCount = state.MeasuredLines.Length;
        state.ExtendBoundaryBottom(state.Offset.Y);

#pragma warning disable SA1101
        if (state.TextState is { VerticalAlignment: > 0f } && args.MaxSize.Y < float.MaxValue)
#pragma warning restore SA1101
        {
            var offset = MathF.Round(
                (args.MaxSize.Y - state.Boundary.Bottom) *
                Math.Clamp(state.TextState.VerticalAlignment, 0f, 1f));
            state.TranslateBoundaries(new(0, offset), data);
            state.TextState.ShiftFromVerticalAlignment = offset;
        }
        else
        {
            state.TextState.ShiftFromVerticalAlignment = 0;
        }
    }

    /// <inheritdoc/>
    public void CommitSpannableMeasurement(scoped in SpannableCommitTransformationArgs args)
    {
        var state = args.State as State ?? new();
        var data = this.GetData();

        state.CommitMeasurement(args);

        for (var i = 0; i < data.Spannables.Length; i++)
        {
            if (state.SpannableStates[i] is not { } spannableState)
                continue;

            args.NotifyChild(
                data.Spannables[i],
                spannableState,
                state.SpannableOffsets[i],
                spannableState.TextState.LastStyle.Italic
                    ? Trss.CreateSkew(new(MathF.Atan(-1 / TextStyleFontData.FakeItalicDivisor), 0))
                    : Trss.Identity);
        }
    }

    /// <inheritdoc/>
    public unsafe void HandleSpannableInteraction(scoped in SpannableHandleInteractionArgs args, out SpannableLinkInteracted link)
    {
        var state = args.State as State ?? new();
        var data = this.GetData();

        for (var i = 0; i < data.Spannables.Length; i++)
        {
            if (state.SpannableStates[i] is not { } spannableState)
                continue;
            args.NotifyChild(data.Spannables[i], spannableState, out link);
            if (!link.IsEmpty)
                return;
        }

        link = default;

        var mouseRel = args.MouseLocalLocation;
        
        state.InteractedLinkRecordIndex = -1;
        state.IsInteractedLinkRecordActive = false;
        link = default;
        foreach (ref var entry in state.LinkBoundaries)
        {
            if (entry.Boundary.Contains(mouseRel) && args.IsItemHoverable(entry.RecordIndex))
            {
                if (this.GetData().TryGetLinkAt(entry.RecordIndex, out link.Link))
                    state.InteractedLinkRecordIndex = entry.RecordIndex;
                else
                    state.InteractedLinkRecordIndex = -1;
        
                break;
            }
        }
        
        var prevLinkRecordIndex = -1;
        foreach (ref var linkBoundary in state.LinkBoundaries)
        {
            if (prevLinkRecordIndex == linkBoundary.RecordIndex)
                continue;
            prevLinkRecordIndex = linkBoundary.RecordIndex;
        
            ref var itemState = ref *(ItemStateStruct*)ImGui.GetStateStorage().GetVoidPtrRef(
                                        args.State.GetGlobalIdFromInnerId(linkBoundary.RecordIndex),
                                        nint.Zero);
        
            if (itemState.IsMouseButtonDownHandled)
            {
                switch (itemState.FirstMouseButton)
                {
                    case var _ when linkBoundary.RecordIndex != state.InteractedLinkRecordIndex:
                        state.InteractedLinkRecordIndex = -1;
                        break;
                    case ImGuiMouseButton.Left when !args.IsMouseButtonDown(ImGuiMouseButton.Left):
                        link.IsMouseClicked = true;
                        link.ClickedMouseButton = ImGuiMouseButton.Left;
                        itemState.IsMouseButtonDownHandled = false;
                        break;
                    case ImGuiMouseButton.Right when !args.IsMouseButtonDown(ImGuiMouseButton.Right):
                        link.IsMouseClicked = true;
                        link.ClickedMouseButton = ImGuiMouseButton.Right;
                        itemState.IsMouseButtonDownHandled = false;
                        break;
                    case ImGuiMouseButton.Middle when !args.IsMouseButtonDown(ImGuiMouseButton.Middle):
                        link.IsMouseClicked = true;
                        link.ClickedMouseButton = ImGuiMouseButton.Middle;
                        itemState.IsMouseButtonDownHandled = false;
                        break;
                }
        
                if (args.MouseButtonStateFlags == 0)
                {
                    itemState.IsMouseButtonDownHandled = false;
                    args.ClearActive();
                }
            }
        
            if (state.InteractedLinkRecordIndex == linkBoundary.RecordIndex)
            {
                args.SetHovered(linkBoundary.RecordIndex);
                if (!itemState.IsMouseButtonDownHandled && args.TryGetAnyHeldMouseButton(out var heldButton))
                {
                    itemState.IsMouseButtonDownHandled = true;
                    itemState.FirstMouseButton = heldButton;
                }
        
                state.IsInteractedLinkRecordActive = itemState.IsMouseButtonDownHandled;
            }
        
            if (itemState.IsMouseButtonDownHandled)
            {
                args.SetHovered(linkBoundary.RecordIndex);
                args.SetActive(linkBoundary.RecordIndex);
            }
        }
        
        if (state.InteractedLinkRecordIndex == -1)
            link = default;
    }

    /// <inheritdoc/>
    public unsafe void DrawSpannable(SpannableDrawArgs args)
    {
        var state = args.State as State ?? new();
        var data = this.GetData();
        state.Offset = new(0, state.TextState.ShiftFromVerticalAlignment);
        state.TextState.LastStyle = state.TextState.InitialStyle;

        var charRenderer = new CharRenderer(args, data, state, false);
        var segment = new DataRef.Segment(data, 0, 0);
        foreach (ref readonly var line in state.MeasuredLines)
        {
            charRenderer.SetLine(line);

            while (segment.Offset < line.Offset)
            {
                if (segment.TryGetRawText(out var rawText))
                {
                    var lineHasMoreText = true;
                    foreach (var c in rawText.EnumerateUtf(UtfEnumeratorFlags.Utf8))
                    {
                        var absOffset = new CompositeOffset(segment, c.ByteOffset);
                        if (absOffset < line.FirstOffset)
                            continue;
                        if (absOffset >= line.Offset)
                        {
                            lineHasMoreText = false;
                            break;
                        }

                        if (absOffset < line.OmitOffset)
                        {
                            if (state.TextState.UseControlCharacter)
                            {
                                var name = c.Value.ShortName;
                                if (!name.IsEmpty)
                                {
                                    var offset = charRenderer.StyleTranslation;
                                    state.Offset += offset;
                                    var old = charRenderer.UpdateSpanParams(
                                        state.TextState.ControlCharactersStyle);
                                    charRenderer.LastRendered.Clear();
                                    foreach (var c2 in name)
                                        charRenderer.RenderOne(c2);
                                    charRenderer.LastRendered.Clear();
                                    _ = charRenderer.UpdateSpanParams(old);
                                    state.Offset -= offset;
                                }
                            }

                            charRenderer.RenderOne(c.EffectiveChar);
                        }

                        charRenderer.LastRendered.SetCodepoint(c.Value);
                    }

                    if (!lineHasMoreText)
                        break;
                }
                else if (segment.TryGetRecord(out var record, out var recordData))
                {
                    if (!record.Type.IsObject() || segment.Offset >= line.FirstOffset)
                    {
                        charRenderer.HandleSpan(record, recordData);
                    }
                }

                if (!segment.TryGetNext(out segment))
                    break;
            }

            state.ProcessPostLine(line, ref charRenderer, args);
        }

        if (state.InteractedLinkRecordIndex != -1)
        {
            args.SwitchToChannel(RenderChannel.BackChannel);

            var color =
                ImGui.GetColorU32(state.IsInteractedLinkRecordActive ? ImGuiCol.ButtonActive : ImGuiCol.ButtonHovered);

            foreach (var entry in state.LinkBoundaries)
            {
                if (entry.RecordIndex != state.InteractedLinkRecordIndex)
                    continue;

                ImGuiNative.ImDrawList_AddQuadFilled(
                    args.DrawListPtr,
                    state.TransformToScreen(entry.Boundary.LeftTop),
                    state.TransformToScreen(entry.Boundary.RightTop),
                    state.TransformToScreen(entry.Boundary.RightBottom),
                    state.TransformToScreen(entry.Boundary.LeftBottom),
                    color);
            }
        }
    }
    
    /// <summary>Gets the data required for rendering.</summary>
    /// <returns>The data.</returns>
    private protected abstract DataRef GetData();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsEffectivelyInfinity(float f) => f >= float.MaxValue;

    private ref struct StateInfo
    {
        public float HorizontalOffsetWrtLine;
        public float VerticalOffsetWrtLine;

        private readonly State state;

        private readonly Vector2 lineBBoxVertical;
        private readonly float lineWidth;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StateInfo(State state, scoped in MeasuredLine lineMeasurement)
        {
            this.state = state;
            this.lineBBoxVertical = lineMeasurement.BBoxVertical;
            this.lineWidth = lineMeasurement.Width;
        }

        public void Update(in TextStyleFontData fontInfo)
        {
            var lineAscentDescent = this.lineBBoxVertical;
            this.VerticalOffsetWrtLine = (fontInfo.BBoxVertical.Y - fontInfo.BBoxVertical.X) *
                                         this.state.TextState.LastStyle.VerticalOffset;
            switch (this.state.TextState.LastStyle.VerticalAlignment)
            {
                case < 0:
                    this.VerticalOffsetWrtLine -= lineAscentDescent.X + (fontInfo.Font.Ascent * fontInfo.Scale);
                    break;
                case >= 1f:
                    this.VerticalOffsetWrtLine += lineAscentDescent.Y - lineAscentDescent.X - fontInfo.ScaledFontSize;
                    break;
                default:
                    this.VerticalOffsetWrtLine +=
                        (lineAscentDescent.Y - lineAscentDescent.X - fontInfo.ScaledFontSize) *
                        this.state.TextState.LastStyle.VerticalAlignment;
                    break;
            }

            this.VerticalOffsetWrtLine = MathF.Round(this.VerticalOffsetWrtLine);

            var alignWidth = this.state.MaxSize.X;
            var alignLeft = 0f;
            if (IsEffectivelyInfinity(alignWidth))
            {
                if (!this.state.Boundary.IsValid)
                {
                    this.HorizontalOffsetWrtLine = 0;
                    return;
                }

                alignWidth = this.state.Boundary.Width;
                alignLeft = this.state.Boundary.Left;
            }

            switch (this.state.TextState.LastStyle.HorizontalAlignment)
            {
                case <= 0f:
                    this.HorizontalOffsetWrtLine = 0;
                    break;

                case >= 1f:
                    this.HorizontalOffsetWrtLine = alignLeft + (alignWidth - this.lineWidth);
                    break;

                default:
                    this.HorizontalOffsetWrtLine =
                        MathF.Round(
                            (alignLeft + (alignWidth - this.lineWidth)) *
                            this.state.TextState.LastStyle.HorizontalAlignment);
                    break;
            }
        }
    }

    private struct BoundaryToRecord
    {
        public RectVector4 Boundary;
        public int RecordIndex;

        public BoundaryToRecord(int recordIndex, RectVector4 boundary)
        {
            this.RecordIndex = recordIndex;
            this.Boundary = boundary;
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 8)]
    private struct ItemStateStruct
    {
        [FieldOffset(0)]
        public uint Flags;
    
        public bool IsMouseButtonDownHandled
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => (this.Flags & 1) != 0;
    
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => this.Flags = (this.Flags & ~1u) | (value ? 1u : 0u);
        }
    
        public ImGuiMouseButton FirstMouseButton
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => (ImGuiMouseButton)((this.Flags >> 1) & 3);
    
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => this.Flags = (this.Flags & ~(3u << 1)) | ((uint)value << 1);
        }
    }
}
