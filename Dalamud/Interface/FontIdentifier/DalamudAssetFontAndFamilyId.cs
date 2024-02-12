using System.Collections.Generic;

using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Storage.Assets;

using ImGuiNET;

using Newtonsoft.Json;

using TerraFX.Interop.DirectX;

namespace Dalamud.Interface.FontIdentifier;

/// <summary>
/// Represents a font from Dalamud assets.
/// </summary>
public sealed class DalamudAssetFontAndFamilyId : IFontFamilyId, IFontId
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DalamudAssetFontAndFamilyId"/> class.
    /// </summary>
    /// <param name="asset">The font asset.</param>
    public DalamudAssetFontAndFamilyId(DalamudAsset asset)
    {
        if (asset.GetPurpose() != DalamudAssetPurpose.Font)
            throw new ArgumentOutOfRangeException(nameof(asset), asset, "The specified asset is not a font asset.");
        this.Asset = asset;
    }

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
    public IReadOnlyList<IFontId> Fonts => new List<IFontId> { this }.AsReadOnly();

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
