using System.Numerics;

namespace Dalamud.Interface.Internal.Badge;

/// <summary>
/// Represents information about a badge.
/// </summary>
/// <param name="Name">Name of the badge.</param>
/// <param name="Description">Description of the badge.</param>
/// <param name="IconIndex">Icon index.</param>
/// <param name="UnlockSha256">Sha256 hash of the unlock password.</param>
/// <param name="UnlockMethod">How the badge is unlocked.</param>
internal record BadgeInfo(
    Func<string> Name,
    Func<string> Description,
    int IconIndex,
    string UnlockSha256,
    BadgeUnlockMethod UnlockMethod)
{
    private const float BadgeWidth = 256;
    private const float BadgeHeight = 256;
    private const float BadgesPerRow = 2;

    /// <summary>
    /// Gets the UV coordinates for the badge icon in the atlas.
    /// </summary>
    /// <param name="atlasWidthPx">Width of the atlas.</param>
    /// <param name="atlasHeightPx">Height of the atlas.</param>
    /// <returns>UV coordinates.</returns>
    public (Vector2 Uv0, Vector2 Uv1) GetIconUv(float atlasWidthPx, float atlasHeightPx)
    {
        // Calculate row and column from icon index
        var col = this.IconIndex % (int)BadgesPerRow;
        var row = this.IconIndex / (int)BadgesPerRow;

        // Calculate pixel positions
        var x0 = col * BadgeWidth;
        var y0 = row * BadgeHeight;
        var x1 = x0 + BadgeWidth;
        var y1 = y0 + BadgeHeight;

        // Convert to UV coordinates (0.0 to 1.0)
        var uv0 = new Vector2(x0 / atlasWidthPx, y0 / atlasHeightPx);
        var uv1 = new Vector2(x1 / atlasWidthPx, y1 / atlasHeightPx);

        return (uv0, uv1);
    }
}
