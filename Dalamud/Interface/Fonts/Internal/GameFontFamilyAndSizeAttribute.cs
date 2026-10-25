namespace Dalamud.Interface.Fonts.Internal;

/// <summary>Marks the files of a game font size.</summary>
[AttributeUsage(AttributeTargets.Field)]
internal sealed class GameFontFamilyAndSizeAttribute(string path, int horizontalOffset) : Attribute
{
    /// <summary>Gets the inner path of the .fdt file.</summary>
    public string Path { get; } = path;

    /// <summary>Gets the horizontal offset of the glyphs.</summary>
    public int HorizontalOffset { get; } = horizontalOffset;
}
