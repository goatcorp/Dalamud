using System.Numerics;
using System.Runtime.CompilerServices;

using Dalamud.Interface.Spannables.Internal;
using Dalamud.Interface.Spannables.Rendering;
using Dalamud.Interface.Spannables.Styles;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Numerics;

namespace Dalamud.Interface.Spannables.Text.Internal;

/// <summary>Helper for finding a word break point.</summary>
internal ref struct TsWordBreaker
{
    private readonly StyledTextSpannable ts;
    private readonly ISpannableRenderer renderer;
    private readonly TsDataSpan data;
    private readonly Vector2 preferredSize;

    private TextStyle currentStyle;
    private TextStyleFontData fontInfo;

    private bool breakOnFirstNormalBreakableOffset;

    private float wrapMarkerWidth;
    private TsMeasuredLine prev;
    private TsMeasuredLine first;

    private TsMeasuredLine normalBreak;
    private TsMeasuredLine wrapMarkerBreak;

    /// <summary>Initializes a new instance of the <see cref="TsWordBreaker"/> struct.</summary>
    /// <param name="ts">The text spannable.</param>
    /// <param name="data">The data span.</param>
    /// <param name="preferredSize">The preferred size.</param>
    public TsWordBreaker(StyledTextSpannable ts, in TsDataSpan data, Vector2 preferredSize)
    {
        this.ts = ts;
        this.renderer = ts.Renderer!;
        this.data = data;
        this.preferredSize = preferredSize;
        this.currentStyle = ts.LastStyle;
        this.prev = TsMeasuredLine.Empty;
        this.first = TsMeasuredLine.Empty;
        this.normalBreak = TsMeasuredLine.Empty;
        this.wrapMarkerBreak = TsMeasuredLine.Empty;

        this.SpanFontOptionsUpdated();
        if (ts.WrapMarker is not null)
            this.UpdateWrapMarker();
    }

    /// <summary>Gets the last measured line.</summary>
    public readonly TsMeasuredLine Last => this.prev;

    /// <summary>Clears the last thing stored.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ResetLastChar() => this.prev.LastThing.Clear();

    /// <summary>Unions the vertical boundary box.</summary>
    /// <param name="line">The line to union.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UnionLineBBoxVertical(ref TsMeasuredLine line) =>
        line.UnionBBoxVertical(
            this.fontInfo.BBoxVertical.X,
            this.fontInfo.BBoxVertical.Y,
            this.fontInfo.RenderScale);

    /// <summary>Handles a span record.</summary>
    /// <param name="record">The record.</param>
    /// <param name="recordData">The record data.</param>
    /// <param name="offset">The offset of the span.</param>
    /// <param name="offsetAfter">The offset after the span.</param>
    /// <returns>The boundary from drawing the spannable, if applicable.</returns>
    public TsMeasuredLine HandleSpan(
        in SpannedRecord record,
        ReadOnlySpan<byte> recordData,
        TsCompositeOffset offset,
        TsCompositeOffset offsetAfter)
    {
        this.currentStyle.UpdateFrom(
            record,
            recordData,
            this.ts.Style,
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
                return this.AddCodepointAndMeasure(offset, offsetAfter, -1, record, recordData);
            case SpannedRecordType.ObjectNewLine
                when (this.ts.AcceptedNewLines & NewLineType.Manual) != 0:
                this.prev.LastThing.SetRecord(offset.Record);
                this.prev.SetOffset(offsetAfter, this.fontInfo.RenderScale, 0);
                this.UnionLineBBoxVertical(ref this.prev);
                return this.prev with { HasNewLineAtEnd = true };
            default:
                this.prev.LastThing.SetRecord(offset.Record);
                return TsMeasuredLine.Empty;
        }
    }

    /// <summary>Adds a codepoint and measure.</summary>
    /// <param name="offset">The offset of the span.</param>
    /// <param name="offsetAfter">The offset after the span.</param>
    /// <param name="c">The codepoint.</param>
    /// <param name="record">The record.</param>
    /// <param name="recordData">The record data.</param>
    /// <param name="pad">The horizontal pad amount.</param>
    /// <returns>Non-empty line if a word break point is found; otherwise, empty.</returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public TsMeasuredLine AddCodepointAndMeasure(
        TsCompositeOffset offset,
        TsCompositeOffset offsetAfter,
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
                                 this.ts.GfdIconMode,
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
                             && this.ts.Children[index] is { } smm:
                    {
                        smm.RenderScale = this.ts.EffectiveRenderScale;
                        smm.RenderPassMeasure(
                            new(
                                float.PositiveInfinity,
                                Math.Min(
                                    this.preferredSize.Y - this.ts.LastOffset.Y,
                                    this.fontInfo.ScaledFontSize)));
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
                current.AddTabCharacter(this.fontInfo, this.ts.TabWidth);
                break;

            // Soft hyphen; only determine if this offset can be used as a word break point.
            case '\u00AD':
                current.AddSoftHyphenCharacter(this.fontInfo);
                current.SetOffset(offsetAfter, this.fontInfo.RenderScale, pad);
                if (current.ContainedInBoundsWithObject(
                        this.fontInfo,
                        this.wrapMarkerWidth,
                        this.preferredSize.X))
                {
                    this.wrapMarkerBreak = this.normalBreak = current;
                }

                break;

            default:
                current.AddStandardCharacter(this.fontInfo, c);
                current.SetOffset(offsetAfter, this.fontInfo.RenderScale, pad);
                break;
        }

        var breakable = c >= 0 && c < AbstractStyledText.WordBreakNormalBreakChars.Length && AbstractStyledText.WordBreakNormalBreakChars[c];
        if (this.breakOnFirstNormalBreakableOffset && breakable)
        {
            this.prev.LastThing.SetCodepoint(c);
            this.prev.SetOffset(offsetAfter, this.fontInfo.RenderScale, 0);
            return this.prev.WithWrapped();
        }

        this.UnionLineBBoxVertical(ref current);
        if (this.first.IsEmpty)
            this.first = current;

        var wrapWidth = this.preferredSize.X;
        if (current.ContainedInBounds(wrapWidth, this.fontInfo.RenderScale))
        {
            if (this.ts.WrapMarker is not null)
            {
                if (current.ContainedInBoundsWithObject(this.fontInfo, this.wrapMarkerWidth, wrapWidth))
                    this.wrapMarkerBreak = current;
                else
                    breakable = false;
            }
        }
        else
        {
            var resolved = TsMeasuredLine.Empty;
            switch (this.ts.WordBreak)
            {
                case WordBreakType.Normal:
                    this.breakOnFirstNormalBreakableOffset = true;
                    resolved = TsMeasuredLine.FirstNonEmpty(this.normalBreak);
                    break;

                case WordBreakType.BreakAll when this.ts.WrapMarker is not null:
                case WordBreakType.KeepAll when this.ts.WrapMarker is not null:
                    resolved = TsMeasuredLine.FirstNonEmpty(this.wrapMarkerBreak, this.first);
                    break;

                case WordBreakType.BreakAll:
                    resolved = TsMeasuredLine.FirstNonEmpty(this.prev, this.first);
                    break;

                case WordBreakType.BreakWord when this.ts.WrapMarker is not null:
                    resolved = TsMeasuredLine.FirstNonEmpty(this.normalBreak, this.wrapMarkerBreak, this.first);
                    break;

                case WordBreakType.BreakWord:
                    resolved = TsMeasuredLine.FirstNonEmpty(this.normalBreak, this.prev, this.first);
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

        return TsMeasuredLine.Empty;
    }

    private void SpanFontOptionsUpdated()
    {
        this.renderer.TryGetFontData(this.ts.EffectiveRenderScale, in this.currentStyle, out this.fontInfo);
        if (this.ts.WrapMarker is not null)
            this.UpdateWrapMarker();
    }

    private void UpdateWrapMarker()
    {
        if (this.ts.WrapMarker is not { } wm)
            return;

        var wmm = wm.CreateSpannable();
        wmm.RenderScale = this.ts.EffectiveRenderScale;
        wmm.RenderPassMeasure(new(float.PositiveInfinity));
        this.wrapMarkerWidth = wmm.Boundary.IsValid ? wmm.Boundary.Right : 0;
    }
}
