using System.Runtime.CompilerServices;

using Dalamud.Interface.Spannables.Helpers;
using Dalamud.Interface.Spannables.Internal;
using Dalamud.Interface.Spannables.Rendering;
using Dalamud.Interface.Spannables.Styles;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Numerics;

namespace Dalamud.Interface.Spannables.Text;

/// <summary>Base class for <see cref="StyledText"/> and <see cref="StyledTextBuilder"/>.</summary>
public abstract partial class AbstractStyledText
{
    private ref struct WordBreaker
    {
        private readonly TextSpannable mm;
        private readonly ISpannableRenderer renderer;
        private readonly DataRef data;

        private TextStyle currentStyle;
        private TextStyleFontData fontInfo;

        private bool breakOnFirstNormalBreakableOffset;

        private float wrapMarkerWidth;
        private MeasuredLine prev;
        private MeasuredLine first;

        private MeasuredLine normalBreak;
        private MeasuredLine wrapMarkerBreak;

        public WordBreaker(TextSpannable mm, in DataRef data)
        {
            this.mm = mm;
            this.renderer = mm.Renderer!;
            this.data = data;
            this.currentStyle = mm.LastStyle;
            this.prev = MeasuredLine.Empty;
            this.first = MeasuredLine.Empty;
            this.normalBreak = MeasuredLine.Empty;
            this.wrapMarkerBreak = MeasuredLine.Empty;

            this.SpanFontOptionsUpdated();
            if (mm.Options.WrapMarker is not null)
                this.UpdateWrapMarker();
        }

        public readonly MeasuredLine Last => this.prev;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ResetLastChar() => this.prev.LastThing.Clear();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnionLineBBoxVertical(ref MeasuredLine line) =>
            line.UnionBBoxVertical(
                this.fontInfo.BBoxVertical.X,
                this.fontInfo.BBoxVertical.Y,
                this.fontInfo.RenderScale);

        public MeasuredLine HandleSpan(
            in SpannedRecord record,
            ReadOnlySpan<byte> recordData,
            CompositeOffset offsetBefore,
            CompositeOffset offsetAfter)
        {
            this.currentStyle.UpdateFrom(
                record,
                recordData,
                this.mm.Options.Style,
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
                    when (this.mm.Options.AcceptedNewLines & NewLineType.Manual) != 0:
                    this.prev.LastThing.SetRecord(offsetBefore.Record);
                    this.prev.SetOffset(offsetAfter, this.fontInfo.RenderScale, 0);
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
                                 && this.renderer.TryGetIcon(
                                     this.mm.Options.GfdIconMode,
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
                                MathF.Ceiling((this.fontInfo.ScaledFontSize * dim.X * this.fontInfo.Scale) / dim.Y)
                                / this.fontInfo.Scale,
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
                                MathF.Ceiling((this.fontInfo.ScaledFontSize * dim.X * this.fontInfo.Scale) / dim.Y)
                                / this.fontInfo.Scale,
                                this.fontInfo.ScaledFontSize);
                            break;
                        }

                        case SpannedRecordType.ObjectSpannable
                            when SpannedRecordCodec.TryDecodeObjectSpannable(
                                     recordData,
                                     out var index)
                                 && this.mm.Children[index] is { } smm:
                        {
                            smm.ImGuiGlobalId = this.mm.GetGlobalIdFromInnerId(offset.Record);
                            smm.Options.RenderScale = this.mm.Options.RenderScale;
                            smm.Options.PreferredSize = new(
                                float.PositiveInfinity,
                                Math.Min(
                                    this.mm.Options.PreferredSize.Y - this.mm.LastOffset.Y,
                                    this.fontInfo.ScaledFontSize));
                            smm.Options.VisibleSize = smm.Options.PreferredSize;
                            smm.RenderPassMeasure();
                            boundary = smm.Boundary;
                            break;
                        }

                        default:
                            boundary = default;
                            break;
                    }

                    current.UnionBBoxVertical(
                        this.fontInfo.BBoxVertical.X,
                        this.fontInfo.BBoxVertical.Y,
                        this.fontInfo.RenderScale);
                    current.AddObject(this.fontInfo, offset.Record, boundary.Left, boundary.Right);
                    current.SetOffset(offsetAfter, this.fontInfo.RenderScale, pad);
                    if (current.Height < boundary.Height)
                        current.BBoxVertical *= boundary.Height / current.Height;

                    break;
                }

