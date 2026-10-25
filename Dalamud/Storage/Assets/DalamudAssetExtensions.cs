using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Dalamud.Utility;

namespace Dalamud.Storage.Assets;

/// <summary>Extension methods for <see cref="DalamudAsset"/>.</summary>
public static class DalamudAssetExtensions
{
    private static readonly DalamudAssetAttribute EmptyAttribute = new(DalamudAssetPurpose.Empty, null, false);
    private static readonly DalamudAssetAttribute[] AttributeCache = CreateCache();

    /// <summary>Gets the purpose.</summary>
    /// <param name="asset">The asset.</param>
    /// <returns>The purpose.</returns>
    public static DalamudAssetPurpose GetPurpose(this DalamudAsset asset) => asset.GetAssetAttribute().Purpose;

    /// <summary>Gets the attribute.</summary>
    /// <param name="asset">The asset.</param>
    /// <returns>The attribute.</returns>
    internal static DalamudAssetAttribute GetAssetAttribute(this DalamudAsset asset) =>
        (int)asset < 0 || (int)asset >= AttributeCache.Length
            ? EmptyAttribute
            : Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(AttributeCache), (int)asset);

    /// <summary>Gets the local override file name for <paramref name="asset"/>.</summary>
    /// <param name="asset">The asset.</param>
    /// <returns><see cref="DalamudAssetLocalFileAttribute.FileName"/> if present, or <c>asset.ToString()</c> otherwise.</returns>
    internal static string GetLocalFileName(this DalamudAsset asset) =>
        asset.GetAttribute<DalamudAssetLocalFileAttribute>()?.FileName ?? asset.ToString();

    private static DalamudAssetAttribute[] CreateCache()
    {
        var assets = Enum.GetValues<DalamudAsset>();
        var table = new DalamudAssetAttribute[assets.Max(x => (int)x) + 1];
        table.AsSpan().Fill(EmptyAttribute);
        foreach (var asset in assets)
            table[(int)asset] = asset.GetAttribute<DalamudAssetAttribute>() ?? EmptyAttribute;
        return table;
    }
}
