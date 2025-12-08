namespace Dalamud.Bindings.ImAnim;

public enum ImAnimRotationMode
{
    /// <summary>
    /// Shortest path (default) - never rotates more than 180 degrees
    /// </summary>
    Shortest,

    /// <summary>
    /// Longest path - always takes the long way around
    /// </summary>
    Longest,

    /// <summary>
    /// Clockwise - always rotates clockwise (positive direction)
    /// </summary>
    Cw,

    /// <summary>
    /// Counter-clockwise - always rotates counter-clockwise
    /// </summary>
    Ccw,

    /// <summary>
    /// Direct lerp - no angle unwrapping, can cause spinning for large deltas
    /// </summary>
    Direct,
}
