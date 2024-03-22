using System.Numerics;
using System.Runtime.CompilerServices;

using ImGuiNET;

namespace Dalamud.Interface.Spannables;

/// <summary>Extension methods for everything under <see cref="Spannables"/>.</summary>
public static class SpannableExtensions
{
    /// <summary>Transforms the local coordinates to screen coordinates.</summary>
    /// <param name="state">The spannable state.</param>
    /// <param name="coordinates">The local coordinates.</param>
    /// <returns>The screen coordinates.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 TransformToScreen(this ISpannableState state, Vector2 coordinates)
    {
        var b = state.Boundary.Size * state.TransformationOrigin;
        return state.ScreenOffset + Vector2.Transform(coordinates - b, state.Transformation) + b;
    }

    /// <summary>Transforms the screen coordinates to local coordinates.</summary>
    /// <param name="state">The spannable state.</param>
    /// <param name="coordinates">The local coordinates.</param>
    /// <returns>The screen coordinates.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 TransformToLocal(this ISpannableState state, Vector2 coordinates)
    {
        var b = state.Boundary.Size * state.TransformationOrigin;
        if (Matrix4x4.Invert(state.Transformation, out var inverted))
            return Vector2.Transform(coordinates - state.ScreenOffset - b, inverted) + b;
        return Vector2.Zero;
    }

    /// <summary>Gets a global ID from an inner ID.</summary>
    /// <param name="state">The spannable state.</param>
    /// <param name="innerId">The inner ID.</param>
    /// <returns>The global ID.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe uint GetGlobalIdFromInnerId(this ISpannableState state, int innerId) =>
        ImGuiNative.igGetID_Ptr((void*)(((ulong)state.ImGuiGlobalId << 32) | (uint)innerId));
}
