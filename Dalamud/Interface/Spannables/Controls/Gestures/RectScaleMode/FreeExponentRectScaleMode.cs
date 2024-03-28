using System.Numerics;

namespace Dalamud.Interface.Spannables.Controls.Gestures.RectScaleMode;

/// <summary>A scale mode that scales by a fixed arbitrary zoom exponent value.</summary>
public class FreeExponentRectScaleMode : IRectScaleMode
{
    /// <summary>Initializes a new instance of the <see cref="FreeExponentRectScaleMode"/> class.</summary>
    /// <param name="exponent">The initial zoom exponent value.</param>
    public FreeExponentRectScaleMode(float exponent) => this.ZoomExponent = exponent;

    /// <summary>Gets or sets the zoom exponent value.</summary>
    public float ZoomExponent { get; set; }

    /// <inheritdoc/>
    public float CalcZoom(Vector2 content, Vector2 client, float exponentDivisor) =>
        IRectScaleMode.ExponentToZoom(this.ZoomExponent, exponentDivisor);

    /// <inheritdoc/>
    public float CalcZoomExponent(Vector2 content, Vector2 client, float exponentDivisor) => this.ZoomExponent;
}
