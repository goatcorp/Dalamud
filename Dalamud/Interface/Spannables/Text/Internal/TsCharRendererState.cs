using System.Numerics;
using System.Runtime.CompilerServices;

using Dalamud.Interface.Spannables.Rendering;

namespace Dalamud.Interface.Spannables.Text.Internal;

/// <summary>Stores extra calculated states for <see cref="TsCharRenderer"/>.</summary>
internal ref struct TsCharRendererState
{
    /// <summary>Line-specific horizontal offset.</summary>
    public float HorizontalOffsetWrtLine;

    /// <summary>Line-specific vertical offset.</summary>
    public float VerticalOffsetWrtLine;

    private readonly StyledTextSpannable ts;
    private readonly Vector2 preferredSize;
    private readonly Vector2 lineBBoxVertical;
    private readonly float lineWidth;

    /// <summary>Initializes a new instance of the <see cref="TsCharRendererState"/> struct.</summary>
    /// <param name="ts">The text spannable.</param>
    /// <param name="lineMeasurement">The line measurement.</param>
    /// <param name="preferredSize">The preferred size.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TsCharRendererState(StyledTextSpannable ts, scoped in TsMeasuredLine lineMeasurement, Vector2 preferredSize)
    {
        this.ts = ts;
        this.preferredSize = preferredSize;
        this.lineBBoxVertical = lineMeasurement.BBoxVertical;
        this.lineWidth = lineMeasurement.Width;
    }

    /// <summary>Updates the state.</summary>
    /// <param name="fontInfo">The font info.</param>
    public void Update(in TextStyleFontData fontInfo)
    {
        var lineAscentDescent = this.lineBBoxVertical;
        this.VerticalOffsetWrtLine = (fontInfo.BBoxVertical.Y - fontInfo.BBoxVertical.X) *
                                     this.ts.LastStyle.VerticalOffset;
        switch (this.ts.LastStyle.VerticalAlignment)
        {
            case < 0:
                this.VerticalOffsetWrtLine -= lineAscentDescent.X + (fontInfo.Font.Ascent * fontInfo.Scale);
                break;
            case >= 1f:
                this.VerticalOffsetWrtLine += lineAscentDescent.Y - lineAscentDescent.X - fontInfo.ScaledFontSize;
                break;
            default:
                this.VerticalOffsetWrtLine +=
                    (lineAscentDescent.Y - lineAscentDescent.X - fontInfo.ScaledFontSize) *
                    this.ts.LastStyle.VerticalAlignment;
                break;
        }

        this.VerticalOffsetWrtLine = MathF.Round(this.VerticalOffsetWrtLine * fontInfo.Scale) / fontInfo.Scale;

        var alignWidth = this.preferredSize.X;
        var alignLeft = 0f;
        if (alignWidth >= float.PositiveInfinity)
        {
            if (!this.ts.Boundary.IsValid)
            {
                this.HorizontalOffsetWrtLine = 0;
                return;
            }

            alignWidth = this.ts.Boundary.Width;
            alignLeft = this.ts.Boundary.Left;
        }

        switch (this.ts.LastStyle.HorizontalAlignment)
        {
            case <= 0f:
                this.HorizontalOffsetWrtLine = 0;
                break;

            case >= 1f:
                this.HorizontalOffsetWrtLine = alignLeft + (alignWidth - this.lineWidth);
                break;

            default:
                this.HorizontalOffsetWrtLine =
                    MathF.Round(
                        (alignLeft + (alignWidth - this.lineWidth)) *
                        this.ts.LastStyle.HorizontalAlignment *
                        fontInfo.Scale)
                    / fontInfo.Scale;
                break;
        }
    }
}
