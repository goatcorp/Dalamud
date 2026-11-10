using System.Numerics;

namespace Dalamud.Interface.Utility;

/// <summary> A utility to evaluate an implicit line at one or multiple points. </summary>
/// <param name="lineStart"> One end of the line. </param>
/// <param name="lineEnd"> The other end of the line. </param>
public readonly struct LineEvaluator(Vector2 lineStart, Vector2 lineEnd)
{
    /// <summary> The factors to multiply a points coordinates with. </summary>
    public readonly Vector2 Factors = new(lineEnd.Y - lineStart.Y, lineStart.X - lineEnd.X);

    /// <summary> The linear offset to add. </summary>
    public readonly float Offset = (lineEnd.X * lineStart.Y) + (lineStart.X * lineEnd.Y);

    /// <summary> Obtain the point closest to <paramref name="point"/> on a line. </summary>
    /// <param name="point"> The observed point. </param>
    /// <param name="lineStart"> One end of the line. </param>
    /// <param name="lineEnd"> The other end of the line. </param>
    /// <returns> The closest point that lies on the line. </returns>
    public static Vector2 ClosestPointOnLine(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
    {
        var startDist = point - lineStart;
        var direction = lineEnd - lineStart;
        var dotProduct = Vector2.Dot(startDist, direction);
        if (dotProduct < 0)
            return lineStart;

        var directionLength = direction.LengthSquared();
        if (dotProduct > directionLength)
            return lineEnd;

        return lineStart + direction * dotProduct / directionLength;
    }

    /// <summary> Evaluate the implicit line at the given point. </summary>
    /// <param name="point"> The point to evaluate. </param>
    /// <returns> The evaluation. </returns>
    public float Evaluate(Vector2 point)
        => Vector2.Dot(this.Factors, point) + this.Offset;
}
