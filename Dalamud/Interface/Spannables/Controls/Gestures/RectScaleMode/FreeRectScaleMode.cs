using System.Numerics;

namespace Dalamud.Interface.Spannables.Controls.Gestures.RectScaleMode;

/// <summary>A scale mode that scales by a fixed arbitrary zoom value.</summary>
public class FreeRectScaleMode : IRectScaleMode
{
    /// <summary>Initializes a new instance of the <see cref="FreeRectScaleMode"/> class.</summary>
    /// <param name="zoom">The initial zoom value.</param>
    public FreeRectScaleMode(float zoom) => this.Zoom = zoom;

    /// <summary>Gets or sets the zoom value.</summary>
    public float Zoom { get; set; }

    /// <inheritdoc/>
    public float CalcZoom(Vector2 content, Vector2 client, float exponentDivisor) => this.Zoom;

    /// <inheritdoc/>
    public float CalcZoomExponent(Vector2 content, Vector2 client, float exponentDivisor) =>
        IRectScaleMode.ZoomToExponent(this.Zoom, exponentDivisor);
}
