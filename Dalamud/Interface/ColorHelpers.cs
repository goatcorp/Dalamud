using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Numerics;

namespace Dalamud.Interface;

/// <summary>
/// Class containing various methods for manipulating colors.
/// </summary>
public static class ColorHelpers
{
    /// <summary>
    /// Pack a vector4 color into a uint for use in ImGui APIs.
    /// </summary>
    /// <param name="color">The color to pack.</param>
    /// <returns>The packed color.</returns>
    public static uint RgbaVector4ToUint(Vector4 color)
    {
        var r = (byte)(color.X * 255);
        var g = (byte)(color.Y * 255);
        var b = (byte)(color.Z * 255);
        var a = (byte)(color.W * 255);

        return (uint)((a << 24) | (b << 16) | (g << 8) | r);
    }

    /// <summary>
    /// Convert a RGBA color in the range of 0.f to 1.f to a uint.
    /// </summary>
    /// <param name="color">The color to pack.</param>
    /// <returns>The packed color.</returns>
    public static Vector4 RgbaUintToVector4(uint color)
    {
        var r = (color & 0x000000FF) / 255f;
        var g = ((color & 0x0000FF00) >> 8) / 255f;
        var b = ((color & 0x00FF0000) >> 16) / 255f;
        var a = ((color & 0xFF000000) >> 24) / 255f;

        return new Vector4(r, g, b, a);
    }

