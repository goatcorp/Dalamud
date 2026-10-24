using System.Numerics;

using FFXIVClientStructs.FFXIV.Client.Graphics;

namespace Dalamud.NativeUi.Extensions;

/// <summary>
/// ByteColor extension methods.
/// </summary>
internal static class ByteColorExtensions
{
    /// <summary>
    /// Converts a ByteColor to a Vector4 with ranges 0.0f to 1.0f.
    /// </summary>
    /// <param name="color">The ByteColor to convert.</param>
    /// <returns>A vector with expected values between 0.0f and 1.0f.</returns>
    public static Vector4 ToVector4(this ByteColor color)
        => new(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, color.A / 255.0f);

    /// <summary>
    /// Converts a Vector4 with ranges 0.0f to 1.0f, to a ByteColor.
    /// </summary>
    /// <param name="vector">The color vector to convert to ByteColor.</param>
    /// <returns>A ByteColor with expected values between 0 and 255.</returns>
    public static ByteColor ToByteColor(this Vector4 vector) => new()
    {
        A = (byte)(vector.W * 255),
        R = (byte)(vector.X * 255),
        G = (byte)(vector.Y * 255),
        B = (byte)(vector.Z * 255),
    };
}
