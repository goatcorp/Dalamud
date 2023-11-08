using System.Linq;
using System.Text.Unicode;

namespace Dalamud.Interface.EasyFonts;

/// <summary>
/// Indicates an entry in a font chain.
/// </summary>
[Serializable]
public record struct FontChainEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FontChainEntry"/> class.
    /// </summary>
    public FontChainEntry()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FontChainEntry"/> class.
    /// </summary>
    /// <param name="ident">Identifier of the font.</param>
    /// <param name="sizePx">Size of the font.</param>
    /// <param name="letterSpacing">Letter spacing of the font.</param>
    /// <param name="offsetX">X offset of the font.</param>
    /// <param name="offsetY">Y offset of the font.</param>
    public FontChainEntry(FontIdent ident, float sizePx, int letterSpacing = 0, float offsetX = 0f, float offsetY = 0f)
    {
        this.Ident = ident;
        this.SizePx = sizePx;
        this.LetterSpacing = letterSpacing;
        this.OffsetX = offsetX;
        this.OffsetY = offsetY;
    }

    /// <summary>
    /// Gets or sets the identifier of the font.
    /// </summary>
    public FontIdent Ident { get; set; }

    /// <summary>
    /// Gets or sets the size of the font.
    /// </summary>
    public float SizePx { get; set; }

    /// <summary>
    /// Gets or sets the letter spacing.
    /// </summary>
    public float LetterSpacing { get; set; }

    /// <summary>
    /// Gets or sets the horizontal offset.
    /// </summary>
    public float OffsetX { get; set; }

    /// <summary>
    /// Gets or sets the vertical offset.
    /// </summary>
    public float OffsetY { get; set; }

    /// <summary>
    /// Gets or sets the unicode ranges to take from this font. Null means full range.
    /// </summary>
    public UnicodeRange[]? Ranges { get; set; }

    /// <inheritdoc/>
    public override int GetHashCode() => 
        HashCode.Combine(this.Ident.GetHashCode(), this.SizePx, this.LetterSpacing, this.OffsetX, this.OffsetY);

    /// <summary>
    /// Determines if <see cref="Ranges"/> contains <paramref name="c"/>.
    /// </summary>
    /// <param name="c">Character to test.</param>
    /// <returns>Whether it's contained.</returns>
    public readonly bool RangeContainsCharacter(int c) =>
        this.Ranges is not { } ranges
        || ranges.Any(x => x.FirstCodePoint <= c && c < x.FirstCodePoint + x.Length);

    /// <inheritdoc/>
    public override string ToString() =>
        $"{this.Ident} ({this.SizePx}px"
        + (this.OffsetX != 0 || this.OffsetY != 0 ? $", off=({this.OffsetX}, {this.OffsetY})" : string.Empty)
        + (this.LetterSpacing != 0 ? $", ls={this.LetterSpacing}" : string.Empty)
        + (this.Ranges is { } ranges ? $", ranges={ranges.Length}" : string.Empty)
        + ")";
}
