using System.Numerics;

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

        private readonly Vector2 startScreenOffset;
        private readonly ref RenderState state;
        private readonly ref SpanStyle currentStyle;
        private readonly ref Vector2 offset;
        private readonly ref int lastCodepoint;

        private SpanStyleFontData fontInfo;
        private StateInfo stateInfo;
        
        private int borderRange;
        private int numBorderDraws;

        private bool useBackground;
        private bool useShadow;
        private bool useBorder;
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
            this.startScreenOffset = state.StartScreenOffset;
            this.currentStyle = ref state.LastSpanStyle;
            this.offset = ref state.Offset;
            this.lastCodepoint = ref state.LastMeasurement.LastGlyphCodepoint;
            
            this.fontInfo = new(renderer.options.Scale);
            this.stateInfo = new(renderer.options.LineWrapWidth, ref state);

            this.BoundsLeftTop = new(float.MaxValue);
            this.BoundsRightBottom = new(float.MinValue);
            this.SpanFontOptionsUpdated();
            this.SpanDrawOptionsUpdated();
        }

        public void HandleSpan(in SpannedRecord record, ReadOnlySpan<byte> recordData, bool dropUntilNextNewline)
        {
            this.currentStyle.UpdateFrom(
                this.data,
                record,
                recordData,
                this.renderer.options.InitialSpanStyle,
                out var fontUpdated,
                out var colorUpdated);
            if (fontUpdated)
                this.SpanFontOptionsUpdated();
            if (colorUpdated)
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
            var old = this.currentStyle;
            this.currentStyle = newStyle;
            this.SpanFontOptionsUpdated();
            this.SpanDrawOptionsUpdated();
            return old;
        }

        /// <summary>Advances the internal cursor and renders the glyph.</summary>
        /// <param name="c">The character to render.</param>
        /// <param name="record">The span to render instead.</param>
        /// <param name="recordData">The span record data.</param>
        /// <returns><c>true</c> if the processing is not skipped.</returns>
        public bool RenderChar(int c, in SpannedRecord record = default, ReadOnlySpan<byte> recordData = default)
        {
            switch (c)
            {
                case '\r' or '\n': // Newline characters are never drawn.
                case '\u00AD': // Soft hyphen is never drawn here, and is not considered for kerning.
                    // Still reflect the last codepoint.
                    this.lastCodepoint = c;
                    return false;
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
                            when SpannedRecordCodec.TryDecodeInsertionCallback(recordData, out var index, out var ratio):
                            xy0 = Vector2.Zero;
                            xy1 = new(advX = this.fontInfo.ScaledFontSize * ratio, this.fontInfo.ScaledFontSize);
                            forceOverrideDraw = false;
                            if (!this.data.TryGetCallbackAt(index, out callback))
                                callback = null;
                            break;

                        default:
                            return false;
                    }

                    break;

                case '\t':
                {
                    var tabWidth = this.renderer.options.TabWidth;
                    var next = MathF.Floor((this.offset.X + tabWidth) / tabWidth) * tabWidth;
                    advX = next - this.offset.X;
                    xy0 = Vector2.Zero;
                    xy1 = new(advX, this.fontInfo.ScaledFontSize);
                    break;
                }

                default:
                {
                    xy0 *= this.fontInfo.Scale;
                    xy1 *= this.fontInfo.Scale;
                    advX *= this.fontInfo.Scale;
                    this.offset.X += this.fontInfo.GetScaledGap(this.lastCodepoint, glyph.Codepoint);
                    break;
                }
            }

            var glyphTopSkewDistance = this.fontInfo.GetScaledTopSkew(xy0);

            xy0 += this.StyleTranslation;
            xy1 += this.StyleTranslation;
            advX = MathF.Round(advX);

            var glyphScreenOffset = this.startScreenOffset + this.offset;
            var glyphVisible = forceOverrideDraw ?? (glyph.Visible && c is not ' ' and not '\t');

            if (this.useBackground)
            {
                ImGuiNative.ImDrawListSplitter_SetCurrentChannel(
                    this.renderer.splitterPtr,
                    this.renderer.options.DrawListPtr,
                    BackChannel);

                var fillSize = new Vector2(
                    advX + this.fontInfo.BoldExtraWidth,
                    this.fontInfo.ScaledFontSize);
                ImGuiNative.ImDrawList_AddRectFilled(
                    this.renderer.options.DrawListPtr,
                    glyphScreenOffset + new Vector2(0, this.stateInfo.VerticalOffsetWrtLine),
                    glyphScreenOffset + fillSize + new Vector2(0, this.stateInfo.VerticalOffsetWrtLine),
                    this.currentStyle.BackColorU32,
                    0,
                    ImDrawFlags.None);
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

                var lt = glyphScreenOffset + this.currentStyle.ShadowOffset + xy0;
                var rb = glyphScreenOffset + this.currentStyle.ShadowOffset + xy1;
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
                                lt + v,
                                rt + v,
                                rb + v,
                                lb + v,
                                uv0,
                                new(uv1.X, uv0.Y),
                                uv1,
                                new(uv0.X, uv1.Y),
                                this.currentStyle.ShadowColorU32);
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

                var lt = glyphScreenOffset + xy0;
                var rb = glyphScreenOffset + xy1;
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
                                lt + v,
                                rt + v,
                                rb + v,
                                lb + v,
                                uv0,
                                new(uv1.X, uv0.Y),
                                uv1,
                                new(uv0.X, uv1.Y),
                                this.currentStyle.EdgeColorU32);
                        }
                    }
                }

                if (push)
                    ImGuiNative.ImDrawList_PopTextureID(this.renderer.options.DrawListPtr);
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

                var lt = glyphScreenOffset + xy0;
                var rb = glyphScreenOffset + xy1;
                var rt = new Vector2(rb.X, lt.Y);
                var lb = new Vector2(lt.X, rb.Y);
                lt.X += glyphTopSkewDistance;
                rt.X += glyphTopSkewDistance;

                for (var h = 0; h <= this.fontInfo.BoldExtraWidth; h++)
                {
                    ImGuiNative.ImDrawList_PrimQuadUV(
                        this.renderer.options.DrawListPtr,
                        lt,
                        rt + new Vector2(h, 0),
                        rb + new Vector2(h, 0),
                        lb,
                        uv0,
                        new(uv1.X, uv0.Y),
                        uv1,
                        new(uv0.X, uv1.Y),
                        this.currentStyle.ForeColorU32);
                }

                if (push)
                    ImGuiNative.ImDrawList_PopTextureID(this.renderer.options.DrawListPtr);
            }

            var glyphBoundsLeftTop = this.offset + xy0;
            var glyphBoundsRightBottom = this.offset + xy1;

            try
            {
                callback?.Invoke(
                    new(
                        this.renderer.options.DrawListPtr,
                        this.renderer.splitterPtr,
                        in this.state,
                        glyphBoundsLeftTop,
                        glyphBoundsRightBottom,
                        this.fontInfo,
                        this.currentStyle));
            }
            catch (Exception e)
            {
                Log.Error(e, $"{nameof(SpannedStringRenderer)}: callback error");
            }

            this.BoundsLeftTop = Vector2.Min(this.BoundsLeftTop, glyphBoundsLeftTop);
            this.BoundsRightBottom = Vector2.Max(this.BoundsRightBottom, glyphBoundsRightBottom);
            this.offset.X += advX;

            this.lastCodepoint = c;
            return true;
        }

        public void SpanFontOptionsUpdated()
        {
            this.fontInfo.Update(in this.currentStyle);
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

            this.useBackground = IsColorVisible(this.currentStyle.BackColorU32);
            this.useShadow = IsColorVisible(this.currentStyle.ShadowColorU32) &&
                             this.currentStyle.ShadowOffset != Vector2.Zero;
            this.useBorder = IsColorVisible(this.currentStyle.EdgeColorU32) && this.currentStyle.BorderWidth >= 1f;
            this.useForeground = IsColorVisible(this.currentStyle.ForeColorU32);
            if (this.useBorder)
            {
                this.borderRange = Math.Max(0, (int)this.currentStyle.BorderWidth);
                this.numBorderDraws = (((2 * this.borderRange) + 1) * ((2 * this.borderRange) + 1)) - 1;
            }
            else
            {
                this.numBorderDraws = this.borderRange = 0;
            }
        }
    }
}
