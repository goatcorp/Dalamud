using System.Numerics;

namespace Dalamud.Interface.Spannables.Controls.Gestures.RectScaleMode;

/// <summary>A scale mode that scales the content region to fit the client region in one direction.</summary>
public class FitToBorderRectScaleMode : IRectScaleModeWithZoomInToFit
{
    private static readonly FitToBorderRectScaleMode HorizontalWithZoomInToFit = new(true, Direction.Horizontal);
    private static readonly FitToBorderRectScaleMode VerticalWithZoomInToFit = new(true, Direction.Vertical);
    private static readonly FitToBorderRectScaleMode HorizontalWithoutZoomInToFit = new(false, Direction.Horizontal);
    private static readonly FitToBorderRectScaleMode VerticalWithoutZoomInToFit = new(false, Direction.Vertical);

    private FitToBorderRectScaleMode(bool zoomInToFit, Direction directionToFit)
    {
        this.ZoomInToFit = zoomInToFit;
        this.DirectionToFit = directionToFit;
    }

    /// <summary>Specifies a fitting direction.</summary>
    public enum Direction
    {
        /// <summary>Fit the content region inside the client region horizontally.</summary>
        Horizontal,

        /// <summary>Fit the content region inside the client region vertically.</summary>
        Vertical,
    }

    /// <inheritdoc/>
    public bool ZoomInToFit { get; }

    /// <summary>Gets the fitting direction.</summary>
    public Direction DirectionToFit { get; }

    /// <summary>Gets the shared immutable instance of <see cref="FitToBorderRectScaleMode"/>.</summary>
    /// <param name="zoomInToFit">Whether to allow zooming in to fit in the client region.</param>
    /// <param name="directionToFit">The direction of the content region to fit to the client region.</param>
    /// <returns>A corresponding shared immutable instance of <see cref="FitToBorderRectScaleMode"/>.</returns>
    public static FitToBorderRectScaleMode GetInstance(bool zoomInToFit, Direction directionToFit) =>
        directionToFit switch
        {
            Direction.Horizontal when zoomInToFit => HorizontalWithZoomInToFit,
            Direction.Vertical when zoomInToFit => VerticalWithZoomInToFit,
            Direction.Horizontal => HorizontalWithoutZoomInToFit,
            Direction.Vertical => VerticalWithoutZoomInToFit,
            _ => throw new ArgumentOutOfRangeException(nameof(directionToFit), directionToFit, null),
        };

    /// <summary>Calculates the zoom for the content of size <paramref name="content"/>,
    /// when being presented in the client region of size <paramref name="client"/>.</summary>
    /// <param name="content">The content region size.</param>
    /// <param name="client">The client region size.</param>
    /// <param name="zoomInToFit">Whether to allow zooming in to fit.</param>
    /// <param name="direction">The direction of the content region to fit to the client region.</param>
    /// <returns>The calculated zoom value.</returns>
    public static float CalcZoomStatic(Vector2 content, Vector2 client, bool zoomInToFit, Direction direction) =>
        content == default || (!zoomInToFit && IRectScaleMode.ContentFitsIn(content, client))
            ? 1f
            : direction switch
            {
                Direction.Horizontal => (1f * client.X) / content.X,
                Direction.Vertical => (1f * client.Y) / content.Y,
                _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null),
            };

    /// <summary>Calculates the zoom exponent for the content of size <paramref name="content"/>,
    /// when being presented in the client region of size <paramref name="client"/>.</summary>
    /// <param name="content">The content region size.</param>
    /// <param name="client">The client region size.</param>
    /// <param name="zoomInToFit">Whether to allow zooming in to fit.</param>
    /// <param name="direction">The direction of the content region to fit to the client region.</param>
    /// <param name="exponentDivisor">The exponent divisor.</param>
    /// <returns>The calculated zoom value.</returns>
    public static float CalcZoomExponentStatic(
        Vector2 content,
        Vector2 client,
        bool zoomInToFit,
        Direction direction,
        float exponentDivisor) =>
        IRectScaleMode.ZoomToExponent(CalcZoomStatic(content, client, zoomInToFit, direction), exponentDivisor);

    /// <inheritdoc/>
    public float CalcZoom(Vector2 content, Vector2 client, float exponentDivisor) =>
        CalcZoomStatic(content, client, this.ZoomInToFit, this.DirectionToFit);

    /// <inheritdoc/>
    public float CalcZoomExponent(Vector2 content, Vector2 client, float exponentDivisor) =>
        CalcZoomExponentStatic(content, client, this.ZoomInToFit, this.DirectionToFit, exponentDivisor);
}
