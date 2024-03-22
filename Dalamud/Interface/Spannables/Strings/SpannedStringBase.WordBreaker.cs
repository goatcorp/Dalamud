using System.Runtime.CompilerServices;

using Dalamud.Interface.Spannables.EventHandlerArgs;
using Dalamud.Interface.Spannables.Internal;
using Dalamud.Interface.Spannables.Rendering;
using Dalamud.Interface.Spannables.Styles;
using Dalamud.Utility.Numerics;

namespace Dalamud.Interface.Spannables.Strings;

/// <summary>Base class for <see cref="SpannedString"/> and <see cref="SpannedStringBuilder"/>.</summary>
public abstract partial class SpannedStringBase
{
    private ref struct WordBreaker
    {
        private readonly SpannableMeasureArgs args;
        private readonly State state;
        private readonly DataRef data;
        
        private TextStyle currentStyle;
        private TextStyleFontData fontInfo;

        private bool breakOnFirstNormalBreakableOffset;

        private float wrapMarkerWidth;
        private MeasuredLine prev;
        private MeasuredLine first;

        private MeasuredLine normalBreak;
        private MeasuredLine wrapMarkerBreak;

        public WordBreaker(SpannableMeasureArgs args, in DataRef data, State state)
        {
            this.args = args;
            this.state = state;
            this.data = data;
            this.currentStyle = state.TextState.LastStyle;
            this.prev = MeasuredLine.Empty;
            this.first = MeasuredLine.Empty;
            this.normalBreak = MeasuredLine.Empty;
            this.wrapMarkerBreak = MeasuredLine.Empty;

            this.SpanFontOptionsUpdated();
            if (this.state.TextState.WrapMarker is not null)
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
                this.state.TextState.InitialStyle,
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
                    when (this.state.TextState.AcceptedNewLines & NewLineType.Manual) != 0:
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
                                 && this.state.Renderer.TryGetIcon(
                                     this.state.TextState.GfdIndex,
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
                                 && this.data.TryGetSpannableAt(index, out var spannable):
                        {
                            ref var spannableState = ref this.state.SpannableStates[index];
                            if (spannableState is not null)
                            {
                                spannable.ReturnState(spannableState);
                                spannableState = null;
                            }

                            spannableState = spannable.RentState(
                                new(
                                    this.state.Renderer,
                                    this.state.GetGlobalIdFromInnerId(offset.Record),
                                    this.state.Scale,
                                    this.state.TextState with
                                    {
                                        InitialStyle = this.currentStyle,
                                        LastStyle = this.currentStyle,
                                    }));
                            spannable.MeasureSpannable(
                                new(
                                    spannableState,
                                    new(
                                        IsEffectivelyInfinity(this.args.MaxSize.X)
                                            ? float.MaxValue
                                            : this.args.MaxSize.X - this.state.Offset.X,
                                        Math.Min(
                                            this.args.MaxSize.Y - this.state.Offset.Y,
                                            this.fontInfo.ScaledFontSize))));
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
                    current.AddTabCharacter(this.fontInfo, this.state.TextState.TabWidth);
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
                if (this.state.TextState.WrapMarker is not null)
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
                switch (this.state.TextState.WordBreak)
                {
                    case WordBreakType.Normal:
                        this.breakOnFirstNormalBreakableOffset = true;
                        resolved = MeasuredLine.FirstNonEmpty(this.normalBreak);
                        break;

                    case WordBreakType.BreakAll when this.state.TextState.WrapMarker is not null:
                    case WordBreakType.KeepAll when this.state.TextState.WrapMarker is not null:
                        resolved = MeasuredLine.FirstNonEmpty(this.wrapMarkerBreak, this.first);
                        break;

                    case WordBreakType.BreakAll:
                        resolved = MeasuredLine.FirstNonEmpty(this.prev, this.first);
                        break;

                    case WordBreakType.BreakWord when this.state.TextState.WrapMarker is not null:
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
            this.state.Renderer.TryGetFontData(this.state.Scale, in this.currentStyle, out this.fontInfo);
            if (this.state.TextState.WrapMarker is not null)
                this.UpdateWrapMarker();
        }

        private void UpdateWrapMarker()
        {
            if (this.state.TextState.WrapMarker is not { } wm)
                return;

            var spannableState = wm.RentState(
                new(
                    this.state.Renderer,
                    0,
                    this.state.Scale,
                    this.state.TextState with
                    {
                        InitialStyle = this.currentStyle,
                        LastStyle = this.currentStyle,
                        WrapMarker = null,
                    }));
            wm.MeasureSpannable(
                new(
                    spannableState,
                    new(
                        IsEffectivelyInfinity(this.args.MaxSize.X)
                            ? float.MaxValue
                            : this.args.MaxSize.X - this.state.Offset.X,
                        this.prev.Height)));
            this.wrapMarkerWidth = spannableState.Boundary.IsValid ? spannableState.Boundary.Right : 0;
            wm.ReturnState(spannableState);
        }
    }
}
