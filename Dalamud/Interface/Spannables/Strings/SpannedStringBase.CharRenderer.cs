using System.Numerics;
using System.Runtime.CompilerServices;

using Dalamud.Interface.Spannables.EventHandlerArgs;
using Dalamud.Interface.Spannables.Internal;
using Dalamud.Interface.Spannables.Rendering;
using Dalamud.Interface.Spannables.Styles;
using Dalamud.Utility.Numerics;

using ImGuiNET;

namespace Dalamud.Interface.Spannables.Strings;

/// <summary>Base class for <see cref="SpannedString"/> and <see cref="SpannedStringBuilder"/>.</summary>
public abstract partial class SpannedStringBase
{
    /// <summary>The display character in place of a soft hyphen character.</summary>
    public const char SoftHyphenReplacementChar = '-';

    private unsafe ref struct CharRenderer
    {
        public Vector2 StyleTranslation;

        public MeasuredLine.LastThingStruct LastRendered;

        private readonly SpannableDrawArgs args;
        private readonly DataRef data;
        private readonly State state;
        private readonly bool skipDraw;

        private TextStyleFontData fontInfo;
        private StateInfo stateInfo;

        private int borderRange;
        private int numBorderDraws;

        private bool useBackground;
        private bool useShadow;
        private bool useBorder;
        private bool useTextDecoration;
        private bool useForeground;

        public CharRenderer(in SpannableDrawArgs args, in DataRef data, State state, bool skipDraw)
        {
            this.args = args;
            this.data = data;
            this.state = state;
            this.skipDraw = skipDraw || args.IsEmpty;

            this.SpanFontOptionsUpdated();
            this.SpanDrawOptionsUpdated();
        }

        public readonly float MostRecentLineHeight
        {
            [MethodImpl(MethodImplOptions.AggressiveOptimization)]
            get => this.fontInfo.BBoxVertical.Y - this.fontInfo.BBoxVertical.X;
        }

        public void SetLine(in MeasuredLine measuredLine)
        {
            this.stateInfo = new(this.state, in measuredLine);
            this.RenderStateUpdated();
        }

        public RectVector4 HandleSpan(in SpannedRecord record, ReadOnlySpan<byte> recordData)
        {
            this.state.TextState.LastStyle.UpdateFrom(
                record,
                recordData,
                this.state.TextState.InitialStyle,
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
                    return this.RenderOne(-1, record, recordData);
                default:
                    return RectVector4.InvertedExtrema;
            }
        }

        public TextStyle UpdateSpanParams(in TextStyle newStyle)
        {
            var old = this.state.TextState.LastStyle;
            this.state.TextState.LastStyle = newStyle;
            this.SpanFontOptionsUpdated();
            this.RenderStateUpdated();
            this.SpanDrawOptionsUpdated();
            return old;
        }

        /// <summary>Advances the internal cursor and renders the glyph.</summary>
        /// <param name="c">The character to render.</param>
        /// <param name="record">The span to render instead.</param>
        /// <param name="recordData">The span record data.</param>
        /// <returns>The boundary of the rendered thing.</returns>
        public RectVector4 RenderOne(int c, in SpannedRecord record = default, ReadOnlySpan<byte> recordData = default)
        {
            // TODO: deal with LineHeight and y offset/fill height adjustment

            switch (c)
            {
                case '\r' or '\n': // Newline characters are never drawn.
                case '\u00AD': // Soft hyphen is never drawn here, and is not considered for kerning.
                    return RectVector4.FromCoordAndSize(
                        this.state.Offset + this.StyleTranslation,
                        new(0, this.fontInfo.ScaledFontSize));
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
            ISpannable? spannable = null;
            switch (c)
            {
                case -1:
                    switch (record.Type)
                    {
                        case SpannedRecordType.ObjectIcon
                            when SpannedRecordCodec.TryDecodeObjectIcon(recordData, out var gfdIcon)
                                 && this.state.Renderer.TryGetIcon(
                                     this.state.TextState.GfdIndex,
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
                            when SpannedRecordCodec.TryDecodeObjectSpannable(
                                     recordData,
                                     out nonNullSpannableStateIndex,
                                     out _)
                                 && this.data.TryGetSpannableAt(nonNullSpannableStateIndex, out spannable)
                                 && this.state.SpannableStates[nonNullSpannableStateIndex] is { } spannableState:
                        {
                            ref readonly var spanBounds = ref spannableState.Boundary;
                            xy0 = spanBounds.LeftTop;
                            xy1 = spanBounds.RightBottom;
                            if (Math.Abs(spanBounds.Height - this.fontInfo.ScaledFontSize) > 0.00001f)
                            {
                                var d = (this.fontInfo.ScaledFontSize - spanBounds.Height) *
                                        this.state.TextState.LastStyle.VerticalAlignment;
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
                    var tabWidth = this.state.TextState.TabWidth;
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
                    if (this.LastRendered.TryGetCodepoint(out var lastCodepoint))
                        this.state.Offset.X += this.fontInfo.GetScaledGap(lastCodepoint, glyph.Codepoint);
                    break;
                }
            }

            xy0 += this.StyleTranslation;
            xy1 += this.StyleTranslation;
            advX = MathF.Round(advX);

            var topSkewDistance = this.fontInfo.GetScaledTopSkew(xy0);
            var bounds = RectVector4.Translate(new(xy0, xy1), this.state.Offset);
            var visible = forceVisible ?? (glyph.Visible && c is not ' ' and not '\t');

            if (this.useBackground)
            {
                this.args.SwitchToChannel(RenderChannel.BackChannel);

                var lt = this.state.Offset + this.StyleTranslation;
                var rb = lt + new Vector2(advX + this.fontInfo.BoldExtraWidth, this.fontInfo.ScaledFontSize);
                var rt = new Vector2(rb.X, lt.Y);
                var lb = new Vector2(lt.X, rb.Y);
                ImGuiNative.ImDrawList_AddQuadFilled(
                    this.args.DrawListPtr,
                    this.state.TransformToScreen(lt),
                    this.state.TransformToScreen(rt),
                    this.state.TransformToScreen(rb),
                    this.state.TransformToScreen(lb),
                    this.state.TextState.LastStyle.BackColor);
            }

            if (visible && this.useShadow)
            {
                this.args.SwitchToChannel(RenderChannel.ShadowChannel);

                var push = texId != this.args.DrawListPtr._CmdHeader.TextureId;
                if (push)
                    ImGuiNative.ImDrawList_PushTextureID(this.args.DrawListPtr, texId);

                var lt = this.state.Offset + this.state.TextState.LastStyle.ShadowOffset + xy0;
                var rb = this.state.Offset + this.state.TextState.LastStyle.ShadowOffset + xy1;
                var rt = new Vector2(rb.X, lt.Y);
                var lb = new Vector2(lt.X, rb.Y);
                lt.X += topSkewDistance;
                rt.X += topSkewDistance;

                ImGuiNative.ImDrawList_PrimReserve(
                    this.args.DrawListPtr,
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
                                this.args.DrawListPtr,
                                this.state.TransformToScreen(lt + v),
                                this.state.TransformToScreen(rt + v),
                                this.state.TransformToScreen(rb + v),
                                this.state.TransformToScreen(lb + v),
                                uv0,
                                new(uv1.X, uv0.Y),
                                uv1,
                                new(uv0.X, uv1.Y),
                                this.state.TextState.LastStyle.ShadowColor);
                        }
                    }
                }

                if (push)
                    ImGuiNative.ImDrawList_PopTextureID(this.args.DrawListPtr);
            }

            if (visible && this.useBorder)
            {
                this.args.SwitchToChannel(RenderChannel.BorderChannel);

                var push = texId != this.args.DrawListPtr._CmdHeader.TextureId;
                if (push)
                    ImGuiNative.ImDrawList_PushTextureID(this.args.DrawListPtr, texId);

                var lt = this.state.Offset + xy0;
                var rb = this.state.Offset + xy1;
                var rt = new Vector2(rb.X, lt.Y);
                var lb = new Vector2(lt.X, rb.Y);
                lt.X += topSkewDistance;
                rt.X += topSkewDistance;

                ImGuiNative.ImDrawList_PrimReserve(
                    this.args.DrawListPtr,
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
                                this.args.DrawListPtr,
                                this.state.TransformToScreen(lt + v),
                                this.state.TransformToScreen(rt + v),
                                this.state.TransformToScreen(rb + v),
                                this.state.TransformToScreen(lb + v),
                                uv0,
                                new(uv1.X, uv0.Y),
                                uv1,
                                new(uv0.X, uv1.Y),
                                this.state.TextState.LastStyle.EdgeColor);
                        }
                    }
                }

                if (push)
                    ImGuiNative.ImDrawList_PopTextureID(this.args.DrawListPtr);
            }

