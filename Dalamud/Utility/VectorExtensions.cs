using System.Diagnostics.CodeAnalysis;
using System.Numerics;

using FFXIVClientStructs.FFXIV.Client.Graphics;

namespace Dalamud.Utility;

#pragma warning disable SA1101

/// <summary>
/// Extension methods for vectors.
/// </summary>
[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Redundant.")]
public static class VectorExtensions
{
    extension(Vector2 vec)
    {
        public Vector2 WithX(float x)
        {
            return new Vector2(x, vec.Y);
        }

        public Vector2 WithY(float y)
        {
            return new Vector2(vec.X, y);
        }
    }

    extension(Vector3 vec)
    {
        public Vector3 WithX(float x)
        {
            return new Vector3(x, vec.Y, vec.Z);
        }

        public Vector3 WithY(float y)
        {
            return new Vector3(vec.X, y, vec.Z);
        }

        public Vector3 WithZ(float z)
        {
            return new Vector3(vec.X, vec.Y, z);
        }
    }

    extension(Vector4 vec)
    {
        public Vector4 WithX(float x)
        {
            return new Vector4(x, vec.Y, vec.Z, vec.W);
        }

        public Vector4 WithY(float y)
        {
            return new Vector4(vec.X, y, vec.Z, vec.W);
        }

        public Vector4 WithZ(float z)
        {
            return new Vector4(vec.X, vec.Y, z, vec.W);
        }

        public Vector4 WithW(float w)
        {
            return new Vector4(vec.X, vec.Y, vec.Z, w);
        }

        public ByteColor ToByteColor()
        {
            return new ByteColor { A = (byte)(vec.W * 255), R = (byte)(vec.X * 255), G = (byte)(vec.Y * 255), B = (byte)(vec.Z * 255) };
        }
    }
}
