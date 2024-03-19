using System.Numerics;
using System.Runtime.CompilerServices;

using Dalamud.Interface.SpannedStrings.Enums;
using Dalamud.Interface.SpannedStrings.Styles;

using ImGuiNET;

using Serilog;

namespace Dalamud.Interface.SpannedStrings.Internal;

/// <summary>A custom text renderer implementation.</summary>
internal sealed unsafe partial class SpannedStringRenderer
{
    private ref struct CharRenderer
    {
        public Vector2 BoundsLeftTop;
        public Vector2 BoundsRightBottom;

        public Vector2 StyleTranslation;

        private readonly SpannedStringRenderer renderer;
        private readonly SpannedStringData data;
        private readonly bool skipDraw;

        private readonly ref RenderState state;

        private SpanStyleFontData fontInfo;
        private StateInfo stateInfo;

        private int borderRange;
        private int numBorderDraws;

        private bool useBackground;
        private bool useShadow;
        private bool useBorder;
        private bool useTextDecoration;
        private bool useForeground;

        public CharRenderer(
            SpannedStringRenderer renderer,
            in SpannedStringData data,
            ref RenderState state,
            bool skipDraw)
        {
            this.renderer = renderer;
            this.data = data;
            this.skipDraw = skipDraw;

            this.state = ref state;

            this.fontInfo = new(renderer.options.Scale);
            this.stateInfo = new(renderer.options.LineWrapWidth, ref state);

            this.BoundsLeftTop = new(float.MaxValue);
            this.BoundsRightBottom = new(float.MinValue);
            this.SpanFontOptionsUpdated();
            this.SpanDrawOptionsUpdated();
        }

        public readonly float LastLineHeight
        {
            [MethodImpl(MethodImplOptions.AggressiveOptimization)]
            get => this.fontInfo.BBoxVertical.Y - this.fontInfo.BBoxVertical.X;
        }

        public void HandleSpan(in SpannedRecord record, ReadOnlySpan<byte> recordData, bool dropUntilNextNewline)
        {
            this.state.LastStyle.UpdateFrom(
                this.data,
                record,
                recordData,
                this.renderer.options.InitialSpanStyle,
                out var fontUpdated,
                out var drawOptionsUpdated);
            if (fontUpdated)
                this.SpanFontOptionsUpdated();
            if (drawOptionsUpdated)
                this.SpanDrawOptionsUpdated();

            switch (record.Type)
            {
                case SpannedRecordType.InsertionIcon:
                case SpannedRecordType.InsertionTexture:
                case SpannedRecordType.InsertionCallback:
                    if (!dropUntilNextNewline)
                        this.RenderChar(-1, record, recordData);
                    return;
            }
        }

        public SpanStyle UpdateSpanParams(in SpanStyle newStyle)
        {
            var old = this.state.LastStyle;
            this.state.LastStyle = newStyle;
            this.SpanFontOptionsUpdated();
            this.SpanDrawOptionsUpdated();
            return old;
        }

        /// <summary>Advances the internal cursor and renders the glyph.</summary>
        /// <param name="c">The character to render.</param>
        /// <param name="record">The span to render instead.</param>
        /// <param name="recordData">The span record data.</param>
        public void RenderChar(int c, in SpannedRecord record = default, ReadOnlySpan<byte> recordData = default)
        {
            switch (c)
            {
                case '\r' or '\n': // Newline characters are never drawn.
                case '\u00AD': // Soft hyphen is never drawn here, and is not considered for kerning.
                    return;
            }

            var glyphIndex = this.fontInfo.Lookup[
                c >= this.fontInfo.HotData.Length || c < 0 ? this.fontInfo.Font.NativePtr->FallbackChar : c];
            if (glyphIndex == ushort.MaxValue)
                glyphIndex = this.fontInfo.Lookup[this.fontInfo.Font.NativePtr->FallbackChar];
            ref var glyph = ref this.fontInfo.Glyphs[glyphIndex];

            var xy0 = glyph.XY0;
            var xy1 = glyph.XY1;
            var advX = glyph.AdvanceX;
            var uv0 = glyph.UV0;
            var uv1 = glyph.UV1;
            var texId = this.fontInfo.Font.ContainerAtlas.Textures[glyph.TextureIndex].TexID;

            bool? forceOverrideDraw = null;
            SpannedStringCallbackDelegate? callback = null;
            switch (c)
            {
                case -1:
                    switch (record.Type)
                    {
                        case SpannedRecordType.InsertionIcon
                            when SpannedRecordCodec.TryDecodeInsertionIcon(recordData, out var gfdIcon)
                                 && this.renderer.factory.GfdFileView.TryGetEntry((uint)gfdIcon, out var entry)
                                 && this.renderer.options.GfdTexture is { } tex:
                        {
                            xy0 = Vector2.Zero;
                            xy1 = new(
                                advX = (entry.Width * this.fontInfo.ScaledFontSize) / entry.Height,
                                this.fontInfo.ScaledFontSize);

                            var useHiRes = entry.Height < this.fontInfo.ScaledFontSize;
                            uv0 = new(entry.Left, entry.Top);
                            uv1 = new(entry.Width, entry.Height);
                            if (useHiRes)
                            {
                                uv0 *= 2;
                                uv0.Y += 341;
                                uv1 *= 2;
                            }

                            uv1 += uv0;

                            uv0 /= tex.Size;
                            uv1 /= tex.Size;
                            texId = tex.ImGuiHandle;
                            forceOverrideDraw = true;
                            break;
                        }

                        case SpannedRecordType.InsertionTexture
                            when SpannedRecordCodec.TryDecodeInsertionTexture(
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
                            forceOverrideDraw = true;
                            break;
                        }

                        case SpannedRecordType.InsertionCallback
                            when SpannedRecordCodec.TryDecodeInsertionCallback(
                                recordData,
                                out var index,
                                out var ratio):
                            xy0 = Vector2.Zero;
                            xy1 = new(advX = this.fontInfo.ScaledFontSize * ratio, this.fontInfo.ScaledFontSize);
                            forceOverrideDraw = false;
                            if (!this.data.TryGetCallbackAt(index, out callback))
                                callback = null;
                            break;

                        default:
                            return;
                    }

                    break;

                case '\t':
                {
                    var tabWidth = this.renderer.options.TabWidth;
                    var next = MathF.Floor((this.state.Offset.X + tabWidth) / tabWidth) * tabWidth;
                    advX = next - this.state.Offset.X;
                    xy0 = Vector2.Zero;
                    xy1 = new(advX, this.fontInfo.ScaledFontSize);
                    break;
                }

                default:
                {
                    xy0 *= this.fontInfo.Scale;
                    xy1 *= this.fontInfo.Scale;
                    advX *= this.fontInfo.Scale;
                    if (this.state.LastMeasurement.LastThing.TryGetCodepoint(out var lastCodepoint))
                        this.state.Offset.X += this.fontInfo.GetScaledGap(lastCodepoint, glyph.Codepoint);
                    break;
                }
            }

            var glyphTopSkewDistance = this.fontInfo.GetScaledTopSkew(xy0);

            xy0 += this.StyleTranslation;
            xy1 += this.StyleTranslation;
            advX = MathF.Round(advX);

            var glyphVisible = forceOverrideDraw ?? (glyph.Visible && c is not ' ' and not '\t');

            if (this.useBackground)
            {
                ImGuiNative.ImDrawListSplitter_SetCurrentChannel(
                    this.renderer.splitterPtr,
                    this.renderer.options.DrawListPtr,
                    BackChannel);

                var lt = this.state.Offset + this.StyleTranslation;
                var rb = lt + new Vector2(advX + this.fontInfo.BoldExtraWidth, this.fontInfo.ScaledFontSize);
                var rt = new Vector2(rb.X, lt.Y);
                var lb = new Vector2(lt.X, rb.Y);
                ImGuiNative.ImDrawList_AddQuadFilled(
                    this.renderer.options.DrawListPtr,
                    this.state.StartScreenOffset + this.renderer.Transform(lt),
                    this.state.StartScreenOffset + this.renderer.Transform(rt),
                    this.state.StartScreenOffset + this.renderer.Transform(rb),
                    this.state.StartScreenOffset + this.renderer.Transform(lb),
                    this.state.LastStyle.BackColor);
            }

            if (glyphVisible && this.useShadow)
            {
                ImGuiNative.ImDrawListSplitter_SetCurrentChannel(
                    this.renderer.splitterPtr,
                    this.renderer.options.DrawListPtr,
                    ShadowChannel);

                var push = texId != this.renderer.options.DrawListPtr->_CmdHeader.TextureId;
                if (push)
                    ImGuiNative.ImDrawList_PushTextureID(this.renderer.options.DrawListPtr, texId);

                var lt = this.state.Offset + this.state.LastStyle.ShadowOffset + xy0;
                var rb = this.state.Offset + this.state.LastStyle.ShadowOffset + xy1;
                var rt = new Vector2(rb.X, lt.Y);
                var lb = new Vector2(lt.X, rb.Y);
                lt.X += glyphTopSkewDistance;
                rt.X += glyphTopSkewDistance;

                ImGuiNative.ImDrawList_PrimReserve(
                    this.renderer.options.DrawListPtr,
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
                                this.renderer.options.DrawListPtr,
                                this.state.StartScreenOffset + this.renderer.Transform(lt + v),
                                this.state.StartScreenOffset + this.renderer.Transform(rt + v),
                                this.state.StartScreenOffset + this.renderer.Transform(rb + v),
                                this.state.StartScreenOffset + this.renderer.Transform(lb + v),
                                uv0,
                                new(uv1.X, uv0.Y),
                                uv1,
                                new(uv0.X, uv1.Y),
                                this.state.LastStyle.ShadowColor);
                        }
                    }
                }

