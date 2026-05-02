using System.Numerics;

namespace Dalamud.Interface.Windowing;

/// <summary>
/// Structure detailing the size constraints of a window.
/// </summary>
public struct WindowSizeConstraints
{
    private Vector2 internalMaxSize = new(float.MaxValue);

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowSizeConstraints"/> struct.
    /// </summary>
    public WindowSizeConstraints()
    {
    }

    /// <summary>
    /// Gets or sets the minimum size of the window.
    /// </summary>
    public Vector2 MinimumSize { get; set; } = new(0);

    /// <summary>
    /// Gets or sets the maximum size of the window.
    /// </summary>
    public Vector2 MaximumSize
    {
        get => this.GetSafeMaxSize();
        set => this.internalMaxSize = value;
    }

    private Vector2 GetSafeMaxSize()
    {
        // TODO Rework for interface
        // Important: There needs to be a valid MaximumSize or 0x0 windows can occur.

        var currentMin = this.MinimumSize;

        if (this.internalMaxSize.X < currentMin.X || this.internalMaxSize.Y < currentMin.Y)
            return new Vector2(float.MaxValue);

        return this.internalMaxSize;
    }
}