            if (this.useTextDecoration)
            {
                this.args.SwitchToChannel(RenderChannel.TextDecorationOverUnderChannel);

                var lt = this.state.Offset + this.StyleTranslation +
                         new Vector2(0, this.stateInfo.VerticalOffsetWrtLine);
                var rbase = lt + new Vector2(advX + this.fontInfo.BoldExtraWidth, -this.fontInfo.BBoxVertical.X);
                var rbottom = lt + new Vector2(advX + this.fontInfo.BoldExtraWidth, this.fontInfo.ScaledFontSize);
                var rt = new Vector2(rbase.X, lt.Y);
                var lbottom = new Vector2(lt.X, rbottom.Y);
                var skew = this.fontInfo.GetScaledTopSkew(default);
                lt.X += skew;
                rt.X += skew;
                var xdivy = this.fontInfo.SlopeVector2;

                if ((this.state.TextState.LastStyle.TextDecoration & TextDecoration.Overline) != 0)
                    this.DrawDecoration(lt, rt, -1, xdivy);

                if ((this.state.TextState.LastStyle.TextDecoration & TextDecoration.Underline) != 0)
                    this.DrawDecoration(rbase with { X = lt.X }, rbase, 1, xdivy);

                if ((this.state.TextState.LastStyle.TextDecoration & TextDecoration.LineThrough) != 0)
                {
                    this.args.SwitchToChannel(RenderChannel.TextDecorationOverUnderChannel);
                    this.DrawDecoration((lt + lbottom) / 2, (rt + rbottom) / 2, 0, xdivy);
                }
            }

