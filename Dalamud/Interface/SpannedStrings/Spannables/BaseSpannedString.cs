using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Unicode;

using Dalamud.Interface.SpannedStrings.Enums;
using Dalamud.Interface.SpannedStrings.Internal;
using Dalamud.Interface.SpannedStrings.Rendering;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using Dalamud.Utility.Text;

using ImGuiNET;

using Microsoft.Extensions.ObjectPool;

namespace Dalamud.Interface.SpannedStrings.Spannables;

/// <summary>Base class for <see cref="SpannedString"/> and <see cref="SpannedStringBuilder"/>.</summary>
public abstract partial class BaseSpannedString : ISpannable
{
    private static readonly BitArray WordBreakNormalBreakChars;

    static BaseSpannedString()
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
    public ISpannableState? RentState(ISpannableRenderer renderer, RenderState initialState, string? args) =>
        State.Rent(renderer, initialState, this.GetData());

    /// <inheritdoc/>
    public void ReturnState(ISpannableState? state)
    {
        if (state is State s)
            State.Return(s, this.GetData());
    }

    /// <inheritdoc/>
    public void Measure(SpannableMeasureArgs args)
    {
        var state = args.State as State ?? new();
        var data = this.GetData();
        var segment = new DataRef.Segment(data, 0, 0);
        var linkRecordIndex = -1;

        var drawArgs = new SpannableDrawArgs(state, default);
        var charRenderer = new CharRenderer(drawArgs, data, state, true);
        var skipNextLine = false;
        while (true)
        {
            state.FindFirstWordWrapByteOffset(segment, new(segment), out var line);
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

                            if (state.RenderState.UseControlCharacter)
                            {
                                var name = c.Value.ShortName;
                                if (!name.IsEmpty)
                                {
                                    var offset = charRenderer.StyleTranslation;
                                    state.RenderState.Offset += offset;
                                    var old = charRenderer.UpdateSpanParams(
                                        state.RenderState.ControlCharactersStyle);
                                    charRenderer.LastRendered.Clear();
                                    foreach (var c2 in name)
                                        charRenderer.RenderOne(c2);
                                    charRenderer.LastRendered.Clear();
                                    _ = charRenderer.UpdateSpanParams(old);
                                    state.RenderState.Offset -= offset;
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
                    state.ProcessPostLine(line, ref charRenderer, default, true));
                state.UpdateAndResetBoundary(ref accumulatedBoundary, linkRecordIndex);

                state.RenderState.Boundary.Bottom =
                    Math.Max(
                        state.RenderState.Boundary.Bottom,
                        state.RenderState.Offset.Y + charRenderer.MostRecentLineHeight);
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

                if (line.HasNewLineAtEnd || (line.IsWrapped && state.RenderState.WordBreak != WordBreakType.KeepAll))
                    state.BreakLineImmediate(line);
            }

            if (lineSegment.Offset == data.EndOffset)
                break;
            segment = lineSegment;
            if (state.RenderState.WordBreak == WordBreakType.KeepAll)
            {
                if (skipNextLine && !line.IsWrapped)
                    state.MeasuredLines[^1].HasNewLineAtEnd = true;
                skipNextLine = line.IsWrapped;
            }
        }

        state.RenderState.LineCount = state.MeasuredLines.Length;
        state.RenderState.Boundary.Bottom = Math.Max(state.RenderState.Boundary.Bottom, state.RenderState.Offset.Y);
    }

