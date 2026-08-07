using System.Numerics;

namespace Dalamud.NativeUi.Classes;

/// <summary>
/// Basic Size/Positioning helper for AtkResNodes.
/// </summary>
internal class Bounds
{
    /// <summary>
    /// Gets or sets the top left coordinate of these bounds.
    /// </summary>
    public required Vector2 TopLeft { get; set; }

    /// <summary>
    /// Gets or sets the bottom right coordinate of these bounds.
    /// </summary>
    public required Vector2 BottomRight { get; set; }

    /// <summary>
    /// Gets the Y position of the top edge.
    /// </summary>
    public float Top => this.TopLeft.Y;

    /// <summary>
    /// Gets the X position of the left edge.
    /// </summary>
    public float Left => this.TopLeft.X;

    /// <summary>
    /// Gets the Y position of the bottom edge.
    /// </summary>
    public float Bottom => this.BottomRight.Y;

    /// <summary>
    /// Gets the X position of the right edge.
    /// </summary>
    public float Right => this.BottomRight.X;

    /// <summary>
    /// Gets the width of these bounds.
    /// </summary>
    public float Width => Math.Abs(this.BottomRight.X - this.TopLeft.X);

    /// <summary>
    /// Gets the height of these bounds.
    /// </summary>
    public float Height => Math.Abs(this.BottomRight.Y - this.TopLeft.Y);

    /// <summary>
    /// Gets the total size of these bounds.
    /// </summary>
    public Vector2 Size => new(this.Width, this.Height);

    /// <summary>
    /// Gets the centerpoint horizontally of these bounds.
    /// </summary>
    public float CenterX => (this.TopLeft.X + this.BottomRight.X) / 2.0f;

    /// <summary>
    /// Gets the centerpoint vertically of these bounds.
    /// </summary>
    public float CenterY => (this.TopLeft.Y + this.BottomRight.Y) / 2.0f;

    /// <summary>
    /// Gets the centerpoint of these bounds.
    /// </summary>
    public Vector2 Center => new(this.CenterX, this.CenterY);

    /// <summary>
    /// Prints the bounds in a somewhat friendly manner.
    /// </summary>
    /// <returns>A human-readable printout of the nodes top-left and bottom-right coordinates.</returns>
    public override string ToString() => $"{this.TopLeft}, {this.BottomRight}";
}
