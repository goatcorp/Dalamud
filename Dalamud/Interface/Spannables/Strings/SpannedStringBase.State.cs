using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

using Dalamud.Interface.Spannables.EventHandlerArgs;
using Dalamud.Interface.Spannables.Helpers;
using Dalamud.Interface.Spannables.Internal;
using Dalamud.Interface.Spannables.Rendering;
using Dalamud.Interface.Spannables.Styles;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using Dalamud.Utility.Numerics;
using Dalamud.Utility.Text;

using ImGuiNET;

using Microsoft.Extensions.ObjectPool;

namespace Dalamud.Interface.Spannables.Strings;

/// <summary>Base class for <see cref="SpannedString"/> and <see cref="SpannedStringBuilder"/>.</summary>
public abstract partial class SpannedStringBase
{
    [SuppressMessage(
        "StyleCop.CSharp.MaintainabilityRules",
        "SA1401:Fields should be private",
        Justification = "This is private")]
    private class RenderPass : ISpannableRenderPass
    {
        /// <summary>The offset currently being drawn to. Not a part of measurement.</summary>
        public Vector2 Offset;

        private static readonly ObjectPool<RenderPass> Pool =
            new DefaultObjectPool<RenderPass>(new DefaultPooledObjectPolicy<RenderPass>());

        private Measurement measured;

        /// <inheritdoc/>
        public ref TextState TextState => ref this.measured.TextState;

        /// <inheritdoc/>
        public ref readonly RectVector4 Boundary => ref this.measured.Boundary;

        /// <inheritdoc/>
        public Vector2 ScreenOffset { get; private set; }

        /// <inheritdoc/>
        public Vector2 TransformationOrigin { get; private set; }

        /// <inheritdoc/>
        public ref readonly Matrix4x4 Transformation => ref this.measured.Transformation;

        /// <inheritdoc/>
        public uint ImGuiGlobalId { get; private set; }

        /// <inheritdoc/>
        /// <remarks>This is not supposed to be called when not rented, so NRE on accessing this is fine.</remarks>
        public ISpannableRenderer Renderer { get; private set; } = null!;

        /// <summary>Gets the span of measured lines.</summary>
        public Span<MeasuredLine> MeasuredLines => CollectionsMarshal.AsSpan(this.measured.Lines);

        /// <summary>Gets the span of mapping between link range to render coordinates.</summary>
        public Span<BoundaryToRecord> LinkBoundaries => CollectionsMarshal.AsSpan(this.measured.LinkBoundaries);

        public Span<ISpannableRenderPass?> SpannableStates => CollectionsMarshal.AsSpan(this.measured.SpannableStates);

        public Span<Vector2> SpannableOffsets => CollectionsMarshal.AsSpan(this.measured.SpannableOffsets);

        public SpannedStringBuilder? TempBuilder { get; set; }

        public ref float Scale => ref this.measured.Scale;

        public ref Vector2 MaxSize => ref this.measured.MaxSize;

        public int InteractedLinkRecordIndex { get; set; }

        public bool IsInteractedLinkRecordActive { get; set; }

        public static RenderPass Rent(in SpannableRentRenderPassArgs args)
        {
            var t = Pool.Get();
            t.Renderer = args.Renderer;
            return t;
        }

        public static void Return(RenderPass renderPass, DataRef data)
        {
            renderPass.Renderer = null!;
            renderPass.measured.Clear(data);
            renderPass.InteractedLinkRecordIndex = -1;
            renderPass.IsInteractedLinkRecordActive = false;
            Pool.Return(renderPass);
        }