                case '\t':
                    current.SetOffset(offsetAfter, this.fontInfo.RenderScale, pad);
                    current.AddTabCharacter(this.fontInfo, this.mm.Options.TabWidth);
                    break;

                // Soft hyphen; only determine if this offset can be used as a word break point.
                case '\u00AD':
                    current.AddSoftHyphenCharacter(this.fontInfo);
                    current.SetOffset(offsetAfter, this.fontInfo.RenderScale, pad);
                    if (current.ContainedInBoundsWithObject(
                            this.fontInfo,
                            this.wrapMarkerWidth,
                            this.mm.Options.PreferredSize.X))
                    {
                        this.wrapMarkerBreak = this.normalBreak = current;
                    }

                    break;

                default:
                    current.AddStandardCharacter(this.fontInfo, c);
                    current.SetOffset(offsetAfter, this.fontInfo.RenderScale, pad);
                    break;
            }

            var breakable = c >= 0 && c < WordBreakNormalBreakChars.Length && WordBreakNormalBreakChars[c];
            if (this.breakOnFirstNormalBreakableOffset && breakable)
            {
                this.prev.LastThing.SetCodepoint(c);
                this.prev.SetOffset(offsetAfter, this.fontInfo.RenderScale, 0);
                return this.prev.WithWrapped();
            }

            this.UnionLineBBoxVertical(ref current);
            if (this.first.IsEmpty)
                this.first = current;

            var wrapWidth = this.mm.Options.PreferredSize.X;
            if (current.ContainedInBounds(wrapWidth, this.fontInfo.RenderScale))
            {
                if (this.mm.Options.WrapMarker is not null)
                {
                    if (current.ContainedInBoundsWithObject(this.fontInfo, this.wrapMarkerWidth, wrapWidth))
                        this.wrapMarkerBreak = current;
                    else
                        breakable = false;
                }
            }
            else
            {
                var resolved = MeasuredLine.Empty;
                switch (this.mm.Options.WordBreak)
                {
                    case WordBreakType.Normal:
                        this.breakOnFirstNormalBreakableOffset = true;
                        resolved = MeasuredLine.FirstNonEmpty(this.normalBreak);
                        break;

                    case WordBreakType.BreakAll when this.mm.Options.WrapMarker is not null:
                    case WordBreakType.KeepAll when this.mm.Options.WrapMarker is not null:
                        resolved = MeasuredLine.FirstNonEmpty(this.wrapMarkerBreak, this.first);
                        break;

                    case WordBreakType.BreakAll:
                        resolved = MeasuredLine.FirstNonEmpty(this.prev, this.first);
                        break;

                    case WordBreakType.BreakWord when this.mm.Options.WrapMarker is not null:
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
            this.renderer.TryGetFontData(this.mm.Options.RenderScale, in this.currentStyle, out this.fontInfo);
            if (this.mm.Options.WrapMarker is not null)
                this.UpdateWrapMarker();
        }

        private void UpdateWrapMarker()
        {
            if (this.mm.Options.WrapMarker is not { } wm)
                return;

            var wmm = wm.CreateSpannable();
            wmm.Options.RenderScale = this.mm.Options.RenderScale;
            wmm.Renderer = this.renderer;
            wmm.RenderPassMeasure();
            this.wrapMarkerWidth = wmm.Boundary.IsValid ? wmm.Boundary.Right : 0;
            wm.RecycleSpannable(wmm);
        }
    }
}