    /// <inheritdoc/>
    public unsafe void InteractWith(SpannableInteractionArgs args, out ReadOnlySpan<byte> linkData)
    {
        var state = args.State as State ?? new();

        var mouseRel = args.GetRelativeMouseCoord();
        var lmb = ImGui.IsMouseDown(ImGuiMouseButton.Left);
        var mmb = ImGui.IsMouseDown(ImGuiMouseButton.Middle);
        var rmb = ImGui.IsMouseDown(ImGuiMouseButton.Right);

        state.InteractedLinkRecordIndex = -1;
        state.IsInteractedLinkRecordActive = false;
        linkData = default;
        foreach (ref var entry in state.LinkBoundaries)
        {
            if (entry.Boundary.Contains(mouseRel) && args.IsItemHoverable(entry.RecordIndex))
            {
                if (this.GetData().TryGetLinkAt(entry.RecordIndex, out linkData))
                    state.InteractedLinkRecordIndex = entry.RecordIndex;
                else
                    state.InteractedLinkRecordIndex = -1;

                break;
            }
        }

        var prevLinkRecordIndex = -1;
        foreach (ref var link in state.LinkBoundaries)
        {
            if (prevLinkRecordIndex == link.RecordIndex)
                continue;
            prevLinkRecordIndex = link.RecordIndex;

            ref var itemState = ref *(ItemStateStruct*)ImGui.GetStateStorage().GetVoidPtrRef(
                                        args.GlobalIdFromInnerId(link.RecordIndex),
                                        nint.Zero);

            if (itemState.IsMouseButtonDownHandled)
            {
                switch (itemState.FirstMouseButton)
                {
                    case var _ when link.RecordIndex != state.InteractedLinkRecordIndex:
                        state.InteractedLinkRecordIndex = -1;
                        break;
                    case ImGuiMouseButton.Left when !lmb:
                        state.RenderState.ClickedMouseButton = ImGuiMouseButton.Left;
                        itemState.IsMouseButtonDownHandled = false;
                        break;
                    case ImGuiMouseButton.Right when !rmb:
                        state.RenderState.ClickedMouseButton = ImGuiMouseButton.Right;
                        itemState.IsMouseButtonDownHandled = false;
                        break;
                    case ImGuiMouseButton.Middle when !mmb:
                        state.RenderState.ClickedMouseButton = ImGuiMouseButton.Middle;
                        itemState.IsMouseButtonDownHandled = false;
                        break;
                }

                if (!lmb && !rmb && !mmb)
                {
                    itemState.IsMouseButtonDownHandled = false;
                    args.ClearActive();
                }
            }

            if (state.InteractedLinkRecordIndex == link.RecordIndex)
            {
                args.SetHovered(link.RecordIndex);
                if (!itemState.IsMouseButtonDownHandled && (lmb || rmb || mmb))
                {
                    itemState.IsMouseButtonDownHandled = true;
                    itemState.FirstMouseButton = lmb ? ImGuiMouseButton.Left :
                                                 rmb ? ImGuiMouseButton.Right : ImGuiMouseButton.Middle;
                }

                state.IsInteractedLinkRecordActive = itemState.IsMouseButtonDownHandled;
            }

            if (itemState.IsMouseButtonDownHandled)
            {
                args.SetHovered(link.RecordIndex);
                args.SetActive(link.RecordIndex);
            }
        }

        if (state.InteractedLinkRecordIndex == -1)
            linkData = default;
    }

