using System.Collections.Generic;

using Dalamud.Interface.ManagedFontAtlas;

using ImGuiNET;

using Newtonsoft.Json;

using TerraFX.Interop.DirectX;

namespace Dalamud.Interface.FontIdentifier;

/// <summary>
/// Represents the default Dalamud font.
/// </summary>
public sealed class DalamudDefaultFontAndFamilyId : IFontId, IFontFamilyId
{
    /// <summary>
    /// The shared instance of <see cref="DalamudDefaultFontAndFamilyId"/>.
    /// </summary>
    public static readonly DalamudDefaultFontAndFamilyId Instance = new();

    private DalamudDefaultFontAndFamilyId()
    {
    }

    /// <inheritdoc cref="IFontId.TypeName"/>
    [JsonProperty]
    public string TypeName => nameof(DalamudDefaultFontAndFamilyId);

    /// <inheritdoc/>
    [JsonIgnore]
    public string EnglishName => "(Default)";

    /// <inheritdoc/>
    [JsonIgnore]
    public string LocalizedName => "(Default)";

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

    /// <inheritdoc/>
    [JsonIgnore]
    public IReadOnlyList<IFontId> Fonts => new List<IFontId> { this }.AsReadOnly();

    public static bool operator ==(DalamudDefaultFontAndFamilyId? left, DalamudDefaultFontAndFamilyId? right) =>
        left is null == right is null;

    public static bool operator !=(DalamudDefaultFontAndFamilyId? left, DalamudDefaultFontAndFamilyId? right) =>
        left is null != right is null;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is DalamudDefaultFontAndFamilyId;

    /// <inheritdoc/>
    public override int GetHashCode() => 12345678;

    /// <inheritdoc/>
    public override string ToString() => nameof(DalamudDefaultFontAndFamilyId);

    /// <inheritdoc/>
    public ImFontPtr AddToBuildToolkit(
        IFontAtlasBuildToolkitPreBuild tk,
        float sizePx,
        ushort[]? glyphRanges,
        ImFontPtr mergeFont)
        => tk.AddDalamudDefaultFont(sizePx, glyphRanges);
    // TODO: mergeFont

    /// <inheritdoc/>
    public int FindBestMatch(int weight, int stretch, int style) => 0;
}
