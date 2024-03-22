using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Dalamud.Utility.Numerics;

/// <summary>A specialization of <see cref="System.Numerics.Vector4"/> that deals with four boundaries of a rectangle.</summary>
[StructLayout(LayoutKind.Explicit, Size = 16)]
public struct RectVector4 : IEquatable<RectVector4>
{
    /// <summary>The <see cref="System.Numerics.Vector4"/> view.</summary>
    [FieldOffset(0)]
    public Vector4 Vector4;

    /// <summary>The <see cref="Vector2"/> view of left and top.</summary>
    [FieldOffset(0)]
    public Vector2 LeftTop;

    /// <summary>The <see cref="Vector2"/> view of right and bottom.</summary>
    [FieldOffset(8)]
    public Vector2 RightBottom;

    /// <summary>The left offset.</summary>
    [FieldOffset(0)]
    public float Left;

    /// <summary>The top offset.</summary>
    [FieldOffset(4)]
    public float Top;

    /// <summary>The right offset.</summary>
    [FieldOffset(8)]
    public float Right;

    /// <summary>The bottom offset.</summary>
    [FieldOffset(12)]
    public float Bottom;

    /// <summary>Initializes a new instance of the <see cref="RectVector4"/> struct.</summary>
    /// <param name="value">The value to use for all fields.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RectVector4(float value) => this.Left = this.Top = this.Right = this.Bottom = value;

    /// <summary>Initializes a new instance of the <see cref="RectVector4"/> struct.</summary>
    /// <param name="xy">The value to use for horizontal and vertical fields, respectively.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RectVector4(Vector2 xy) => this.LeftTop = this.RightBottom = xy;

    /// <summary>Initializes a new instance of the <see cref="RectVector4"/> struct.</summary>
    /// <param name="x">The value to use for horizontal fields.</param>
    /// <param name="y">The value to use for vertical fields.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RectVector4(float x, float y) => this.LeftTop = this.RightBottom = new(x, y);

    /// <summary>Initializes a new instance of the <see cref="RectVector4"/> struct.</summary>
    /// <param name="left">The left value.</param>
    /// <param name="top">The top value.</param>
    /// <param name="right">The right value.</param>
    /// <param name="bottom">The bottom value.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RectVector4(float left, float top, float right, float bottom) =>
        this.Vector4 = new(left, top, right, bottom);

    /// <summary>Initializes a new instance of the <see cref="RectVector4"/> struct.</summary>
    /// <param name="leftTop">The left top coordinates.</param>
    /// <param name="rightBottom">The right bottom coordinates.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RectVector4(Vector2 leftTop, Vector2 rightBottom) =>
        this.Vector4 = new(leftTop, rightBottom.X, rightBottom.Y);

    /// <summary>Initializes a new instance of the <see cref="RectVector4"/> struct.</summary>
    /// <param name="vector4">A <see cref="Vector4"/> value to copy from.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RectVector4(Vector4 vector4) => this.Vector4 = vector4;

