using System.Collections.Generic;
using System.Linq;

using Dalamud.Interface.Fonts.Internal;

using Newtonsoft.Json;

namespace Dalamud.Interface.Fonts;

/// <summary>
/// Represents a font from system.
/// </summary>
public sealed class SystemFontFamilyId : IFontFamilyId
{
    /// <summary>Initializes a new instance of the <see cref="SystemFontFamilyId"/> class.</summary>
    /// <remarks>The family is looked up when a font of it loads.</remarks>
    /// <param name="name">The family name, in any language the font has it in.</param>
    public SystemFontFamilyId(string name) => this.Name = new(name);

    /// <summary>Initializes a new instance of the <see cref="SystemFontFamilyId"/> class.</summary>
    /// <param name="name">The name of the family.</param>
    internal SystemFontFamilyId(LocalizedName name) => this.Name = name;

    [JsonConstructor]
    private SystemFontFamilyId(string? englishName, IReadOnlyDictionary<string, string>? localeNames) =>
        this.Name = new(englishName ?? string.Empty, localeNames);

    /// <inheritdoc/>
    [JsonIgnore]
    public LocalizedName Name { get; }

    /// <inheritdoc/>
    [JsonIgnore]
    [field: JsonIgnore]
    public IReadOnlyList<IFontFaceId> Fonts => field ??= DirectWriteUtilities.ListFaces(this);

    /// <inheritdoc/>
    string IFontFamilyId.ShortName => this.Name.English;

    // Gets the English name of Name, as saved.
    [JsonProperty("EnglishName")]
    private string SavedEnglishName => this.Name.English;

    // Gets the translations of Name, as saved.
    [JsonProperty("LocaleNames")]
    private IReadOnlyDictionary<string, string>? SavedLocaleNames => this.Name.ByLocale;

    public static bool operator ==(SystemFontFamilyId? left, SystemFontFamilyId? right) => Equals(left, right);

    public static bool operator !=(SystemFontFamilyId? left, SystemFontFamilyId? right) => !Equals(left, right);

    /// <inheritdoc/>
    public int FindBestMatch(FontStyle style)
    {
        var (weight, stretch, italic) = (style.EffectiveWeight, style.EffectiveStretch, style.Italicized);
        var candidates = this.Fonts.ToList();
        var minGap = int.MaxValue;
        foreach (var c in candidates)
            minGap = Math.Min(minGap, Math.Abs(c.Style.EffectiveWeight - weight));
        candidates.RemoveAll(c => Math.Abs(c.Style.EffectiveWeight - weight) != minGap);

        minGap = int.MaxValue;
        foreach (var c in candidates)
            minGap = Math.Min(minGap, Math.Abs(c.Style.EffectiveStretch - stretch));
        candidates.RemoveAll(c => Math.Abs(c.Style.EffectiveStretch - stretch) != minGap);

        if (candidates.Any(x => x.Style.Italicized == italic))
            candidates.RemoveAll(x => x.Style.Italicized != italic);

        if (candidates.Count == 0)
            return 0;

        for (var i = 0; i < this.Fonts.Count; i++)
        {
            if (Equals(this.Fonts[i], candidates[0]))
                return i;
        }

        return 0;
    }

    /// <inheritdoc/>
    public override string ToString() => $"{nameof(SystemFontFamilyId)}:{this.Name.English}";

    /// <inheritdoc/>
    public override bool Equals(object? obj) =>
        ReferenceEquals(this, obj) || (obj is SystemFontFamilyId other && this.Equals(other));

    /// <inheritdoc/>
    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(this.Name.English);

    // Windows finds families by name without case. Names in another language than English make another id.
    private bool Equals(SystemFontFamilyId other) =>
        string.Equals(this.Name.English, other.Name.English, StringComparison.OrdinalIgnoreCase);
}
