using System.Numerics;

namespace Dalamud.Interface.Windowing;

/// <summary>
/// Structure detailing the size constraints of a window.
/// </summary>
public struct WindowSizeConstraints
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WindowSizeConstraints"/> struct.
    /// </summary>
    public WindowSizeConstraints()
    {
    }

    /// <summary>
    /// Gets or sets the minimum size of the window.
    /// </summary>
    public Vector2 MinimumSize { get; set; }

    /// <summary>
    /// Gets or sets the maximum size of the window.
    /// </summary>
    public Vector2 MaximumSize { get; set; }
}
