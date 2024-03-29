using System.Numerics;

namespace Dalamud.Interface.Spannables.Controls.Gestures.RectScaleMode;

/// <summary>A scale mode that scales the content region to fit the client region.</summary>
public class FitInClientRectScaleMode : IRectScaleModeWithZoomInToFit
{
    private static readonly FitInClientRectScaleMode WithZoomInToFit = new(true);
    private static readonly FitInClientRectScaleMode WithoutZoomInToFit = new(false);

    private FitInClientRectScaleMode(bool zoomInToFit) => this.ZoomInToFit = zoomInToFit;

    /// <inheritdoc/>
    public bool ZoomInToFit { get; }

    /// <summary>Gets the shared immutable instance of <see cref="FitInClientRectScaleMode"/>.</summary>
    /// <param name="zoomInToFit">Whether to allow zooming in to fit in the client region.</param>
    /// <returns>A corresponding shared immutable instance of <see cref="FitInClientRectScaleMode"/>.</returns>
    public static FitInClientRectScaleMode GetInstance(bool zoomInToFit) =>
        zoomInToFit ? WithZoomInToFit : WithoutZoomInToFit;

    /// <summary>Calculates the zoom for the content of size <paramref name="content"/>,
    /// when being presented in the client region of size <paramref name="client"/>.</summary>
    /// <param name="content">The content region size.</param>
    /// <param name="client">The client region size.</param>
    /// <param name="zoomInToFit">Whether to allow zooming in to fit.</param>
    /// <returns>The calculated zoom value.</returns>
    public static float CalcZoomStatic(Vector2 content, Vector2 client, bool zoomInToFit) =>
        content == default || (!zoomInToFit && IRectScaleMode.ContentFitsIn(content, client))
            ? 1f
            : client.X * content.Y > content.X * client.Y
                ? (1f * client.Y) / content.Y
                : (1f * client.X) / content.X;

    /// <summary>Calculates the zoom exponent for the content of size <paramref name="content"/>,
    /// when being presented in the client region of size <paramref name="client"/>.</summary>
    /// <param name="content">The content region size.</param>
    /// <param name="client">The client region size.</param>
    /// <param name="zoomInToFit">Whether to allow zooming in to fit.</param>
    /// <param name="exponentDivisor">The exponent divisor.</param>
    /// <returns>The calculated zoom exponent value.</returns>
    public static float CalcZoomExponentStatic(
        Vector2 content, Vector2 client, bool zoomInToFit, float exponentDivisor) =>
        IRectScaleMode.ZoomToExponent(CalcZoomStatic(content, client, zoomInToFit), exponentDivisor);

    /// <inheritdoc/>
    public float CalcZoom(Vector2 content, Vector2 client, float exponentDivisor) =>
        CalcZoomStatic(content, client, this.ZoomInToFit);

    /// <inheritdoc/>
    public float CalcZoomExponent(Vector2 content, Vector2 client, float exponentDivisor) =>
        CalcZoomExponentStatic(content, client, this.ZoomInToFit, exponentDivisor);
}
