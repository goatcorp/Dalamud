using System.Numerics;
using System.Runtime.CompilerServices;

using Dalamud.Utility.Numerics;

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
        return state.ScreenOffset + Trss.TransformVector(coordinates - b, state.Transformation) + b;
    }

    /// <summary>Transforms the screen coordinates to local coordinates.</summary>
    /// <param name="state">The spannable state.</param>
    /// <param name="coordinates">The local coordinates.</param>
    /// <returns>The screen coordinates.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 TransformToLocal(this ISpannableState state, Vector2 coordinates)
    {
        var b = state.Boundary.Size * state.TransformationOrigin;
        return Trss.TransformVectorInverse(coordinates - state.ScreenOffset - b, state.Transformation) + b;
    }

    /// <summary>Gets a global ID from an inner ID.</summary>
    /// <param name="state">The spannable state.</param>
    /// <param name="innerId">The inner ID.</param>
    /// <returns>The global ID.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe uint GetGlobalIdFromInnerId(this ISpannableState state, int innerId) =>
        ImGuiNative.igGetID_Ptr((void*)(((ulong)state.ImGuiGlobalId << 32) | (uint)innerId));
}
