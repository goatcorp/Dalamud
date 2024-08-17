using System.Collections.Frozen;
using System.Collections.Generic;

using Dalamud.Utility;

namespace Dalamud.Storage.Assets;

/// <summary>
/// Extension methods for <see cref="DalamudAsset"/>.
/// </summary>
public static class DalamudAssetExtensions
{
    private static readonly FrozenDictionary<DalamudAsset, DalamudAssetAttribute> AttributeCache = CreateCache();

    /// <summary>
    /// Gets the purpose.
    /// </summary>
    /// <param name="asset">The asset.</param>
    /// <returns>The purpose.</returns>
    public static DalamudAssetPurpose GetPurpose(this DalamudAsset asset)
    {
        return GetAssetAttribute(asset)?.Purpose ?? DalamudAssetPurpose.Empty;
    }

    /// <summary>
    /// Gets the attribute.
    /// </summary>
    /// <param name="asset">The asset.</param>
    /// <returns>The attribute.</returns>
    internal static DalamudAssetAttribute? GetAssetAttribute(this DalamudAsset asset)
    {
        return AttributeCache.GetValueOrDefault(asset);
    }

    private static FrozenDictionary<DalamudAsset, DalamudAssetAttribute> CreateCache()
    {
        var dict = new Dictionary<DalamudAsset, DalamudAssetAttribute>();

        foreach (var asset in Enum.GetValues<DalamudAsset>())
        {
            dict.Add(asset, asset.GetAttribute<DalamudAssetAttribute>());
        }

        return dict.ToFrozenDictionary();
    }
}
