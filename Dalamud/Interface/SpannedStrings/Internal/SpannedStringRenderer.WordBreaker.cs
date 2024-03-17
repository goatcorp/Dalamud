using System.Numerics;
using System.Runtime.CompilerServices;

using Dalamud.Interface.SpannedStrings.Enums;
using Dalamud.Interface.SpannedStrings.Styles;

namespace Dalamud.Interface.SpannedStrings.Internal;

/// <summary>A custom text renderer implementation.</summary>
internal sealed partial class SpannedStringRenderer
{
    private ref struct WordBreaker
    {
        private readonly SpannedStringRenderer renderer;
        private readonly SpannedStringData data;

        private SpanStyle currentStyle;
        private SpanStyleFontData fontInfo;

        private bool breakOnFirstNormalBreakableOffset;

        private MeasuredLine wrapMarker;
        private MeasuredLine current;

        private MeasuredLine normalBreak;
        private MeasuredLine wrapMarkerBreak;

        public WordBreaker(SpannedStringRenderer renderer, in SpannedStringData data, in RenderState state)
        {
            this.renderer = renderer;
            this.data = data;
            this.currentStyle = state.LastSpanStyle;
            this.current = state.LastMeasurement;
            this.normalBreak = MeasuredLine.Empty;
            this.wrapMarkerBreak = MeasuredLine.Empty;
            this.fontInfo = new(renderer.options.Scale);

            this.SpanFontOptionsUpdated();
            if (this.renderer.options.UseWrapMarkerParams)
                this.UpdateWrapMarker(this.renderer.options.WrapMarkerStyle);
            else
                this.wrapMarker = MeasuredLine.Empty;
        }

        public readonly MeasuredLine Last => this.current;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ResetLastChar() => this.current.LastGlyphCodepoint = 0;

        public MeasuredLine HandleSpan(
            in SpannedRecord record,
            ReadOnlySpan<byte> recordData,
            SpannedOffset offsetBefore,
            SpannedOffset offsetAfter)
        {
            this.currentStyle.UpdateFrom(
                this.data,
                record,
                recordData,
                this.renderer.options.InitialSpanStyle,
                out var fontUpdated,
                out _);
            if (fontUpdated)
                this.SpanFontOptionsUpdated();

            switch (record.Type)
            {
                case SpannedRecordType.InsertionIcon:
                case SpannedRecordType.InsertionTexture:
                case SpannedRecordType.InsertionCallback:
                    return this.AddCodepointAndMeasure(offsetBefore, offsetAfter, -1, record, recordData);
                case SpannedRecordType.InsertionManualNewLine
                    when (this.renderer.options.AcceptedNewLines & NewLineType.Manual) != 0:
                    this.current.SetOffset(offsetAfter);
                    return this.current;
                default:
                    return MeasuredLine.Empty;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public MeasuredLine AddCodepointAndMeasure(
            SpannedOffset offsetBefore,
            SpannedOffset offsetAfter,
            int c,
            in SpannedRecord record = default,
            ReadOnlySpan<byte> recordData = default,
            float pad = 0)
        {
            var breakable = c >= 0 && c < WordBreakNormalBreakChars.Length && WordBreakNormalBreakChars[c];
            if (this.breakOnFirstNormalBreakableOffset)
            {
                if (breakable)
                {
                    this.current.SetOffset(offsetBefore);
                    return this.current.WithWrapped();
                }

                return MeasuredLine.Empty;
            }

            switch (c)
            {
                case -1:
                {
                    Vector2 dim;
                    switch (record.Type)
                    {
                        case SpannedRecordType.InsertionIcon
                            when SpannedRecordCodec.TryDecodeInsertionIcon(recordData, out var gfdIcon)
                                 && this.renderer.factory.GfdFileView.TryGetEntry((uint)gfdIcon, out var entry):
                            dim = new(entry.Width, entry.Height);
                            break;

                        case SpannedRecordType.InsertionTexture
                            when SpannedRecordCodec.TryDecodeInsertionTexture(
                                     recordData,
                                     out var index,
                                     out var uv0,
                                     out var uv1)
                                 && this.data.TryGetTextureAt(index, out var tex):
                            dim = tex.Size * (uv1 - uv0);
                            break;

                        case SpannedRecordType.InsertionCallback
                            when SpannedRecordCodec.TryDecodeInsertionCallback(
                                recordData,
                                out _,
                                out var ratio):
                            dim = new(ratio, 1);
                            break;

                        default:
                            this.current.SetOffset(offsetAfter, pad);
                            return MeasuredLine.Empty;
                    }

                    var width = MathF.Round((this.fontInfo.ScaledFontSize * dim.X) / dim.Y);
                    this.current.AddObject(this.fontInfo, 0, width);
                    this.current.SetOffset(offsetAfter, pad);
                    break;
                }

                case '\t':
                    this.current.SetOffset(offsetAfter, pad);
                    this.current.AddTabCharacter(this.fontInfo, this.renderer.options.TabWidth);
                    break;

                // Soft hyphen; only determine if this offset can be used as a word break point.
                case '\u00AD':
                    this.current.AddSoftHyphenCharacter(this.fontInfo);
                    this.current.SetOffset(offsetAfter, pad);
                    if (this.current.WithObject(this.fontInfo, 0, this.wrapMarker.Width).BBoxHorizontal.Y
                        <= this.renderer.options.LineWrapWidth)
                        this.wrapMarkerBreak = this.normalBreak = this.current;

                    return MeasuredLine.Empty;

                default:
                    this.current.AddStandardCharacter(this.fontInfo, c);
                    this.current.SetOffset(offsetAfter, pad);
                    break;
            }

            if (this.current.BBoxHorizontal.Y <= this.renderer.options.LineWrapWidth)
            {
                if (this.current.WithObject(this.fontInfo, 0, this.wrapMarker.Width).BBoxHorizontal.Y
                    <= this.renderer.options.LineWrapWidth)
                    this.wrapMarkerBreak = this.current;
                else
                    breakable = false;
            }
            else
            {
                switch (this.renderer.options.WordBreak)
                {
                    case WordBreakType.Normal:
                        this.breakOnFirstNormalBreakableOffset = true;
                        return MeasuredLine.FirstNonEmpty(this.normalBreak)
                                           .WithWrapped();

                    case WordBreakType.BreakAll:
                    case WordBreakType.KeepAll when this.renderer.options.UseWrapMarker:
                        return MeasuredLine.FirstNonEmpty(this.wrapMarkerBreak, this.current)
                                           .WithWrapped();

                    case WordBreakType.BreakWord:
                        return MeasuredLine.FirstNonEmpty(this.normalBreak, this.wrapMarkerBreak, this.current)
                                           .WithWrapped();

                    case WordBreakType.KeepAll:
                    default:
                        break;
                }
            }

            if (breakable)
                this.normalBreak = this.current;

            return MeasuredLine.Empty;
        }

        private void SpanFontOptionsUpdated()
        {
            this.fontInfo.Update(in this.currentStyle);
            this.current.UnionBBoxVertical(this.fontInfo.BBoxVertical.X, this.fontInfo.BBoxVertical.Y);
            if (!this.renderer.options.UseWrapMarkerParams)
                this.UpdateWrapMarker(in this.currentStyle);
        }

        private void UpdateWrapMarker(in SpanStyle wmdp)
        {
            if (!this.renderer.options.UseWrapMarker)
                return;

            var wmfi = new SpanStyleFontData(this.renderer.options.Scale);
            wmfi.Update(in wmdp);
            this.wrapMarker = MeasuredLine.Empty;
            foreach (var c in this.renderer.options.WrapMarker)
                this.wrapMarker.AddStandardCharacter(wmfi, c);
        }
    }
}
