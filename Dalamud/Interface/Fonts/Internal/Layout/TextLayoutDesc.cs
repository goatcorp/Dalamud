using Dalamud.Bindings.ImGui;

namespace Dalamud.Interface.Fonts.Internal.Layout;

/// <summary>Parameters that determine a <see cref="TextLayout"/>.</summary>
/// <param name="Size">The ImGui font size, which is the line box, in pixels.</param>
/// <param name="EmSize">The glyph size in pixels.</param>
/// <param name="GlobalScale">The global scale, for lengths in <see cref="StyleUnit.Px"/>.</param>
/// <param name="Density">Device pixels per logical pixel.</param>
/// <param name="LineHeight">Line advance in pixels if positive, as a multiple of <paramref name="Size"/> if negative,
/// or <c>0</c> for <paramref name="Size"/>.</param>
/// <param name="WrapWidth">Wrap width, or <c>0</c> or less for no wrapping.</param>
/// <param name="MaxWidth">Width to stop or truncate at, or <c>0</c> or less for unlimited.</param>
/// <param name="Flags">Layout flags.</param>
/// <param name="MaskChar">Character for <see cref="ImTextLayoutFlags.PasswordMask"/>, or <c>0</c> for <c>'*'</c>.
/// </param>
/// <param name="TabSize">Spaces between tab stops, counted from the start of the line, or <c>0</c> for <c>4</c>.
/// </param>
/// <param name="MaxLines">Maximum line count, or <c>0</c> or less for unlimited. Truncates the last line with an
/// ellipsis if more text remains.</param>
/// <param name="Align">Where lines go across the wrap width, or across the widest line without wrapping. <c>0</c> is
/// left, <c>0.5</c> is centered, and <c>1</c> is right.</param>
/// <remarks>The line height works as CSS <c>line-height</c>, and may be less than the text.</remarks>
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
    int TabSize = 0,
    int MaxLines = 0,
    float Align = 0)
{
    /// <summary>Gets a normalized copy where equivalent parameter values compare equal for cache keys.</summary>
    public TextLayoutDesc Canonical => this with
    {
        Density = this.Density > 0 ? this.Density : 1,
        LineHeight = this.LineHeight is > 0 or (< 0 and not -1) ? this.LineHeight : 0,
        WrapWidth = this.WrapWidth > 0 ? this.WrapWidth : 0,
        MaxWidth = this.MaxWidth is > 0 and < float.MaxValue ? this.MaxWidth : 0,
        MaskChar = (this.Flags & ImTextLayoutFlags.PasswordMask) == 0 ? 0 : this.MaskChar != 0 ? this.MaskChar : '*',
        TabSize = this.TabSize is > 0 and not 4 ? this.TabSize : 0,
        MaxLines = Math.Max(this.MaxLines, 0),
        Align = this.Align is > 0 ? Math.Min(this.Align, 1) : 0,
    };

    /// <summary>Converts native <see cref="ImTextDesc"/> parameters for <paramref name="font"/>.
    /// <see cref="ImTextDesc.Locale"/> is ignored because <paramref name="font"/> carries the locale.</summary>
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
            desc.MaskChar,
            0,
            desc.MaxLines,
            desc.Align);
}
