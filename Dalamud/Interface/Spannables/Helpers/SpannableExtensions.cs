using System.Numerics;
using System.Runtime.CompilerServices;

using ImGuiNET;

namespace Dalamud.Interface.Spannables.Helpers;

/// <summary>Extension methods for everything under <see cref="Spannables"/>.</summary>
public static class SpannableExtensions
{
    /// <summary>Transforms the local coordinates to screen coordinates.</summary>
    /// <param name="renderPass">The spannable state.</param>
    /// <param name="coordinates">The local coordinates.</param>
    /// <returns>The screen coordinates.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 TransformToScreen(this ISpannableRenderPass renderPass, Vector2 coordinates)
    {
        var b = renderPass.Boundary.Size * renderPass.TransformationOrigin;
        return renderPass.ScreenOffset + Vector2.Transform(coordinates - b, renderPass.Transformation) + b;
    }

    /// <summary>Transforms the screen coordinates to local coordinates.</summary>
    /// <param name="renderPass">The spannable state.</param>
    /// <param name="coordinates">The local coordinates.</param>
    /// <returns>The screen coordinates.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 TransformToLocal(this ISpannableRenderPass renderPass, Vector2 coordinates)
    {
        var b = renderPass.Boundary.Size * renderPass.TransformationOrigin;
        if (Matrix4x4.Invert(renderPass.Transformation, out var inverted))
            return Vector2.Transform(coordinates - renderPass.ScreenOffset - b, inverted) + b;
        return Vector2.Zero;
    }

    /// <summary>Gets a global ID from an inner ID.</summary>
    /// <param name="renderPass">The spannable state.</param>
    /// <param name="innerId">The inner ID.</param>
    /// <returns>The global ID.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe uint GetGlobalIdFromInnerId(this ISpannableRenderPass renderPass, int innerId) =>
        ImGuiNative.igGetID_Ptr((void*)(((ulong)renderPass.ImGuiGlobalId << 32) | (uint)innerId));
}