        /// <inheritdoc/>
        public void MeasureSpannable(scoped in SpannableMeasureArgs args)
        {
            var state = args.RenderPass as RenderPass ?? new();
            if (args.Sender is not SpannedStringBase ssb)
                return;
            var data = ssb.GetData();

            // Did nothing change? Skip the measurement.
            if (this.measured.UpdateMeasureParams(args, ssb))
                return;

            state.Offset = Vector2.Zero;
            state.TextState.LastStyle = state.TextState.InitialStyle;
            state.MaxSize = args.MaxSize;
            state.ClearBoundary();

            var segment = new DataRef.Segment(data, 0, 0);
            var linkRecordIndex = -1;

            var drawArgs = new SpannableDrawArgs(ssb, state, default, default);
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

                                case SpannedRecordType.Link
                                    when SpannedRecordCodec.TryDecodeLink(recordData, out var link):
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
            var state = args.RenderPass as RenderPass ?? new();
            if (args.Sender is not SpannedStringBase ssb)
                return;
            var data = ssb.GetData();

            this.ScreenOffset = args.ScreenOffset;
            this.TransformationOrigin = args.TransformationOrigin;
            this.measured.Transformation = args.Transformation;

            for (var i = 0; i < data.Spannables.Length; i++)
            {
                if (state.SpannableStates[i] is not { } spannableState)
                    continue;

                args.NotifyChild(
                    data.Spannables[i],
                    spannableState,
                    state.SpannableOffsets[i],
                    spannableState.TextState.LastStyle.Italic
                        ? new(Matrix3x2.CreateSkew(MathF.Atan(-1 / TextStyleFontData.FakeItalicDivisor), 0))
                        : Matrix4x4.Identity);
            }
        }

