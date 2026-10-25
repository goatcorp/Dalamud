using Dalamud.Bindings.ImGui;

namespace Dalamud.Interface.Fonts.Internal.Layout;

/// <summary>Everything that changes the result of layout.</summary>
/// <param name="Size">The ImGui font size, which is the line box, in pixels.</param>
/// <param name="EmSize">The glyph size in pixels.</param>
/// <param name="GlobalScale">The global scale, for lengths in <see cref="StyleUnit.Px"/>.</param>
/// <param name="Density">Device pixels per logical pixel.</param>
/// <param name="LineHeight">Line advance in pixels if positive, as a multiple of <paramref name="Size"/> if negative,
/// or 0 for <paramref name="Size"/>.</param>
/// <param name="WrapWidth">Wrap width, or 0 or less for no wrapping.</param>
/// <param name="MaxWidth">Width to stop or truncate at, or 0 or less for unlimited.</param>
/// <param name="Flags">Layout flags.</param>
/// <param name="MaskChar">Character for <see cref="ImTextLayoutFlags.PasswordMask"/>, or 0 for '*'.</param>
/// <param name="TabSize">Spaces between tab stops, counted from the start of the line, or 0 for 4.</param>
/// <remarks>The line height works as CSS line-height, and may be less than the text.</remarks>
/// <seealso cref="ImTextLayoutFlags.Ellipsis"/>
internal readonly record struct TextLayoutDesc(
    float Size,
    float EmSize,
    float GlobalScale = 1,
    float Density = 1,
    float LineHeight = 0,
    float WrapWidth = 0,
    float MaxWidth = 0,
    ImTextLayoutFlags Flags = ImTextLayoutFlags.None,
    uint MaskChar = 0,
    int TabSize = 0)
{
    /// <summary>Gets the parameters with one value for each meaning, so that parameters that lay out alike are equal.
    /// </summary>
    public TextLayoutDesc Canonical => this with
    {
        Density = this.Density > 0 ? this.Density : 1,
        WrapWidth = this.WrapWidth > 0 ? this.WrapWidth : 0,
        MaxWidth = this.MaxWidth is > 0 and < float.MaxValue ? this.MaxWidth : 0,
        MaskChar = (this.Flags & ImTextLayoutFlags.PasswordMask) == 0 ? 0 : this.MaskChar != 0 ? this.MaskChar : '*',
        TabSize = this.TabSize is > 0 and not 4 ? this.TabSize : 0,
    };

    /// <summary>Makes the parameters ImGui asks for. The locale is not used.</summary>
    /// <param name="desc">ImGui's parameters.</param>
    /// <param name="font">The font the text is laid out in.</param>
    /// <returns>The parameters.</returns>
    public static TextLayoutDesc FromNative(in ImTextDesc desc, FontImpl font) =>
        new(
            desc.Size,
            desc.Size / font.LineRatio,
            FontSizes.GlobalScale,
            desc.Density,
            desc.LineHeight,
            desc.WrapWidth,
            desc.MaxWidth >= float.MaxValue ? 0 : desc.MaxWidth,
            desc.Flags,
            desc.MaskChar);
}
