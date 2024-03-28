using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using Dalamud.Interface.GameFonts;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Utility;

using ImGuiNET;

using Newtonsoft.Json;

using TerraFX.Interop.DirectX;

namespace Dalamud.Interface.FontIdentifier;

/// <summary>
/// Represents a font from the game.
/// </summary>
public sealed class GameFontAndFamilyId : IFontId, IFontFamilyId
{
    private static readonly ImmutableList<GameFontAndFamilyId>?[] InstancesArray;

    static GameFontAndFamilyId()
    {
        InstancesArray = new ImmutableList<GameFontAndFamilyId>?[Enum.GetValues<GameFontFamily>().Max(x => (int)x) + 1];
        foreach (var v in Enum.GetValues<GameFontFamily>())
        {
            if (v == GameFontFamily.Undefined)
                continue;

#pragma warning disable CS0618 // Type or member is obsolete
            InstancesArray[(int)v] = new[] { new GameFontAndFamilyId(v) }.ToImmutableList();
#pragma warning restore CS0618 // Type or member is obsolete
        }
    }

    /// <summary>Initializes a new instance of the <see cref="GameFontAndFamilyId"/> class.</summary>
    /// <param name="family">The game font family.</param>
    [Obsolete($"Use {nameof(From)} instead.")]
    [Api10ToDo("Make private")]
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
    public IReadOnlyList<IFontId> Fonts => InstancesArray[(int)this.GameFontFamily]!;

    public static bool operator ==(GameFontAndFamilyId? left, GameFontAndFamilyId? right) => Equals(left, right);

    public static bool operator !=(GameFontAndFamilyId? left, GameFontAndFamilyId? right) => !Equals(left, right);

    /// <summary>Gets the preallocated instance.</summary>
    /// <param name="what">The game font family.</param>
    /// <returns>The preallocated instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="what"/> is invalid.</exception>
    public static GameFontAndFamilyId From(GameFontFamily what)
    {
        var whatInt = (int)what;
        if (whatInt < 0 || whatInt >= InstancesArray.Length || InstancesArray[whatInt] is null)
            throw new ArgumentOutOfRangeException(nameof(what), what, null);

        return InstancesArray[whatInt][0] ?? throw new ArgumentOutOfRangeException(nameof(what), what, null);
    }

    /// <summary>Gets the preallocated instance.</summary>
    /// <param name="what">The game font family.</param>
    /// <param name="familyId">The retrieved family ID.</param>
    /// <returns><c>true</c> if retrieved.</returns>
    public static bool TryGetInstance(GameFontFamily what, [NotNullWhen(true)] out GameFontAndFamilyId? familyId)
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
