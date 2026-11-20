using System.Collections.Generic;

using Dalamud.Interface.Fonts.Internal;

using Newtonsoft.Json;

namespace Dalamud.Interface.Fonts;

/// <summary>Represents an installed or named system font family.</summary>
public sealed class SystemFontFamilyId : IFontFamilyId
{
    /// <summary>Initializes a new instance of the <see cref="SystemFontFamilyId"/> class.</summary>
    /// <remarks>If installed at construction time, resolves to the family's canonical localized names. Otherwise
    /// retains <paramref name="name"/> as-is.</remarks>
    /// <param name="name">The family name, in any language the font has it in.</param>
    public SystemFontFamilyId(string name) =>
        this.Name = DirectWriteUtilities.TryGetFamilyName(name, out var localized) ? localized : new(name);

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

    [JsonProperty("EnglishName")]
    private string SavedEnglishName => this.Name.English;

    [JsonProperty("LocaleNames")]
    private IReadOnlyDictionary<string, string>? SavedLocaleNames => this.Name.ByLocale;

    public static bool operator ==(SystemFontFamilyId? left, SystemFontFamilyId? right) => Equals(left, right);

    public static bool operator !=(SystemFontFamilyId? left, SystemFontFamilyId? right) => !Equals(left, right);

    /// <inheritdoc/>
    public int FindBestMatch(FontStyle style)
    {
        var (weight, stretch, italic) = (style.EffectiveWeight, style.EffectiveStretch, style.Italicized);
        var fonts = this.Fonts;
        var best = 0;
        var bestGap = (int.MaxValue, int.MaxValue, int.MaxValue);
        for (var i = 0; i < fonts.Count; i++)
        {
            var s = fonts[i].Style;
            var gap = (
                Math.Abs(s.EffectiveWeight - weight),
                Math.Abs(s.EffectiveStretch - stretch),
                s.Italicized == italic ? 0 : 1);
            if (gap.CompareTo(bestGap) < 0)
                (best, bestGap) = (i, gap);
        }

        return best;
    }

    /// <inheritdoc/>
    IFontFaceId IFontFamilyId.GetFace(FontStyle style) => new SystemFontFaceId(this, style);

    /// <inheritdoc/>
    public override string ToString() => $"{nameof(SystemFontFamilyId)}:{this.Name.English}";

    /// <inheritdoc/>
    /// <remarks>Windows matches family names case-insensitively. Names of an installed family in any language
    /// canonicalize to the same English name.</remarks>
    public override bool Equals(object? obj) =>
        ReferenceEquals(this, obj)
        || (obj is SystemFontFamilyId other
            && string.Equals(this.Name.English, other.Name.English, StringComparison.OrdinalIgnoreCase));

    /// <inheritdoc/>
    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(this.Name.English);
}
