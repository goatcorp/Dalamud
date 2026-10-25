using Dalamud.Storage.Assets;

using Newtonsoft.Json;

namespace Dalamud.Interface.Fonts;

/// <summary>
/// Represents a font from Dalamud assets.
/// </summary>
public sealed class DalamudAssetFontId : SingleFontId
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DalamudAssetFontId"/> class.
    /// </summary>
    /// <param name="asset">The font asset.</param>
    /// <param name="fontNo">Index of the face in a collection.</param>
    [JsonConstructor]
    internal DalamudAssetFontId(DalamudAsset asset, int fontNo = 0)
    {
        if (asset.GetPurpose() != DalamudAssetPurpose.Font)
            throw new ArgumentOutOfRangeException(nameof(asset), asset, "The specified asset is not a font asset.");
        this.Asset = asset;
        this.FontNo = fontNo;
    }

    /// <summary>
    /// Gets the font asset.
    /// </summary>
    [JsonProperty]
    public DalamudAsset Asset { get; init; }

    /// <summary>Gets the index of the face in a font collection, or 0.</summary>
    [JsonProperty]
    public int FontNo { get; init; }

    /// <inheritdoc/>
    [JsonIgnore]
    [field: JsonIgnore]
    public override LocalizedName Name => field ??= new($"Dalamud: {this.Asset}");

    /// <inheritdoc/>
    private protected override string ShortName => this.FontNo == 0 ? $"{this.Asset}" : $"{this.Asset}#{this.FontNo}";

    public static bool operator ==(DalamudAssetFontId? left, DalamudAssetFontId? right) =>
        Equals(left, right);

    public static bool operator !=(DalamudAssetFontId? left, DalamudAssetFontId? right) =>
        !Equals(left, right);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is DalamudAssetFontId other && this.Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(this.Asset, this.FontNo);

    /// <inheritdoc/>
    public override string ToString() => $"{nameof(DalamudAssetFontId)}:{this.Asset}#{this.FontNo}";

    private bool Equals(DalamudAssetFontId other) =>
        this.Asset == other.Asset && this.FontNo == other.FontNo;
}
