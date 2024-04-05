using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

using Dalamud.Interface.Spannables.EventHandlers;
using Dalamud.Interface.Spannables.Internal;
using Dalamud.Interface.Spannables.Rendering;
using Dalamud.Interface.Spannables.Styles;
using Dalamud.Interface.Spannables.Text.Internal;
using Dalamud.Interface.Utility;
using Dalamud.Utility;
using Dalamud.Utility.Numerics;
using Dalamud.Utility.Text;

using ImGuiNET;

namespace Dalamud.Interface.Spannables.Text;

#pragma warning disable SA1010

/// <summary>Spannable dealing with <see cref="StyledText"/>.</summary>
public sealed partial class StyledTextSpannable : Spannable
{
    private readonly List<TsBoundaryToRecord> linkBoundaries = [];
    private readonly List<TsMeasuredLine> lines = [];
    private readonly Spannable?[] children;
    private readonly Vector2[] childOffsets;

    private readonly TsDataMemory dataMemory;

    private TextStyle lastStyle;
    private Vector2 lastOffset;
    private Vector2 preferredSize;

    private float shiftFromVerticalAlignment;

    private int interactedLinkIndex = -1;
    private AbstractStyledText.LinkState interactedLinkState = AbstractStyledText.LinkState.Clear;

    /// <summary>Initializes a new instance of the <see cref="StyledTextSpannable"/> class.</summary>
    /// <param name="styledText">Styled text.</param>
    public StyledTextSpannable(AbstractStyledText styledText)
    {
        this.dataMemory = styledText.AsMemory();

        var data = this.dataMemory.AsSpan();
        this.children = new Spannable?[data.Children.Length];
        this.childOffsets = new Vector2[data.Children.Length];

        for (var i = 0; i < this.children.Length; i++)
        {
            if (data.Children[i]?.CreateSpannable() is not { } cs)
                continue;

            this.children[i] = cs;
            this.AddChild(cs);
        }
    }

    /// <summary>Gets a reference to the last text style used.</summary>
    public ref TextStyle LastStyle => ref this.lastStyle;

    /// <summary>Gets a reference to the last cursor offset.</summary>
    public ref Vector2 LastOffset => ref this.lastOffset;

    /// <summary>Gets the number of lines.</summary>
    public int LineCount => this.lines.Count;

    /// <summary>Gets the span of measurements of inner spannables.</summary>
    public Span<Spannable?> Children => this.children;

    /// <summary>Gets the span of offsets of inner spannables.</summary>
    public Span<Vector2> ChildOffsets => this.childOffsets;

    /// <summary>Gets the measured lines so far.</summary>
    private Span<TsMeasuredLine> MeasuredLines => CollectionsMarshal.AsSpan(this.lines);

    /// <summary>Gets the span of mapping between link range to render coordinates.</summary>
    private Span<TsBoundaryToRecord> LinkBoundaries => CollectionsMarshal.AsSpan(this.linkBoundaries);

    /// <summary>Clears measured data.</summary>
    public void ClearMeasurement()
    {
        this.Boundary = RectVector4.InvertedExtrema;
        this.linkBoundaries.Clear();
        this.lines.Clear();
        this.lastStyle = this.Style;
        this.lastOffset = Vector2.Zero;
    }

    /// <inheritdoc/>
    protected override void OnMouseDown(SpannableMouseEventArgs args)
    {
        base.OnMouseDown(args);
        if (args.SuppressHandling
            || this.interactedLinkIndex == -1
            || this.interactedLinkState is AbstractStyledText.LinkState.Clear)
            return;

        if (!this.dataMemory.AsSpan().TryGetLinkAt(this.interactedLinkIndex, out var linkData))
            return;

        var e = SpannableEventArgsPool.Rent<SpannableMouseLinkEventArgs>();
        e.Initialize(this);
        e.InitializeMouseLinkEvent(linkData.ToArray(), args.Button);
        this.OnLinkMouseDown(e);
        if (e.SuppressHandling)
        {
            SpannableEventArgsPool.Return(e);
            return;
        }

        SpannableEventArgsPool.Return(e);
        this.interactedLinkState = AbstractStyledText.LinkState.Active;
        this.CaptureMouse = true;
        args.SuppressHandling = true;
    }

