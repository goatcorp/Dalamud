using System.Collections.Generic;

using Dalamud.Interface.GameFonts;
using Dalamud.Interface.ManagedFontAtlas;

using ImGuiNET;

using Newtonsoft.Json;

using TerraFX.Interop.DirectX;

namespace Dalamud.Interface.FontIdentifier;

/// <summary>
/// Represents a font from the game.
/// </summary>
public sealed class GameFontAndFamilyId : IFontId, IFontFamilyId
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GameFontAndFamilyId"/> class.
    /// </summary>
    /// <param name="family">The game font family.</param>
    public GameFontAndFamilyId(GameFontFamily family) => this.GameFontFamily = family;

    /// <summary>
    /// Gets the game font family.
    /// </summary>
    [JsonProperty]
    public GameFontFamily GameFontFamily { get; init; }

    /// <inheritdoc/>
    [JsonIgnore]
    public string EnglishName => $"Game: {Enum.GetName(this.GameFontFamily) ?? throw new NotSupportedException()}";

    /// <inheritdoc/>
    [JsonIgnore]
    public IReadOnlyDictionary<string, string>? LocaleNames => null;

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

    public static bool operator ==(GameFontAndFamilyId? left, GameFontAndFamilyId? right) => Equals(left, right);

    public static bool operator !=(GameFontAndFamilyId? left, GameFontAndFamilyId? right) => !Equals(left, right);

    /// <inheritdoc/>
    public override bool Equals(object? obj) =>
        ReferenceEquals(this, obj) || (obj is GameFontAndFamilyId other && this.Equals(other));

    /// <inheritdoc/>
    public override int GetHashCode() => (int)this.GameFontFamily;

    /// <inheritdoc/>
    public int FindBestMatch(int weight, int stretch, int style) => 0;

    /// <inheritdoc/>
    public override string ToString() => $"{nameof(GameFontAndFamilyId)}:{this.GameFontFamily}";

    /// <inheritdoc/>
    public ImFontPtr AddToBuildToolkit(IFontAtlasBuildToolkitPreBuild tk, in SafeFontConfig config) =>
        tk.AddGameGlyphs(new(this.GameFontFamily, config.SizePx), config.GlyphRanges, config.MergeFont);

    private bool Equals(GameFontAndFamilyId other) => this.GameFontFamily == other.GameFontFamily;
}
