using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text.Unicode;

using Dalamud.Interface.SpannedStrings.Enums;
using Dalamud.Utility;
using Dalamud.Utility.Text;

using ImGuiNET;

namespace Dalamud.Interface.SpannedStrings.Internal;

/// <summary>A custom text renderer implementation.</summary>
internal sealed unsafe partial class SpannedStringRenderer : ISpannedStringRenderer
{
    /// <summary>The display character in place of a soft hyphen character.</summary>
    public const char SoftHyphenReplacementChar = '-';

    /// <summary>The total number of channels.</summary>
    public const int TotalChannels = 6;

    /// <summary>The text decoration channel.</summary>
    public const int TextDecorationThroughChannel = 5;

    /// <summary>The foreground channel.</summary>
    public const int ForeChannel = 4;

    /// <summary>The text decoration channel.</summary>
    public const int TextDecorationOverUnderChannel = 3;

    /// <summary>The border channel.</summary>
    public const int BorderChannel = 2;

    /// <summary>The shadow channel.</summary>
    public const int ShadowChannel = 1;

    /// <summary>The background channel.</summary>
    public const int BackChannel = 0;

    private const int CImGuiSetActiveIdOffset = 0x483f0;
    private const int CImGuiSetHoverIdOffset = 0x48e80;
    private const int CImGuiContextCurrentWindowOffset = 0x3ff0;

    private static readonly BitArray WordBreakNormalBreakChars;
    private static readonly delegate* unmanaged<uint, nint, void> ImGuiSetActiveId;
    private static readonly delegate* unmanaged<uint, void> ImGuiSetHoveredId;

    private readonly SpannableFactory factory;
    private readonly SpannedStringBuilder builder;

    /// <summary>Stores which links are rendered at which coordinates.</summary>
    private readonly List<LinkRangeToRenderCoordinates> linkRenderCoordinatesList = new();

    /// <summary>The pointer to our unique instance of <see cref="ImDrawListSplitter"/>. Practically readonly, but for
    /// the sake of clearing on <see cref="DisposeInternal"/>, not actually set as readonly.</summary>
    private ImDrawListSplitter* splitterPtr;

    /// <summary>Whether <see cref="Render(out RenderState, out ReadOnlySpan{byte})"/> has been called.</summary>
    private bool rendered;

    /// <summary>Set from <see cref="Initialize"/>.</summary>
    private Options options;