    /// <inheritdoc/>
    protected override void OnMouseMove(SpannableMouseEventArgs args)
    {
        base.OnMouseMove(args);
        if (args.SuppressHandling)
            return;

        var data = this.dataMemory.AsSpan();
        var linkIndex = -1;

        if (this.IsMouseHovered)
        {
            foreach (ref var entry in this.LinkBoundaries)
            {
                if (entry.Boundary.Contains(args.LocalLocation))
                {
                    if (data.TryGetLinkAt(entry.RecordIndex, out _))
                        linkIndex = entry.RecordIndex;

                    break;
                }
            }
        }

        if (!this.dataMemory.AsSpan().TryGetLinkAt(this.interactedLinkIndex, out var prevData))
            prevData = default;
        if (!this.dataMemory.AsSpan().TryGetLinkAt(linkIndex, out var currData))
            currData = default;

        var prev = (this.interactedLinkIndex, this.interactedLinkState);
        if (this.interactedLinkState is AbstractStyledText.LinkState.Clear or AbstractStyledText.LinkState.Hovered)
        {
            this.interactedLinkIndex = linkIndex;
            this.interactedLinkState =
                linkIndex == -1 ? AbstractStyledText.LinkState.Clear : AbstractStyledText.LinkState.Hovered;
        }
        else
        {
            this.interactedLinkState =
                this.interactedLinkIndex == linkIndex
                    ? AbstractStyledText.LinkState.Active
                    : AbstractStyledText.LinkState.ActiveNotHovered;
        }

        if (prev != (this.interactedLinkIndex, this.interactedLinkState))
        {
            var e = SpannableEventArgsPool.Rent<SpannableMouseLinkEventArgs>();
            e.Initialize(this);
            switch (this.interactedLinkState)
            {
                case AbstractStyledText.LinkState.Active when currData != default:
                    e.InitializeMouseLinkEvent(currData.ToArray(), args.Button);
                    this.OnLinkMouseEnter(e);
                    break;
                case AbstractStyledText.LinkState.ActiveNotHovered when currData != default:
                    e.InitializeMouseLinkEvent(currData.ToArray(), args.Button);
                    this.OnLinkMouseLeave(e);
                    break;
                case AbstractStyledText.LinkState.Clear when prevData != default:
                    e.InitializeMouseLinkEvent(prevData.ToArray(), args.Button);
                    this.OnLinkMouseLeave(e);
                    break;
                case AbstractStyledText.LinkState.Hovered:
                    if (prevData != default)
                    {
                        e.InitializeMouseLinkEvent(prevData.ToArray(), args.Button);
                        this.OnLinkMouseLeave(e);

                        e.Initialize(this);
                    }

                    if (currData != default)
                    {
                        e.InitializeMouseLinkEvent(currData.ToArray(), args.Button);
                        this.OnLinkMouseEnter(e);
                    }

                    break;
            }

            SpannableEventArgsPool.Return(e);
        }
    }

    /// <inheritdoc/>
    protected override void OnMouseUp(SpannableMouseEventArgs args)
    {
        base.OnMouseUp(args);

        if (this.interactedLinkIndex != -1 && !args.SuppressHandling)
        {
            if (!this.dataMemory.AsSpan().TryGetLinkAt(this.interactedLinkIndex, out var currData))
                currData = default;

            var e = SpannableEventArgsPool.Rent<SpannableMouseLinkEventArgs>();
            e.Initialize(this, args.SuppressHandling);
            e.InitializeMouseLinkEvent(currData.ToArray(), args.Button);
            this.OnLinkMouseUp(e);

            if (!e.SuppressHandling && this.interactedLinkState is AbstractStyledText.LinkState.Active)
            {
                e.Initialize(this);
                this.OnLinkMouseClick(e);
            }

            SpannableEventArgsPool.Return(e);

            if (this.interactedLinkState is AbstractStyledText.LinkState.ActiveNotHovered
                or AbstractStyledText.LinkState.Clear)
                this.interactedLinkIndex = -1;
            this.interactedLinkState =
                this.interactedLinkState is AbstractStyledText.LinkState.Active
                    ? AbstractStyledText.LinkState.Hovered
                    : AbstractStyledText.LinkState.Clear;
            args.SuppressHandling = true;
        }

        this.CaptureMouse = false;
    }

