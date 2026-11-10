using System.Numerics;

using Dalamud.Bindings.ImGui;

namespace Dalamud.Interface.Utility;

/// <inheritdoc cref="ImRect"/>
/// <remarks> C# version using <seealso cref="Vector2"/> instead of <seealso cref="ImGui.ImVec2()"/>. </remarks>
public readonly record struct Rectangle(Vector2 Minimum, Vector2 Maximum)
{
    /// <summary> A rectangle that is only the point Zero. </summary>
    public static readonly Rectangle Zero = new(Vector2.Zero, Vector2.Zero);

    /// <inheritdoc cref="ImRect"/>
    public Rectangle(float xMinimum, float yMinimum, float xMaximum, float yMaximum)
        : this(new Vector2(xMinimum, yMinimum), new Vector2(xMaximum, yMaximum))
    {
    }

    /// <inheritdoc cref="ImRect"/>
    public Vector2 Size => new(this.Width, this.Height);

    /// <summary> Gets the width of this rectangle. </summary>
    public float Width => this.Maximum.X - this.Minimum.X;

    /// <summary> Gets the height of this rectangle. </summary>
    public float Height => this.Maximum.Y - this.Minimum.Y;

    /// <summary> Gets the center of this rectangle. </summary>
    public Vector2 Center => (this.Maximum + this.Minimum) / 2;

    /// <summary> Gets a value indicating whether this rectangle is not correctly <see cref="Orient"/>ed. </summary>
    public bool IsInverted => this.Minimum.X > this.Maximum.X || this.Minimum.Y > this.Maximum.Y;

    /// <summary> Gets one of the corners not stored. If this rectangle is oriented (see <see cref="Orient"/>), this is at the bottom right. </summary>
    public Vector2 BottomRight
        => new(this.Maximum.X, this.Minimum.Y);

    /// <summary> Gets one of the corners not stored. If this rectangle is oriented (see <see cref="Orient"/>), this is at the top left. </summary>
    public Vector2 TopLeft
        => new(this.Minimum.X, this.Maximum.Y);

    public static implicit operator Rectangle(ImRect rect)
        => new(rect.Min, rect.Max);

    public static implicit operator ImRect(Rectangle rect)
        => new(rect.Minimum, rect.Maximum);

    /// <inheritdoc cref="ImRect"/>
    public static Rectangle FromSize(Vector2 minimum, Vector2 size)
        => new(minimum, minimum + size);

    /// <inheritdoc cref="ImRect"/>
    public static Rectangle FromSize(Vector2 size)
        => new(Vector2.Zero, size);

    /// <inheritdoc cref="ImRect"/>
    public static Rectangle FromSize(float sizeX, float sizeY)
        => new(Vector2.Zero, new Vector2(sizeX, sizeY));

    /// <summary> Whether this rectangle contains the given point. </summary>
    /// <param name="point"> The point to check. </param>
    /// <returns> Whether the point is contained in the rectangle. </returns>
    public bool Contains(Vector2 point)
        => point.X >= this.Minimum.X && point.X <= this.Maximum.X && point.Y >= this.Minimum.Y && point.Y <= this.Maximum.Y;

    /// <summary> Add a point to a rectangle, i.e. ensure that the point is within the rectangle. </summary>
    /// <param name="point"> The point to add. </param>
    /// <returns> The minimum rectangle containing the added point and the entire prior rectangle. </returns>
    public Rectangle Add(Vector2 point)
        => new(MathF.Min(point.X, this.Minimum.X), MathF.Min(point.Y, this.Minimum.Y), MathF.Max(point.X, this.Maximum.X), MathF.Max(point.Y, this.Maximum.Y));

    /// <summary> Expand the rectangle in all directions. </summary>
    /// <param name="amount"> The amount to expand the rectangle by. </param>
    /// <returns> The expanded rectangle. </returns>
    public Rectangle Expand(float amount)
        => new(this.Minimum.X - amount, this.Minimum.Y - amount, this.Maximum.X + amount, this.Maximum.Y + amount);

    /// <summary> Expand the rectangle in all directions. </summary>
    /// <param name="amount"> The amount to expand the rectangle by in X and Y direction. </param>
    /// <returns> The expanded rectangle. </returns>
    public Rectangle Expand(Vector2 amount)
        => new(this.Minimum.X - amount.X, this.Minimum.Y - amount.Y, this.Maximum.X + amount.X, this.Maximum.Y + amount.Y);

    /// <summary> Check whether this rectangle overlaps the given rectangle. </summary>
    /// <param name="rect"> The other rectangle to check. </param>
    /// <returns> Whether a real overlap with measure greater than 0 exists between the two rectangles. </returns>
    public bool Overlaps(in Rectangle rect)
        => rect.Minimum.Y < this.Maximum.Y && rect.Maximum.Y > this.Minimum.Y && rect.Minimum.X < this.Maximum.X && rect.Maximum.X > this.Minimum.X;

    /// <summary> Check whether this rectangle overlaps the given line on at least one point. </summary>
    /// <param name="lineStart"> One end of the line. </param>
    /// <param name="lineEnd"> The other end of the line. </param>
    /// <returns> Whether at least one point lies both in the rectangle and on the line. </returns>
    public bool OverlapsLine(Vector2 lineStart, Vector2 lineEnd)
    {
        if (this.Contains(lineStart) || this.Contains(lineEnd))
            return true;

        var oriented = this.Orient();
        // Check the full line being on any side of the rectangle.
        if ((lineStart.X < oriented.Minimum.X && lineEnd.X < oriented.Minimum.X)
         || (lineStart.X > oriented.Maximum.X && lineEnd.X > oriented.Maximum.X)
         || (lineStart.Y < oriented.Minimum.Y && lineEnd.Y < oriented.Minimum.Y)
         || (lineStart.Y > oriented.Maximum.Y && lineEnd.Y > oriented.Maximum.Y))
            return false;

        // Evaluate the corners of the rectangle against the line.
        var lineEvaluator = new LineEvaluator(lineStart, lineEnd);
        var (sign0, sign1, sign2, sign3) = (Math.Sign(lineEvaluator.Evaluate(oriented.Minimum)),
            Math.Sign(lineEvaluator.Evaluate(oriented.TopLeft)),
            Math.Sign(lineEvaluator.Evaluate(oriented.BottomRight)),
            Math.Sign(lineEvaluator.Evaluate(oriented.Maximum)));
        return Math.Abs(sign0 + sign1 + sign2 + sign3) != Math.Abs(sign0) + Math.Abs(sign1) + Math.Abs(sign2) + Math.Abs(sign3);
    }

    /// <summary> Orient this rectangle, so that <see cref="Minimum"/> is the lower left corner and <see cref="Maximum"/> is the upper right corner. </summary>
    /// <returns>An oriented rectangle.</returns>
    public Rectangle Orient()
    {
        var (minX, maxX) = this.Minimum.X > this.Maximum.X ? (this.Maximum.X, this.Minimum.X) : (this.Minimum.X, this.Maximum.X);
        var (minY, maxY) = this.Minimum.Y > this.Maximum.Y ? (this.Maximum.Y, this.Minimum.Y) : (this.Minimum.Y, this.Maximum.Y);
        return new Rectangle(minX, minY, maxX, maxY);
    }
}
