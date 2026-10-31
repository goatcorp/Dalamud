namespace Dalamud.Interface.Fonts.Internal;

/// <summary>Marks the files of a game font size.</summary>
[AttributeUsage(AttributeTargets.Field)]
internal sealed class GameFontFamilyAndSizeAttribute(string path, string texPathFormat, int horizontalOffset)
    : Attribute
{
    /// <summary>Gets the inner path of the .fdt file.</summary>
    public string Path { get; } = path;

    /// <summary>Gets the format of the paths of the .tex files, with the file number as the argument.</summary>
    /// <seealso cref="string.Format(string, object?)"/>
    public string TexPathFormat { get; } = texPathFormat;

    /// <summary>Gets the horizontal offset of the glyphs.</summary>
    public int HorizontalOffset { get; } = horizontalOffset;
}
