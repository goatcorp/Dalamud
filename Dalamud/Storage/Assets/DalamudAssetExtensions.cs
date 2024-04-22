using Dalamud.Utility;

namespace Dalamud.Storage.Assets;

/// <summary>
/// Extension methods for <see cref="DalamudAsset"/>.
/// </summary>
public static class DalamudAssetExtensions
{
    /// <summary>
    /// Gets the purpose.
    /// </summary>
    /// <param name="asset">The asset.</param>
    /// <returns>The purpose.</returns>
    public static DalamudAssetPurpose GetPurpose(this DalamudAsset asset) =>
        asset.GetAttribute<DalamudAssetAttribute>()?.Purpose ?? DalamudAssetPurpose.Empty;
}