    /// <summary>Initializes static members of the <see cref="SpannedStringRenderer"/> class.</summary>
    static SpannedStringRenderer()
    {
        _ = ImGui.GetCurrentContext();

        var cimgui = Process.GetCurrentProcess().Modules.Cast<ProcessModule>()
                            .First(x => x.ModuleName == "cimgui.dll")
                            .BaseAddress;
        ImGuiSetActiveId = (delegate* unmanaged<uint, IntPtr, void>)(cimgui + CImGuiSetActiveIdOffset);
        ImGuiSetHoveredId = (delegate* unmanaged<uint, void>)(cimgui + CImGuiSetHoverIdOffset);

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

    /// <summary>Initializes a new instance of the <see cref="SpannedStringRenderer"/> class.</summary>
    /// <param name="factory">Owner to return on <see cref="Dispose"/>.</param>
    public SpannedStringRenderer(SpannableFactory factory)
    {
        this.factory = factory;
        this.splitterPtr = ImGuiNative.ImDrawListSplitter_ImDrawListSplitter();
        this.builder = new();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.builder.TryReset();
        this.linkRenderCoordinatesList.Clear();
        this.options = default;
        this.factory.Return(this);
    }

    /// <summary>Clear the resources used by this instance.</summary>
    public void DisposeInternal()
    {
        if (this.splitterPtr is null)
            return;

        ImGuiNative.ImDrawListSplitter_destroy(this.splitterPtr);
        this.splitterPtr = null;
    }

    /// <inheritdoc/>
    public void Render() => this.Render(out _, out _);

    /// <inheritdoc/>
    public void Render(out RenderState state) => this.Render(out state, out _);

    /// <inheritdoc/>
    public bool Render(out RenderState state, out ReadOnlySpan<byte> hoveredLink) =>
        this.Render(this.builder.GetData(), out state, out hoveredLink);

    /// <inheritdoc/>
    public bool Render(SpannedString spannedString, out RenderState state, out ReadOnlySpan<byte> hoveredLink) =>
        this.Render(spannedString.GetData(), out state, out hoveredLink);

    /// <summary>Renders the given spannable data.</summary>
    /// <param name="data">The spannable data.</param>
    /// <param name="state">The final render state.</param>
    /// <param name="hoveredLink">The payload being hovered, if any.</param>
    /// <returns><c>true</c> if any payload is currently being hovered.</returns>
    private bool Render(SpannedStringData data, out RenderState state, out ReadOnlySpan<byte> hoveredLink)
    {
        if (this.rendered)
        {
            state = default;
            hoveredLink = default;
            return false;
        }

        state = new()
        {
            StartScreenOffset = ImGui.GetCursorScreenPos(),
            Offset = Vector2.Zero,
            BoundsLeftTop = new(float.MaxValue),
            BoundsRightBottom = new(float.MinValue),
            LastLineIndex = 0,
            ClickedMouseButton = unchecked((ImGuiMouseButton)(-1)),
            LastStyle = this.options.InitialSpanStyle,
            LastMeasurement = default,
        };

        if (this.options.DrawListPtr is not null)
            ImGuiNative.ImDrawListSplitter_Split(this.splitterPtr, this.options.DrawListPtr, TotalChannels);

        var linkEntity = default(SpannedRecord);
        var dropUntilNextNewline = false;
        var charRenderer = new CharRenderer(this, data, ref state, false);
        foreach (var segment in data)
        {
            if (new SpannedOffset(segment) >= state.LastMeasurement.Offset)
            {
                this.OnMeasuredLineEnd(ref state, ref charRenderer, in linkEntity, ref dropUntilNextNewline);
                this.FindFirstWordWrapByteOffset(ref state, segment, new(segment), ref dropUntilNextNewline);
                charRenderer.SpanFontOptionsUpdated();
            }

            if (segment.TryGetRawText(out var rawText))
            {
                foreach (var c in rawText.EnumerateUtf(UtfEnumeratorFlags.Utf8))
                {
                    var absOffset = new SpannedOffset(segment, c.ByteOffset);
                    if (absOffset >= state.LastMeasurement.Offset)
                    {
                        this.OnMeasuredLineEnd(ref state, ref charRenderer, in linkEntity, ref dropUntilNextNewline);
                        this.FindFirstWordWrapByteOffset(ref state, segment, absOffset, ref dropUntilNextNewline);
                        charRenderer.SpanFontOptionsUpdated();
                    }

                    if (dropUntilNextNewline)
                    {
                        state.LastMeasurement.LastThing.SetCodepoint(c.Value);
                        continue;
                    }

                    if (this.options.UseControlCharacter)
                    {
                        var name = c.Value.ShortName;
                        if (!name.IsEmpty)
                        {
                            var offset = charRenderer.StyleTranslation;
                            state.Offset += offset;
                            var old = charRenderer.UpdateSpanParams(this.options.ControlCharactersSpanStyle);
                            state.LastMeasurement.LastThing.Clear();
                            foreach (var c2 in name)
                                charRenderer.RenderChar(c2);
                            state.LastMeasurement.LastThing.Clear();
                            _ = charRenderer.UpdateSpanParams(old);
                            state.Offset -= offset;
                        }
                    }

                    charRenderer.RenderChar(c.EffectiveChar);
                    state.LastMeasurement.LastThing.SetCodepoint(c.Value);
                }
            }
            else if (segment.TryGetRecord(out var record, out var recordData))
            {
                switch (record.Type)
                {
                    case SpannedRecordType.Link when record.IsRevert:
                        this.OnLinkOrRenderEnd(ref state, ref charRenderer, linkEntity);
                        linkEntity = default;
                        break;

                    case SpannedRecordType.Link
                        when SpannedRecordCodec.TryDecodeLink(recordData, out var link):
                        this.OnLinkOrRenderEnd(ref state, ref charRenderer, linkEntity);
                        linkEntity = record.IsRevert || link.IsEmpty ? default : record;
                        break;

                    case SpannedRecordType.InsertionManualNewLine
                        when (this.options.AcceptedNewLines & NewLineType.Manual) != 0:
                        this.OnLinkOrRenderEnd(ref state, ref charRenderer, linkEntity);
                        this.BreakLineImmediate(ref state, ref charRenderer);
                        state.LastMeasurement.LastThing.SetRecord(segment.RecordIndex);
                        dropUntilNextNewline = false;
                        break;
                }

                charRenderer.HandleSpan(record, recordData, dropUntilNextNewline);
            }
        }

        this.OnLinkOrRenderEnd(ref state, ref charRenderer, linkEntity);
        state.BoundsRightBottom.Y = Math.Max(state.BoundsRightBottom.Y, state.Offset.Y);

        hoveredLink = default;
        if (this.options is { DrawListPtr: not null, UseLinks: true, ImGuiGlobalId: not 0u })
        {
            ref var itemState = ref *(ItemStateStruct*)ImGui.GetStateStorage().GetVoidPtrRef(
                                        this.options.ImGuiGlobalId,
                                        nint.Zero);
            var mouse = ImGui.GetMousePos();
            var mouseRel = mouse - state.StartScreenOffset;
            var hoveredLinkDataBegin = -1;
            if (ImGui.IsWindowHovered() || itemState.IsMouseButtonDownHandled)
            {
                foreach (var entry in this.linkRenderCoordinatesList)
                {
                    if (entry.LeftTop.X <= mouseRel.X
                        && entry.LeftTop.Y <= mouseRel.Y
                        && mouseRel.X < entry.RightBottom.X
                        && mouseRel.Y < entry.RightBottom.Y)
                    {
                        hoveredLinkDataBegin = entry.DataBegin;
                        if (hoveredLinkDataBegin == -1
                            || !SpannedRecordCodec.TryDecodeLink(
                                data.DataStream[entry.DataBegin..entry.DataEnd],
                                out hoveredLink))
                            hoveredLink = default;
                        break;
                    }
                }
            }

            var lmb = ImGui.IsMouseDown(ImGuiMouseButton.Left);
            var mmb = ImGui.IsMouseDown(ImGuiMouseButton.Middle);
            var rmb = ImGui.IsMouseDown(ImGuiMouseButton.Right);
            if (itemState.IsMouseButtonDownHandled)
            {
                switch (itemState.FirstMouseButton)
                {
                    case ImGuiMouseButton.Left when !lmb && itemState.LinkDataOffset == hoveredLinkDataBegin:
                        state.ClickedMouseButton = ImGuiMouseButton.Left;
                        itemState.IsMouseButtonDownHandled = false;
                        break;
                    case ImGuiMouseButton.Right when !rmb && itemState.LinkDataOffset == hoveredLinkDataBegin:
                        state.ClickedMouseButton = ImGuiMouseButton.Right;
                        itemState.IsMouseButtonDownHandled = false;
                        break;
                    case ImGuiMouseButton.Middle when !mmb && itemState.LinkDataOffset == hoveredLinkDataBegin:
                        state.ClickedMouseButton = ImGuiMouseButton.Middle;
                        itemState.IsMouseButtonDownHandled = false;
                        break;
                }

                if (!lmb && !rmb && !mmb)
                {
                    itemState.IsMouseButtonDownHandled = false;
                    ImGuiSetActiveId(0, 0);
                }

                if (itemState.LinkDataOffset != hoveredLinkDataBegin)
                {
                    hoveredLinkDataBegin = -1;
                    hoveredLink = default;
                }
            }

            if (hoveredLinkDataBegin == -1)
            {
                itemState.LinkDataOffset = -1;
            }
            else
            {
                ImGuiSetHoveredId(this.options.ImGuiGlobalId);
                ImGuiNative.ImDrawListSplitter_SetCurrentChannel(
                    this.splitterPtr,
                    this.options.DrawListPtr,
                    BackChannel);

                if (!itemState.IsMouseButtonDownHandled && (lmb || rmb || mmb))
                {
                    itemState.LinkDataOffset = hoveredLinkDataBegin;
                    itemState.IsMouseButtonDownHandled = true;
                    itemState.FirstMouseButton = lmb ? ImGuiMouseButton.Left :
                                                 rmb ? ImGuiMouseButton.Right : ImGuiMouseButton.Middle;
                    ImGuiSetActiveId(
                        this.options.ImGuiGlobalId,
                        *(nint*)(ImGui.GetCurrentContext() + CImGuiContextCurrentWindowOffset));
                }

                var color =
                    itemState.IsMouseButtonDownHandled
                        ? itemState.LinkDataOffset == hoveredLinkDataBegin
                              ? ImGui.GetColorU32(ImGuiCol.ButtonActive)
                              : 0u
                        : ImGui.GetColorU32(ImGuiCol.ButtonHovered);

                if (color != 0)
                {
                    var rounding = ImGui.GetStyle().FrameRounding;
                    foreach (var entry in this.linkRenderCoordinatesList)
                    {
                        if (entry.DataBegin != hoveredLinkDataBegin)
                            continue;

                        ImGuiNative.ImDrawList_AddRectFilled(
                            this.options.DrawListPtr,
                            state.StartScreenOffset + entry.LeftTop,
                            state.StartScreenOffset + entry.RightBottom,
                            color,
                            rounding,
                            ImDrawFlags.None);
                    }
                }
            }
        }

        if (this.options.DrawListPtr is not null)
            ImGuiNative.ImDrawListSplitter_Merge(this.splitterPtr, this.options.DrawListPtr);

        if (this.options.PutDummyAfterRender)
        {
            if (state.BoundsRightBottom is { X: >= 0, Y: >= 0 })
                ImGui.Dummy(state.BoundsRightBottom);
        }

        this.rendered = true;
        return !hoveredLink.IsEmpty;
    }

    /// <summary>Finds the first newline sequence (CR, LF, or CRLF).</summary>
    /// <param name="span">The span to look the sequence from.</param>
    /// <returns>The first index after the newline sequence, or the length of <paramref name="span"/>.</returns>
    private int FindFirstNewLine(ReadOnlySpan<byte> span)
    {
        if (this.options.AcceptedNewLines == NewLineType.None)
            return span.Length;

        var acceptCrLf = (this.options.AcceptedNewLines & NewLineType.CrLf) != 0;
        var acceptCr = (this.options.AcceptedNewLines & NewLineType.Cr) != 0;
        var acceptLf = (this.options.AcceptedNewLines & NewLineType.Lf) != 0;
        for (var i = 0; i < span.Length; i++)
        {
            if (acceptCrLf && span[i..].StartsWith("\r\n"u8))
                return i + 2;

            if (acceptCr && span[i] == '\r')
                return i + 1;

            if (acceptLf && span[i] == '\n')
                return i + 1;
        }

        return span.Length;
    }

    private void OnMeasuredLineEnd(
        ref RenderState state,
        ref CharRenderer charRenderer,
        in SpannedRecord linkEntity,
        ref bool dropUntilNextNewline)
    {
        if (state.LastMeasurement.HasNewLineAtEnd)
        {
            this.OnLinkOrRenderEnd(ref state, ref charRenderer, linkEntity);
            this.BreakLineImmediate(ref state, ref charRenderer);
        }
        else if (state.LastMeasurement.IsWrapped)
        {
            if (state.LastMeasurement.LastThing.IsCodepoint(0x00AD) && this.options.WordBreak != WordBreakType.KeepAll)
                charRenderer.RenderChar(SoftHyphenReplacementChar);

            if (this.options.UseWrapMarker && !dropUntilNextNewline)
            {
                if (this.options.UseWrapMarkerParams)
                {
                    var offset = charRenderer.StyleTranslation;
                    state.Offset += offset;
                    var old = charRenderer.UpdateSpanParams(this.options.WrapMarkerStyle);
                    foreach (var c2 in this.options.WrapMarker)
                        charRenderer.RenderChar(c2);
                    _ = charRenderer.UpdateSpanParams(old);
                    state.Offset -= offset;
                }
                else
                {
                    foreach (var c2 in this.options.WrapMarker)
                        charRenderer.RenderChar(c2);
                }
            }

            this.OnLinkOrRenderEnd(ref state, ref charRenderer, linkEntity);

            if (this.options.WordBreak == WordBreakType.KeepAll)
                dropUntilNextNewline = true;
            else
                this.BreakLineImmediate(ref state, ref charRenderer);
        }
    }

    private void OnLinkOrRenderEnd(
        ref RenderState state,
        ref CharRenderer charRenderer,
        in SpannedRecord linkRecord)
    {
        if (!(charRenderer.BoundsLeftTop.X <= charRenderer.BoundsRightBottom.X)
            || !(charRenderer.BoundsLeftTop.Y <= charRenderer.BoundsRightBottom.Y))
        {
            // Nothing has been rendered since the last call to this function.
            return;
        }
        
        if (linkRecord.Type == SpannedRecordType.Link)
        {
            this.linkRenderCoordinatesList.Add(
                new()
                {
                    DataBegin = linkRecord.DataStart,
                    DataEnd = linkRecord.DataStart + linkRecord.DataLength,
                    LeftTop = charRenderer.BoundsLeftTop,
                    RightBottom = charRenderer.BoundsRightBottom,
                });
        }
            
        state.BoundsLeftTop = Vector2.Min(state.BoundsLeftTop, charRenderer.BoundsLeftTop);
        state.BoundsRightBottom = Vector2.Max(state.BoundsRightBottom, charRenderer.BoundsRightBottom);
        charRenderer.BoundsLeftTop = new(float.MaxValue);
        charRenderer.BoundsRightBottom = new(float.MinValue);
    }

    /// <summary>Forces a line break.</summary>
    private void BreakLineImmediate(ref RenderState state, ref CharRenderer charRenderer)
    {
        state.LastLineIndex++;
        state.Offset = new(0, MathF.Round(state.Offset.Y + state.LastMeasurement.Height));
        state.BoundsRightBottom.Y = Math.Max(state.BoundsRightBottom.Y, state.Offset.Y + charRenderer.LastLineHeight);
    }

    /// <summary>Finds the first line break point, only taking word wrapping into account.</summary>
    /// <param name="state">The accumulated render state.</param>
    /// <param name="segment">The current segment.</param>
    /// <param name="lineStartOffset">The line to start looking from.</param>
    /// <param name="dropUntilNextNewline">Do not render until the next new line.</param>
    private void FindFirstWordWrapByteOffset(
        ref RenderState state,
        SpannedStringData.Segment segment,
        SpannedOffset lineStartOffset,
        ref bool dropUntilNextNewline)
    {
        ref var measuredLine = ref state.LastMeasurement;
        measuredLine = MeasuredLine.Empty;

        var wordBreaker = new WordBreaker(this, segment.Data, in state);
        var startOffset = lineStartOffset;
        do
        {
            if (segment.TryGetRawText(out var rawText))
            {
                foreach (var c in rawText[(startOffset.Text - segment.TextOffset)..]
                             .EnumerateUtf(UtfEnumeratorFlags.Utf8))
                {
                    var currentOffset = new SpannedOffset(startOffset.Text + c.ByteOffset, segment.RecordIndex);
                    var nextOffset = currentOffset.AddTextOffset(c.ByteLength);

                    var pad = 0f;
                    if (this.options.UseControlCharacter && c.Value.ShortName is { IsEmpty: false } name)
                    {
                        var state2 = new RenderState { LastStyle = this.options.ControlCharactersSpanStyle };
                        var measurePass = new CharRenderer(this, segment.Data, ref state2, true);
                        foreach (var c2 in name)
                            measurePass.RenderChar(c2);
                        if (measurePass.BoundsRightBottom.X > measurePass.BoundsLeftTop.X)
                        {
                            pad = MathF.Round(measurePass.BoundsRightBottom.X - measurePass.BoundsLeftTop.X);
                            wordBreaker.ResetLastChar();
                        }
                    }

                    switch (c.Value.IntValue)
                    {
                        case '\r'
                            when nextOffset.Text < segment.Data.TextStream.Length
                                 && segment.Data.TextStream[nextOffset.Text] == '\n'
                                 && (this.options.AcceptedNewLines & NewLineType.CrLf) != 0:
                            measuredLine = wordBreaker.Last;
                            measuredLine.SetOffset(nextOffset.AddTextOffset(1), pad);
                            measuredLine.HasNewLineAtEnd = true;
                            dropUntilNextNewline = false;
                            return;

                        case '\r' when (this.options.AcceptedNewLines & NewLineType.Cr) != 0:
                        case '\n' when (this.options.AcceptedNewLines & NewLineType.Lf) != 0:
                            measuredLine = wordBreaker.Last;
                            measuredLine.SetOffset(nextOffset, pad);
                            measuredLine.HasNewLineAtEnd = true;
                            dropUntilNextNewline = false;
                            return;

                        case '\r' or '\n':
                            measuredLine = wordBreaker.AddCodepointAndMeasure(currentOffset, nextOffset, -1, pad: pad);
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

                startOffset = new(segment.TextOffset + rawText.Length, segment.RecordIndex);
            }
            else if (segment.TryGetRecord(out var record, out var recordData))
            {
                measuredLine = wordBreaker.HandleSpan(record, recordData, new(segment), new(segment, 0, 1));
                if (measuredLine.HasNewLineAtEnd)
                    dropUntilNextNewline = false;
                if (!measuredLine.IsEmpty)
                    return;
            }
        }
        while (segment.TryGetNext(out segment));

        measuredLine = wordBreaker.Last;
        measuredLine.SetOffset(new(segment.TextOffset, segment.RecordIndex));
    }
}
