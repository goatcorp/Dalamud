using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Dalamud.Interface.Fonts.Internal;

using Newtonsoft.Json;

namespace Dalamud.Interface.Fonts;

/// <summary>An ordered fallback chain of font families, where each family supplies glyphs missing from earlier ones.</summary>
/// <remarks>To adjust glyphs of a member family, wrap it first with
/// <see cref="IUiBuilder.RegisterWrappedFont"/>.</remarks>
/// <example><code>
/// // Comic Sans MS, then Gungsuh at 90% for Hangul, then the default font.
/// var gungsuh = uiBuilder.RegisterWrappedFont(
///     new FontAdjustments { SizeScale = 0.9f, CodepointRanges = [0xAC00, 0xD7A3] },
///     new SystemFontFamilyId("Gungsuh"));
/// var family = ChainFamilyId.Of(new SystemFontFamilyId("Comic Sans MS"), gungsuh, DalamudDefaultFontId.Instance);
/// </code></example>
public sealed class ChainFamilyId : IFontFamilyId, IEquatable<ChainFamilyId>
{
    private readonly EquatableArray<IFontFamilyId> elements;
    private int hashCode;

    /// <summary>Initializes a new instance of the <see cref="ChainFamilyId"/> class.</summary>
    /// <param name="elements">The families, in order of priority.</param>
    /// <exception cref="ArgumentException">If an element is <see langword="null"/>.</exception>
    [JsonConstructor]
    public ChainFamilyId(ImmutableArray<IFontFamilyId> elements)
    {
        if (!elements.IsDefault && elements.Contains(null!))
            throw new ArgumentException("An element is null.", nameof(elements));
        this.elements = new(elements);
    }

    /// <summary>Gets the families, in order of priority.</summary>
    [JsonProperty]
    public ImmutableArray<IFontFamilyId> Elements => this.elements.Array;

    /// <inheritdoc/>
    [JsonIgnore]
    public LocalizedName Name => field ??= new(string.Join(" + ", this.Elements.Select(e => e.Name.English)));

    /// <inheritdoc/>
    /// <remarks>The faces of the first family.</remarks>
    [JsonIgnore]
    public IReadOnlyList<IFontFaceId> Fonts => this.Elements.IsEmpty ? [] : this.Elements[0].Fonts;

    /// <inheritdoc/>
    bool IFontFamilyId.EndsWithDefault => this.Elements is [.., DalamudDefaultFontId];

    /// <inheritdoc/>
    IEnumerable<FontRegistration> IFontFamilyId.Registrations => this.Elements.SelectMany(static e => e.Registrations);

    public static bool operator ==(ChainFamilyId? left, ChainFamilyId? right) => Equals(left, right);

    public static bool operator !=(ChainFamilyId? left, ChainFamilyId? right) => !Equals(left, right);

    /// <summary>Gets a chain of families.</summary>
    /// <param name="families">The families, in order of priority.</param>
    /// <returns>The chain.</returns>
    /// <exception cref="ArgumentException">If a family is <see langword="null"/>.</exception>
    public static ChainFamilyId Of(params IFontFamilyId[] families) => new([.. families]);

    /// <inheritdoc/>
    public int FindBestMatch(FontStyle style) =>
        this.Elements.IsEmpty ? 0 : this.Elements[0].FindBestMatch(style);

    /// <inheritdoc/>
    void IFontFamilyId.AppendTo(FontComposer composer) => composer.AddComposite(this, this.Elements, default);

    /// <inheritdoc/>
    public bool Equals(ChainFamilyId? other) =>
        other is not null && (ReferenceEquals(this, other) || this.elements.Equals(other.elements));

    /// <inheritdoc/>
    public override bool Equals(object? obj) =>
        obj is ChainFamilyId other && this.Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() =>
        this.hashCode != 0 ? this.hashCode : this.hashCode = this.elements.GetHashCode();

    /// <inheritdoc/>
    /// <remarks>The short names of the families, as in <c>Chain[Segoe UI, Default]</c>.</remarks>
    public override string ToString() => $"Chain[{string.Join(", ", this.Elements.Select(static e => e.ShortName))}]";
}
