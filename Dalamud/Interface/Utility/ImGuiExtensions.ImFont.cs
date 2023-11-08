using System.Numerics;

using ImGuiNET;

namespace Dalamud.Interface.Utility;

/// <summary>
/// Class containing various extensions to ImGui, aiding with building custom widgets.
/// </summary>
// TODO: This should go into ImDrawList.Manual.cs in ImGui.NET...
public static partial class ImGuiExtensions
{
    /// <summary>
    /// Calculate text size.
    /// </summary>
    /// <param name="font">The font.</param>
    /// <param name="utf8String">The string.</param>
    /// <param name="remaining">Remaining portion of the given <paramref name="utf8String"/> outside the region.</param>
    /// <param name="fontSize">The size of the font. If a non-positive value is given. <see cref="ImFont.FontSize"/> is used.</param>
    /// <param name="maxWidth">The maximum width.</param>
    /// <param name="wrapWidth">The wrap width. If a non-positive value is given, no wrapping is performed.</param>
    /// <returns>Calculated text size.</returns>
    public static unsafe Vector2 CalcTextSize(
        this in ImFont font,
        Span<byte> utf8String,
        out Span<byte> remaining,
        float fontSize = float.NaN,
        float maxWidth = float.MaxValue,
        float wrapWidth = 0f)
    {
        fontSize = fontSize > 0 ? fontSize : font.FontSize;
        var val = default(Vector2);
        fixed (ImFont* pFont = &font)
        {
            fixed (byte* begin = utf8String)
            {
                byte* remainingBegin = null;
                ImGuiNative.ImFont_CalcTextSizeA(
                    &val,
                    pFont,
                    fontSize,
                    maxWidth,
                    wrapWidth,
                    begin,
                    begin + utf8String.Length,
                    &remainingBegin);
                remaining = utf8String[(int)(remainingBegin - begin)..];
            }
        }

        return val;
    }

    /// <summary>
    /// Calculate text size.
    /// </summary>
    /// <param name="font">The font.</param>
    /// <param name="utf8String">The string.</param>
    /// <param name="remaining">Remaining portion of the given <paramref name="utf8String"/> outside the region.</param>
    /// <param name="fontSize">The size of the font. If a non-positive value is given. <see cref="ImFont.FontSize"/> is used.</param>
    /// <param name="maxWidth">The maximum width.</param>
    /// <param name="wrapWidth">The wrap width. If a non-positive value is given, no wrapping is performed.</param>
    /// <returns>Calculated text size.</returns>
    public static unsafe Vector2 CalcTextSize(
        this ImFontPtr font,
        Span<byte> utf8String,
        out Span<byte> remaining,
        float fontSize = float.NaN,
        float maxWidth = float.MaxValue,
        float wrapWidth = 0f) => font.NativePtr->CalcTextSize(utf8String, out remaining, fontSize, maxWidth, wrapWidth);

    /// <summary>
    /// Calculate unbounded text size.
    /// </summary>
    /// <param name="font">The font.</param>
    /// <param name="utf8String">The string.</param>
    /// <param name="fontSize">The size of the font. If a non-positive value is given. <see cref="ImFont.FontSize"/> is used.</param>
    /// <param name="wrapWidth">The wrap width. If a non-positive value is given, no wrapping is performed.</param>
    /// <returns>Calculated text size.</returns>
    public static Vector2 CalcTextSize(
        this in ImFont font,
        Span<byte> utf8String,
        float fontSize = float.NaN,
        float wrapWidth = 0f) => font.CalcTextSize(utf8String, out _, fontSize: fontSize, wrapWidth: wrapWidth);

    /// <summary>
    /// Calculate unbounded text size.
    /// </summary>
    /// <param name="font">The font.</param>
    /// <param name="utf8String">The string.</param>
    /// <param name="fontSize">The size of the font. If a non-positive value is given. <see cref="ImFont.FontSize"/> is used.</param>
    /// <param name="wrapWidth">The wrap width. If a non-positive value is given, no wrapping is performed.</param>
    /// <returns>Calculated text size.</returns>
    public static unsafe Vector2 CalcTextSize(
        this ImFontPtr font,
        Span<byte> utf8String,
        float fontSize = float.NaN,
        float wrapWidth = 0f) => font.NativePtr->CalcTextSize(
        utf8String,
        out _,
        fontSize: fontSize,
        wrapWidth: wrapWidth);
}
