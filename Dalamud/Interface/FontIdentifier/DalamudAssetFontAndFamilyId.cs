using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Storage.Assets;
using Dalamud.Utility;

using ImGuiNET;

using Newtonsoft.Json;

using TerraFX.Interop.DirectX;

namespace Dalamud.Interface.FontIdentifier;

/// <summary>
/// Represents a font from Dalamud assets.
/// </summary>
public sealed class DalamudAssetFontAndFamilyId : IFontFamilyId, IFontId
{
    private static readonly ImmutableList<DalamudAssetFontAndFamilyId>?[] InstancesArray;

    static DalamudAssetFontAndFamilyId()
    {
        InstancesArray =
            new ImmutableList<DalamudAssetFontAndFamilyId>?[Enum.GetValues<DalamudAsset>().Max(x => (int)x) + 1];
        foreach (var v in Enum.GetValues<DalamudAsset>())
        {
            if (v.GetPurpose() == DalamudAssetPurpose.Font)
#pragma warning disable CS0618 // Type or member is obsolete
                InstancesArray[(int)v] = new[] { new DalamudAssetFontAndFamilyId(v) }.ToImmutableList();
#pragma warning restore CS0618 // Type or member is obsolete
        }
    }

    /// <summary>Initializes a new instance of the <see cref="DalamudAssetFontAndFamilyId"/> class. </summary>
    /// <param name="asset">The font asset.</param>
    [Obsolete($"Use {nameof(From)} instead.")]
    [Api10ToDo("Make private")]
    public DalamudAssetFontAndFamilyId(DalamudAsset asset) => this.Asset = asset;

    /// <summary>
    /// Gets the font asset.
    /// </summary>
    [JsonProperty]
    public DalamudAsset Asset { get; init; }

    /// <inheritdoc/>
    [JsonIgnore]
    public string EnglishName => $"Dalamud: {this.Asset}";

    /// <inheritdoc/>
    [JsonIgnore]
    public IReadOnlyDictionary<string, string>? LocaleNames => null;

    /// <inheritdoc/>
    [JsonIgnore]
    public IReadOnlyList<IFontId> Fonts => InstancesArray[(int)this.Asset]!;

    /// <inheritdoc/>
    [JsonIgnore]
    public IFontFamilyId Family => this;

    /// <inheritdoc/>
    [JsonIgnore]
    public int Weight => (int)DWRITE_FONT_WEIGHT.DWRITE_FONT_WEIGHT_NORMAL;

    /// <inheritdoc/>
    [JsonIgnore]
    public int Stretch => (int)DWRITE_FONT_STRETCH.DWRITE_FONT_STRETCH_NORMAL;

    /// <inheritdoc/>
    [JsonIgnore]
    public int Style => (int)DWRITE_FONT_STYLE.DWRITE_FONT_STYLE_NORMAL;

    public static bool operator ==(DalamudAssetFontAndFamilyId? left, DalamudAssetFontAndFamilyId? right) =>
        Equals(left, right);

    public static bool operator !=(DalamudAssetFontAndFamilyId? left, DalamudAssetFontAndFamilyId? right) =>
        !Equals(left, right);

    /// <summary>Gets the preallocated instance.</summary>
    /// <param name="what">The Dalamud asset specifier.</param>
    /// <returns>The preallocated instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="what"/> is invalid.</exception>
    public static DalamudAssetFontAndFamilyId From(DalamudAsset what)
    {
        var whatInt = (int)what;
        if (whatInt < 0 || whatInt >= InstancesArray.Length || InstancesArray[whatInt] is null)
            throw new ArgumentOutOfRangeException(nameof(what), what, null);

        return InstancesArray[whatInt][0] ?? throw new ArgumentOutOfRangeException(nameof(what), what, null);
    }

    /// <summary>Gets the preallocated instance.</summary>
    /// <param name="what">The Dalamud asset specifier.</param>
    /// <param name="familyId">The retrieved family ID.</param>
    /// <returns><c>true</c> if retrieved.</returns>
    public static bool TryGetInstance(DalamudAsset what, [NotNullWhen(true)] out DalamudAssetFontAndFamilyId? familyId)
    {
        var whatInt = (int)what;
        if (whatInt < 0 || whatInt >= InstancesArray.Length || InstancesArray[whatInt] is null)
        {
            familyId = null;
            return false;
        }

        familyId = InstancesArray[whatInt][0];
        return familyId is not null;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is DalamudAssetFontAndFamilyId other && this.Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => (int)this.Asset;

    /// <inheritdoc/>
    public override string ToString() => $"{nameof(DalamudAssetFontAndFamilyId)}:{this.Asset}";

    /// <inheritdoc/>
    public int FindBestMatch(int weight, int stretch, int style) => 0;

    /// <inheritdoc/>
    public ImFontPtr AddToBuildToolkit(IFontAtlasBuildToolkitPreBuild tk, in SafeFontConfig config) =>
        tk.AddDalamudAssetFont(this.Asset, config);

    private bool Equals(DalamudAssetFontAndFamilyId other) => this.Asset == other.Asset;
}