    /// <inheritdoc/>
    protected override void OnMeasure(SpannableMeasureEventArgs args)
    {
        base.OnMeasure(args);

        this.preferredSize = args.PreferredSize;
        this.ClearMeasurement();

        var boundary = RectVector4.InvertedExtrema;

        var data = this.dataMemory.AsSpan();
        var segment = new TsDataSpan.Segment(data, 0, 0);
        var linkRecordIndex = -1;

        var charRenderer = new TsCharRenderer(this, data, default);
        var skipNextLine = false;
        while (true)
        {
            // Find the first line break point, only taking word wrapping into account.
            // this.FindFirstWordWrapByteOffset(args, segment, new(segment), out var line);
            var line = TsMeasuredLine.Empty;
            {
                var testSegment = segment;
                var wordBreaker = new TsWordBreaker(this, data, args.PreferredSize);
                var startOffset = new TsCompositeOffset(testSegment);
                do
                {
                    if (testSegment.TryGetRawText(out var rawText))
                    {
                        foreach (var c in rawText[(startOffset.Text - testSegment.Offset.Text)..]
                                     .EnumerateUtf(UtfEnumeratorFlags.Utf8))
                        {
                            var currentOffset = new TsCompositeOffset(
                                startOffset.Text + c.ByteOffset,
                                testSegment.Offset.Record);
                            var nextOffset = currentOffset.AddTextOffset(c.ByteLength);

                            var pad = 0f;
                            if (this.DisplayControlCharacters &&
                                c.Value.ShortName is { IsEmpty: false } name)
                            {
                                var ssb = this.Renderer.RentBuilder();
                                ssb.Clear().Append(name);

                                var ccm = ssb.CreateSpannable();
                                ccm.RenderScale = this.EffectiveRenderScale;
                                ccm.ControlCharactersStyle = this.ControlCharactersStyle;
                                ccm.RenderPassMeasure(new(float.PositiveInfinity));

                                if (ccm.Boundary.IsValid)
                                {
                                    pad = MathF.Ceiling(ccm.Boundary.Width * this.EffectiveRenderScale) /
                                          this.EffectiveRenderScale;
                                    wordBreaker.ResetLastChar();
                                }

                                this.Renderer.ReturnBuilder(ssb);
                            }

                            switch (c.Value.IntValue)
                            {
                                case '\r'
                                    when testSegment.Data.TryGetCodepointAt(
                                             nextOffset.Text,
                                             0,
                                             out var nextCodepoint)
                                         && nextCodepoint == '\n'
                                         && (this.AcceptedNewLines & NewLineType.CrLf) != 0:
                                    line = wordBreaker.Last;
                                    line.SetOffset(nextOffset.AddTextOffset(1), this.EffectiveRenderScale, pad);
                                    line.HasNewLineAtEnd = true;
                                    wordBreaker.UnionLineBBoxVertical(ref line);
                                    break;

                                case '\r' when (this.AcceptedNewLines & NewLineType.Cr) != 0:
                                case '\n' when (this.AcceptedNewLines & NewLineType.Lf) != 0:
                                    line = wordBreaker.Last;
                                    line.SetOffset(nextOffset, this.EffectiveRenderScale, pad);
                                    line.HasNewLineAtEnd = true;
                                    wordBreaker.UnionLineBBoxVertical(ref line);
                                    break;

                                case '\r' or '\n':
                                    line = wordBreaker.AddCodepointAndMeasure(
                                        currentOffset,
                                        nextOffset,
                                        -1,
                                        pad: pad);
                                    break;

                                default:
                                    line = wordBreaker.AddCodepointAndMeasure(
                                        currentOffset,
                                        nextOffset,
                                        c.EffectiveChar,
                                        pad: pad);
                                    break;
                            }

                            if (!line.IsEmpty)
                                break;
                        }

                        startOffset = new(testSegment.Offset.Text + rawText.Length, testSegment.Offset.Record);
                    }
                    else if (testSegment.TryGetRecord(out var record, out var recordData))
                    {
                        line = wordBreaker.HandleSpan(record, recordData, new(testSegment), new(testSegment, 0, 1));
                    }
                }
                while (line.IsEmpty && testSegment.TryGetNext(out testSegment));

                if (line.IsEmpty)
                {
                    line = wordBreaker.Last;
                    line.SetOffset(
                        new(testSegment.Offset.Text, testSegment.Offset.Record),
                        this.EffectiveRenderScale,
                        0f);
                }
            }

            line.FirstOffset = new(segment);
            var lineSegment = new TsDataSpan.Segment(data, line.Offset.Text, line.Offset.Record);

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
                this.lines.Add(line);
                charRenderer.SetLine(line, this.preferredSize);

                var accumulatedBoundary = RectVector4.InvertedExtrema;

                for (var seg = segment; seg.Offset < line.Offset;)
                {
                    if (seg.TryGetRawText(out var rawText))
                    {
                        foreach (var c in rawText.EnumerateUtf(UtfEnumeratorFlags.Utf8))
                        {
                            var absOffset = new TsCompositeOffset(seg, c.ByteOffset);
                            if (absOffset.Text < seg.Offset.Text)
                                continue;
                            if (absOffset.Text >= line.OmitOffset.Text)
                                break;

                            if (this.DisplayControlCharacters)
                            {
                                var name = c.Value.ShortName;
                                if (!name.IsEmpty)
                                {
                                    var offset = charRenderer.StyleTranslation;
                                    this.lastOffset += offset;
                                    var old = charRenderer.UpdateTextStyle(this.ControlCharactersStyle);
                                    charRenderer.LastRendered.Clear();
                                    foreach (var c2 in name)
                                        charRenderer.RenderOne(c2);
                                    charRenderer.LastRendered.Clear();
                                    _ = charRenderer.UpdateTextStyle(old);
                                    this.lastOffset -= offset;
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
                                this.UpdateAndResetBoundary(ref boundary, ref accumulatedBoundary, linkRecordIndex);
                                linkRecordIndex = -1;
                                break;

                            case SpannedRecordType.Link
                                when SpannedRecordCodec.TryDecodeLink(recordData, out var link):
                                this.UpdateAndResetBoundary(ref boundary, ref accumulatedBoundary, linkRecordIndex);
                                linkRecordIndex = record.IsRevert || link.IsEmpty ? -1 : seg.Offset.Record;
                                break;
                        }

                        accumulatedBoundary = RectVector4.Union(
                            accumulatedBoundary,
                            charRenderer.HandleSpanRecord(record, recordData));
                    }

                    if (!seg.TryGetNext(out seg))
                        break;
                }

                accumulatedBoundary = RectVector4.Union(
                    accumulatedBoundary,
                    this.ProcessPostLine(line, ref charRenderer, default));

                this.UpdateAndResetBoundary(ref boundary, ref accumulatedBoundary, linkRecordIndex);
                this.ExtendBoundaryDownward(ref boundary, this.lastOffset.Y + charRenderer.MostRecentLineHeight);
            }
            else
            {
                for (var seg = segment; seg.Offset < line.OmitOffset;)
                {
                    if (seg.TryGetRecord(out var record, out var recordData))
                    {
                        charRenderer.HandleSpanRecord(record, recordData);
                    }

                    if (!seg.TryGetNext(out seg))
                        break;
                }

                if (line.HasNewLineAtEnd ||
                    (line.IsWrapped && this.WordBreak != WordBreakType.KeepAll))
                    this.AddLineBreak(line);
            }

            if (lineSegment.Offset == data.EndOffset)
                break;
            segment = lineSegment;
            if (this.WordBreak == WordBreakType.KeepAll)
            {
                if (skipNextLine && !line.IsWrapped)
                    this.MeasuredLines[^1].HasNewLineAtEnd = true;
                skipNextLine = line.IsWrapped;
            }
        }

        this.ExtendBoundaryDownward(ref boundary, this.lastOffset.Y);

        if (!boundary.IsValid)
            boundary = default;
        else
            boundary.Right += 1;

        // if (this.Size.X < float.PositiveInfinity)
        //     this.boundary.Right = this.Size.X;
        // if (this.Size.Y < float.PositiveInfinity)
        //     this.boundary.Bottom = this.Size.Y;

        if (this.VerticalAlignment > 0f && args.PreferredSize.Y < float.PositiveInfinity)
        {
            var offset =
                MathF.Round(
                    (args.PreferredSize.Y - boundary.Height) *
                    Math.Clamp(this.VerticalAlignment, 0f, 1f) *
                    this.EffectiveRenderScale) /
                this.EffectiveRenderScale;
            this.TranslateSubBoundaries(ref boundary, new(0, offset), data);
            this.shiftFromVerticalAlignment = offset;
        }
        else
        {
            this.shiftFromVerticalAlignment = 0;
        }

        this.Boundary = boundary;
    }

    /// <inheritdoc/>
    protected override void OnPlace(SpannableEventArgs args)
    {
        base.OnPlace(args);

        var o = this.ChildOffsets;
        var c = this.Children;
        for (var i = 0; i < o.Length; i++)
            c[i]?.RenderPassPlace(Matrix4x4.CreateTranslation(new(o[i], 0)), this.FullTransformation);
    }

    /// <inheritdoc/>
    protected override unsafe void OnDraw(SpannableDrawEventArgs args)
    {
        base.OnDraw(args);

        var data = this.dataMemory.AsSpan();

        this.lastOffset = new(0, this.shiftFromVerticalAlignment);
        this.lastStyle = this.Style;

        var charRenderer = new TsCharRenderer(this, data, args.DrawListPtr);
        try
        {
            var segment = new TsDataSpan.Segment(data, 0, 0);
            foreach (ref readonly var line in this.MeasuredLines)
            {
                charRenderer.SetLine(line, this.preferredSize);

                while (segment.Offset < line.Offset)
                {
                    if (segment.TryGetRawText(out var rawText))
                    {
                        var lineHasMoreText = true;
                        foreach (var c in rawText.EnumerateUtf(UtfEnumeratorFlags.Utf8))
                        {
                            var absOffset = new TsCompositeOffset(segment, c.ByteOffset);
                            if (absOffset < line.FirstOffset)
                                continue;
                            if (absOffset >= line.Offset)
                            {
                                lineHasMoreText = false;
                                break;
                            }

                            if (absOffset < line.OmitOffset)
                            {
                                if (this.DisplayControlCharacters)
                                {
                                    var name = c.Value.ShortName;
                                    if (!name.IsEmpty)
                                    {
                                        var offset = charRenderer.StyleTranslation;
                                        this.lastOffset += offset;
                                        var old = charRenderer.UpdateTextStyle(
                                            this.ControlCharactersStyle);
                                        charRenderer.LastRendered.Clear();
                                        foreach (var c2 in name)
                                            charRenderer.RenderOne(c2);
                                        charRenderer.LastRendered.Clear();
                                        _ = charRenderer.UpdateTextStyle(old);
                                        this.lastOffset -= offset;
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
                            charRenderer.HandleSpanRecord(record, recordData);
                        }
                    }

                    if (!segment.TryGetNext(out segment))
                        break;
                }

                // TODO: render to correct place?
                this.ProcessPostLine(line, ref charRenderer, args.DrawListPtr);
            }

            foreach (var entry in this.LinkBoundaries)
            {
                if (entry.RecordIndex != this.interactedLinkIndex)
                    continue;

                var color = this.interactedLinkState switch
                {
                    AbstractStyledText.LinkState.Hovered => ImGui.GetColorU32(ImGuiCol.ButtonHovered),
                    AbstractStyledText.LinkState.Active => ImGui.GetColorU32(ImGuiCol.ButtonActive),
                    AbstractStyledText.LinkState.Clear => 0u,
                    _ => 0u,
                };
                if (color == 0u)
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
        finally
        {
            charRenderer.AppendAndReturnChannels(this.LocalTransformation);
        }
    }

    /// <summary>Extends the bottom boundary by given amount.</summary>
    /// <param name="boundary">Mutable reference to the boundary accumulator.</param>
    /// <param name="b">The amount to extend.</param>
    private void ExtendBoundaryDownward(ref RectVector4 boundary, float b)
    {
        if (boundary.IsValid)
            boundary.Bottom = Math.Max(boundary.Bottom, b);
    }

    /// <summary>Adds a line break.</summary>
    /// <param name="lineBefore">The line that came right before this line break.</param>
    private void AddLineBreak(in TsMeasuredLine lineBefore) =>
        this.lastOffset = new(
            0,
            MathF.Round((this.lastOffset.Y + lineBefore.Height) * this.EffectiveRenderScale) /
            this.EffectiveRenderScale);

    /// <summary>Add decorations and breaks line once a line ends.</summary>
    /// <param name="line">The line that came right before this.</param>
    /// <param name="charRenderer">The renderer.</param>
    /// <param name="drawListPtr">The draw list to draw to.</param>
    /// <returns>The additional region to add to boundary, from adding decorations.</returns>
    private unsafe RectVector4 ProcessPostLine(
        in TsMeasuredLine line,
        ref TsCharRenderer charRenderer,
        ImDrawListPtr drawListPtr)
    {
        var accumulatedBoundary = RectVector4.InvertedExtrema;
        if (line.IsWrapped)
        {
            if (line.LastThing.IsCodepoint(0x00AD) && this.WordBreak != WordBreakType.KeepAll)
            {
                accumulatedBoundary = RectVector4.Union(
                    accumulatedBoundary,
                    charRenderer.RenderOne(AbstractStyledText.SoftHyphenReplacementChar));
            }

            if (this.WrapMarker is { } wm)
            {
                var wmm = wm.CreateSpannable();
                wmm.RenderScale = this.EffectiveRenderScale;
                wmm.RenderPassMeasure(new(float.PositiveInfinity));

                if (wmm.Boundary.IsValid)
                {
                    if (drawListPtr.NativePtr is not null)
                    {
                        var wmLocalTransformation = Matrix4x4.CreateTranslation(
                            new(this.lastOffset + charRenderer.StyleTranslation, 0));
                        if (this.lastStyle.Italic)
                        {
                            wmLocalTransformation = Matrix4x4.Multiply(
                                wmLocalTransformation,
                                new Matrix4x4(
                                    Matrix3x2.CreateSkew(MathF.Atan(-1 / TextStyleFontData.FakeItalicDivisor), 0)));
                        }

                        wmm.RenderPassPlace(wmLocalTransformation, this.FullTransformation);

                        var tmpDrawList = this.Renderer.RentDrawList(drawListPtr);
                        try
                        {
                            wmm.RenderPassDraw(tmpDrawList);
                            tmpDrawList.CopyDrawListDataTo(drawListPtr, this.LocalTransformation, Vector4.One);
                        }
                        finally
                        {
                            this.Renderer.ReturnDrawList(tmpDrawList);
                        }
                    }

                    accumulatedBoundary = RectVector4.Union(
                        accumulatedBoundary,
                        RectVector4.Translate(
                            wmm.Boundary,
                            this.lastOffset + charRenderer.StyleTranslation));
                    this.lastOffset.X += wmm.Boundary.Right;
                    charRenderer.LastRendered.Clear();
                }
            }
        }

        if (line.HasNewLineAtEnd || (line.IsWrapped && this.WordBreak != WordBreakType.KeepAll))
            this.AddLineBreak(line);
        return accumulatedBoundary;
    }

    /// <summary>Updates <see cref="Spannable.Boundary"/> and <see cref="linkBoundaries"/>, and resets
    /// <paramref name="accumulator"/>.</summary>
    /// <param name="boundary">Mutable reference to the boundary accumulator.</param>
    /// <param name="accumulator">Mutalbe reference to the temporary accumulator.</param>
    /// <param name="linkRecordIndex">The link record index.</param>
    private void UpdateAndResetBoundary(ref RectVector4 boundary, ref RectVector4 accumulator, int linkRecordIndex)
    {
        if (!accumulator.IsValid)
            return;

        if (linkRecordIndex != -1)
            this.linkBoundaries!.Add(new(linkRecordIndex, accumulator));

        boundary = RectVector4.Union(accumulator, boundary);
        accumulator = RectVector4.InvertedExtrema;
    }

    /// <summary>Translates the boundaries of inner boundary rectangles.</summary>
    /// <param name="boundary">Mutable reference to the boundary accumulator.</param>
    /// <param name="translation">The translation amount.</param>
    /// <param name="data">The data.</param>
    private void TranslateSubBoundaries(ref RectVector4 boundary, Vector2 translation, TsDataSpan data)
    {
        foreach (ref var b in this.LinkBoundaries)
            b.Boundary = RectVector4.Translate(b.Boundary, translation);
        foreach (ref var v in this.ChildOffsets[..data.Children.Length])
            v += translation;
        boundary = RectVector4.Translate(boundary, translation);
    }
}