    /// <summary>Gets an instance of <see cref="RectVector4"/> containing zeroes.</summary>
    public static RectVector4 Zero
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => default;
    }

    /// <summary>Gets an instance of <see cref="RectVector4"/> containing the inverted extrema, so that extending with
    /// any other valid instance of <see cref="RectVector4"/> can work reliably.</summary>
    public static RectVector4 InvertedExtrema
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(new Vector2(float.MaxValue), new(float.MinValue));
    }

    /// <summary>Gets or sets the left bottom coordinates.</summary>
    public Vector2 LeftBottom
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => new(this.Left, this.Bottom);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            this.Left = value.X;
            this.Bottom = value.Y;
        }
    }

    /// <summary>Gets or sets the right top coordinates.</summary>
    public Vector2 RightTop
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => new(this.Right, this.Top);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            this.Right = value.X;
            this.Top = value.Y;
        }
    }

    /// <summary>Gets a value indicating whether this vector is valid.</summary>
    /// <remarks>It is valid if Left &lt;= Right &amp;&amp; Top &lt; Bottom.</remarks>
    public readonly bool IsValid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.Left <= this.Right && this.Top <= this.Bottom;
    }

    /// <summary>Gets the width, if <see cref="IsValid"/> is <c>true</c>.</summary>
    public readonly float Width
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.IsValid ? this.Right - this.Left : 0f;
    }

    /// <summary>Gets the height, if <see cref="IsValid"/> is <c>true</c>.</summary>
    public readonly float Height
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.IsValid ? this.Bottom - this.Top : 0f;
    }

    /// <summary>Gets the size, if <see cref="IsValid"/> is <c>true</c>.</summary>
    public readonly Vector2 Size
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.IsValid ? this.RightBottom - this.LeftTop : Vector2.Zero;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(in RectVector4 left, in RectVector4 right) => left.Equals(in right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(in RectVector4 left, in RectVector4 right) => !left.Equals(in right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RectVector4 operator -(in RectVector4 a) => new(-a.Vector4);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RectVector4 operator +(in RectVector4 left, in RectVector4 right) =>
        new(left.Vector4 + right.Vector4);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RectVector4 operator -(in RectVector4 left, in RectVector4 right) =>
        new(left.Vector4 - right.Vector4);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RectVector4 operator *(in RectVector4 left, in RectVector4 right) =>
        new(left.Vector4 * right.Vector4);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RectVector4 operator /(in RectVector4 left, in RectVector4 right) =>
        new(left.Vector4 / right.Vector4);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RectVector4 operator *(in RectVector4 left, in float right) =>
        new(left.Vector4 * right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RectVector4 operator /(in RectVector4 left, in float right) =>
        new(left.Vector4 / right);

    /// <summary>Extrudes <paramref name="what"/> by <paramref name="by"/>, by subtracting <see cref="LeftTop"/>
    /// and adding <see cref="RightBottom"/>.</summary>
    /// <param name="what">The rect vector to extrude from.</param>
    /// <param name="by">The extrusion distance.</param>
    /// <returns>The extruded rect vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RectVector4 Extrude(in RectVector4 what, in RectVector4 by) =>
        new(what.LeftTop - by.LeftTop, what.RightBottom + by.RightBottom);

    /// <summary>Creates a new instance of <see cref="RectVector4"/> from a vector containing the coordinates at
    /// left top and a vector containing the size of rect.</summary>
    /// <param name="coordinates">The coordinates at left top.</param>
    /// <param name="size">The size.</param>
    /// <returns>The new instance of <see cref="RectVector4"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RectVector4 FromCoordAndSize(in Vector2 coordinates, in Vector2 size) =>
        new(coordinates, coordinates + size);

    /// <summary>Normalizes this rect vector so that left &lt;= right &amp;&amp; top &lt;= bottom.</summary>
    /// <param name="rv">The rect vector.</param>
    /// <returns>The normalized rect vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RectVector4 Normalize(in RectVector4 rv)
    {
        var res = rv;
        if (res.Left > res.Right)
            res.Left = res.Right = MathF.Round((res.Left + res.Right) / 2);
        if (res.Top > res.Bottom)
            res.Top = res.Bottom = MathF.Round((res.Top + res.Bottom) / 2);
        return res;
    }

    /// <summary>Translates <paramref name="what"/> by <paramref name="by"/>.</summary>
    /// <param name="what">The rect vector to translate.</param>
    /// <param name="by">The translation distance.</param>
    /// <returns>The translated rect vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RectVector4 Translate(in RectVector4 what, in Vector2 by) =>
        new(what.LeftTop + by, what.RightBottom + by);

    /// <summary>Unions <paramref name="a"/> with <paramref name="b"/>.</summary>
    /// <param name="a">The first rect vector.</param>
    /// <param name="b">The second rect vector.</param>
    /// <returns>The unioned rect vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RectVector4 Union(in RectVector4 a, in RectVector4 b) =>
        new(
            Vector2.Min(a.LeftTop, b.LeftTop),
            Vector2.Max(a.RightBottom, b.RightBottom));

    /// <summary>Determines if the coordinates is contained within this rect vector.</summary>
    /// <param name="coord">The coordiantes to test.</param>
    /// <returns><c>true</c> if it is contained within.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Contains(Vector2 coord) =>
        this.Left <= coord.X && this.Top <= coord.Y && coord.X < this.Right && coord.Y < this.Bottom;

    /// <inheritdoc/>
    public override readonly string ToString() =>
        $"{nameof(RectVector4)}<({this.Left:g}, {this.Top:g})-({this.Right:g}, {this.Bottom:g})>";

    /// <inheritdoc cref="object.Equals(object?)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(in RectVector4 other) => this.Vector4.Equals(other.Vector4);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    readonly bool IEquatable<RectVector4>.Equals(RectVector4 other) => this.Vector4.Equals(other.Vector4);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override readonly bool Equals(object? obj) => obj is RectVector4 other && this.Equals(in other);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override readonly int GetHashCode() => this.Vector4.GetHashCode();
}