            if (visible && this.useForeground)
            {
                this.args.SwitchToChannel(RenderChannel.ForeChannel);

                var push = texId != this.args.DrawListPtr._CmdHeader.TextureId;
                if (push)
                    ImGuiNative.ImDrawList_PushTextureID(this.args.DrawListPtr, texId);
                ImGuiNative.ImDrawList_PrimReserve(
                    this.args.DrawListPtr,
                    6 * (1 + this.fontInfo.BoldExtraWidth),
                    4 * (1 + this.fontInfo.BoldExtraWidth));

                var lt = this.state.Offset + xy0;
                var rb = this.state.Offset + xy1;
                var rt = new Vector2(rb.X, lt.Y);
                var lb = new Vector2(lt.X, rb.Y);
                lt.X += topSkewDistance;
                rt.X += topSkewDistance;

                for (var h = 0; h <= this.fontInfo.BoldExtraWidth; h++)
                {
                    ImGuiNative.ImDrawList_PrimQuadUV(
                        this.args.DrawListPtr,
                        this.state.TransformToScreen(lt),
                        this.state.TransformToScreen(rt + new Vector2(h, 0)),
                        this.state.TransformToScreen(rb + new Vector2(h, 0)),
                        this.state.TransformToScreen(lb),
                        uv0,
                        new(uv1.X, uv0.Y),
                        uv1,
                        new(uv0.X, uv1.Y),
                        this.state.TextState.LastStyle.ForeColor);
                }

                if (push)
                    ImGuiNative.ImDrawList_PopTextureID(this.args.DrawListPtr);
            }

            if (spannable is not null && nonNullSpannableStateIndex != -1)
            {
                ref var spannableState = ref this.state.SpannableStates[nonNullSpannableStateIndex]!;
                if (this.skipDraw)
                {
                    // Measure pass
                    spannableState.TextState = this.state.TextState;
                    this.state.SpannableOffsets[nonNullSpannableStateIndex] =
                        this.state.Offset + this.StyleTranslation + xySpannableBase;
                }
                else
                {
                    spannable.Draw(this.args with { State = spannableState });
                }
            }

            this.state.Offset.X += advX;
            return bounds;
        }

        public void SpanFontOptionsUpdated()
        {
            this.state.Renderer.TryGetFontData(
                this.state.Scale,
                in this.state.TextState.LastStyle,
                out this.fontInfo);
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
            if (this.skipDraw)
                return;

            this.useBackground =
                IsColorVisible(this.state.TextState.LastStyle.BackColor);
            this.useShadow =
                IsColorVisible(this.state.TextState.LastStyle.ShadowColor)
                && this.state.TextState.LastStyle.ShadowOffset != Vector2.Zero;
            this.useBorder =
                IsColorVisible(this.state.TextState.LastStyle.EdgeColor) &&
                this.state.TextState.LastStyle.BorderWidth >= 1f;
            this.useTextDecoration =
                this.state.TextState.LastStyle.TextDecoration != TextDecoration.None
                && IsColorVisible(this.state.TextState.LastStyle.TextDecorationColor)
                && this.state.TextState.LastStyle.TextDecorationThickness > 0f;
            this.useForeground =
                IsColorVisible(this.state.TextState.LastStyle.ForeColor);
            if (this.useBorder)
            {
                this.borderRange = Math.Max(0, (int)this.state.TextState.LastStyle.BorderWidth);
                this.numBorderDraws = (((2 * this.borderRange) + 1) * ((2 * this.borderRange) + 1)) - 1;
            }
            else
            {
                this.numBorderDraws = this.borderRange = 0;
            }
        }

        private void DrawDecoration(Vector2 xy0, Vector2 xy1, int direction, Vector2 xdivy)
        {
            var dlptr = this.args.DrawListPtr;
            var thicc = this.fontInfo.ScaledTextDecorationThickness;
            var color = this.state.TextState.LastStyle.TextDecorationColor;
            switch (this.state.TextState.LastStyle.TextDecorationStyle)
            {
                case TextDecorationStyle.Solid:
                default:
                    ImGuiNative.ImDrawList_AddLine(
                        dlptr,
                        this.state.TransformToScreen(xy0),
                        this.state.TransformToScreen(xy1),
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
                        this.state.TransformToScreen(xy0 + dispUp),
                        this.state.TransformToScreen(xy1 + dispUp),
                        color,
                        thicc);
                    ImGuiNative.ImDrawList_AddLine(
                        dlptr,
                        this.state.TransformToScreen(xy0 + dispDown),
                        this.state.TransformToScreen(xy1 + dispDown),
                        color,
                        thicc);
                    break;
                }
            }
        }
    }
}
