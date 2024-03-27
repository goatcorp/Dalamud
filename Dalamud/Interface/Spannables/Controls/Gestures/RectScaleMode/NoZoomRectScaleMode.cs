using System.Numerics;

namespace Dalamud.Interface.Spannables.Controls.Gestures.RectScaleMode;

/// <summary>A scale mode where no zooming is allowed.</summary>
public class NoZoomRectScaleMode : IRectScaleMode
{
    /// <summary>The shared immutable instance of <see cref="NoZoomRectScaleMode"/>.</summary>
    public static readonly NoZoomRectScaleMode Instance = new();

    private NoZoomRectScaleMode()
    {
    }

    /// <inheritdoc/>
    public float CalcZoom(Vector2 content, Vector2 client, float exponentDivisor) => 1f;

    /// <inheritdoc/>
    public float CalcZoomExponent(Vector2 content, Vector2 client, float exponentDivisor) => 0f;
}
