namespace Dalamud.Interface.GameFonts;

/// <summary>
/// Marks the path for an enum value.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
internal class GameFontFamilyAndSizeAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GameFontFamilyAndSizeAttribute"/> class.
    /// </summary>
    /// <param name="path">Inner path of the file.</param>
    /// <param name="texPathFormat">the file path format for the relevant .tex files.</param>
    /// <param name="horizontalOffset">Horizontal offset of the corresponding font.</param>
    public GameFontFamilyAndSizeAttribute(string path, string texPathFormat, int horizontalOffset)
    {
        this.Path = path;
        this.TexPathFormat = texPathFormat;
        this.HorizontalOffset = horizontalOffset;
    }

    /// <summary>
    /// Gets the path.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets the file path format for the relevant .tex files.<br />
    /// Used for <see cref="string.Format(string,object?)"/>(<see cref="TexPathFormat"/>, <see cref="int"/>).
    /// </summary>
    public string TexPathFormat { get; }

    /// <summary>
    /// Gets the horizontal offset of the corresponding font.
    /// </summary>
    public int HorizontalOffset { get; }
}
