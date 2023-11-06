using Dalamud.Interface.GameFonts;

namespace Dalamud.Interface.EasyFonts;

/// <summary>
/// Indicates a font family and variant, or a font path and inner font index inside a font file.
/// </summary>
/// <remarks>
/// If a font file and the inner index are specified, and the corresponding file exists,
/// font family name and variant information will not be used.
/// </remarks>
[Serializable]
public record struct FontIdent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FontIdent"/> class.
    /// </summary>
    public FontIdent()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FontIdent"/> class.
    /// </summary>
    /// <param name="useNotoSansJ">Whether to use the fallback font.</param>
    public FontIdent(bool useNotoSansJ) => this.NotoSansJ = useNotoSansJ;

    /// <summary>
    /// Initializes a new instance of the <see cref="FontIdent"/> class.
    /// </summary>
    /// <param name="game">Game font family to use.</param>
    public FontIdent(GameFontFamily game) => this.Game = game;

    /// <summary>
    /// Initializes a new instance of the <see cref="FontIdent"/> class.
    /// </summary>
    /// <param name="path">Path of the font file.</param>
    /// <param name="index">Index of the font within.</param>
    public FontIdent(string path, int index) => this.File = (path, index);

    /// <summary>
    /// Initializes a new instance of the <see cref="FontIdent"/> class.
    /// </summary>
    /// <param name="name">Name of the font family. A font may have multiple names, and it can be one of those.</param>
    /// <param name="variant">Variant of the font.</param>
    public FontIdent(string name, FontVariant variant) => this.System = (name, variant);

    /// <summary>
    /// Initializes a new instance of the <see cref="FontIdent"/> class.
    /// </summary>
    /// <param name="path">Path of the font file.</param>
    /// <param name="index">Index of the font within.</param>
    /// <param name="name">Name of the font family. A font may have multiple names, and it can be one of those.</param>
    /// <param name="variant">Variant of the font.</param>
    public FontIdent(string path, int index, string name, FontVariant variant)
    {
        this.File = (path, index);
        this.System = (name, variant);
    }

    /// <summary>
    /// Gets or sets a value indicating whether to use the fallback font.
    /// </summary>
    public bool NotoSansJ { get; set; } = false;

    /// <summary>
    /// Gets or sets the game font.
    /// </summary>
    public GameFontFamily Game { get; set; } = GameFontFamily.Undefined;

    /// <summary>
    /// Gets or sets the path of the font file and the font index within.
    /// </summary>
    public (string Path, int Index)? File { get; set; }

    /// <summary>
    /// Gets or sets the name and variant of a font.
    /// </summary>
    public (string Name, FontVariant Variant)? System { get; set; }
}
