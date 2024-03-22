using System.Numerics;
using System.Runtime.CompilerServices;

namespace Dalamud.Utility.Numerics;

/// <summary>Extension methods for matrices.</summary>
internal static class MatrixExtensions
{
    /// <summary>Removes the translation component from a transformation matrix.</summary>
    /// <param name="mtx">The matrix.</param>
    /// <returns>The matrix without rtanslation component.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 WithoutTranslation(this Matrix4x4 mtx) =>
        mtx with { M41 = 0, M42 = 0, M43 = 0 };
}