    /// <inheritdoc/>
    public unsafe void Draw(SpannableDrawArgs args)
    {
        var state = args.State as State ?? new();
        var data = this.GetData();
        state.RenderState.Offset = Vector2.Zero;
        state.RenderState.LastStyle = state.RenderState.InitialStyle;

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
                            if (state.RenderState.UseControlCharacter)
                            {
                                var name = c.Value.ShortName;
                                if (!name.IsEmpty)
                                {
                                    var offset = charRenderer.StyleTranslation;
                                    state.RenderState.Offset += offset;
                                    var old = charRenderer.UpdateSpanParams(
                                        state.RenderState.ControlCharactersStyle);
                                    charRenderer.LastRendered.Clear();
                                    foreach (var c2 in name)
                                        charRenderer.RenderOne(c2);
                                    charRenderer.LastRendered.Clear();
                                    _ = charRenderer.UpdateSpanParams(old);
                                    state.RenderState.Offset -= offset;
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

            state.ProcessPostLine(line, ref charRenderer, args, false);
        }

        if (state.InteractedLinkRecordIndex != -1)
        {
            args.SwitchToChannel(RenderChannel.BackChannel);

            var color =
                ImGui.GetColorU32(state.IsInteractedLinkRecordActive ? ImGuiCol.ButtonActive : ImGuiCol.ButtonHovered);

            var sso = state.RenderState.StartScreenOffset;
            foreach (var entry in state.LinkBoundaries)
            {
                if (entry.RecordIndex != state.InteractedLinkRecordIndex)
                    continue;

                ImGuiNative.ImDrawList_AddQuadFilled(
                    state.RenderState.DrawListPtr,
                    sso + state.RenderState.Transform(entry.Boundary.LeftTop),
                    sso + state.RenderState.Transform(entry.Boundary.RightTop),
                    sso + state.RenderState.Transform(entry.Boundary.RightBottom),
                    sso + state.RenderState.Transform(entry.Boundary.LeftBottom),
                    color);
            }
        }
    }

    /// <summary>Gets the data required for rendering.</summary>
    /// <returns>The data.</returns>
    private protected abstract DataRef GetData();

    /// <summary>Tests if a codepoint is a whitespace, and permits breaking under normal word break rules.</summary>
    /// <param name="c">The codepoint.</param>
    /// <returns><c>true</c> if it is the case.</returns>
    private static bool IsBreakableWhitespace(int c) => c != 0x00A0 && Rune.IsValid(c) && Rune.IsWhiteSpace(new(c));

    private ref struct StateInfo
    {
        public float HorizontalOffsetWrtLine;
        public float VerticalOffsetWrtLine;

        private readonly ref readonly RenderState state;
        private readonly float wrapWidth;

        private readonly Vector2 lineBBoxVertical;
        private readonly float lineWidth;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StateInfo(float wrapWidth, in RenderState state, scoped in MeasuredLine lineMeasurement)
        {
            this.state = ref state;
            this.wrapWidth = wrapWidth;
            this.lineBBoxVertical = lineMeasurement.BBoxVertical;
            this.lineWidth = lineMeasurement.Width;
        }

        public void Update(in SpanStyleFontData fontInfo)
        {
            var lineAscentDescent = this.lineBBoxVertical;
            this.VerticalOffsetWrtLine = (fontInfo.BBoxVertical.Y - fontInfo.BBoxVertical.X) *
                                         this.state.LastStyle.VerticalOffset;
            switch (this.state.LastStyle.VerticalAlignment)
            {
                case VerticalAlignment.Baseline:
                    this.VerticalOffsetWrtLine -= lineAscentDescent.X + (fontInfo.Font.Ascent * fontInfo.Scale);
                    break;
                case VerticalAlignment.Middle:
                    this.VerticalOffsetWrtLine +=
                        (lineAscentDescent.Y - lineAscentDescent.X - fontInfo.ScaledFontSize) / 2;
                    break;
                case VerticalAlignment.Bottom:
                    this.VerticalOffsetWrtLine += lineAscentDescent.Y - lineAscentDescent.X - fontInfo.ScaledFontSize;
                    break;
                case VerticalAlignment.Top:
                default:
                    break;
            }

            this.VerticalOffsetWrtLine = MathF.Round(this.VerticalOffsetWrtLine);

            switch (this.state.LastStyle.HorizontalAlignment)
            {
                case HorizontalAlignment.Right:
                    this.HorizontalOffsetWrtLine = this.wrapWidth - this.lineWidth;
                    break;

                case HorizontalAlignment.Center:
                    this.HorizontalOffsetWrtLine = MathF.Round((this.wrapWidth - this.lineWidth) / 2);
                    break;

                case HorizontalAlignment.Left:
                case var _ when this.wrapWidth is <= 0 or >= float.MaxValue or float.NaN:
                default:
                    this.HorizontalOffsetWrtLine = 0;
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
        public const ImGuiMouseButton InvalidMouseButton = (ImGuiMouseButton)3;

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

    private class State : ISpannableState
    {
        private static readonly ObjectPool<State> Pool =
            new DefaultObjectPool<State>(new DefaultPooledObjectPolicy<State>());

        private readonly List<BoundaryToRecord> linkBoundaries = new();
        private readonly List<MeasuredLine> measuredLines = new();
        private readonly List<ISpannableState?> spannableStates = new();
        private RenderState renderState;

        /// <inheritdoc/>
        public ref RenderState RenderState => ref this.renderState;

        /// <inheritdoc/>
        /// <remarks>This is not supposed to be called when not rented, so NRE on accessing this is fine.</remarks>
        public ISpannableRenderer Renderer { get; private set; } = null!;

        /// <summary>Gets the span of measured lines.</summary>
        public Span<MeasuredLine> MeasuredLines => CollectionsMarshal.AsSpan(this.measuredLines);

        /// <summary>Gets the span of mapping between link range to render coordinates.</summary>
        public Span<BoundaryToRecord> LinkBoundaries => CollectionsMarshal.AsSpan(this.linkBoundaries);

        /// <summary>Gets the span of mapping between link range to render coordinates.</summary>
        public Span<ISpannableState?> SpannableStates => CollectionsMarshal.AsSpan(this.spannableStates);

        public SpannedStringBuilder? TempBuilder { get; set; }

        public int InteractedLinkRecordIndex { get; set; }

        public bool IsInteractedLinkRecordActive { get; set; }

        public static State Rent(ISpannableRenderer renderer, RenderState initialState, DataRef data)
        {
            var t = Pool.Get();
            t.Renderer = renderer;
            t.renderState = initialState;
            t.spannableStates.EnsureCapacity(data.Spannables.Length);
            return t;
        }

        public static void Return(State state, DataRef data)
        {
            state.Renderer = null!;
            state.linkBoundaries.Clear();
            state.measuredLines.Clear();
            state.InteractedLinkRecordIndex = -1;
            state.IsInteractedLinkRecordActive = false;

            for (var i = 0; i < state.spannableStates.Count; i++)
                data.Spannables[i]?.ReturnState(state.spannableStates[i]);
            state.SpannableStates.Clear();
            Pool.Return(state);
        }

        /// <summary>Adds a line, from <see cref="BaseSpannedString.Measure"/> step.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddLine(in MeasuredLine line) => this.measuredLines.Add(line);

        /// <summary>Updates <see cref="Rendering.RenderState.Boundary"/>, <see cref="linkBoundaries"/>,
        /// and resets <paramref name="boundary"/>, from <see cref="BaseSpannedString.Measure"/> step.</summary>
        public void UpdateAndResetBoundary(ref RectVector4 boundary, int linkRecordIndex)
        {
            if (!boundary.IsValid)
                return;

            if (linkRecordIndex != -1 && this.renderState.UseLinks)
                this.linkBoundaries.Add(new(linkRecordIndex, boundary));

            this.renderState.Boundary = RectVector4.Union(boundary, this.renderState.Boundary);
            boundary = RectVector4.InvertedExtrema;
        }

        /// <summary>Finds the first line break point, only taking word wrapping into account, from
        /// <see cref="BaseSpannedString.Measure"/> step.</summary>
        public void FindFirstWordWrapByteOffset(
            DataRef.Segment segment,
            CompositeOffset lineStartOffset,
            out MeasuredLine measuredLine)
        {
            measuredLine = MeasuredLine.Empty;

            var wordBreaker = new WordBreaker(segment.Data, this);
            var startOffset = lineStartOffset;
            do
            {
                if (segment.TryGetRawText(out var rawText))
                {
                    foreach (var c in rawText[(startOffset.Text - segment.Offset.Text)..]
                                 .EnumerateUtf(UtfEnumeratorFlags.Utf8))
                    {
                        var currentOffset = new CompositeOffset(startOffset.Text + c.ByteOffset, segment.Offset.Record);
                        var nextOffset = currentOffset.AddTextOffset(c.ByteLength);

                        var pad = 0f;
                        if (this.renderState.UseControlCharacter && c.Value.ShortName is { IsEmpty: false } name)
                        {
                            var ssb = this.TempBuilder ??= new();
                            ssb.Clear().Append(name);

                            var state2 = Rent(
                                this.Renderer,
                                this.renderState with
                                {
                                    LastStyle = this.renderState.ControlCharactersStyle,
                                    InitialStyle = this.renderState.ControlCharactersStyle,
                                    Offset = Vector2.Zero,
                                    DrawListPtr = null,
                                    Boundary = RectVector4.InvertedExtrema,
                                },
                                ssb.GetData());
                            ssb.Measure(new(state2));

                            if (state2.renderState.Boundary.IsValid)
                            {
                                pad = MathF.Round(state2.renderState.Boundary.Width);
                                wordBreaker.ResetLastChar();
                            }

                            Return(state2, ssb.GetData());
                        }

                        switch (c.Value.IntValue)
                        {
                            case '\r'
                                when segment.Data.TryGetCodepointAt(nextOffset.Text, 0, out var nextCodepoint)
                                     && nextCodepoint == '\n'
                                     && (this.renderState.AcceptedNewLines & NewLineType.CrLf) != 0:
                                measuredLine = wordBreaker.Last;
                                measuredLine.SetOffset(nextOffset.AddTextOffset(1), pad);
                                measuredLine.HasNewLineAtEnd = true;
                                wordBreaker.UnionLineBBoxVertical(ref measuredLine);
                                return;

                            case '\r' when (this.renderState.AcceptedNewLines & NewLineType.Cr) != 0:
                            case '\n' when (this.renderState.AcceptedNewLines & NewLineType.Lf) != 0:
                                measuredLine = wordBreaker.Last;
                                measuredLine.SetOffset(nextOffset, pad);
                                measuredLine.HasNewLineAtEnd = true;
                                wordBreaker.UnionLineBBoxVertical(ref measuredLine);
                                return;

                            case '\r' or '\n':
                                measuredLine = wordBreaker.AddCodepointAndMeasure(
                                    currentOffset,
                                    nextOffset,
                                    -1,
                                    pad: pad);
                                break;

                            default:
                                measuredLine = wordBreaker.AddCodepointAndMeasure(
                                    currentOffset,
                                    nextOffset,
                                    c.EffectiveChar,
                                    pad: pad);
                                break;
                        }

                        if (!measuredLine.IsEmpty)
                            return;
                    }

                    startOffset = new(segment.Offset.Text + rawText.Length, segment.Offset.Record);
                }
                else if (segment.TryGetRecord(out var record, out var recordData))
                {
                    measuredLine = wordBreaker.HandleSpan(record, recordData, new(segment), new(segment, 0, 1));
                    if (!measuredLine.IsEmpty)
                        return;
                }
            }
            while (segment.TryGetNext(out segment));

            measuredLine = wordBreaker.Last;
            measuredLine.SetOffset(new(segment.Offset.Text, segment.Offset.Record));
        }

        /// <summary>Forces a line break, from both <see cref="BaseSpannedString.Measure"/> and
        /// <see cref="BaseSpannedString.Draw"/> step.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BreakLineImmediate(in MeasuredLine mostRecentLine) =>
            this.renderState.Offset = new(0, MathF.Round(this.renderState.Offset.Y + mostRecentLine.Height));

        /// <summary>Add decorations and break line once a line ends, from both <see cref="BaseSpannedString.Measure"/>
        /// and <see cref="BaseSpannedString.Draw"/> step.</summary>
        public RectVector4 ProcessPostLine(
            in MeasuredLine line, ref CharRenderer charRenderer, SpannableDrawArgs args, bool measureOnly)
        {
            var accumulatedBoundary = RectVector4.InvertedExtrema;
            if (line.IsWrapped)
            {
                if (line.LastThing.IsCodepoint(0x00AD) && this.renderState.WordBreak != WordBreakType.KeepAll)
                {
                    accumulatedBoundary = RectVector4.Union(
                        accumulatedBoundary,
                        charRenderer.RenderOne(SoftHyphenReplacementChar));
                }

                if (this.renderState.WrapMarker is { } wrapMarker)
                {
                    var state2 = wrapMarker.RentState(
                        this.Renderer,
                        this.renderState with
                        {
                            DrawListPtr = measureOnly ? default : this.renderState.DrawListPtr,
                            StartScreenOffset = this.renderState.StartScreenOffset +
                                                this.renderState.Offset +
                                                charRenderer.StyleTranslation,
                            Offset = Vector2.Zero,
                            Boundary = RectVector4.InvertedExtrema,
                            WordBreak = WordBreakType.KeepAll,
                            WrapMarker = null,
                            MaxSize = new(float.MaxValue),
                        },
                        null);
                    wrapMarker.Measure(new(state2));
                    if (state2.RenderState.Boundary.IsValid)
                    {
                        wrapMarker.Draw(args.WithState(state2));
                        accumulatedBoundary = RectVector4.Union(
                            accumulatedBoundary,
                            RectVector4.Translate(
                                state2.RenderState.Boundary,
                                this.renderState.Offset + charRenderer.StyleTranslation));
                        this.renderState.Offset.X += state2.RenderState.Boundary.Right;
                        charRenderer.LastRendered.Clear();
                    }

                    wrapMarker.ReturnState(state2);
                }
            }

            if (line.HasNewLineAtEnd || (line.IsWrapped && this.renderState.WordBreak != WordBreakType.KeepAll))
                this.BreakLineImmediate(line);
            return accumulatedBoundary;
        }
    }
}
