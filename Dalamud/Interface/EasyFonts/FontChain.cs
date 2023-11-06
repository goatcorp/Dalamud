using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using Newtonsoft.Json;

namespace Dalamud.Interface.EasyFonts;

/// <summary>
/// Indicates a whole font chain.
/// </summary>
[Serializable]
public record struct FontChain
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FontChain"/> class.
    /// </summary>
    public FontChain()
    {
        this.FontsNullable = ImmutableList<FontChainEntry>.Empty;
        this.LineHeight = 1f;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FontChain"/> class.
    /// </summary>
    /// <param name="fonts">Fonts to include.</param>
    /// <param name="lineHeight">Ratio of the line height.</param>
    public FontChain(IEnumerable<FontChainEntry> fonts, float lineHeight = 1f)
    {
        this.FontsNullable = fonts.ToImmutableList();
        this.LineHeight = lineHeight;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FontChain"/> class.
    /// </summary>
    /// <param name="font">Font to include.</param>
    /// <param name="lineHeight">Ratio of the line height.</param>
    public FontChain(FontChainEntry font, float lineHeight = 1f)
        : this(new[] { font }, lineHeight)
    {
    }

    /// <summary>
    /// Gets or sets the entries in the font chain.
    /// </summary>
    public ImmutableList<FontChainEntry>? FontsNullable { get; set; }

    /// <summary>
    /// Gets the entries in the font chain, in a non-null manner.
    /// </summary>
    [JsonIgnore]
    public ImmutableList<FontChainEntry> Fonts => this.FontsNullable ?? ImmutableList<FontChainEntry>.Empty;
    
    /// <summary>
    /// Gets or sets the ratio of line height of the final font, relative to the first font of the chain.
    /// </summary>
    public float LineHeight { get; set; }

    /// <inheritdoc cref="object.Equals(object?)" />
    [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator", Justification = "It's an Equals function")]
    public bool Equals(FontChain? other) =>
        other is { } o && this.Fonts.SequenceEqual(o.Fonts) && this.LineHeight == o.LineHeight;
}