    /// <summary>
    /// Convert a RGBA color in the range of 0.f to 1.f to a HSV color.
    /// </summary>
    /// <param name="color">The color to convert.</param>
    /// <returns>The color in a HSV representation.</returns>
    public static HsvaColor RgbaToHsv(Vector4 color)
    {
        var r = color.X;
        var g = color.Y;
        var b = color.Z;

        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));

        var h = max;
        var s = max;
        var v = max;

        var d = max - min;
        s = max == 0 ? 0 : d / max;

        if (max == min)
        {
            h = 0; // achromatic
        }
        else
        {
            if (max == r)
            {
                h = ((g - b) / d) + (g < b ? 6 : 0);
            }
            else if (max == g)
            {
                h = ((b - r) / d) + 2;
            }
            else if (max == b)
            {
                h = ((r - g) / d) + 4;
            }

            h /= 6;
        }

        return new HsvaColor(h, s, v, color.W);
    }

    /// <summary>
    /// Convert a HSV color to a RGBA color in the range of 0.f to 1.f.
    /// </summary>
    /// <param name="hsv">The color to convert.</param>
    /// <returns>The RGB color.</returns>
    public static Vector4 HsvToRgb(HsvaColor hsv)
    {
        var h = hsv.H;
        var s = hsv.S;
        var v = hsv.V;

        var r = 0f;
        var g = 0f;
        var b = 0f;

        var i = (int)Math.Floor(h * 6);
        var f = (h * 6) - i;
        var p = v * (1 - s);
        var q = v * (1 - (f * s));
        var t = v * (1 - ((1 - f) * s));

        switch (i % 6)
        {
            case 0:
                r = v;
                g = t;
                b = p;
                break;

            case 1:
                r = q;
                g = v;
                b = p;
                break;

            case 2:
                r = p;
                g = v;
                b = t;
                break;

            case 3:
                r = p;
                g = q;
                b = v;
                break;

            case 4:
                r = t;
                g = p;
                b = v;
                break;

            case 5:
                r = v;
                g = p;
                b = q;
                break;
        }

        return new Vector4(r, g, b, hsv.A);
    }

    /// <summary>
    /// Lighten a color.
    /// </summary>
    /// <param name="color">The color to lighten.</param>
    /// <param name="amount">The amount to lighten.</param>
    /// <returns>The lightened color.</returns>
    public static Vector4 Lighten(this Vector4 color, float amount)
    {
        var hsv = RgbaToHsv(color);
        hsv.V += amount;
        return HsvToRgb(hsv);
    }

    /// <summary>
    /// Lighten a color.
    /// </summary>
    /// <param name="color">The color to lighten.</param>
    /// <param name="amount">The amount to lighten.</param>
    /// <returns>The lightened color.</returns>
    public static uint Lighten(uint color, float amount)
        => RgbaVector4ToUint(Lighten(RgbaUintToVector4(color), amount));

    /// <summary>
    /// Darken a color.
    /// </summary>
    /// <param name="color">The color to lighten.</param>
    /// <param name="amount">The amount to lighten.</param>
    /// <returns>The darkened color.</returns>
    public static Vector4 Darken(this Vector4 color, float amount)
    {
        var hsv = RgbaToHsv(color);
        hsv.V -= amount;
        return HsvToRgb(hsv);
    }

    /// <summary>
    /// Darken a color.
    /// </summary>
    /// <param name="color">The color to lighten.</param>
    /// <param name="amount">The amount to lighten.</param>
    /// <returns>The darkened color.</returns>
    public static uint Darken(uint color, float amount)
        => RgbaVector4ToUint(Darken(RgbaUintToVector4(color), amount));

    /// <summary>
    /// Saturate a color.
    /// </summary>
    /// <param name="color">The color to lighten.</param>
    /// <param name="amount">The amount to lighten.</param>
    /// <returns>The saturated color.</returns>
    public static Vector4 Saturate(this Vector4 color, float amount)
    {
        var hsv = RgbaToHsv(color);
        hsv.S += amount;
        return HsvToRgb(hsv);
    }

    /// <summary>
    /// Saturate a color.
    /// </summary>
    /// <param name="color">The color to lighten.</param>
    /// <param name="amount">The amount to lighten.</param>
    /// <returns>The saturated color.</returns>
    public static uint Saturate(uint color, float amount)
        => RgbaVector4ToUint(Saturate(RgbaUintToVector4(color), amount));

    /// <summary>
    /// Desaturate a color.
    /// </summary>
    /// <param name="color">The color to lighten.</param>
    /// <param name="amount">The amount to lighten.</param>
    /// <returns>The desaturated color.</returns>
    public static Vector4 Desaturate(this Vector4 color, float amount)
    {
        var hsv = RgbaToHsv(color);
        hsv.S -= amount;
        return HsvToRgb(hsv);
    }

    /// <summary>
    /// Desaturate a color.
    /// </summary>
    /// <param name="color">The color to lighten.</param>
    /// <param name="amount">The amount to lighten.</param>
    /// <returns>The desaturated color.</returns>
    public static uint Desaturate(uint color, float amount)
        => RgbaVector4ToUint(Desaturate(RgbaUintToVector4(color), amount));

    /// <summary>
    /// Fade a color.
    /// </summary>
    /// <param name="color">The color to lighten.</param>
    /// <param name="amount">The amount to lighten.</param>
    /// <returns>The faded color.</returns>
    public static Vector4 Fade(this Vector4 color, float amount)
    {
        var hsv = RgbaToHsv(color);
        hsv.A -= amount;
        return HsvToRgb(hsv);
    }
    
    /// <summary>
    /// Set alpha of a color.
    /// </summary>
    /// <param name="color">The color.</param>
    /// <param name="alpha">The alpha value to set.</param>
    /// <returns>The color with the set alpha value.</returns>
    public static Vector4 WithAlpha(this Vector4 color, float alpha)
        => color with { W = alpha };

    /// <summary>
    /// Fade a color.
    /// </summary>
    /// <param name="color">The color to lighten.</param>
    /// <param name="amount">The amount to lighten.</param>
    /// <returns>The faded color.</returns>
    public static uint Fade(uint color, float amount)
        => RgbaVector4ToUint(Fade(RgbaUintToVector4(color), amount));

    /// <summary>
    /// Convert a KnownColor to a RGBA vector with values between 0.0f and 1.0f.
    /// </summary>
    /// <param name="knownColor">Known Color to convert.</param>
    /// <returns>RGBA Vector with values between 0.0f and 1.0f.</returns>
    public static Vector4 Vector(this KnownColor knownColor)
    {
        var rgbColor = Color.FromKnownColor(knownColor);
        return new Vector4(rgbColor.R, rgbColor.G, rgbColor.B, rgbColor.A) / 255.0f;
    }

    /// <summary>
    /// Normalizes a Vector4 with RGBA 255 color values to values between 0.0f and 1.0f
    /// If values are out of RGBA 255 range, the original value is returned.
    /// </summary>
    /// <param name="color">The color vector to convert.</param>
    /// <returns>A vector with values between 0.0f and 1.0f.</returns>
    public static Vector4 NormalizeToUnitRange(this Vector4 color) => color switch
    {
        // If any components are out of range, return original value.
        { W: > 255.0f or < 0.0f } or { X: > 255.0f or < 0.0f } or { Y: > 255.0f or < 0.0f } or { Z: > 255.0f or < 0.0f } => color,
            
        // If all components are already unit range, return original value.
        { W: >= 0.0f and <= 1.0f, X: >= 0.0f and <= 1.0f, Y: >= 0.0f and <= 1.0f, Z: >= 0.0f and <= 1.0f } => color,
            
        _ => color / 255.0f,
    };
    
    /// <summary>
    /// A struct representing a color using HSVA coordinates.
    /// </summary>
    /// <param name="H">The hue represented by this struct.</param>
    /// <param name="S">The saturation represented by this struct.</param>
    /// <param name="V">The value represented by this struct.</param>
    /// <param name="A">The alpha represented by this struct.</param>
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter",
                     Justification = "I don't like it.")]
    public record struct HsvaColor(float H, float S, float V, float A);
}
