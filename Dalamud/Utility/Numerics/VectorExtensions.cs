using System.Diagnostics.CodeAnalysis;
using System.Numerics;

using FFXIVClientStructs.FFXIV.Client.Graphics;

namespace Dalamud.Utility.Numerics;

/// <summary>
/// Extension methods for vectors.
/// </summary>
[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Redundant.")]
public static class VectorExtensions
{
    public static Vector4 WithX(this Vector4 v, float x)
    {
        return new Vector4(x, v.Y, v.Z, v.W);
    }

    public static Vector4 WithY(this Vector4 v, float y)
    {
        return new Vector4(v.X, y, v.Z, v.W);
    }

    public static Vector4 WithZ(this Vector4 v, float z)
    {
        return new Vector4(v.X, v.Y, z, v.W);
    }

    public static Vector4 WithW(this Vector4 v, float w)
    {
        return new Vector4(v.X, v.Y, v.Z, w);
    }

    public static Vector3 WithX(this Vector3 v, float x)
    {
        return new Vector3(x, v.Y, v.Z);
    }

    public static Vector3 WithY(this Vector3 v, float y)
    {
        return new Vector3(v.X, y, v.Z);
    }

    public static Vector3 WithZ(this Vector3 v, float z)
    {
        return new Vector3(v.X, v.Y, z);
    }

    public static Vector2 WithX(this Vector2 v, float x)
    {
        return new Vector2(x, v.Y);
    }

    public static Vector2 WithY(this Vector2 v, float y)
    {
        return new Vector2(v.X, y);
    }

    public static ByteColor ToByteColor(this Vector4 v)
    {
        return new ByteColor { A = (byte)(v.W * 255), R = (byte)(v.X * 255), G = (byte)(v.Y * 255), B = (byte)(v.Z * 255) };
    }
}