        /// <inheritdoc/>
        public unsafe void HandleSpannableInteraction(
            scoped in SpannableHandleInteractionArgs args,
            out SpannableLinkInteracted link)
        {
            link = default;
            this.ImGuiGlobalId = args.ImGuiGlobalId;

            var state = args.RenderPass as RenderPass ?? new();
            if (args.Sender is not SpannedStringBase ssb)
                return;
            var data = ssb.GetData();

            for (var i = 0; i < data.Spannables.Length; i++)
            {
                if (state.SpannableStates[i] is not { } spannableState)
                    continue;
                args.NotifyChild(data.Spannables[i], spannableState, i, out link);
                if (!link.IsEmpty)
                    return;
            }

            var mouseRel = args.MouseLocalLocation;

            state.InteractedLinkRecordIndex = -1;
            state.IsInteractedLinkRecordActive = false;
            link = default;
            foreach (ref var entry in state.LinkBoundaries)
            {
                if (entry.Boundary.Contains(mouseRel) && args.IsItemHoverable(entry.RecordIndex))
                {
                    if (ssb.GetData().TryGetLinkAt(entry.RecordIndex, out link.Link))
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

                ref var itemState = ref *(ImGuiItemStateStruct*)ImGui.GetStateStorage().GetVoidPtrRef(
                                            args.RenderPass.GetGlobalIdFromInnerId(linkBoundary.RecordIndex),
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
            var state = args.RenderPass as RenderPass ?? new();
            if (args.Sender is not SpannedStringBase ssb)
                return;
            var data = ssb.GetData();
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
                    ImGui.GetColorU32(
                        state.IsInteractedLinkRecordActive ? ImGuiCol.ButtonActive : ImGuiCol.ButtonHovered);

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

        /// <summary>Adds a line, from <see cref="MeasureSpannable"/> step.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddLine(in MeasuredLine line) => this.measured.Lines!.Add(line);

        /// <summary>Clears the boundary, to be used from <see cref="MeasureSpannable"/> step.</summary>
        public void ClearBoundary() => this.measured.Boundary = RectVector4.InvertedExtrema;

        /// <summary>Updates <see cref="Measurement.Boundary"/>, <see cref="Measurement.LinkBoundaries"/>,
        /// and resets <paramref name="accumulator"/>, from <see cref="MeasureSpannable"/> step.</summary>
        public void UpdateAndResetBoundary(ref RectVector4 accumulator, int linkRecordIndex)
        {
            if (!accumulator.IsValid)
                return;

            if (linkRecordIndex != -1)
                this.measured.LinkBoundaries!.Add(new(linkRecordIndex, accumulator));

            this.measured.Boundary = RectVector4.Union(accumulator, this.measured.Boundary);
            accumulator = RectVector4.InvertedExtrema;
        }

        public void ExtendBoundaryBottom(float b) =>
            this.measured.Boundary.Bottom = Math.Max(this.measured.Boundary.Bottom, b);

        public void TranslateBoundaries(Vector2 translation, DataRef data)
        {
            foreach (ref var b in this.LinkBoundaries)
                b.Boundary = RectVector4.Translate(b.Boundary, translation);
            foreach (ref var v in this.SpannableOffsets[..data.Spannables.Length])
                v += translation;
            this.measured.Boundary = RectVector4.Translate(this.Boundary, translation);
        }

        /// <summary>Finds the first line break point, only taking word wrapping into account, from
        /// <see cref="MeasureSpannable"/> step.</summary>
        public void FindFirstWordWrapByteOffset(
            SpannableMeasureArgs args,
            DataRef.Segment segment,
            CompositeOffset lineStartOffset,
            out MeasuredLine measuredLine)
        {
            measuredLine = MeasuredLine.Empty;

            var wordBreaker = new WordBreaker(args, segment.Data, this);
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
                        if (this.measured.TextState.UseControlCharacter && c.Value.ShortName is { IsEmpty: false } name)
                        {
                            var ssb = this.TempBuilder ??= new();
                            ssb.Clear().Append(name);

                            var state2 = Rent(new(this.Renderer));
                            state2.MeasureSpannable(
                                new(
                                    ssb,
                                    state2,
                                    new(float.MaxValue),
                                    this.Scale,
                                    this.measured.TextState with
                                    {
                                        LastStyle = this.measured.TextState.ControlCharactersStyle,
                                        InitialStyle = this.measured.TextState.ControlCharactersStyle,
                                    }));

                            if (state2.Boundary.IsValid)
                            {
                                pad = MathF.Round(state2.Boundary.Width);
                                wordBreaker.ResetLastChar();
                            }

                            Return(state2, ssb.GetData());
                        }

                        switch (c.Value.IntValue)
                        {
                            case '\r'
                                when segment.Data.TryGetCodepointAt(nextOffset.Text, 0, out var nextCodepoint)
                                     && nextCodepoint == '\n'
                                     && (this.measured.TextState.AcceptedNewLines & NewLineType.CrLf) != 0:
                                measuredLine = wordBreaker.Last;
                                measuredLine.SetOffset(nextOffset.AddTextOffset(1), pad);
                                measuredLine.HasNewLineAtEnd = true;
                                wordBreaker.UnionLineBBoxVertical(ref measuredLine);
                                return;

                            case '\r' when (this.measured.TextState.AcceptedNewLines & NewLineType.Cr) != 0:
                            case '\n' when (this.measured.TextState.AcceptedNewLines & NewLineType.Lf) != 0:
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

        /// <summary>Forces a line break, from both <see cref="MeasureSpannable"/> and <see cref="DrawSpannable"/>
        /// step.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BreakLineImmediate(in MeasuredLine mostRecentLine) =>
            this.Offset = new(0, MathF.Round(this.Offset.Y + mostRecentLine.Height));

        /// <summary>Add decorations and break line once a line ends, from both <see cref="MeasureSpannable"/>
        /// and <see cref="DrawSpannable"/> step.</summary>
        public RectVector4 ProcessPostLine(in MeasuredLine line, ref CharRenderer charRenderer, SpannableDrawArgs args)
        {
            var accumulatedBoundary = RectVector4.InvertedExtrema;
            if (line.IsWrapped)
            {
                if (line.LastThing.IsCodepoint(0x00AD) && this.measured.TextState.WordBreak != WordBreakType.KeepAll)
                {
                    accumulatedBoundary = RectVector4.Union(
                        accumulatedBoundary,
                        charRenderer.RenderOne(SoftHyphenReplacementChar));
                }

                if (this.measured.TextState.WrapMarker is { } wrapMarker)
                {
                    var state2 = wrapMarker.RentRenderPass(new(this.Renderer));
                    state2.MeasureSpannable(
                        new(
                            wrapMarker,
                            state2,
                            new(float.MaxValue),
                            this.Scale,
                            this.measured.TextState with
                            {
                                InitialStyle = this.measured.TextState.LastStyle,
                                WordBreak = WordBreakType.KeepAll,
                                WrapMarker = null,
                            }));
                    if (state2.Boundary.IsValid)
                    {
                        if (!args.IsEmpty)
                        {
                            var mtx = Matrix4x4.Identity;
                            if (this.measured.TextState.LastStyle.Italic)
                            {
                                mtx = new(
                                    Matrix3x2.CreateSkew(MathF.Atan(-1 / TextStyleFontData.FakeItalicDivisor), 0));
                            }

                            state2.CommitSpannableMeasurement(
                                new(
                                    wrapMarker,
                                    state2,
                                    this.TransformToScreen(this.Offset + charRenderer.StyleTranslation),
                                    this.TransformationOrigin,
                                    Matrix4x4.Multiply(mtx, this.Transformation.WithoutTranslation())));
                            args.NotifyChild(wrapMarker, state2);
                        }

                        accumulatedBoundary = RectVector4.Union(
                            accumulatedBoundary,
                            RectVector4.Translate(
                                state2.Boundary,
                                this.Offset + charRenderer.StyleTranslation));
                        this.Offset.X += state2.Boundary.Right;
                        charRenderer.LastRendered.Clear();
                    }

                    wrapMarker.ReturnRenderPass(state2);
                }
            }

            if (line.HasNewLineAtEnd || (line.IsWrapped && this.measured.TextState.WordBreak != WordBreakType.KeepAll))
                this.BreakLineImmediate(line);
            return accumulatedBoundary;
        }

        private struct Measurement
        {
            public List<BoundaryToRecord>? LinkBoundaries;
            public List<MeasuredLine>? Lines;
            public List<ISpannableRenderPass?>? SpannableStates;
            public List<Vector2>? SpannableOffsets;
            public List<int>? SpannableGenerations;
            public Matrix4x4 Transformation;
            public RectVector4 Boundary;
            public Vector2 MaxSize;
            public TextState TextState;
            public int SpanCount;
            public float Scale;
            public int StateGeneration;

            /// <summary>Updates measurement parameters.</summary>
            /// <returns><c>true</c> if nothing has been changed.</returns>
            [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator", Justification = "opportunistic")]
            public bool UpdateMeasureParams(scoped in SpannableMeasureArgs args, SpannedStringBase ss)
            {
                var sp = ss.GetData().Spannables;
                if (this.Scale == args.Scale
                    && this.MaxSize == args.MaxSize
                    && this.StateGeneration == ss.StateGeneration
                    && this.SpanCount == sp.Length
                    && TextState.PropertyReferenceEquals(this.TextState, args.TextState))
                {
                    var i = 0;
                    for (; i < this.SpanCount; i++)
                    {
                        if (this.SpannableGenerations?[i] != sp[i]?.StateGeneration)
                            break;
                    }

                    return i == this.SpanCount;
                }

                var data = ss.GetData();
                this.TextState = args.TextState;
                this.Scale = args.Scale;
                this.LinkBoundaries ??= new();
                this.Lines ??= new();
                this.SpannableStates ??= new();
                this.SpannableOffsets ??= new();
                this.SpannableGenerations ??= new();
                this.MaxSize = Vector2.Zero;
                this.SpanCount = data.Spannables.Length;
                this.StateGeneration = ss.StateGeneration;
                this.SpannableStates.EnsureCapacity(this.SpanCount);
                this.SpannableOffsets.EnsureCapacity(this.SpanCount);
                this.SpannableGenerations.EnsureCapacity(this.SpanCount);
                while (this.SpannableStates.Count < this.SpanCount)
                {
                    this.SpannableStates.Add(null);
                    this.SpannableOffsets.Add(default);
                    this.SpannableGenerations.Add(-1);
                }
    
                for (var i = 0; i < data.Spannables.Length; i++)
                    this.SpannableStates[i] = data.Spannables[i]?.RentRenderPass(new(args.RenderPass.Renderer));

                return false;
            }

            public void Clear(DataRef data)
            {
                this.LinkBoundaries?.Clear();
                this.Lines?.Clear();
                if (this.SpannableStates is not null)
                {
                    for (var i = 0; i < this.SpanCount; i++)
                    {
                        data.Spannables[i]?.ReturnRenderPass(this.SpannableStates[i]);
                        this.SpannableStates[i] = null;
                    }
                }
            }
        }
    }
}
