using System.Numerics;
using System.Runtime.CompilerServices;

namespace Dalamud.Interface.Spannables.Styles;

/// <summary>Direction of laying out the controls.</summary>
public enum LinearDirection
{
    /// <summary>Lay out controls, left to right.</summary>
    LeftToRight,

    /// <summary>Lay out controls, right to left.</summary>
    RightToLeft,

    /// <summary>Lay out controls, top to bottom.</summary>
    TopToBottom,

    /// <summary>Lay out controls, bottom to top.</summary>
    BottomToTop,
}

/// <summary>Extension methods for <see cref="LinearDirection"/>.</summary>
public static class LinearDirectionExtensions
{
    /// <summary>Determines if a direction is vertical.</summary>
    /// <param name="direction">Direction to determine.</param>
    /// <returns><c>true</c> if it is vertical.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsVertical(this LinearDirection direction) =>
        direction is LinearDirection.TopToBottom or LinearDirection.BottomToTop;

    /// <summary>Determines if a direction is horizontal.</summary>
    /// <param name="direction">Direction to determine.</param>
    /// <returns><c>true</c> if it is horizontal.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsHorizontal(this LinearDirection direction) =>
        direction is LinearDirection.LeftToRight or LinearDirection.RightToLeft;

    /// <summary>Determines if a direction is consistent with the item index within.</summary>
    /// <param name="direction">Direction to determine.</param>
    /// <returns><c>true</c> if it is consistent.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsDirectionConsistentWithIndex(this LinearDirection direction) =>
        direction is LinearDirection.LeftToRight or LinearDirection.TopToBottom;

    /// <summary>Flips a gravity value expected to be in the range of [0, 1] if the direction is not consistent with
    /// the supposed indices of the items inside.</summary>
    /// <param name="direction">Direction to refer to.</param>
    /// <param name="gravity">Gravity to conditionally flip.</param>
    /// <returns>Flipped gravity value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float ConvertGravity(this LinearDirection direction, float gravity) =>
        direction.IsDirectionConsistentWithIndex() ? gravity : 1 - gravity;

    /// <summary>Gets the main direction component of a <see cref="Vector2"/>.</summary>
    /// <param name="v2">Vector to extract from.</param>
    /// <param name="direction">Direction to refer to.</param>
    /// <returns>Updated vector.</returns>
    public static float GetMainDirection(this Vector2 v2, LinearDirection direction) =>
        direction.IsVertical() ? v2.Y : v2.X;

    /// <summary>Gets the off direction component of a <see cref="Vector2"/>.</summary>
    /// <param name="v2">Vector to extract from.</param>
    /// <param name="direction">Direction to refer to.</param>
    /// <returns>Updated vector.</returns>
    public static float GetOffDirection(this Vector2 v2, LinearDirection direction) =>
        direction.IsHorizontal() ? v2.Y : v2.X;

    /// <summary>Updates the main direction component of a <see cref="Vector2"/>.</summary>
    /// <param name="v2">Vector to update.</param>
    /// <param name="direction">Direction to refer to.</param>
    /// <param name="newValue">New value for the component.</param>
    /// <returns>Updated vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 UpdateMainDirection(this Vector2 v2, LinearDirection direction, float newValue) =>
        direction.IsHorizontal() ? v2 with { X = newValue } : v2 with { Y = newValue };

    /// <summary>Updates off main direction component of a <see cref="Vector2"/>.</summary>
    /// <param name="v2">Vector to update.</param>
    /// <param name="direction">Direction to refer to.</param>
    /// <param name="newValue">New value for the component.</param>
    /// <returns>Updated vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 UpdateOffDirection(this Vector2 v2, LinearDirection direction, float newValue) =>
        direction.IsVertical() ? v2 with { X = newValue } : v2 with { Y = newValue };
}
