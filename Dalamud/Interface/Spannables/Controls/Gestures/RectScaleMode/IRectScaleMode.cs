using System.Numerics;
using System.Runtime.CompilerServices;

namespace Dalamud.Interface.Spannables.Controls.Gestures.RectScaleMode;

/// <summary>Formula for scaling a content to be shown in a client area.</summary>
public interface IRectScaleMode
{
    /// <summary>Calculates the zoom for the content of size <paramref name="content"/>,
    /// when being presented in the client region of size <paramref name="client"/>.</summary>
    /// <param name="content">The content region size.</param>
    /// <param name="client">The client region size.</param>
    /// <param name="exponentDivisor">The exponent divisor.</param>
    /// <returns>The calculated zoom value.</returns>
    float CalcZoom(Vector2 content, Vector2 client, float exponentDivisor);

    /// <summary>Calculates the zoom exponent for the content of size <paramref name="content"/>,
    /// when being presented in the client region of size <paramref name="client"/>.</summary>
    /// <param name="content">The content region size.</param>
    /// <param name="client">The client region size.</param>
    /// <param name="exponentDivisor">The exponent divisor.</param>
    /// <returns>The calculated zoom exponent value.</returns>
    float CalcZoomExponent(Vector2 content, Vector2 client, float exponentDivisor);

    /// <summary>Calculates the content size after having the results from <see cref="CalcZoom"/> applied.</summary>
    /// <param name="content">The content region size.</param>
    /// <param name="client">The client region size.</param>
    /// <param name="exponentDivisor">The exponent divisor.</param>
    /// <returns>The calculated scaled content region size.</returns>
    Vector2 CalcSize(Vector2 content, Vector2 client, float exponentDivisor)
    {
        var zoom = this.CalcZoom(content, client, exponentDivisor);
        return new(content.X * zoom, content.Y * zoom);
    }

    /// <summary>Determines whether a content region completely fits into a client region without scaling.</summary>
    /// <param name="content">The content region size.</param>
    /// <param name="client">The client region size.</param>
    /// <returns><c>true</c> if it fits.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ContentFitsIn(Vector2 content, Vector2 client) =>
        content.X <= client.X && content.Y <= client.Y;

    /// <summary>Calculates a exponent dividend value from a raw zoom scale and an exponent divisor.</summary>
    /// <param name="zoom">Raw zoom scale value.</param>
    /// <param name="exponentDivisor">The exponent divisor.</param>
    /// <returns>The calculated zoom exponent.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float ZoomToExponent(float zoom, float exponentDivisor) => MathF.Log2(zoom) * exponentDivisor;

    /// <summary>Calculates a raw zoom scale value from a exponent divident and divisor values.</summary>
    /// <param name="exponent">Zoom exponent value.</param>
    /// <param name="exponentDivisor">The exponent divisor.</param>
    /// <returns>The calculated raw zoom scale value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float ExponentToZoom(float exponent, float exponentDivisor) => MathF.Pow(2, exponent / exponentDivisor);
}
