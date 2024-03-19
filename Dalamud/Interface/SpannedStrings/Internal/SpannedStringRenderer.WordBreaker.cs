using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

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
        private bool breakOnFirstNonWhitespace;

        private MeasuredLine wrapMarker;
        private MeasuredLine prev;
        private MeasuredLine first;

        private MeasuredLine normalBreak;
        private MeasuredLine wrapMarkerBreak;

        public WordBreaker(SpannedStringRenderer renderer, in SpannedStringData data, in RenderState state)
        {
            this.renderer = renderer;
            this.data = data;
            this.currentStyle = state.LastStyle;
            this.prev = MeasuredLine.Empty;
            this.first = MeasuredLine.Empty;
            this.normalBreak = MeasuredLine.Empty;
            this.wrapMarkerBreak = MeasuredLine.Empty;
            this.fontInfo = new(renderer.options.Scale);

            this.SpanFontOptionsUpdated();
            if (this.renderer.options.UseWrapMarkerParams)
                this.UpdateWrapMarker(this.renderer.options.WrapMarkerStyle);
            else
                this.wrapMarker = MeasuredLine.Empty;
        }

        public readonly MeasuredLine Last => this.prev;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ResetLastChar() => this.prev.LastThing.Clear();

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
                    this.prev.LastThing.SetRecord(offsetBefore.Record);
                    this.prev.SetOffset(offsetAfter);
                    return this.prev;
                default:
                    this.prev.LastThing.SetRecord(offsetBefore.Record);
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
            var current = this.prev;
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
                            dim = new(0, 1);
                            break;
                    }

                    var width = MathF.Round((this.fontInfo.ScaledFontSize * dim.X) / dim.Y);
                    current.AddObject(this.fontInfo, offsetBefore.Record, 0, width);
                    current.SetOffset(offsetAfter, pad);
                    break;
                }

                case '\t':
                    current.SetOffset(offsetAfter, pad);
                    current.AddTabCharacter(this.fontInfo, this.renderer.options.TabWidth);
                    break;

                // Soft hyphen; only determine if this offset can be used as a word break point.
                case '\u00AD':
                    current.AddSoftHyphenCharacter(this.fontInfo);
                    current.SetOffset(offsetAfter, pad);
                    if (current.ContainedInBoundsWithObject(
                            this.fontInfo,
                            this.wrapMarker.Width,
                            this.renderer.options.LineWrapWidth))
                    {
                        this.wrapMarkerBreak = this.normalBreak = current;
                    }
                    else if (this.renderer.options.WordBreak != WordBreakType.KeepAll)
                    {
                        this.prev = current;
                        this.prev.LastThing.SetCodepoint(c);
                        this.prev.SetOffset(offsetAfter);
                        return this.prev.WithWrapped();
                    }

                    break;

                default:
                    current.AddStandardCharacter(this.fontInfo, c);
                    current.SetOffset(offsetAfter, pad);
                    break;
            }

            if (this.breakOnFirstNonWhitespace)
            {
                if (!IsBreakableWhitespace(c))
                {
                    this.prev.LastThing.SetCodepoint(c);
                    this.prev.SetOffset(offsetBefore);
                    return this.prev.WithWrapped();
                }

                return MeasuredLine.Empty;
            }

            var breakable = c >= 0 && c < WordBreakNormalBreakChars.Length && WordBreakNormalBreakChars[c];
            if (this.breakOnFirstNormalBreakableOffset)
            {
                if (breakable)
                {
                    if (IsBreakableWhitespace(c))
                    {
                        this.breakOnFirstNonWhitespace = true;
                    }
                    else
                    {
                        this.prev.LastThing.SetCodepoint(c);
                        this.prev.SetOffset(offsetBefore);
                        return this.prev.WithWrapped();
                    }
                }
            }

            if (this.first.IsEmpty)
                this.first = current;

            if (current.ContainedInBoundsWithObject(this.fontInfo, 0, this.renderer.options.LineWrapWidth))
            {
                if (current.ContainedInBoundsWithObject(this.fontInfo, this.wrapMarker.Width, this.renderer.options.LineWrapWidth))
                    this.wrapMarkerBreak = current;
                else
                    breakable = false;
            }
            else
            {
                switch (this.renderer.options.WordBreak)
                {
                    case not WordBreakType.KeepAll when IsBreakableWhitespace(c):
                        this.breakOnFirstNonWhitespace = true;
                        return MeasuredLine.Empty;

                    case WordBreakType.Normal:
                        this.breakOnFirstNormalBreakableOffset = true;
                        return MeasuredLine.FirstNonEmpty(this.normalBreak)
                                           .WithWrapped();

                    case WordBreakType.BreakAll when this.renderer.options.UseWrapMarker:
                        return MeasuredLine.FirstNonEmpty(this.wrapMarkerBreak, this.first)
                                           .WithWrapped();
                    
                    case WordBreakType.BreakAll:
                        return MeasuredLine.FirstNonEmpty(this.prev, this.first)
                                           .WithWrapped();

                    case WordBreakType.BreakWord when this.renderer.options.UseWrapMarker:
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

        /// <summary>Tests if a codepoint is a whitespace, and permits breaking under normal word break rules.</summary>
        /// <param name="c">The codepoint.</param>
        /// <returns><c>true</c> if it is the case.</returns>
        private static bool IsBreakableWhitespace(int c) => c != 0x00A0 && Rune.IsValid(c) && Rune.IsWhiteSpace(new(c)); 

        private void SpanFontOptionsUpdated()
        {
            this.fontInfo.Update(in this.currentStyle);
            this.prev.UnionBBoxVertical(this.fontInfo.BBoxVertical.X, this.fontInfo.BBoxVertical.Y);
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
