using System.Numerics;

namespace Dalamud.Utility;

/// <summary>
/// Extension methods for System.Numerics.VectorN and SharpDX.VectorN.
/// </summary>
public static class VectorExtensions
{
    /// <summary>
    /// Converts a SharpDX vector to System.Numerics.
    /// </summary>
    /// <param name="vec">Vector to convert.</param>
    /// <returns>A converted vector.</returns>
    public static Vector2 ToSystem(this SharpDX.Vector2 vec) => new(x: vec.X, y: vec.Y);

    /// <summary>
    /// Converts a SharpDX vector to System.Numerics.
    /// </summary>
    /// <param name="vec">Vector to convert.</param>
    /// <returns>A converted vector.</returns>
    public static Vector3 ToSystem(this SharpDX.Vector3 vec) => new(x: vec.X, y: vec.Y, z: vec.Z);

    /// <summary>
    /// Converts a SharpDX vector to System.Numerics.
    /// </summary>
    /// <param name="vec">Vector to convert.</param>
    /// <returns>A converted vector.</returns>
    public static Vector4 ToSystem(this SharpDX.Vector4 vec) => new(x: vec.X, y: vec.Y, z: vec.Z, w: vec.W);

    /// <summary>
    /// Converts a System.Numerics vector to SharpDX.
    /// </summary>
    /// <param name="vec">Vector to convert.</param>
    /// <returns>A converted vector.</returns>
    public static SharpDX.Vector2 ToSharpDX(this Vector2 vec) => new(x: vec.X, y: vec.Y);

    /// <summary>
    /// Converts a System.Numerics vector to SharpDX.
    /// </summary>
    /// <param name="vec">Vector to convert.</param>
    /// <returns>A converted vector.</returns>
    public static SharpDX.Vector3 ToSharpDX(this Vector3 vec) => new(x: vec.X, y: vec.Y, z: vec.Z);

    /// <summary>
    /// Converts a System.Numerics vector to SharpDX.
    /// </summary>
    /// <param name="vec">Vector to convert.</param>
    /// <returns>A converted vector.</returns>
    public static SharpDX.Vector4 ToSharpDX(this Vector4 vec) => new(x: vec.X, y: vec.Y, z: vec.Z, w: vec.W);
}
