using System.Numerics;
using System.Runtime.InteropServices;

namespace Dalamud.Bindings.ImGui;

/// <summary>Helper struct for converting ImGui-accepted colors to a <see langword="uint"/>.</summary>
[StructLayout(LayoutKind.Explicit)]
public readonly struct ImTextColor : IEquatable<ImTextColor>
{
    /// <summary>The packed color.</summary>
    [FieldOffset(0)]
    public readonly uint Value;

    /// <summary>The red component.</summary>
    [FieldOffset(0)]
    public readonly byte Red;

    /// <summary>The green component.</summary>
    [FieldOffset(1)]
    public readonly byte Green;

    /// <summary>The blue component.</summary>
    [FieldOffset(2)]
    public readonly byte Blue;

    /// <summary>The alpha component.</summary>
    [FieldOffset(3)]
    public readonly byte Alpha;

    /// <summary>Initializes a new instance of the <see cref="ImTextColor"/> struct.</summary>
    /// <param name="value">The packed color.</param>
    public ImTextColor(uint value) => this.Value = value;

    /// <summary>Initializes a new instance of the <see cref="ImTextColor"/> struct.</summary>
    /// <param name="value">Red, green, blue, and alpha, from 0 to 1.</param>
    public ImTextColor(Vector4 value)
    {
        this.Red = ToByte(value.X);
        this.Green = ToByte(value.Y);
        this.Blue = ToByte(value.Z);
        this.Alpha = ToByte(value.W);
    }

    public static implicit operator ImTextColor(uint value) => new(value);
    public static implicit operator ImTextColor(Vector4 value) => new(value);
    public static implicit operator uint(ImTextColor color) => color.Value;
    public static explicit operator Vector4(ImTextColor color) => color.Vector4;

    /// <summary>Gets red, green, blue, and alpha, from 0 to 1.</summary>
    public Vector4 Vector4 => new Vector4(this.Red, this.Green, this.Blue, this.Alpha) / byte.MaxValue;

    /// <inheritdoc/>
    public bool Equals(ImTextColor other) => this.Value == other.Value;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is ImTextColor other && this.Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => (int)this.Value;

    /// <inheritdoc/>
    /// <remarks>The digits are alpha, blue, green and red, in this order.</remarks>
    public override string ToString() => $"#{this.Value:X8}";

    // As IM_F32_TO_INT8_SAT.
    private static byte ToByte(float v) => (byte)((Math.Clamp(v, 0f, 1f) * byte.MaxValue) + 0.5f);
}
