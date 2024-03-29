using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Dalamud.Utility.Numerics;

/// <summary>A specialization of <see cref="System.Numerics.Vector4"/> that deals with four borders of a rectangle.</summary>
[StructLayout(LayoutKind.Explicit, Size = 16)]
[DebuggerDisplay("<{Left}, {Top}, {Right}, {Bottom}>")]
public struct BorderVector4 : IEquatable<BorderVector4>
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

    /// <summary>Initializes a new instance of the <see cref="BorderVector4"/> struct.</summary>
    /// <param name="value">The value to use for all fields.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BorderVector4(float value) => this.Left = this.Top = this.Right = this.Bottom = value;

    /// <summary>Initializes a new instance of the <see cref="BorderVector4"/> struct.</summary>
    /// <param name="xy">The value to use for horizontal and vertical fields, respectively.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BorderVector4(Vector2 xy) => this.LeftTop = this.RightBottom = xy;

    /// <summary>Initializes a new instance of the <see cref="BorderVector4"/> struct.</summary>
    /// <param name="x">The value to use for horizontal fields.</param>
    /// <param name="y">The value to use for vertical fields.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BorderVector4(float x, float y) => this.LeftTop = this.RightBottom = new(x, y);

    /// <summary>Initializes a new instance of the <see cref="BorderVector4"/> struct.</summary>
    /// <param name="left">The left value.</param>
    /// <param name="top">The top value.</param>
    /// <param name="right">The right value.</param>
    /// <param name="bottom">The bottom value.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BorderVector4(float left, float top, float right, float bottom) =>
        this.Vector4 = new(left, top, right, bottom);

    /// <summary>Initializes a new instance of the <see cref="BorderVector4"/> struct.</summary>
    /// <param name="leftTop">The left top coordinates.</param>
    /// <param name="rightBottom">The right bottom coordinates.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BorderVector4(Vector2 leftTop, Vector2 rightBottom) =>
        this.Vector4 = new(leftTop, rightBottom.X, rightBottom.Y);

    /// <summary>Initializes a new instance of the <see cref="BorderVector4"/> struct.</summary>
    /// <param name="vector4">A <see cref="Vector4"/> value to copy from.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BorderVector4(Vector4 vector4) => this.Vector4 = vector4;

    /// <summary>Gets an instance of <see cref="BorderVector4"/> containing zeroes.</summary>
    public static BorderVector4 Zero
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => default;
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

    /// <summary>Gets the sum of horizontal thickness of the borders.</summary>
    public readonly float Width
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.Right + this.Left;
    }

    /// <summary>Gets the sum of vertical thickness of the borders.</summary>
    public readonly float Height
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.Bottom + this.Top;
    }

    /// <summary>Gets the total thickness of the borders in both directions.</summary>
    public readonly Vector2 Size
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.RightBottom + this.LeftTop;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(in BorderVector4 left, in BorderVector4 right) => left.Equals(in right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(in BorderVector4 left, in BorderVector4 right) => !left.Equals(in right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BorderVector4 operator -(in BorderVector4 a) => new(-a.Vector4);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BorderVector4 operator +(in BorderVector4 left, in BorderVector4 right) =>
        new(left.Vector4 + right.Vector4);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BorderVector4 operator -(in BorderVector4 left, in BorderVector4 right) =>
        new(left.Vector4 - right.Vector4);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BorderVector4 operator *(in BorderVector4 left, in float right) =>
        new(left.Vector4 * right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BorderVector4 operator /(in BorderVector4 left, in float right) =>
        new(left.Vector4 / right);

    /// <summary>Rounds every components of <paramref name="rv"/>.</summary>
    /// <param name="rv">The rect vector.</param>
    /// <returns>The rounded rect vector.</returns>
    public static BorderVector4 Round(in BorderVector4 rv) =>
        new(MathF.Round(rv.Left), MathF.Round(rv.Top), MathF.Round(rv.Right), MathF.Round(rv.Bottom));

    /// <inheritdoc/>
    public override readonly string ToString() => $"<{this.Left:g}, {this.Top:g}, {this.Right:g}, {this.Bottom:g}>";

    /// <inheritdoc cref="object.Equals(object?)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(in BorderVector4 other) => this.Vector4.Equals(other.Vector4);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    readonly bool IEquatable<BorderVector4>.Equals(BorderVector4 other) => this.Vector4.Equals(other.Vector4);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override readonly bool Equals(object? obj) => obj is BorderVector4 other && this.Equals(in other);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override readonly int GetHashCode() => this.Vector4.GetHashCode();
}
