using System.Numerics;

namespace Dalamud.Interface.Animation;

/// <summary>
/// Class providing helper functions when facilitating animations.
/// </summary>
public static class AnimUtil
{
    /// <summary>
    /// Lerp between two floats.
    /// </summary>
    /// <param name="firstFloat">The first float.</param>
    /// <param name="secondFloat">The second float.</param>
    /// <param name="by">The point to lerp to.</param>
    /// <returns>The lerped value.</returns>
    public static float Lerp(float firstFloat, float secondFloat, float by)
    {
        return (firstFloat * (1 - @by)) + (secondFloat * by);
    }

    /// <summary>
    /// Lerp between two vectors.
    /// </summary>
    /// <param name="firstVector">The first vector.</param>
    /// <param name="secondVector">The second float.</param>
    /// <param name="by">The point to lerp to.</param>
    /// <returns>The lerped vector.</returns>
    public static Vector2 Lerp(Vector2 firstVector, Vector2 secondVector, float by)
    {
        var retX = Lerp(firstVector.X, secondVector.X, by);
        var retY = Lerp(firstVector.Y, secondVector.Y, by);
        return new Vector2(retX, retY);
    }
}
