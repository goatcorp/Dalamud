using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Dalamud.Utility;

/// <summary>Represents a 32-bit RGBA color.</summary>
[StructLayout(LayoutKind.Explicit, Size = 4)]
[DebuggerDisplay("#{Rgba,h} ({R}, {G}, {B} / {A})")]
public struct Rgba32 : ISpanParsable<Rgba32>, ISpanFormattable
{
    /// <summary>The RGBA value.</summary>
    [FieldOffset(0)]
    public uint Rgba;

    /// <summary>Red byte value.</summary>
    [FieldOffset(0)]
    public byte R;

    /// <summary>Green byte value.</summary>
    [FieldOffset(1)]
    public byte G;

    /// <summary>Blue byte value.</summary>
    [FieldOffset(2)]
    public byte B;

    /// <summary>Opacity byte value.</summary>
    [FieldOffset(3)]
    public byte A;

    /// <summary>Initializes a new instance of the <see cref="Rgba32"/> struct.</summary>
    /// <param name="rgba">The RGBA color value.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Rgba32(uint rgba) => this.Rgba = rgba;

    /// <summary>Initializes a new instance of the <see cref="Rgba32"/> struct.</summary>
    /// <param name="r">The red value.</param>
    /// <param name="g">The green value.</param>
    /// <param name="b">The blue value.</param>
    /// <param name="a">The opacity value.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Rgba32(byte r, byte g, byte b, byte a = 255) => (this.R, this.G, this.B, this.A) = (r, g, b, a);

    /// <summary>Initializes a new instance of the <see cref="Rgba32"/> struct.</summary>
    /// <param name="color">The RGBA color value.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Rgba32(Vector4 color)
        : this(
            (byte)Math.Clamp(color.X * 256, 0, 255),
            (byte)Math.Clamp(color.Y * 256, 0, 255),
            (byte)Math.Clamp(color.Z * 256, 0, 255),
            (byte)Math.Clamp(color.W * 256, 0, 255))
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Rgba32(uint color) => new(color);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Rgba32(Vector4 color) => new(color);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator uint(Rgba32 color) => color.Rgba;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Vector4(Rgba32 color) => color.AsVector4();

    /// <summary>Creates an RGBA value from a BGRA value.</summary>
    /// <param name="value">The BGRA color value.</param>
    /// <returns>The RGBA color.</returns>
    public static Rgba32 FromBgra(uint value)
    {
        var t = new Rgba32(value);
        return new(t.B, t.G, t.R, t.A);
    }

    /// <inheritdoc cref="IParsable{TSelf}.Parse(string, IFormatProvider?)"/>
    public static Rgba32 Parse(string s, IFormatProvider? provider) =>
        TryParseCore(s, provider, out var res) is { } err ? throw new FormatException(err) : res;

    /// <inheritdoc cref="IParsable{TSelf}.TryParse(string?, IFormatProvider?, out TSelf)"/>
    public static bool TryParse(string? s, IFormatProvider? provider, out Rgba32 result)
    {
        if (s is null)
        {
            result = default;
            return false;
        }

        return TryParseCore(s, provider, out result) is null;
    }

    /// <inheritdoc cref="ISpanParsable{TSelf}.Parse(ReadOnlySpan{char}, IFormatProvider?)"/>
    public static Rgba32 Parse(ReadOnlySpan<char> s, IFormatProvider? provider) =>
        TryParseCore(s, provider, out var res) is { } err ? throw new FormatException(err) : res;

