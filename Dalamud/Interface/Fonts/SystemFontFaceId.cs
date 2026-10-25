using System.Collections.Generic;
using System.Collections.Immutable;

using Dalamud.Interface.Fonts.Internal;

using Newtonsoft.Json;

namespace Dalamud.Interface.Fonts;

/// <summary>
/// Represents a font installed in the system.
/// </summary>
public sealed class SystemFontFaceId : IFontFaceId
{
    private LocalizedName? name;

    /// <summary>Initializes a new instance of the <see cref="SystemFontFaceId"/> class.</summary>
    /// <param name="family">The parent font family.</param>
    /// <param name="style">The style of the face.</param>
    /// <param name="isSymbolFont">Whether the face is a symbol font.</param>
    /// <param name="name">The face name, or <see langword="null"/> to derive it lazily from <paramref name="style"/>.</param>
    internal SystemFontFaceId(
        SystemFontFamilyId family, FontStyle style, bool isSymbolFont = false, LocalizedName? name = null)
    {
        this.Family = family;
        this.Style = style;
        this.IsSymbolFont = isSymbolFont;
        this.name = name;
    }

    [JsonConstructor]
    private SystemFontFaceId(string? englishName, IReadOnlyDictionary<string, string>? localeNames, IFontFamilyId family)
    {
        this.name = new(englishName ?? string.Empty, localeNames);
        this.Family = family;
        this.Style = default;
    }

    /// <inheritdoc/>
    [JsonIgnore]
    public LocalizedName Name => this.name ??= new(this.Style.ToString());

    /// <inheritdoc/>
    [JsonProperty]
    public IFontFamilyId Family { get; init; }

    /// <inheritdoc/>
    [JsonProperty("FaceStyle")]
    public FontStyle Style
    {
        get;
        init => field = new(value.EffectiveWeight, value.Italicized, value.EffectiveStretch);
    }

    /// <inheritdoc/>
    /// <remarks>A symbol font draws dingbats for the letters of its name.</remarks>
    bool IFontFaceId.PreviewsName => !this.IsSymbolFont;

    /// <summary>Gets a value indicating whether the face is a symbol font.</summary>
    /// <remarks>Best-effort basis.</remarks>
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    internal bool IsSymbolFont { get; private init; }

    [JsonProperty("EnglishName")]
    private string SavedEnglishName => this.Name.English;

    [JsonProperty("LocaleNames")]
    private IReadOnlyDictionary<string, string>? SavedLocaleNames => this.Name.ByLocale;

    // Legacy JSON property written before FaceStyle existed.
    [JsonProperty("Weight")]
    private int SavedWeight
    {
        init => this.Style = this.Style with { Weight = value };
    }

    // Legacy JSON property written before FaceStyle existed.
    [JsonProperty("Stretch")]
    private int SavedStretch
    {
        init => this.Style = this.Style with { Stretch = value };
    }

    // Legacy JSON property for DirectWrite DWRITE_FONT_STYLE values written before FaceStyle existed.
    [JsonProperty("Style")]
    private int SavedStyle
    {
        init => this.Style = this.Style with { Italicized = value != 0 };
    }

    public static bool operator ==(SystemFontFaceId? left, SystemFontFaceId? right) => Equals(left, right);

    public static bool operator !=(SystemFontFaceId? left, SystemFontFaceId? right) => !Equals(left, right);

    /// <inheritdoc/>
    public override bool Equals(object? obj) =>
        ReferenceEquals(this, obj)
        || (obj is SystemFontFaceId other && Equals(this.Family, other.Family) && this.Style == other.Style);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(this.Family, this.Style);

    /// <inheritdoc/>
    public override string ToString() => $"{nameof(SystemFontFaceId)}:{this.Style}:{this.Family}";

    /// <summary>Gets the file containing this font, the font index within, and the axis values of a named instance of a
    /// variable font, such as Bahnschrift Bold.</summary>
    /// <returns>The path, the index, and the axis values, empty for other fonts.</returns>
    /// <exception cref="System.IO.FileNotFoundException">If the family is not installed.</exception>
    /// <exception cref="NotSupportedException">If the face is not backed by a single file on disk.</exception>
    /// <seealso cref="FontSource.FromFile"/>
    public (string Path, int Index, ImmutableArray<FontAxis> Axes) GetFileAndIndex() =>
        DirectWriteUtilities.GetFileAndIndex(this);
}
