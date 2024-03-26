using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

using Dalamud.Interface.Spannables.Helpers;
using Dalamud.Interface.Spannables.Internal;
using Dalamud.Interface.Spannables.Rendering;
using Dalamud.Interface.Spannables.RenderPassMethodArgs;
using Dalamud.Interface.Spannables.Styles;
using Dalamud.Interface.Utility;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using Dalamud.Utility.Numerics;
using Dalamud.Utility.Text;

using ImGuiNET;

using Microsoft.Extensions.ObjectPool;

namespace Dalamud.Interface.Spannables.Text;

/// <summary>Base class for <see cref="TextSpannable"/> and <see cref="TextSpannableBuilder"/>.</summary>
public abstract partial class TextSpannableBase
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
        public ref TextState ActiveTextState => ref this.measured.TextState;

        /// <inheritdoc/>
        public uint ImGuiGlobalId { get; private set; }

        /// <inheritdoc/>
        public ref readonly RectVector4 Boundary => ref this.measured.Boundary;

        /// <inheritdoc/>
        public Vector2 InnerOrigin { get; private set; }

        /// <inheritdoc/>
        public ref readonly Matrix4x4 TransformationFromParent => ref this.measured.TransformationFromParent;

        /// <inheritdoc/>
        public ref readonly Matrix4x4 TransformationFromAncestors => ref this.measured.TransformationFromAncestors;

        /// <inheritdoc/>
        /// <remarks>This is not supposed to be called when not rented, so NRE on accessing this is fine.</remarks>
        public ISpannableRenderer Renderer { get; private set; } = null!;

        /// <summary>Gets the span of measured lines.</summary>
        public Span<MeasuredLine> MeasuredLines => CollectionsMarshal.AsSpan(this.measured.Lines);

        /// <summary>Gets the span of mapping between link range to render coordinates.</summary>
        public Span<BoundaryToRecord> LinkBoundaries => CollectionsMarshal.AsSpan(this.measured.LinkBoundaries);

        public Span<ISpannableRenderPass?> SpannableStates => CollectionsMarshal.AsSpan(this.measured.SpannableStates);

        public Span<Vector2> SpannableOffsets => CollectionsMarshal.AsSpan(this.measured.SpannableOffsets);

        public TextSpannableBuilder? TempBuilder { get; set; }

        public ref readonly float Scale => ref this.measured.Scale;

        public ref readonly Vector2 MaxSize => ref this.measured.MaxSize;

        public static RenderPass Rent(in ISpannableRenderer renderer)
        {
            var t = Pool.Get();
            t.Renderer = renderer;
            return t;
        }

        public static void Return(RenderPass renderPass, DataRef data)
        {
            renderPass.Renderer = null!;
            renderPass.measured.Clear(data);
            Pool.Return(renderPass);
        }

        /// <inheritdoc/>
        public void MeasureSpannable(scoped in SpannableMeasureArgs args)
        {
            if (args.Sender is not TextSpannableBase ssb)
                return;

            this.ImGuiGlobalId = args.ImGuiGlobalId;

            var data = ssb.GetData();

            // Did nothing change? Skip the measurement.
            if (this.measured.UpdateMeasureParams(args, ssb))
                return;

            this.Offset = Vector2.Zero;
            this.ActiveTextState.LastStyle = this.ActiveTextState.InitialStyle;
            this.measured.Clear(data);
            this.measured.Boundary = RectVector4.InvertedExtrema;

            var segment = new DataRef.Segment(data, 0, 0);
            var linkRecordIndex = -1;

            var drawArgs = new SpannableDrawArgs(ssb, this, default);
            var charRenderer = new CharRenderer(drawArgs, data, this, true);
            var skipNextLine = false;
            while (true)
            {
                this.FindFirstWordWrapByteOffset(args, segment, new(segment), out var line);
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
                    this.AddLine(line);
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

                                if (this.ActiveTextState.UseControlCharacter)
                                {
                                    var name = c.Value.ShortName;
                                    if (!name.IsEmpty)
                                    {
                                        var offset = charRenderer.StyleTranslation;
                                        this.Offset += offset;
                                        var old = charRenderer.UpdateSpanParams(
                                            this.ActiveTextState.ControlCharactersStyle);
                                        charRenderer.LastRendered.Clear();
                                        foreach (var c2 in name)
                                            charRenderer.RenderOne(c2);
                                        charRenderer.LastRendered.Clear();
                                        _ = charRenderer.UpdateSpanParams(old);
                                        this.Offset -= offset;
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
                                    this.UpdateAndResetBoundary(ref accumulatedBoundary, linkRecordIndex);
                                    linkRecordIndex = -1;
                                    break;

                                case SpannedRecordType.Link
                                    when SpannedRecordCodec.TryDecodeLink(recordData, out var link):
                                    this.UpdateAndResetBoundary(ref accumulatedBoundary, linkRecordIndex);
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
                        this.ProcessPostLine(line, ref charRenderer, default));

                    this.UpdateAndResetBoundary(ref accumulatedBoundary, linkRecordIndex);
                    this.ExtendBoundaryBottom(this.Offset.Y + charRenderer.MostRecentLineHeight);
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

                    if (line.HasNewLineAtEnd ||
                        (line.IsWrapped && this.ActiveTextState.WordBreak != WordBreakType.KeepAll))
                        this.BreakLineImmediate(line);
                }

                if (lineSegment.Offset == data.EndOffset)
                    break;
                segment = lineSegment;
                if (this.ActiveTextState.WordBreak == WordBreakType.KeepAll)
                {
                    if (skipNextLine && !line.IsWrapped)
                        this.MeasuredLines[^1].HasNewLineAtEnd = true;
                    skipNextLine = line.IsWrapped;
                }
            }

            this.ActiveTextState.LineCount = this.MeasuredLines.Length;
            this.ExtendBoundaryBottom(this.Offset.Y);

            if (!this.Boundary.IsValid)
                this.measured.Boundary = default;

            if (this.measured.Boundary.Width < args.MinSize.X)
                this.measured.Boundary.Right = this.measured.Boundary.Left + args.MinSize.X;

            if (this.measured.Boundary.Height < args.MinSize.Y)
                this.measured.Boundary.Bottom = this.measured.Boundary.Top + args.MinSize.Y;

            if (this.ActiveTextState is { VerticalAlignment: > 0f } && args.MaxSize.Y < float.PositiveInfinity)
            {
                var offset = MathF.Round(
                    (args.MaxSize.Y - this.Boundary.Height) *
                    Math.Clamp(this.ActiveTextState.VerticalAlignment, 0f, 1f));
                this.TranslateBoundaries(new(0, offset), data);
                this.ActiveTextState.ShiftFromVerticalAlignment = offset;
            }
            else
            {
                this.ActiveTextState.ShiftFromVerticalAlignment = 0;
            }
        }

        /// <inheritdoc/>
        public void CommitSpannableMeasurement(scoped in SpannableCommitTransformationArgs args)
        {
            if (args.Sender is not TextSpannableBase ssb)
                return;
            var data = ssb.GetData();

            this.InnerOrigin = args.InnerOrigin;
            this.measured.TransformationFromParent = args.TransformationFromParent;
            this.measured.TransformationFromAncestors = args.TransformationFromAncestors;

            for (var i = 0; i < data.Spannables.Length; i++)
            {
                if (this.SpannableStates[i] is not { } spannableState)
                    continue;

                args.NotifyChild(
                    data.Spannables[i],
                    spannableState,
                    this.SpannableOffsets[i],
                    spannableState.ActiveTextState.LastStyle.Italic
                        ? new(Matrix3x2.CreateSkew(MathF.Atan(-1 / TextStyleFontData.FakeItalicDivisor), 0))
                        : Matrix4x4.Identity);
            }
        }

        /// <inheritdoc/>
        public unsafe void DrawSpannable(SpannableDrawArgs args)
        {
            if (args.Sender is not TextSpannableBase ssb)
                return;

            var data = ssb.GetData();
            this.Offset = new(0, this.ActiveTextState.ShiftFromVerticalAlignment);
            this.ActiveTextState.LastStyle = this.ActiveTextState.InitialStyle;

            var charRenderer = new CharRenderer(args, data, this, false);
            try
            {
                var segment = new DataRef.Segment(data, 0, 0);
                foreach (ref readonly var line in this.MeasuredLines)
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
                                    if (this.ActiveTextState.UseControlCharacter)
                                    {
                                        var name = c.Value.ShortName;
                                        if (!name.IsEmpty)
                                        {
                                            var offset = charRenderer.StyleTranslation;
                                            this.Offset += offset;
                                            var old = charRenderer.UpdateSpanParams(
                                                this.ActiveTextState.ControlCharactersStyle);
                                            charRenderer.LastRendered.Clear();
                                            foreach (var c2 in name)
                                                charRenderer.RenderOne(c2);
                                            charRenderer.LastRendered.Clear();
                                            _ = charRenderer.UpdateSpanParams(old);
                                            this.Offset -= offset;
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

                    this.ProcessPostLine(line, ref charRenderer, args);
                }

                ref var itemState = ref *(ImGuiItemStateStruct*)ImGui.GetStateStorage().GetVoidPtrRef(
                                            args.RenderPass.ImGuiGlobalId,
                                            nint.Zero);

                if (itemState.State != ImGuiItemStateStruct.InteractionState.Clear)
                {
                    var color =
                        ImGui.GetColorU32(
                            itemState.State == ImGuiItemStateStruct.InteractionState.Active
                                ? ImGuiCol.ButtonActive
                                : ImGuiCol.ButtonHovered);

                    foreach (var entry in this.LinkBoundaries)
                    {
                        if (entry.RecordIndex != itemState.InteractedLinkRecordIndex)
                            continue;

                        ImGuiNative.ImDrawList_AddRectFilled(
                            charRenderer.BackChannel,
                            entry.Boundary.LeftTop,
                            entry.Boundary.RightBottom,
                            color,
                            0f,
                            ImDrawFlags.None);
                    }
                }
            }
            finally
            {
                charRenderer.AppendAndReturnChannels(this.TransformationFromParent);
            }
        }

        /// <inheritdoc/>
        public unsafe void HandleSpannableInteraction(
            scoped in SpannableHandleInteractionArgs args,
            out SpannableLinkInteracted link)
        {
            link = default;

            if (args.Sender is not TextSpannableBase ssb)
                return;
            var data = ssb.GetData();

            for (var i = 0; i < data.Spannables.Length; i++)
            {
                if (this.SpannableStates[i] is not { } spannableState)
                    continue;
                args.NotifyChild(data.Spannables[i], spannableState, out link);
                if (!link.IsEmpty)
                    return;
            }

            var mouseRel = args.MouseLocalLocation;

            ref var itemState = ref *(ImGuiItemStateStruct*)ImGui.GetStateStorage().GetVoidPtrRef(
                                        args.RenderPass.ImGuiGlobalId,
                                        nint.Zero);

            itemState.InteractedLinkRecordIndex = -1;
            itemState.State = ImGuiItemStateStruct.InteractionState.Clear;
            link = default;
            foreach (ref var entry in this.LinkBoundaries)
            {
                if (entry.Boundary.Contains(mouseRel) && args.IsItemHoverable(entry.Boundary, entry.RecordIndex))
                {
                    if (ssb.GetData().TryGetLinkAt(entry.RecordIndex, out link.Link))
                        itemState.InteractedLinkRecordIndex = entry.RecordIndex;
                    else
                        itemState.InteractedLinkRecordIndex = -1;

                    break;
                }
            }

            var prevLinkRecordIndex = -1;
            foreach (ref var linkBoundary in this.LinkBoundaries)
            {
                if (prevLinkRecordIndex == linkBoundary.RecordIndex)
                    continue;
                prevLinkRecordIndex = linkBoundary.RecordIndex;

                ref var linkState = ref *(ImGuiLinkStateStruct*)ImGui.GetStateStorage().GetVoidPtrRef(
                                            args.RenderPass.GetGlobalIdFromInnerId(linkBoundary.RecordIndex),
                                            nint.Zero);

                if (linkState.IsMouseButtonDownHandled)
                {
                    switch (linkState.FirstMouseButton)
                    {
                        case var _ when linkBoundary.RecordIndex != itemState.InteractedLinkRecordIndex:
                            itemState.InteractedLinkRecordIndex = -1;
                            break;
                        case ImGuiMouseButton.Left when !args.IsMouseButtonDown(ImGuiMouseButton.Left):
                            link.IsMouseClicked = true;
                            link.ClickedMouseButton = ImGuiMouseButton.Left;
                            linkState.IsMouseButtonDownHandled = false;
                            break;
                        case ImGuiMouseButton.Right when !args.IsMouseButtonDown(ImGuiMouseButton.Right):
                            link.IsMouseClicked = true;
                            link.ClickedMouseButton = ImGuiMouseButton.Right;
                            linkState.IsMouseButtonDownHandled = false;
                            break;
                        case ImGuiMouseButton.Middle when !args.IsMouseButtonDown(ImGuiMouseButton.Middle):
                            link.IsMouseClicked = true;
                            link.ClickedMouseButton = ImGuiMouseButton.Middle;
                            linkState.IsMouseButtonDownHandled = false;
                            break;
                    }

                    if (args.MouseButtonStateFlags == 0)
                    {
                        linkState.IsMouseButtonDownHandled = false;
                        args.ClearActive();
                    }
                }

                if (itemState.InteractedLinkRecordIndex == linkBoundary.RecordIndex)
                {
                    args.SetHovered(linkBoundary.RecordIndex);
                    if (!linkState.IsMouseButtonDownHandled && args.TryGetAnyHeldMouseButton(out var heldButton))
                    {
                        linkState.IsMouseButtonDownHandled = true;
                        linkState.FirstMouseButton = heldButton;
                    }

                    itemState.State =
                        linkState.IsMouseButtonDownHandled
                            ? ImGuiItemStateStruct.InteractionState.Active
                            : ImGuiItemStateStruct.InteractionState.Hover;
                }

                if (linkState.IsMouseButtonDownHandled)
                {
                    args.SetHovered(linkBoundary.RecordIndex);
                    args.SetActive(linkBoundary.RecordIndex);
                }
            }

            if (itemState.InteractedLinkRecordIndex == -1)
            {
                itemState.State = ImGuiItemStateStruct.InteractionState.Clear;
                link = default;
            }
        }

        /// <summary>Adds a line, from <see cref="MeasureSpannable"/> step.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddLine(in MeasuredLine line) => this.measured.Lines!.Add(line);

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

        public void ExtendBoundaryBottom(float b)
        {
            if (!this.measured.Boundary.IsValid)
                return;

            this.measured.Boundary.Bottom = Math.Max(this.measured.Boundary.Bottom, b);
        }

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

                            var state2 = Rent(this.Renderer);
                            state2.MeasureSpannable(
                                new(
                                    ssb,
                                    state2,
                                    Vector2.Zero,
                                    new(float.PositiveInfinity),
                                    this.measured.Scale,
                                    this.measured.TextState with
                                    {
                                        LastStyle = this.measured.TextState.ControlCharactersStyle,
                                        InitialStyle = this.measured.TextState.ControlCharactersStyle,
                                    },
                                    0));

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

                if (this.measured.TextState.WrapMarker is { } wm)
                {
                    var wmrp = wm.RentRenderPass(this.Renderer);
                    wmrp.MeasureSpannable(
                        new(
                            wm,
                            wmrp,
                            Vector2.Zero,
                            new(float.PositiveInfinity),
                            this.measured.Scale,
                            this.measured.TextState with
                            {
                                InitialStyle = this.measured.TextState.LastStyle,
                                WordBreak = WordBreakType.KeepAll,
                                WrapMarker = null,
                            },
                            0));
                    if (wmrp.Boundary.IsValid)
                    {
                        if (!args.IsEmpty)
                        {
                            var mtx = Matrix4x4.Identity;
                            if (this.measured.TextState.LastStyle.Italic)
                            {
                                mtx = new(
                                    Matrix3x2.CreateSkew(MathF.Atan(-1 / TextStyleFontData.FakeItalicDivisor), 0));
                            }

                            new SpannableCommitTransformationArgs(
                                    args.Sender,
                                    args.RenderPass,
                                    this.InnerOrigin,
                                    this.TransformationFromParent,
                                    this.TransformationFromAncestors)
                                .NotifyChild(
                                    wm,
                                    wmrp,
                                    this.Offset + charRenderer.StyleTranslation,
                                    mtx);

                            var tmpDrawList = this.Renderer.RentDrawList(args.DrawListPtr);
                            try
                            {
                                var tmpargs = args with { DrawListPtr = tmpDrawList };
                                tmpargs.NotifyChild(wm, wmrp);
                                tmpDrawList.CopyDrawListDataTo(
                                    args.DrawListPtr,
                                    this.TransformationFromParent,
                                    Vector4.One);
                            }
                            finally
                            {
                                this.Renderer.ReturnDrawList(tmpDrawList);
                            }
                        }

                        accumulatedBoundary = RectVector4.Union(
                            accumulatedBoundary,
                            RectVector4.Translate(
                                wmrp.Boundary,
                                this.Offset + charRenderer.StyleTranslation));
                        this.Offset.X += wmrp.Boundary.Right;
                        charRenderer.LastRendered.Clear();
                    }

                    wm.ReturnRenderPass(wmrp);
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
            public Matrix4x4 TransformationFromParent;
            public Matrix4x4 TransformationFromAncestors;
            public Vector2 MinSize;
            public Vector2 MaxSize;
            public RectVector4 Boundary;
            public TextState TextState;
            public int SpanCount;
            public float Scale;
            public int StateGeneration;

            /// <summary>Updates measurement parameters.</summary>
            /// <returns><c>true</c> if nothing has been changed.</returns>
            [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator", Justification = "opportunistic")]
            public bool UpdateMeasureParams(scoped in SpannableMeasureArgs args, TextSpannableBase ss)
            {
                var data = ss.GetData();
                var spannables = data.Spannables;
                if (this.Scale == args.Scale
                    && this.MinSize == args.MinSize
                    && this.MaxSize == args.MaxSize
                    && this.StateGeneration == ss.StateGeneration
                    && this.SpanCount == spannables.Length
                    && TextState.PropertyReferenceEquals(this.TextState, args.TextState))
                {
                    var i = 0;
                    for (; i < this.SpanCount; i++)
                    {
                        if (this.SpannableGenerations?[i] != spannables[i]?.StateGeneration)
                            break;
                    }

                    return i == this.SpanCount;
                }

                this.TextState = args.TextState;
                this.MinSize = args.MinSize;
                this.MaxSize = args.MaxSize;
                this.Scale = args.Scale;

                this.LinkBoundaries ??= new();
                this.Lines ??= new();
                this.SpannableStates ??= new();
                this.SpannableOffsets ??= new();
                this.SpannableGenerations ??= new();
                this.SpanCount = spannables.Length;
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

                for (var i = 0; i < spannables.Length; i++)
                    this.SpannableStates[i] = spannables[i]?.RentRenderPass(args.RenderPass.Renderer);

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