    /// <inheritdoc cref="ISpanParsable{TSelf}.TryParse(ReadOnlySpan{char}, IFormatProvider?, out TSelf)"/>
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Rgba32 result) =>
        TryParseCore(s, provider, out result) is null;

    /// <summary>Gets the equivalent <see cref="Vector4"/> color value.</summary>
    /// <returns>A new <see cref="Vector4"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Vector4 AsVector4() => new Vector4(this.R, this.G, this.B, this.A) / byte.MaxValue;

    /// <summary>Multiplies elements by the given value.</summary>
    /// <param name="by">The mulitiplication factor.</param>
    /// <returns>The multiplied value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Rgba32 Multiply(float by) => this.Multiply(new Vector4(by));

    /// <inheritdoc cref="Multiply(float)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Rgba32 Multiply(Vector4 by) => this.AsVector4() * by;

    /// <summary>Multiplies opacity by the given value.</summary>
    /// <param name="by">The mulitiplication factor.</param>
    /// <returns>The multiplied value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Rgba32 MultiplyOpacity(float by) => this.Multiply(new Vector4(1, 1, 1, by));

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override readonly string ToString() => $"#{this.Rgba:X08}";

    /// <inheritdoc/>
    public readonly string ToString(string? format, IFormatProvider? formatProvider) => $"#{this.Rgba:X08}";

    /// <inheritdoc/>
    public readonly bool TryFormat(
        Span<char> destination,
        out int charsWritten,
        ReadOnlySpan<char> format,
        IFormatProvider? provider)
    {
        if (destination.Length < 9)
        {
            charsWritten = 0;
            return false;
        }

        charsWritten = 9;
        $"#{this.Rgba:X08}".AsSpan().CopyTo(destination);
        return true;
    }

    private static string? TryParseCore(ReadOnlySpan<char> s, IFormatProvider? provider, out Rgba32 result)
    {
        const string whitespaces = " \t\r\n";
        uint u32;
        result = default;
        s = s.Trim();

        // https://developer.mozilla.org/en-US/docs/Web/CSS/color_value/rgb
        if (s.StartsWith("rgb", StringComparison.InvariantCultureIgnoreCase))
        {
            ReadOnlySpan<char> rspan, gspan, bspan, aspan;

            s = s[3..];
            if (s.StartsWith("a", StringComparison.InvariantCultureIgnoreCase))
                s = s[1..];

            s = s.TrimStart();
            if (!s.StartsWith("(") || !s.EndsWith(")"))
                return "rgba requires parenthesis.";
            s = s[1..^1].Trim();

            var sep = s.IndexOf(',');
            if (sep != -1)
            {
                // Old CSS notation; uses comma as the only type of separator
                // ^\s*rgba?\s*\(\s*VALUE\s*,\s*VALUE\s*,\s*VALUE\s*(,\s*VALUE\s*)?\)\s*$
                rspan = s[..sep];
                s = s[(sep + 1)..];

                sep = s.IndexOf(',');
                if (sep == -1)
                    return "not enough commas.";
                gspan = s[..sep];
                s = s[(sep + 1)..];

                sep = s.IndexOf(',');
                if (sep == -1)
                {
                    bspan = s;
                    aspan = "255";
                }
                else
                {
                    bspan = s[..sep];
                    aspan = s[(sep + 1)..];
                }
            }
            else
            {
                sep = s.IndexOfAny(whitespaces);
                if (sep != -1)
                {
                    // New notation; R _ G _ B / A
                    rspan = s[..sep];
                    s = s[(sep + 1)..].TrimStart();

                    sep = s.IndexOfAny(whitespaces);
                    if (sep == -1)
                        return "not enough items.";
                    gspan = s[..sep];
                    s = s[(sep + 1)..].TrimStart();

                    sep = s.IndexOf('/');
                    if (sep == -1)
                    {
                        bspan = s;
                        aspan = "255";
                    }
                    else
                    {
                        bspan = s[..sep];
                        aspan = s[(sep + 1)..];
                    }
                }
                else
                {
                    return "unsupported format.";
                }
            }

            if (TryParseValue(rspan, provider, out result.R) is { } rerr)
                return $"red: {rerr}";
            if (TryParseValue(gspan, provider, out result.G) is { } gerr)
                return $"green: {gerr}";
            if (TryParseValue(bspan, provider, out result.B) is { } berr)
                return $"blue: {berr}";
            if (TryParseValue(aspan, provider, out result.A) is { } aerr)
                return $"alpha: {aerr}";
            return null;
        }

        if (s.StartsWith("#"))
        {
            s = s[1..].Trim();
            if (uint.TryParse(s, NumberStyles.HexNumber, provider, out u32))
            {
                if (s.Length <= 3)
                {
                    var r = (u32 >> 8) & 0xF;
                    var g = (u32 >> 4) & 0xF;
                    var b = (u32 >> 0) & 0xF;
                    result.R = (byte)(r | (r << 4));
                    result.G = (byte)(g | (g << 4));
                    result.B = (byte)(b | (b << 4));
                    result.A = 0xFF;
                }
                else if (s.Length <= 6)
                {
                    result.A = 0xFF;
                }
                else
                {
                    result = u32;
                }

                return null;
            }

            return "non-hexadecimal string followed a sharp(#).";
        }

        if (uint.TryParse(s, provider, out u32))
        {
            result = u32;
            return null;
        }

        return "unsupported format.";

        static string? TryParseValue(ReadOnlySpan<char> t, IFormatProvider? provider, out byte value)
        {
            float f32;
            value = 0;

            t = t.Trim();
            if (t.EndsWith("%"))
            {
                if (float.TryParse(t[..^1], provider, out f32)
                    && !float.IsNaN(f32))
                {
                    value = (byte)Math.Clamp(f32 * 256, 0, 255);
                    return null;
                }

                return "invalid percent notation.";
            }

            if (float.TryParse(t, provider, out f32) && !float.IsNaN(f32))
            {
                value = (byte)Math.Clamp(f32, 0, 255);
                return null;
            }

            return "invalid value.";
        }
    }
}
