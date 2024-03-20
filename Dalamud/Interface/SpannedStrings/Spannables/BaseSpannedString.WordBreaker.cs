using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;

using Dalamud.Interface.SpannedStrings.Enums;
using Dalamud.Interface.SpannedStrings.Internal;
using Dalamud.Interface.SpannedStrings.Rendering;
using Dalamud.Interface.SpannedStrings.Styles;

using ImGuiNET;

namespace Dalamud.Interface.SpannedStrings.Spannables;

/// <summary>Base class for <see cref="SpannedString"/> and <see cref="SpannedStringBuilder"/>.</summary>
public abstract partial class BaseSpannedString
{
    private ref struct WordBreaker
    {
        private readonly State state;
        private readonly DataRef data;

        private SpanStyle currentStyle;
        private FontData fontInfo;

        private bool breakOnFirstNormalBreakableOffset;

        private float wrapMarkerWidth;
        private MeasuredLine prev;
        private MeasuredLine first;

        private MeasuredLine normalBreak;
        private MeasuredLine wrapMarkerBreak;

        public WordBreaker(in DataRef data, State state)
        {
            this.state = state;
            this.data = data;
            this.currentStyle = state.RenderState.LastStyle;
            this.prev = MeasuredLine.Empty;
            this.first = MeasuredLine.Empty;
            this.normalBreak = MeasuredLine.Empty;
            this.wrapMarkerBreak = MeasuredLine.Empty;
            this.fontInfo = new(this.state.RenderState.Scale);

            this.SpanFontOptionsUpdated();
            if (this.state.RenderState.WrapMarker is not null)
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
                this.state.RenderState.InitialStyle,
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
                    when (this.state.RenderState.AcceptedNewLines & NewLineType.Manual) != 0:
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
            CompositeOffset offsetBefore,
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
                                 && this.state.Renderer.TryGetIcon(
                                     this.state.RenderState.GfdIndex,
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
                                     out var index,
                                     out var spannableArgs)
                                 && this.data.TryGetSpannableAt(index, out var spannable):
                        {
                            ref var spannableState = ref this.state.SpannableStates[index];
                            if (spannableState is not null)
                            {
                                spannable.ReturnState(spannableState);
                                spannableState = null;
                            }

                            spannableState = spannable.RentState(
                                this.state.Renderer,
                                this.state.RenderState with
                                {
                                    PutDummyAfterRender = false,
                                    ImGuiGlobalId = 0, // TODO
                                    StartScreenOffset = new(float.NaN),
                                    InitialStyle = this.state.RenderState.LastStyle,
                                    LineCount = 0,
                                    Offset = Vector2.Zero,
                                    Boundary = RectVector4.InvertedExtrema,
                                    ClickedMouseButton = (ImGuiMouseButton)(-1),
                                    DrawListPtr = null,
                                    MaxSize = new(
                                        IsEffectivelyInfinity(this.state.RenderState.MaxSize.X)
                                            ? float.MaxValue
                                            : this.state.RenderState.MaxSize.X - this.state.RenderState.Offset.X,
                                        Math.Min(
                                            this.state.RenderState.MaxSize.Y - this.state.RenderState.Offset.Y,
                                            this.fontInfo.ScaledFontSize)),
                                },
                                spannableArgs);
                            spannable.Measure(new(spannableState));
                            boundary = spannableState.RenderState.Boundary;
                            break;
                        }

                        default:
                            boundary = default;
                            break;
                    }

                    current.AddObject(this.fontInfo, offsetBefore.Record, boundary.Left, boundary.Right);
                    current.SetOffset(offsetAfter, pad);
                    break;
                }

                case '\t':
                    current.SetOffset(offsetAfter, pad);
                    current.AddTabCharacter(this.fontInfo, this.state.RenderState.TabWidth);
                    break;

                // Soft hyphen; only determine if this offset can be used as a word break point.
                case '\u00AD':
                    current.AddSoftHyphenCharacter(this.fontInfo);
                    current.SetOffset(offsetAfter, pad);
                    if (current.ContainedInBoundsWithObject(
                            this.fontInfo,
                            this.wrapMarkerWidth,
                            this.state.RenderState.MaxSize.X))
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
                this.prev.SetOffset(offsetBefore);
                return this.prev.WithWrapped();
            }

            this.UnionLineBBoxVertical(ref current);
            if (this.first.IsEmpty)
                this.first = current;

            if (current.ContainedInBounds(this.fontInfo, this.state.RenderState.MaxSize.X))
            {
                if (current.ContainedInBoundsWithObject(
                        this.fontInfo,
                        this.wrapMarkerWidth,
                        this.state.RenderState.MaxSize.X))
                    this.wrapMarkerBreak = current;
                else
                    breakable = false;
            }
            else
            {
                switch (this.state.RenderState.WordBreak)
                {
                    case WordBreakType.Normal:
                        this.breakOnFirstNormalBreakableOffset = true;
                        return MeasuredLine.FirstNonEmpty(this.normalBreak)
                                           .WithWrapped();

                    case WordBreakType.BreakAll when this.state.RenderState.WrapMarker is not null:
                    case WordBreakType.KeepAll when this.state.RenderState.WrapMarker is not null:
                        return MeasuredLine.FirstNonEmpty(this.wrapMarkerBreak, this.first)
                                           .WithWrapped();

                    case WordBreakType.BreakAll:
                        return MeasuredLine.FirstNonEmpty(this.prev, this.first)
                                           .WithWrapped();

                    case WordBreakType.BreakWord when this.state.RenderState.WrapMarker is not null:
                        return MeasuredLine.FirstNonEmpty(this.normalBreak, this.wrapMarkerBreak, this.first)
                                           .WithWrapped();

                    case WordBreakType.BreakWord:
                        return MeasuredLine.FirstNonEmpty(this.normalBreak, this.prev, this.first)
                                           .WithWrapped();

                    case WordBreakType.KeepAll:
                    default:
                        break;
                }
            }

            this.prev = current;
            if (breakable)
                this.normalBreak = current;

            return MeasuredLine.Empty;
        }

        [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator", Justification = "no")]
        private static bool IsEffectivelyInfinity(float f) => f == float.MaxValue || float.IsPositiveInfinity(f);

        private void SpanFontOptionsUpdated()
        {
            this.fontInfo.Update(in this.currentStyle);
            if (this.state.RenderState.WrapMarker is not null)
                this.UpdateWrapMarker();
        }

        private void UpdateWrapMarker()
        {
            if (this.state.RenderState.WrapMarker is not { } wm)
                return;

            var state2 = wm.RentState(
                this.state.Renderer,
                this.state.RenderState with
                {
                    Offset = Vector2.Zero,
                    Boundary = RectVector4.InvertedExtrema,
                    WordBreak = WordBreakType.KeepAll,
                    WrapMarker = null,
                    DrawListPtr = null,
                    MaxSize = new(
                        IsEffectivelyInfinity(this.state.RenderState.MaxSize.X)
                            ? float.MaxValue
                            : this.state.RenderState.MaxSize.X - this.state.RenderState.Offset.X,
                        this.prev.Height),
                },
                null);
            this.state.RenderState.WrapMarker.Measure(new(state2));
            if (state2.RenderState.Boundary.IsValid)
                this.wrapMarkerWidth = state2.RenderState.Boundary.Right;
            else
                this.wrapMarkerWidth = 0;
            wm.ReturnState(state2);
        }
    }
}
