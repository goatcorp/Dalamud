using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Data;
using Dalamud.Interface.Internal;
using Dalamud.Interface.SpannedStrings.Enums;
using Dalamud.Interface.SpannedStrings.Internal;
using Dalamud.Interface.SpannedStrings.Spannables;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Utility;
using Dalamud.Utility.Text;

using ImGuiNET;

using Lumina.Data.Files;

namespace Dalamud.Interface.SpannedStrings.Rendering.Internal;

/// <summary>A custom text renderer implementation.</summary>
[ServiceManager.EarlyLoadedService]
[PluginInterface]
[InterfaceVersion("1.0")]
#pragma warning disable SA1015
[ResolveVia<ISpannableRenderer>]
#pragma warning restore SA1015
internal sealed unsafe partial class SpannableRenderer : ISpannableRenderer, IInternalDisposableService
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

    [ServiceManager.ServiceDependency]
    private readonly DataManager dataManager = Service<DataManager>.Get();

    [ServiceManager.ServiceDependency]
    private readonly TextureManager textureManager = Service<TextureManager>.Get();

    /// <summary>Stores which links are rendered at which coordinates.</summary>
    private readonly List<LinkRangeToRenderCoordinates> linkRenderCoordinatesList = new();

    [ServiceManager.ServiceConstructor]
    private SpannableRenderer(InterfaceManager.InterfaceManagerWithScene imws)
    {
        var t = this.dataManager.GetFile("common/font/gfdata.gfd")!.Data;
        t.CopyTo((this.gfdFile = GC.AllocateUninitializedArray<byte>(t.Length, true)).AsSpan());
        this.gfdTextures =
            GfdTexturePaths
                .Select(x => this.textureManager.GetTexture(this.dataManager.GetFile<TexFile>(x)!))
                .ToArray();
    }

    /// <summary>Finalizes an instance of the <see cref="SpannableRenderer"/> class.</summary>
    ~SpannableRenderer() => this.ReleaseUnmanagedResources();

    /// <inheritdoc/>
    public void DisposeService() => this.ReleaseUnmanagedResources();

    /// <inheritdoc/>
    public void Render(ReadOnlySpan<char> sequence, RenderState state)
    {
        var ssb = this.RentBuilder();
        ssb.Append(sequence);
        this.Render(ssb, ref state);
        this.ReturnBuilder(ssb);
    }

    /// <inheritdoc/>
    public void Render(ReadOnlySpan<char> sequence, ref RenderState state)
    {
        var ssb = this.RentBuilder();
        ssb.Append(sequence);
        this.Render(ssb, ref state);
        this.ReturnBuilder(ssb);
    }

    /// <inheritdoc/>
    public void Render(IBlockSpannable spannable, RenderState state) =>
        this.Render(spannable, ref state, out _);

    /// <inheritdoc/>
#pragma warning disable CS9087 // This returns a parameter by reference but it is not a ref parameter
    public bool Render(IBlockSpannable spannable, RenderState state, out ReadOnlySpan<byte> hoveredLink) =>
        this.Render(spannable, ref state, out hoveredLink);
