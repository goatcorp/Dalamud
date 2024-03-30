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
    /// <param name="direction">The direction to determine.</param>
    /// <returns><c>true</c> if it is vertical.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsVertical(this LinearDirection direction) =>
        direction is LinearDirection.TopToBottom or LinearDirection.BottomToTop;

    /// <summary>Determines if a direction is horizontal.</summary>
    /// <param name="direction">The direction to determine.</param>
    /// <returns><c>true</c> if it is horizontal.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsHorizontal(this LinearDirection direction) =>
        direction is LinearDirection.LeftToRight or LinearDirection.RightToLeft;

    /// <summary>Determines if a direction is consistent with the item index within.</summary>
    /// <param name="direction">The direction to determine.</param>
    /// <returns><c>true</c> if it is consistent.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsDirectionConsistentWithIndex(this LinearDirection direction) =>
        direction is LinearDirection.LeftToRight or LinearDirection.TopToBottom;
}
