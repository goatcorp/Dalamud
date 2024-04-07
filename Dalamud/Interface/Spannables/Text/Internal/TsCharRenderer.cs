using System.Numerics;
using System.Runtime.CompilerServices;

using Dalamud.Interface.Colors;
using Dalamud.Interface.Spannables.Internal;
using Dalamud.Interface.Spannables.Rendering;
using Dalamud.Interface.Spannables.Styles;
using Dalamud.Interface.Utility;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using Dalamud.Utility.Numerics;

using ImGuiNET;

namespace Dalamud.Interface.Spannables.Text.Internal;

/// <summary>Renders a single character on each iteration.</summary>
internal unsafe ref struct TsCharRenderer
{
    /// <summary>The background draw channel.</summary>
    public readonly ImDrawListPtr BackChannel;

    /// <summary>The shadow draw channel.</summary>
    public readonly ImDrawListPtr ShadowChannel;

    /// <summary>The border draw channel.</summary>
    public readonly ImDrawListPtr BorderChannel;

    /// <summary>The selection draw channel.</summary>
    public readonly ImDrawListPtr SelectionChannel;
    
    /// <summary>The text decoration under foreground draw channel.</summary>
    public readonly ImDrawListPtr TextDecorationOverUnderChannel;

    /// <summary>The text foreground draw channel.</summary>
    public readonly ImDrawListPtr ForeChannel;

    /// <summary>The text decoration over foreground draw channel.</summary>
    public readonly ImDrawListPtr TextDecorationThroughChannel;

    /// <summary>Translation distance from styles.</summary>
    public Vector2 StyleTranslation;

    /// <summary>Last rendered thing.</summary>
    public TsMeasuredLine.LastThingStruct LastRendered;

    private readonly StyledTextSpannable ts;
    private readonly ImDrawListPtr drawListPtr;
    private readonly ISpannableRenderer renderer;
    private readonly TsDataSpan data;

    private ReadOnlySpan<(int Begin, int End, int Caret)> selections;

    private TextStyleFontData fontInfo;
    private TsCharRendererState state;

    private int borderRange;
    private int numBorderDraws;

    private bool useBackground;
    private bool useShadow;
    private bool useBorder;
    private bool useTextDecoration;
    private bool useForeground;

    /// <summary>Initializes a new instance of the <see cref="TsCharRenderer"/> struct.</summary>
    /// <param name="ts">The text spannable.</param>
    /// <param name="data">The text data span.</param>
    /// <param name="drawListPtr">The target draw list.</param>
    public TsCharRenderer(StyledTextSpannable ts, in TsDataSpan data, ImDrawListPtr drawListPtr)
    {
        this.ts = ts;
        this.renderer = ts.Renderer!;
        this.data = data;
        this.drawListPtr = drawListPtr;
        this.selections = ts.Selections;

        if (drawListPtr.NativePtr is not null)
        {
            this.BackChannel = this.renderer.RentDrawList(drawListPtr);
            this.ShadowChannel = this.renderer.RentDrawList(drawListPtr);
            this.BorderChannel = this.renderer.RentDrawList(drawListPtr);
            this.SelectionChannel = this.renderer.RentDrawList(drawListPtr);
            this.TextDecorationOverUnderChannel = this.renderer.RentDrawList(drawListPtr);
            this.ForeChannel = this.renderer.RentDrawList(drawListPtr);
            this.TextDecorationThroughChannel = this.renderer.RentDrawList(drawListPtr);
        }

        this.SpanFontOptionsUpdated();
        this.SpanDrawOptionsUpdated();
    }

    /// <summary>Gets the most recent line height.</summary>
    public readonly float MostRecentLineHeight
    {
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        get => this.fontInfo.BBoxVertical.Y - this.fontInfo.BBoxVertical.X;
    }

    /// <summary>Appends all channels into the target draw list and returns all borrowed draw lists.</summary>
    /// <param name="transformation">Transformation to apply.</param>
    public void AppendAndReturnChannels(in Matrix4x4 transformation)
    {
        if (this.drawListPtr.NativePtr is null)
            return;
        this.BackChannel.CopyDrawListDataTo(this.drawListPtr, transformation, Vector4.One);
        this.ShadowChannel.CopyDrawListDataTo(this.drawListPtr, transformation, Vector4.One);
        this.BorderChannel.CopyDrawListDataTo(this.drawListPtr, transformation, Vector4.One);
        this.SelectionChannel.CopyDrawListDataTo(this.drawListPtr, transformation, Vector4.One);
        this.TextDecorationOverUnderChannel.CopyDrawListDataTo(this.drawListPtr, transformation, Vector4.One);
        this.ForeChannel.CopyDrawListDataTo(this.drawListPtr, transformation, Vector4.One);
        this.TextDecorationThroughChannel.CopyDrawListDataTo(this.drawListPtr, transformation, Vector4.One);
        this.renderer.ReturnDrawList(this.BackChannel);
        this.renderer.ReturnDrawList(this.ShadowChannel);
        this.renderer.ReturnDrawList(this.BorderChannel);
        this.renderer.ReturnDrawList(this.SelectionChannel);
        this.renderer.ReturnDrawList(this.TextDecorationOverUnderChannel);
        this.renderer.ReturnDrawList(this.ForeChannel);
        this.renderer.ReturnDrawList(this.TextDecorationThroughChannel);
    }

    /// <summary>Sets the current line information.</summary>
    /// <param name="line">The line.</param>
    /// <param name="preferredSize">The preferred size.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetLine(in TsMeasuredLine line, Vector2 preferredSize)
    {
        this.state = new(this.ts, in line, preferredSize);
        this.RenderStateUpdated();
    }

    /// <summary>Handles a span record.</summary>
    /// <param name="record">The record.</param>
    /// <param name="recordData">The record data.</param>
    /// <returns>The boundary from drawing the spannable, if applicable.</returns>
    public RectVector4 HandleSpanRecord(in SpannedRecord record, ReadOnlySpan<byte> recordData)
    {
        this.ts.LastStyle.UpdateFrom(
            record,
            recordData,
            this.ts.Style,
            this.data.FontSets,
            out var fontUpdated,
            out var drawOptionsUpdated);
        if (fontUpdated)
        {
            this.SpanFontOptionsUpdated();
            this.RenderStateUpdated();
        }

        if (drawOptionsUpdated)
            this.SpanDrawOptionsUpdated();

        switch (record.Type)
        {
            case SpannedRecordType.ObjectIcon:
            case SpannedRecordType.ObjectTexture:
            case SpannedRecordType.ObjectSpannable:
                return this.RenderOne(record.TextStart, -1, record, recordData);
            default:
                return RectVector4.InvertedExtrema;
        }
    }

    /// <summary>Updates text style.</summary>
    /// <param name="newStyle">The new text style.</param>
    /// <returns>Old text style.</returns>
    public TextStyle UpdateTextStyle(in TextStyle newStyle)
    {
        var old = this.ts.LastStyle;
        this.ts.LastStyle = newStyle;
        this.SpanFontOptionsUpdated();
        this.RenderStateUpdated();
        this.SpanDrawOptionsUpdated();
        return old;
    }

    /// <summary>Advances the internal cursor and renders the glyph.</summary>
    /// <param name="textOffset">The text offset.</param>
    /// <param name="c">The character to render.</param>
    /// <param name="record">The span to render instead.</param>
    /// <param name="recordData">The span record data.</param>
    /// <returns>The boundary of the rendered thing.</returns>
    public RectVector4 RenderOne(
        int textOffset,
        int c,
        SpannedRecord record = default,
        ReadOnlySpan<byte> recordData = default)
    {
        // TODO: deal with LineHeight and y offset/fill height adjustment

        switch (c)
        {
            case '\r' or '\n': // Newline characters are never drawn.
            case '\u00AD': // Soft hyphen is never drawn here, and is not considered for kerning.
                return RectVector4.FromCoordAndSize(
                    this.ts.LastOffset + this.StyleTranslation,
                    new(0, this.fontInfo.ScaledFontSize));
        }

        var selected = false;
        if (textOffset != -1)
        {
            selected = this.ts.DragSelectionBegin <= textOffset && textOffset < this.ts.DragSelectionEnd;

            if (!selected)
            {
                while (!this.selections.IsEmpty)
                {
                    if (this.selections[0].Begin > textOffset)
                        break;
                    if (this.selections[0].End > textOffset)
                    {
                        selected = true;
                        break;
                    }

                    this.selections = this.selections[1..];
                }
            }
        }

        ref readonly var glyph = ref this.fontInfo.GetEffectiveGlyph(c);

        var xySpannableBase = Vector2.Zero;
        var xy0 = glyph.XY0;
        var xy1 = glyph.XY1;
        var advX = glyph.AdvanceX;
        var uv0 = glyph.UV0;
        var uv1 = glyph.UV1;
        var texId = this.fontInfo.Font.ContainerAtlas.Textures[glyph.TextureIndex].TexID;
        var nonNullSpannableStateIndex = -1;

        bool? forceVisible = null;
        ISpannableTemplate? spannable = null;
        switch (c)
        {
            case -1:
                switch (record.Type)
                {
                    case SpannedRecordType.ObjectIcon
                        when SpannedRecordCodec.TryDecodeObjectIcon(recordData, out var gfdIcon)
                             && this.renderer.TryGetIcon(
                                 this.ts.GfdIconMode,
                                 (uint)gfdIcon,
                                 new(0, this.fontInfo.ScaledFontSize),
                                 out var tex,
                                 out uv0,
                                 out uv1):
                    {
                        var entrySize = tex.Size * (uv1 - uv0);
                        xy0 = Vector2.Zero;
                        xy1 = new(
                            advX = (entrySize.X * this.fontInfo.ScaledFontSize) / entrySize.Y,
                            this.fontInfo.ScaledFontSize);

                        texId = tex.ImGuiHandle;
                        forceVisible = true;
                        break;
                    }

                    case SpannedRecordType.ObjectTexture
                        when SpannedRecordCodec.TryDecodeObjectTexture(
                                 recordData,
                                 out var index,
                                 out uv0,
                                 out uv1)
                             && this.data.TryGetTextureAt(index, out var tex):
                    {
                        xy0 = Vector2.Zero;
                        xy1 = new(
                            advX = (tex.Width * (uv1.X - uv0.X) * this.fontInfo.ScaledFontSize) /
                                   (tex.Height * (uv1.Y - uv0.Y)),
                            this.fontInfo.ScaledFontSize);
                        texId = tex.ImGuiHandle;
                        forceVisible = true;
                        break;
                    }

                    case SpannedRecordType.ObjectSpannable
                        when SpannedRecordCodec.TryDecodeObjectSpannable(recordData, out nonNullSpannableStateIndex)
                             && this.data.TryGetSpannableAt(nonNullSpannableStateIndex, out spannable)
                             && this.ts.Children[nonNullSpannableStateIndex] is { } spannableState:
                    {
                        var spanBounds = spannableState.Boundary;
                        xy0 = spanBounds.LeftTop;
                        xy1 = spanBounds.RightBottom;
                        if (Math.Abs(spanBounds.Height - this.fontInfo.ScaledFontSize) > 0.00001f)
                        {
                            var d = (this.fontInfo.ScaledFontSize - spanBounds.Height) *
                                    this.ts.LastStyle.VerticalAlignment;
                            xy0.Y += d;
                            xy1.Y += d;
                            xySpannableBase = new(0, d);
                            if (spanBounds.Height > this.fontInfo.ScaledFontSize)
                            {
                                xy0.Y -= this.StyleTranslation.Y;
                                xy0.Y -= this.StyleTranslation.Y;
                            }
                        }

                        advX = xy1.X;
                        forceVisible = false;
                        break;
                    }

                    default:
                        return RectVector4.InvertedExtrema;
                }

                break;

            case '\t':
            {
                var tabWidth = this.fontInfo.CalculateTabSize(this.ts.TabWidth);
                var next = MathF.Floor((this.ts.LastOffset.X + tabWidth) / tabWidth) * tabWidth;
                advX = next - this.ts.LastOffset.X;
                xy0 = Vector2.Zero;
                xy1 = new(advX, this.fontInfo.ScaledFontSize);
                break;
            }

            default:
            {
                xy0 *= this.fontInfo.Scale;
                xy1 *= this.fontInfo.Scale;
                advX *= this.fontInfo.Scale;
                if (this.LastRendered.TryGetCodepoint(out var lastCodepoint))
                    this.ts.LastOffset.X += this.fontInfo.GetScaledGap(lastCodepoint, glyph.Codepoint);
                break;
            }
        }

        xy0 += this.StyleTranslation;
        xy1 += this.StyleTranslation;
        advX = MathF.Round(advX * this.ts.EffectiveRenderScale) / this.ts.EffectiveRenderScale;

        var topSkewDistance = this.fontInfo.GetScaledTopSkew(xy0);
        var bounds = RectVector4.Translate(new(xy0, xy1), this.ts.LastOffset);
        var visible = forceVisible ?? (glyph.Visible && c is not ' ' and not '\t');

        if (this.useBackground)
        {
            var lt = this.ts.LastOffset + this.StyleTranslation;
            var rb = lt + new Vector2(advX + this.fontInfo.BoldExtraWidth, this.fontInfo.ScaledFontSize);
            ImGuiNative.ImDrawList_AddRectFilled(
                this.BackChannel,
                lt,
                rb,
                this.ts.LastStyle.BackColor,
                0,
                ImDrawFlags.None);
        }

        if (visible && this.useShadow)
        {
            var push = texId != this.ShadowChannel._CmdHeader.TextureId;
            if (push)
                ImGuiNative.ImDrawList_PushTextureID(this.ShadowChannel, texId);

            var lt = this.ts.LastOffset + this.ts.LastStyle.ShadowOffset + xy0;
            var rb = this.ts.LastOffset + this.ts.LastStyle.ShadowOffset + xy1;
            var rt = new Vector2(rb.X, lt.Y);
            var lb = new Vector2(lt.X, rb.Y);
            lt.X += topSkewDistance;
            rt.X += topSkewDistance;

            ImGuiNative.ImDrawList_PrimReserve(
                this.ShadowChannel,
                6 * (this.numBorderDraws + 1) * (1 + this.fontInfo.BoldExtraWidth),
                4 * (this.numBorderDraws + 1) * (1 + this.fontInfo.BoldExtraWidth));
            for (var h = 0; h <= this.fontInfo.BoldExtraWidth; h++)
            {
                for (var x = -this.borderRange; x <= this.borderRange; x++)
                {
                    for (var y = -this.borderRange; y <= this.borderRange; y++)
                    {
                        var v = new Vector2(x + h, y);
                        ImGuiNative.ImDrawList_PrimQuadUV(
                            this.ShadowChannel,
                            lt + v,
                            rt + v,
                            rb + v,
                            lb + v,
                            uv0,
                            new(uv1.X, uv0.Y),
                            uv1,
                            new(uv0.X, uv1.Y),
                            this.ts.LastStyle.ShadowColor);
                    }
                }
            }

            if (push)
                ImGuiNative.ImDrawList_PopTextureID(this.ShadowChannel);
        }

        if (visible && this.useBorder)
        {
            var push = texId != this.BorderChannel._CmdHeader.TextureId;
            if (push)
                ImGuiNative.ImDrawList_PushTextureID(this.BorderChannel, texId);

            var lt = this.ts.LastOffset + xy0;
            var rb = this.ts.LastOffset + xy1;
            var rt = new Vector2(rb.X, lt.Y);
            var lb = new Vector2(lt.X, rb.Y);
            lt.X += topSkewDistance;
            rt.X += topSkewDistance;

            ImGuiNative.ImDrawList_PrimReserve(
                this.BorderChannel,
                6 * this.numBorderDraws * (1 + this.fontInfo.BoldExtraWidth),
                4 * this.numBorderDraws * (1 + this.fontInfo.BoldExtraWidth));
            for (var h = 0; h <= this.fontInfo.BoldExtraWidth; h++)
            {
                for (var x = -this.borderRange; x <= this.borderRange; x++)
                {
                    for (var y = -this.borderRange; y <= this.borderRange; y++)
                    {
                        if (x == 0 && y == 0)
                            continue;
                        var v = new Vector2(x + h, y);
                        ImGuiNative.ImDrawList_PrimQuadUV(
                            this.BorderChannel,
                            lt + v,
                            rt + v,
                            rb + v,
                            lb + v,
                            uv0,
                            new(uv1.X, uv0.Y),
                            uv1,
                            new(uv0.X, uv1.Y),
                            this.ts.LastStyle.EdgeColor);
                    }
                }
            }

            if (push)
                ImGuiNative.ImDrawList_PopTextureID(this.BorderChannel);
        }

        if (selected && this.drawListPtr.NativePtr is not null)
        {
            var lt = this.ts.LastOffset + this.StyleTranslation;
            var rb = lt + new Vector2(advX + this.fontInfo.BoldExtraWidth, this.fontInfo.ScaledFontSize);
            ImGuiNative.ImDrawList_AddRectFilled(
                this.SelectionChannel,
                lt,
                rb,
                new Rgba32(ImGuiColors.ParsedBlue),
                0,
                ImDrawFlags.None);
        }

        if (this.useTextDecoration)
        {
            var lt = this.ts.LastOffset + this.StyleTranslation +
                     new Vector2(0, this.state.VerticalOffsetWrtLine);
            var rbase = lt + new Vector2(advX + this.fontInfo.BoldExtraWidth, -this.fontInfo.BBoxVertical.X);
            var rbottom = lt + new Vector2(advX + this.fontInfo.BoldExtraWidth, this.fontInfo.ScaledFontSize);
            var rt = new Vector2(rbase.X, lt.Y);
            var lbottom = new Vector2(lt.X, rbottom.Y);
            var skew = this.fontInfo.GetScaledTopSkew(default);
            lt.X += skew;
            rt.X += skew;
            var xdivy = this.fontInfo.SlopeVector2;
            if ((this.ts.LastStyle.TextDecoration
                 & (TextDecoration.Overline | TextDecoration.Underline)) != 0)
            {
                var push = texId != this.TextDecorationOverUnderChannel._CmdHeader.TextureId;
                if (push)
                    ImGuiNative.ImDrawList_PushTextureID(this.TextDecorationOverUnderChannel, texId);

                if ((this.ts.LastStyle.TextDecoration & TextDecoration.Overline) != 0)
                    this.DrawDecoration(this.TextDecorationOverUnderChannel, lt, rt, -1, xdivy);

                if ((this.ts.LastStyle.TextDecoration & TextDecoration.Underline) != 0)
                {
                    this.DrawDecoration(
                        this.TextDecorationOverUnderChannel,
                        rbase with { X = lt.X },
                        rbase,
                        1,
                        xdivy);
                }

                if (push)
                    ImGuiNative.ImDrawList_PopTextureID(this.TextDecorationOverUnderChannel);
            }

            if ((this.ts.LastStyle.TextDecoration & TextDecoration.LineThrough) != 0)
            {
                var push = texId != this.TextDecorationThroughChannel._CmdHeader.TextureId;
                if (push)
                    ImGuiNative.ImDrawList_PushTextureID(this.TextDecorationThroughChannel, texId);

                this.DrawDecoration(
                    this.TextDecorationThroughChannel,
                    (lt + lbottom) / 2,
                    (rt + rbottom) / 2,
                    0,
                    xdivy);

                if (push)
                    ImGuiNative.ImDrawList_PopTextureID(this.TextDecorationThroughChannel);
            }
        }

        if (visible && this.useForeground)
        {
            var push = texId != this.ForeChannel._CmdHeader.TextureId;
            if (push)
                ImGuiNative.ImDrawList_PushTextureID(this.ForeChannel, texId);
            ImGuiNative.ImDrawList_PrimReserve(
                this.ForeChannel,
                6 * (1 + this.fontInfo.BoldExtraWidth),
                4 * (1 + this.fontInfo.BoldExtraWidth));

            var lt = this.ts.LastOffset + xy0;
            var rb = this.ts.LastOffset + xy1;
            var rt = new Vector2(rb.X, lt.Y);
            var lb = new Vector2(lt.X, rb.Y);
            lt.X += topSkewDistance;
            rt.X += topSkewDistance;

            for (var h = 0; h <= this.fontInfo.BoldExtraWidth; h++)
            {
                ImGuiNative.ImDrawList_PrimQuadUV(
                    this.ForeChannel,
                    lt,
                    rt + new Vector2(h, 0),
                    rb + new Vector2(h, 0),
                    lb,
                    uv0,
                    new(uv1.X, uv0.Y),
                    uv1,
                    new(uv0.X, uv1.Y),
                    this.ts.LastStyle.ForeColor);
            }

            if (push)
                ImGuiNative.ImDrawList_PopTextureID(this.ForeChannel);
        }

        if (spannable is not null && nonNullSpannableStateIndex != -1)
        {
            ref var spannableState = ref this.ts.Children[nonNullSpannableStateIndex]!;
            if (this.drawListPtr.NativePtr is null)
            {
                // Measure pass
                spannableState.RenderScale = this.ts.EffectiveRenderScale;
                this.ts.ChildOffsets[nonNullSpannableStateIndex] =
                    this.ts.LastOffset + this.StyleTranslation + xySpannableBase;
            }
            else
            {
                spannableState.RenderPassDraw(this.drawListPtr);
            }
        }

        this.ts.LastOffset.X += advX;
        return bounds;
    }

    /// <summary>Updates <see cref="fontInfo"/> according to font options.</summary>
    public void SpanFontOptionsUpdated() =>
        this.renderer.TryGetFontData(this.ts.EffectiveRenderScale, in this.ts.LastStyle, out this.fontInfo);

    private static bool IsColorVisible(uint color) => color >= 0x1000000;

    private void RenderStateUpdated()
    {
        this.state.Update(in this.fontInfo);
        this.StyleTranslation = new(
            this.fontInfo.ScaledHorizontalOffset + this.state.HorizontalOffsetWrtLine,
            this.state.VerticalOffsetWrtLine);
    }

    private void SpanDrawOptionsUpdated()
    {
        if (this.drawListPtr.NativePtr is null)
            return;

        this.useBackground =
            IsColorVisible(this.ts.LastStyle.BackColor);
        this.useShadow =
            IsColorVisible(this.ts.LastStyle.ShadowColor)
            && this.ts.LastStyle.ShadowOffset != Vector2.Zero;
        this.useBorder =
            IsColorVisible(this.ts.LastStyle.EdgeColor) &&
            this.ts.LastStyle.EdgeWidth >= 1f;
        this.useTextDecoration =
            this.ts.LastStyle.TextDecoration != TextDecoration.None
            && IsColorVisible(this.ts.LastStyle.TextDecorationColor)
            && this.ts.LastStyle.TextDecorationThickness > 0f;
        this.useForeground =
            IsColorVisible(this.ts.LastStyle.ForeColor);
        if (this.useBorder)
        {
            this.borderRange = Math.Max(0, (int)this.ts.LastStyle.EdgeWidth);
            this.numBorderDraws = (((2 * this.borderRange) + 1) * ((2 * this.borderRange) + 1)) - 1;
        }
        else
        {
            this.numBorderDraws = this.borderRange = 0;
        }
    }

    private void DrawDecoration(ImDrawListPtr drawList, Vector2 xy0, Vector2 xy1, int direction, Vector2 xdivy)
    {
        var thicc = this.fontInfo.ScaledTextDecorationThickness;
        var color = this.ts.LastStyle.TextDecorationColor;
        switch (this.ts.LastStyle.TextDecorationStyle)
        {
            case TextDecorationStyle.Solid:
            default:
                ImGuiNative.ImDrawList_AddLine(drawList, xy0, xy1, color, thicc);
                break;
            case TextDecorationStyle.Double:
            {
                Vector2 dispUp, dispDown;
                switch (direction)
                {
                    case -1:
                        dispUp = thicc * xdivy * -2;
                        dispDown = Vector2.Zero;
                        xy0.Y -= 0.5f;
                        xy1.Y -= 0.5f;
                        break;
                    default:
                        dispDown = thicc * xdivy;
                        dispUp = -dispDown;
                        xy0.Y += 0.5f;
                        xy1.Y += 0.5f;
                        break;
                    case 1:
                        dispUp = Vector2.Zero;
                        dispDown = thicc * xdivy * 2;
                        xy0.Y += 0.5f;
                        xy1.Y += 0.5f;
                        break;
                }

                ImGuiNative.ImDrawList_AddLine(drawList, xy0 + dispUp, xy1 + dispUp, color, thicc);
                ImGuiNative.ImDrawList_AddLine(drawList, xy0 + dispDown, xy1 + dispDown, color, thicc);
                break;
            }
        }
    }
}