#pragma warning restore CS9087 // This returns a parameter by reference but it is not a ref parameter

    /// <inheritdoc/>
    public void Render(IBlockSpannable spannable, ref RenderState state) =>
        this.Render(spannable, ref state, out _);

    /// <inheritdoc/>
    public bool Render(IBlockSpannable spannable, ref RenderState state, out ReadOnlySpan<byte> hoveredLink)
    {
        ThreadSafety.AssertMainThread();

        this.linkRenderCoordinatesList.Clear();
        // TODO: how to deal with this state?
        
        var data = spannable.GetData();
        
        using var splitter = state.UseDrawing ? this.RentSplitter(state.DrawListPtr, TotalChannels) : default;

        var linkRecordIndex = -1;
        var dropUntilNextNewline = false;
        var charRenderer = new CharRenderer(this, data, splitter, ref state, false);
        foreach (var segment in data)
        {
            if (new SpannedOffset(segment) >= state.LastMeasurement.Offset)
            {
                this.OnMeasuredLineEnd(ref state, ref charRenderer, linkRecordIndex, ref dropUntilNextNewline);
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
                        this.OnMeasuredLineEnd(ref state, ref charRenderer, linkRecordIndex, ref dropUntilNextNewline);
                        this.FindFirstWordWrapByteOffset(ref state, segment, absOffset, ref dropUntilNextNewline);
                        charRenderer.SpanFontOptionsUpdated();
                    }

                    if (dropUntilNextNewline)
                    {
                        state.LastMeasurement.LastThing.SetCodepoint(c.Value);
                        continue;
                    }

                    if (state.UseControlCharacter)
                    {
                        // TODO: render control character
                        // var name = c.Value.ShortName;
                        // if (!name.IsEmpty)
                        // {
                        //     var offset = charRenderer.StyleTranslation;
                        //     state.Offset += offset;
                        //     var old = charRenderer.UpdateSpanParams(state.ControlCharactersSpanStyle);
                        //     state.LastMeasurement.LastThing.Clear();
                        //     foreach (var c2 in name)
                        //         charRenderer.RenderChar(c2);
                        //     state.LastMeasurement.LastThing.Clear();
                        //     _ = charRenderer.UpdateSpanParams(old);
                        //     state.Offset -= offset;
                        // }
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
                        this.OnLinkOrRenderEnd(ref state, ref charRenderer, linkRecordIndex);
                        linkRecordIndex = default;
                        break;

                    case SpannedRecordType.Link
                        when SpannedRecordCodec.TryDecodeLink(recordData, out var link):
                        this.OnLinkOrRenderEnd(ref state, ref charRenderer, linkRecordIndex);
                        linkRecordIndex = record.IsRevert || link.IsEmpty ? -1 : segment.RecordIndex;
                        break;

                    case SpannedRecordType.InsertionManualNewLine
                        when (state.AcceptedNewLines & NewLineType.Manual) != 0:
                        this.OnLinkOrRenderEnd(ref state, ref charRenderer, linkRecordIndex);
                        this.BreakLineImmediate(ref state, ref charRenderer);
                        state.LastMeasurement.LastThing.SetRecord(segment.RecordIndex);
                        dropUntilNextNewline = false;
                        break;
                }

                charRenderer.HandleSpan(record, recordData, dropUntilNextNewline);
            }
        }

        this.OnLinkOrRenderEnd(ref state, ref charRenderer, linkRecordIndex);
        state.BoundsRightBottom.Y = Math.Max(state.BoundsRightBottom.Y, state.Offset.Y);

        hoveredLink = default;
        if (state.UseLinks)
        {
            ref var itemState = ref *(ItemStateStruct*)ImGui.GetStateStorage().GetVoidPtrRef(
                                        state.ImGuiGlobalId,
                                        nint.Zero);
            var mouse = ImGui.GetMousePos();
            var mouseRel = state.TransformInverse(mouse - state.StartScreenOffset);
            var hoveredRecordIndex = -1;
            if (ImGui.IsWindowHovered() || itemState.IsMouseButtonDownHandled)
            {
                foreach (var entry in this.linkRenderCoordinatesList)
                {
                    if (entry.LeftTop.X <= mouseRel.X
                        && entry.LeftTop.Y <= mouseRel.Y
                        && mouseRel.X < entry.RightBottom.X
                        && mouseRel.Y < entry.RightBottom.Y)
                    {
                        if (data.TryGetLinkAt(entry.RecordIndex, out hoveredLink))
                            hoveredRecordIndex = entry.RecordIndex;
                        else
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
                    case ImGuiMouseButton.Left when !lmb && itemState.LinkRecordIndex == hoveredRecordIndex:
                        state.ClickedMouseButton = ImGuiMouseButton.Left;
                        itemState.IsMouseButtonDownHandled = false;
                        break;
                    case ImGuiMouseButton.Right when !rmb && itemState.LinkRecordIndex == hoveredRecordIndex:
                        state.ClickedMouseButton = ImGuiMouseButton.Right;
                        itemState.IsMouseButtonDownHandled = false;
                        break;
                    case ImGuiMouseButton.Middle when !mmb && itemState.LinkRecordIndex == hoveredRecordIndex:
                        state.ClickedMouseButton = ImGuiMouseButton.Middle;
                        itemState.IsMouseButtonDownHandled = false;
                        break;
                }

                if (!lmb && !rmb && !mmb)
                {
                    itemState.IsMouseButtonDownHandled = false;
                    ImGuiSetActiveId(0, 0);
                }

                if (itemState.LinkRecordIndex != hoveredRecordIndex)
                {
                    hoveredRecordIndex = -1;
                    hoveredLink = default;
                }
            }

            if (hoveredRecordIndex == -1)
            {
                itemState.LinkRecordIndex = -1;
            }
            else
            {
                ImGuiSetHoveredId(state.ImGuiGlobalId);
                splitter.SetChannel(BackChannel);

                if (!itemState.IsMouseButtonDownHandled && (lmb || rmb || mmb))
                {
                    itemState.LinkRecordIndex = hoveredRecordIndex;
                    itemState.IsMouseButtonDownHandled = true;
                    itemState.FirstMouseButton = lmb ? ImGuiMouseButton.Left :
                                                 rmb ? ImGuiMouseButton.Right : ImGuiMouseButton.Middle;
                    ImGuiSetActiveId(
                        state.ImGuiGlobalId,
                        *(nint*)(ImGui.GetCurrentContext() + CImGuiContextCurrentWindowOffset));
                }

                var color =
                    itemState.IsMouseButtonDownHandled
                        ? itemState.LinkRecordIndex == hoveredRecordIndex
                              ? ImGui.GetColorU32(ImGuiCol.ButtonActive)
                              : 0u
                        : ImGui.GetColorU32(ImGuiCol.ButtonHovered);

                if (color != 0)
                {
                    foreach (var entry in this.linkRenderCoordinatesList)
                    {
                        if (entry.RecordIndex != hoveredRecordIndex)
                            continue;

                        ImGuiNative.ImDrawList_AddQuadFilled(
                            state.DrawListPtr,
                            state.StartScreenOffset + state.Transform(entry.LeftTop),
                            state.StartScreenOffset + state.Transform(new(entry.RightBottom.X, entry.LeftTop.Y)),
                            state.StartScreenOffset + state.Transform(entry.RightBottom),
                            state.StartScreenOffset + state.Transform(new(entry.LeftTop.X, entry.RightBottom.Y)),
                            color);
                    }
                }
            }
        }

        if (state.PutDummyAfterRender)
        {
            var lt = state.Transform(state.BoundsLeftTop);
            var rt = state.Transform(new(state.BoundsRightBottom.X, state.BoundsLeftTop.Y));
            var rb = state.Transform(state.BoundsRightBottom);
            var lb = state.Transform(new(state.BoundsLeftTop.X, state.BoundsRightBottom.Y));
            var minPos = Vector2.Min(Vector2.Min(lt, rt), Vector2.Min(lb, rb));
            var maxPos = Vector2.Max(Vector2.Max(lt, rt), Vector2.Max(lb, rb));
            if (minPos.X <= maxPos.X && minPos.Y <= maxPos.Y)
            {
                ImGui.SetCursorPos(ImGui.GetCursorPos() + minPos);
                ImGui.Dummy(maxPos - minPos);
            }
        }

        return !hoveredLink.IsEmpty;
    }

    /// <summary>Clear the resources used by this instance.</summary>
    private void ReleaseUnmanagedResources()
    {
        this.DisposePooledObjects();
    }

    private void OnMeasuredLineEnd(
        ref RenderState state,
        ref CharRenderer charRenderer,
        int linkRecordIndex,
        ref bool dropUntilNextNewline)
    {
        if (state.LastMeasurement.HasNewLineAtEnd)
        {
            this.OnLinkOrRenderEnd(ref state, ref charRenderer, linkRecordIndex);
            this.BreakLineImmediate(ref state, ref charRenderer);
        }
        else if (state.LastMeasurement.IsWrapped)
        {
            if (state.LastMeasurement.LastThing.IsCodepoint(0x00AD) && state.WordBreak != WordBreakType.KeepAll)
                charRenderer.RenderChar(SoftHyphenReplacementChar);

            if (state.UseWrapMarker && !dropUntilNextNewline)
            {
                if (state.UseWrapMarkerParams)
                {
                    var offset = charRenderer.StyleTranslation;
                    state.Offset += offset;
                    var old = charRenderer.UpdateSpanParams(state.WrapMarkerStyle);
                    foreach (var c2 in state.WrapMarker)
                        charRenderer.RenderChar(c2);
                    _ = charRenderer.UpdateSpanParams(old);
                    state.Offset -= offset;
                }
                else
                {
                    foreach (var c2 in state.WrapMarker)
                        charRenderer.RenderChar(c2);
                }
            }

            this.OnLinkOrRenderEnd(ref state, ref charRenderer, linkRecordIndex);

            if (state.WordBreak == WordBreakType.KeepAll)
                dropUntilNextNewline = true;
            else
                this.BreakLineImmediate(ref state, ref charRenderer);
        }
    }

    private void OnLinkOrRenderEnd(ref RenderState state, ref CharRenderer charRenderer, int linkRecordIndex)
    {
        if (!(charRenderer.BoundsLeftTop.X <= charRenderer.BoundsRightBottom.X)
            || !(charRenderer.BoundsLeftTop.Y <= charRenderer.BoundsRightBottom.Y))
        {
            // Nothing has been rendered since the last call to this function.
            return;
        }
        
        if (linkRecordIndex != -1)
        {
            this.linkRenderCoordinatesList.Add(
                new()
                {
                    RecordIndex = linkRecordIndex,
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
                    if (state.UseControlCharacter && c.Value.ShortName is { IsEmpty: false } name)
                    {
                        // TODO: measure control character
                        // var state2 = new RenderState { LastStyle = state.ControlCharactersSpanStyle };
                        // var measurePass = new CharRenderer(this, segment.Data, ref state2, true);
                        // foreach (var c2 in name)
                        //     measurePass.RenderChar(c2);
                        // if (measurePass.BoundsRightBottom.X > measurePass.BoundsLeftTop.X)
                        // {
                        //     pad = MathF.Round(measurePass.BoundsRightBottom.X - measurePass.BoundsLeftTop.X);
                        //     wordBreaker.ResetLastChar();
                        // }
                    }

                    switch (c.Value.IntValue)
                    {
                        case '\r'
                            when segment.Data.TryGetCodepointAt(nextOffset.Text, 0, out var nextCodepoint)
                                 && nextCodepoint == '\n'
                                 && (state.AcceptedNewLines & NewLineType.CrLf) != 0:
                            measuredLine = wordBreaker.Last;
                            measuredLine.SetOffset(nextOffset.AddTextOffset(1), pad);
                            measuredLine.HasNewLineAtEnd = true;
                            dropUntilNextNewline = false;
                            return;

                        case '\r' when (state.AcceptedNewLines & NewLineType.Cr) != 0:
                        case '\n' when (state.AcceptedNewLines & NewLineType.Lf) != 0:
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
