using System.Runtime.CompilerServices;

using Dalamud.Interface.Spannables.Helpers;
using Dalamud.Interface.Spannables.Internal;
using Dalamud.Interface.Spannables.Rendering;
using Dalamud.Interface.Spannables.RenderPassMethodArgs;
using Dalamud.Interface.Spannables.Styles;
using Dalamud.Utility.Numerics;

namespace Dalamud.Interface.Spannables.Strings;

/// <summary>Base class for <see cref="SpannedString"/> and <see cref="SpannedStringBuilder"/>.</summary>
public abstract partial class SpannedStringBase
{
    private ref struct WordBreaker
    {
        private readonly SpannableMeasureArgs args;
        private readonly RenderPass renderPass;
        private readonly DataRef data;

        private TextStyle currentStyle;
        private TextStyleFontData fontInfo;

        private bool breakOnFirstNormalBreakableOffset;

        private float wrapMarkerWidth;
        private MeasuredLine prev;
        private MeasuredLine first;

        private MeasuredLine normalBreak;
        private MeasuredLine wrapMarkerBreak;

        public WordBreaker(SpannableMeasureArgs args, in DataRef data, RenderPass renderPass)
        {
            this.args = args;
            this.renderPass = renderPass;
            this.data = data;
            this.currentStyle = renderPass.ActiveTextState.LastStyle;
            this.prev = MeasuredLine.Empty;
            this.first = MeasuredLine.Empty;
            this.normalBreak = MeasuredLine.Empty;
            this.wrapMarkerBreak = MeasuredLine.Empty;

            this.SpanFontOptionsUpdated();
            if (this.renderPass.ActiveTextState.WrapMarker is not null)
                this.UpdateWrapMarker();
        }

        public readonly MeasuredLine Last => this.prev;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ResetLastChar() => this.prev.LastThing.Clear();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnionLineBBoxVertical(ref MeasuredLine line) =>
            line.UnionBBoxVertical(this.fontInfo.BBoxVertical.X, this.fontInfo.BBoxVertical.Y);

        public MeasuredLine HandleSpan(
            in SpannedRecord record,
            ReadOnlySpan<byte> recordData,
            CompositeOffset offsetBefore,
            CompositeOffset offsetAfter)
        {
            this.currentStyle.UpdateFrom(
                record,
                recordData,
                this.renderPass.ActiveTextState.InitialStyle,
                this.data.FontSets,
                out var fontUpdated,
                out _);
            if (fontUpdated)
                this.SpanFontOptionsUpdated();

            switch (record.Type)
            {
                case SpannedRecordType.ObjectIcon:
                case SpannedRecordType.ObjectTexture:
                case SpannedRecordType.ObjectSpannable:
                    return this.AddCodepointAndMeasure(offsetBefore, offsetAfter, -1, record, recordData);
                case SpannedRecordType.ObjectNewLine
                    when (this.renderPass.ActiveTextState.AcceptedNewLines & NewLineType.Manual) != 0:
                    this.prev.LastThing.SetRecord(offsetBefore.Record);
                    this.prev.SetOffset(offsetAfter);
                    this.UnionLineBBoxVertical(ref this.prev);
                    return this.prev with { HasNewLineAtEnd = true };
                default:
                    this.prev.LastThing.SetRecord(offsetBefore.Record);
                    return MeasuredLine.Empty;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public MeasuredLine AddCodepointAndMeasure(
            CompositeOffset offset,
            CompositeOffset offsetAfter,
            int c,
            in SpannedRecord record = default,
            ReadOnlySpan<byte> recordData = default,
            float pad = 0)
        {
            var current = this.prev;
            switch (c)
            {
                case -1:
                {
                    RectVector4 boundary;
                    switch (record.Type)
                    {
                        case SpannedRecordType.ObjectIcon
                            when SpannedRecordCodec.TryDecodeObjectIcon(recordData, out var gfdIcon)
                                 && this.renderPass.Renderer.TryGetIcon(
                                     this.renderPass.ActiveTextState.GfdIndex,
                                     (uint)gfdIcon,
                                     new(0, this.fontInfo.ScaledFontSize),
                                     out var tex,
                                     out var uv0,
                                     out var uv1):
                        {
                            var dim = tex.Size * (uv1 - uv0);
                            boundary = new(
                                0,
                                0,
                                MathF.Round((this.fontInfo.ScaledFontSize * dim.X) / dim.Y),
                                this.fontInfo.ScaledFontSize);
                            break;
                        }

                        case SpannedRecordType.ObjectTexture
                            when SpannedRecordCodec.TryDecodeObjectTexture(
                                     recordData,
                                     out var index,
                                     out var uv0,
                                     out var uv1)
                                 && this.data.TryGetTextureAt(index, out var tex):
                        {
                            var dim = tex.Size * (uv1 - uv0);
                            boundary = new(
                                0,
                                0,
                                MathF.Round((this.fontInfo.ScaledFontSize * dim.X) / dim.Y),
                                this.fontInfo.ScaledFontSize);
                            break;
                        }

                        case SpannedRecordType.ObjectSpannable
                            when SpannedRecordCodec.TryDecodeObjectSpannable(
                                     recordData,
                                     out var index)
                                 && this.data.TryGetSpannableAt(index, out var spannable)
                                 && this.renderPass.SpannableStates[index] is { } spannableState:
                        {
                            spannableState.MeasureSpannable(
                                new(
                                    spannable,
                                    spannableState,
                                    new(
                                        IsEffectivelyInfinity(this.args.MaxSize.X)
                                            ? float.PositiveInfinity
                                            : this.args.MaxSize.X - this.renderPass.Offset.X,
                                        Math.Min(
                                            this.args.MaxSize.Y - this.renderPass.Offset.Y,
                                            this.fontInfo.ScaledFontSize)),
                                    this.renderPass.Scale,
                                    this.renderPass.ActiveTextState with
                                    {
                                        InitialStyle = this.currentStyle,
                                        LastStyle = this.currentStyle,
                                    },
                                    this.args.RenderPass.GetGlobalIdFromInnerId(offset.Record)));
                            boundary = spannableState.Boundary;
                            break;
                        }

                        default:
                            boundary = default;
                            break;
                    }

                    current.UnionBBoxVertical(this.fontInfo.BBoxVertical.X, this.fontInfo.BBoxVertical.Y);
                    current.AddObject(this.fontInfo, offset.Record, boundary.Left, boundary.Right);
                    current.SetOffset(offsetAfter, pad);
                    if (current.Height < boundary.Height)
                        current.BBoxVertical *= boundary.Height / current.Height;

                    break;
                }

                case '\t':
                    current.SetOffset(offsetAfter, pad);
                    current.AddTabCharacter(this.fontInfo, this.renderPass.ActiveTextState.TabWidth);
                    break;

                // Soft hyphen; only determine if this offset can be used as a word break point.
                case '\u00AD':
                    current.AddSoftHyphenCharacter(this.fontInfo);
                    current.SetOffset(offsetAfter, pad);
                    if (current.ContainedInBoundsWithObject(
                            this.fontInfo,
                            this.wrapMarkerWidth,
                            this.args.MaxSize.X))
                    {
                        this.wrapMarkerBreak = this.normalBreak = current;
                    }

                    break;

                default:
                    current.AddStandardCharacter(this.fontInfo, c);
                    current.SetOffset(offsetAfter, pad);
                    break;
            }

            var breakable = c >= 0 && c < WordBreakNormalBreakChars.Length && WordBreakNormalBreakChars[c];
            if (this.breakOnFirstNormalBreakableOffset && breakable)
            {
                this.prev.LastThing.SetCodepoint(c);
                this.prev.SetOffset(offsetAfter);
                return this.prev.WithWrapped();
            }

            this.UnionLineBBoxVertical(ref current);
            if (this.first.IsEmpty)
                this.first = current;

            if (current.ContainedInBounds(this.fontInfo, this.args.MaxSize.X))
            {
                if (this.renderPass.ActiveTextState.WrapMarker is not null)
                {
                    if (current.ContainedInBoundsWithObject(
                            this.fontInfo,
                            this.wrapMarkerWidth,
                            this.args.MaxSize.X))
                        this.wrapMarkerBreak = current;
                    else
                        breakable = false;
                }
            }
            else
            {
                var resolved = MeasuredLine.Empty;
                switch (this.renderPass.ActiveTextState.WordBreak)
                {
                    case WordBreakType.Normal:
                        this.breakOnFirstNormalBreakableOffset = true;
                        resolved = MeasuredLine.FirstNonEmpty(this.normalBreak);
                        break;

                    case WordBreakType.BreakAll when this.renderPass.ActiveTextState.WrapMarker is not null:
                    case WordBreakType.KeepAll when this.renderPass.ActiveTextState.WrapMarker is not null:
                        resolved = MeasuredLine.FirstNonEmpty(this.wrapMarkerBreak, this.first);
                        break;

                    case WordBreakType.BreakAll:
                        resolved = MeasuredLine.FirstNonEmpty(this.prev, this.first);
                        break;

                    case WordBreakType.BreakWord when this.renderPass.ActiveTextState.WrapMarker is not null:
                        resolved = MeasuredLine.FirstNonEmpty(this.normalBreak, this.wrapMarkerBreak, this.first);
                        break;

                    case WordBreakType.BreakWord:
                        resolved = MeasuredLine.FirstNonEmpty(this.normalBreak, this.prev, this.first);
                        break;

                    case WordBreakType.KeepAll:
                    default:
                        break;
                }

                if (!resolved.IsEmpty)
                    return resolved.WithWrapped();
            }

            this.prev = current;
            if (breakable)
                this.normalBreak = current;

            return MeasuredLine.Empty;
        }

        private void SpanFontOptionsUpdated()
        {
            this.renderPass.Renderer.TryGetFontData(this.renderPass.Scale, in this.currentStyle, out this.fontInfo);
            if (this.renderPass.ActiveTextState.WrapMarker is not null)
                this.UpdateWrapMarker();
        }

        private void UpdateWrapMarker()
        {
            if (this.renderPass.ActiveTextState.WrapMarker is not { } wm)
                return;

            var spannableState = wm.RentRenderPass(this.renderPass.Renderer);
            spannableState.MeasureSpannable(
                new(
                    wm,
                    spannableState,
                    new(
                        IsEffectivelyInfinity(this.args.MaxSize.X)
                            ? float.PositiveInfinity
                            : this.args.MaxSize.X - this.renderPass.Offset.X,
                        this.prev.Height),
                    this.renderPass.Scale,
                    this.renderPass.ActiveTextState with
                    {
                        InitialStyle = this.currentStyle,
                        LastStyle = this.currentStyle,
                        WrapMarker = null,
                    },
                    0));
            this.wrapMarkerWidth = spannableState.Boundary.IsValid ? spannableState.Boundary.Right : 0;
            wm.ReturnRenderPass(spannableState);
        }
    }
}