                if (push)
                    ImGuiNative.ImDrawList_PopTextureID(this.renderer.options.DrawListPtr);
            }

            if (glyphVisible && this.useBorder)
            {
                ImGuiNative.ImDrawListSplitter_SetCurrentChannel(
                    this.renderer.splitterPtr,
                    this.renderer.options.DrawListPtr,
                    BorderChannel);

                var push = texId != this.renderer.options.DrawListPtr->_CmdHeader.TextureId;
                if (push)
                    ImGuiNative.ImDrawList_PushTextureID(this.renderer.options.DrawListPtr, texId);

                var lt = this.state.Offset + xy0;
                var rb = this.state.Offset + xy1;
                var rt = new Vector2(rb.X, lt.Y);
                var lb = new Vector2(lt.X, rb.Y);
                lt.X += glyphTopSkewDistance;
                rt.X += glyphTopSkewDistance;

                ImGuiNative.ImDrawList_PrimReserve(
                    this.renderer.options.DrawListPtr,
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
                                this.renderer.options.DrawListPtr,
                                this.state.StartScreenOffset + this.renderer.Transform(lt + v),
                                this.state.StartScreenOffset + this.renderer.Transform(rt + v),
                                this.state.StartScreenOffset + this.renderer.Transform(rb + v),
                                this.state.StartScreenOffset + this.renderer.Transform(lb + v),
                                uv0,
                                new(uv1.X, uv0.Y),
                                uv1,
                                new(uv0.X, uv1.Y),
                                this.state.LastStyle.EdgeColor);
                        }
                    }
                }

                if (push)
                    ImGuiNative.ImDrawList_PopTextureID(this.renderer.options.DrawListPtr);
            }

            if (this.useTextDecoration)
            {
                ImGuiNative.ImDrawListSplitter_SetCurrentChannel(
                    this.renderer.splitterPtr,
                    this.renderer.options.DrawListPtr,
                    TextDecorationOverUnderChannel);

                var lt = this.state.Offset + this.StyleTranslation +
                         new Vector2(0, this.stateInfo.VerticalOffsetWrtLine);
                var rbase = lt + new Vector2(advX + this.fontInfo.BoldExtraWidth, -this.fontInfo.BBoxVertical.X);
                var rbottom = lt + new Vector2(advX + this.fontInfo.BoldExtraWidth, this.fontInfo.ScaledFontSize);
                var rt = new Vector2(rbase.X, lt.Y);
                var lbottom = new Vector2(lt.X, rbottom.Y);
                var skew = this.fontInfo.GetScaledTopSkew(default);
                lt.X += skew;
                rt.X += skew;
                var xdivy = new Vector2(this.fontInfo.FakeItalic ? 1f / SpanStyleFontData.FakeItalicDivisor : 0, 1f);

                if ((this.state.LastStyle.TextDecoration & TextDecoration.Overline) != 0)
                    this.DrawDecoration(this.state.StartScreenOffset, lt, rt, -1, xdivy);

                if ((this.state.LastStyle.TextDecoration & TextDecoration.Underline) != 0)
                    this.DrawDecoration(this.state.StartScreenOffset, rbase with { X = lt.X }, rbase, 1, xdivy);

                ImGuiNative.ImDrawListSplitter_SetCurrentChannel(
                    this.renderer.splitterPtr,
                    this.renderer.options.DrawListPtr,
                    TextDecorationThroughChannel);

                if ((this.state.LastStyle.TextDecoration & TextDecoration.LineThrough) != 0)
                    this.DrawDecoration(this.state.StartScreenOffset, (lt + lbottom) / 2, (rt + rbottom) / 2, 0, xdivy);
            }

            if (glyphVisible && this.useForeground)
            {
                ImGuiNative.ImDrawListSplitter_SetCurrentChannel(
                    this.renderer.splitterPtr,
                    this.renderer.options.DrawListPtr,
                    ForeChannel);

                var push = texId != this.renderer.options.DrawListPtr->_CmdHeader.TextureId;
                if (push)
                    ImGuiNative.ImDrawList_PushTextureID(this.renderer.options.DrawListPtr, texId);
                ImGuiNative.ImDrawList_PrimReserve(
                    this.renderer.options.DrawListPtr,
                    6 * (1 + this.fontInfo.BoldExtraWidth),
                    4 * (1 + this.fontInfo.BoldExtraWidth));

                var lt = this.state.Offset + xy0;
                var rb = this.state.Offset + xy1;
                var rt = new Vector2(rb.X, lt.Y);
                var lb = new Vector2(lt.X, rb.Y);
                lt.X += glyphTopSkewDistance;
                rt.X += glyphTopSkewDistance;

                for (var h = 0; h <= this.fontInfo.BoldExtraWidth; h++)
                {
                    ImGuiNative.ImDrawList_PrimQuadUV(
                        this.renderer.options.DrawListPtr,
                        this.state.StartScreenOffset + this.renderer.Transform(lt),
                        this.state.StartScreenOffset + this.renderer.Transform(rt + new Vector2(h, 0)),
                        this.state.StartScreenOffset + this.renderer.Transform(rb + new Vector2(h, 0)),
                        this.state.StartScreenOffset + this.renderer.Transform(lb),
                        uv0,
                        new(uv1.X, uv0.Y),
                        uv1,
                        new(uv0.X, uv1.Y),
                        this.state.LastStyle.ForeColor);
                }

                if (push)
                    ImGuiNative.ImDrawList_PopTextureID(this.renderer.options.DrawListPtr);
            }

            var glyphBoundsLeftTop = this.state.Offset + xy0;
            var glyphBoundsRightBottom = this.state.Offset + xy1;

            if (callback is not null)
            {
                ImGuiNative.ImDrawListSplitter_SetCurrentChannel(
                    this.renderer.splitterPtr,
                    this.renderer.options.DrawListPtr,
                    ForeChannel);

                try
                {
                    callback.Invoke(
                        new(
                            this.renderer.options.DrawListPtr,
                            this.renderer.splitterPtr,
                            in this.state,
                            glyphBoundsLeftTop,
                            glyphBoundsRightBottom,
                            this.fontInfo,
                            this.state.LastStyle,
                            this.renderer.options.Transformation));
                }
                catch (Exception e)
                {
                    Log.Error(e, $"{nameof(SpannedStringRenderer)}: callback error");
                }
            }

            this.BoundsLeftTop = Vector2.Min(this.BoundsLeftTop, glyphBoundsLeftTop);
            this.BoundsRightBottom = Vector2.Max(this.BoundsRightBottom, glyphBoundsRightBottom);
            this.state.Offset.X += advX;
        }

        public void SpanFontOptionsUpdated()
        {
            this.fontInfo.Update(in this.state.LastStyle);
            this.RenderStateUpdated();
        }

        private static bool IsColorVisible(uint color) => color >= 0x1000000;

        private void RenderStateUpdated()
        {
            this.stateInfo.Update(in this.fontInfo);
            this.StyleTranslation = new(
                this.fontInfo.ScaledHorizontalOffset + this.stateInfo.HorizontalOffsetWrtLine,
                this.stateInfo.VerticalOffsetWrtLine);
        }

        private void SpanDrawOptionsUpdated()
        {
            if (this.renderer.options.DrawListPtr is null || this.skipDraw)
                return;

            this.useBackground =
                IsColorVisible(this.state.LastStyle.BackColor);
            this.useShadow =
                IsColorVisible(this.state.LastStyle.ShadowColor)
                && this.state.LastStyle.ShadowOffset != Vector2.Zero;
            this.useBorder =
                IsColorVisible(this.state.LastStyle.EdgeColor) && this.state.LastStyle.BorderWidth >= 1f;
            this.useTextDecoration =
                this.state.LastStyle.TextDecoration != TextDecoration.None
                && IsColorVisible(this.state.LastStyle.TextDecorationColor)
                && this.state.LastStyle.TextDecorationThickness > 0f;
            this.useForeground =
                IsColorVisible(this.state.LastStyle.ForeColor);
            if (this.useBorder)
            {
                this.borderRange = Math.Max(0, (int)this.state.LastStyle.BorderWidth);
                this.numBorderDraws = (((2 * this.borderRange) + 1) * ((2 * this.borderRange) + 1)) - 1;
            }
            else
            {
                this.numBorderDraws = this.borderRange = 0;
            }
        }

        private void DrawDecoration(Vector2 glyphScreenOffset, Vector2 xy0, Vector2 xy1, int direction, Vector2 xdivy)
        {
            var dlptr = this.renderer.options.DrawListPtr;
            var thicc = this.fontInfo.ScaledTextDecorationThickness;
            var color = this.state.LastStyle.TextDecorationColor;
            switch (this.state.LastStyle.TextDecorationStyle)
            {
                case TextDecorationStyle.Solid:
                default:
                    ImGuiNative.ImDrawList_AddLine(
                        dlptr,
                        glyphScreenOffset + this.renderer.Transform(xy0),
                        glyphScreenOffset + this.renderer.Transform(xy1),
                        color,
                        thicc);
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

                    ImGuiNative.ImDrawList_AddLine(
                        dlptr,
                        glyphScreenOffset + this.renderer.Transform(xy0 + dispUp),
                        glyphScreenOffset + this.renderer.Transform(xy1 + dispUp),
                        color,
                        thicc);
                    ImGuiNative.ImDrawList_AddLine(
                        dlptr,
                        glyphScreenOffset + this.renderer.Transform(xy0 + dispDown),
                        glyphScreenOffset + this.renderer.Transform(xy1 + dispDown),
                        color,
                        thicc);
                    break;
                }
            }
        }
    }
}
