using System.Numerics;
using System.Threading.Tasks;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Fonts.Internal;

using Lumina.Text.ReadOnly;

namespace Dalamud.Interface.Fonts;

public partial record struct TextStyle
{
    /// <summary>Pushes the font of this style into the ImGui font stack, loading its family if needed.</summary>
    /// <returns>A disposable object that pops the font.</returns>
    /// <remarks><ul>
    /// <li>A default <see cref="Size"/> keeps the current size, and a default <see cref="LineHeight"/> the current line
    /// height.</li>
    /// <li>It does not wait for the load. Until the family is loaded, and if it could not be loaded, the text is drawn
    /// with the default font. Use <see cref="GetFont"/> to load ahead.</li>
    /// </ul></remarks>
    /// <seealso cref="PushFamily"/>
    public readonly ImPopFontDisposable Push() => Service<FontResolver>.Get().Push(this);

    /// <summary>Pushes the current style with the family of this one.</summary>
    /// <returns>A disposable object that pops the font.</returns>
    /// <seealso cref="Push"/>
    public readonly ImPopFontDisposable PushFamily() =>
        ImGui.PushFamily(this.FamilyOrDefault);

    /// <summary>Measures text in this style, as <see cref="ImGui.CalcTextSize(ImU8String, bool, float)"/> does.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="wrapWidth">The width to wrap at, or a negative number for no wrapping.</param>
    /// <returns>The size of the text.</returns>
    public readonly Vector2 CalcTextSize(ImU8String text, float wrapWidth = -1)
    {
        using (this.Push())
            return ImGui.CalcTextSize(text, false, wrapWidth);
    }

    /// <inheritdoc cref="ImGuiTextExtensions.extension(ImGui).CreateTextLayout(ReadOnlySpan{byte},ReadOnlySpan{TextStyleRange},float,int,ImTextLayoutFlags)"/>
    public readonly ImTextLayoutRef CreateTextLayout(
        ReadOnlySpan<byte> text, ReadOnlySpan<TextStyleRange> ranges = default, float wrapWidth = 0,
        int tabSize = 0, ImTextLayoutFlags flags = ImTextLayoutFlags.None)
    {
        using (this.Push())
            return ImGui.CreateTextLayout(text, ranges, wrapWidth, tabSize, flags);
    }

    /// <inheritdoc cref="ImGuiTextExtensions.extension(ImGui).CreateTextLayout(ReadOnlySpan{char},ReadOnlySpan{TextStyleRange},float,int,ImTextLayoutFlags)"/>
    public readonly ImTextLayoutRef CreateTextLayout(
        ReadOnlySpan<char> text, ReadOnlySpan<TextStyleRange> ranges = default, float wrapWidth = 0,
        int tabSize = 0, ImTextLayoutFlags flags = ImTextLayoutFlags.None)
    {
        using (this.Push())
            return ImGui.CreateTextLayout(text, ranges, wrapWidth, tabSize, flags);
    }

    /// <inheritdoc cref="ImGuiTextExtensions.extension(ImGui).CreateTextLayout(ReadOnlySpan{byte},TextMarkup,float,int,ImTextLayoutFlags)"/>
    public readonly ImTextLayoutRef CreateTextLayout(
        ReadOnlySpan<byte> text, TextMarkup markup, float wrapWidth = 0, int tabSize = 0,
        ImTextLayoutFlags flags = ImTextLayoutFlags.None)
    {
        using (this.Push())
            return ImGui.CreateTextLayout(text, markup, wrapWidth, tabSize, flags);
    }

    /// <inheritdoc cref="ImGuiTextExtensions.extension(ImGui).CreateTextLayout(ReadOnlySeStringSpan,float,int,ImTextLayoutFlags)"/>
    public readonly ImTextLayoutRef CreateTextLayout(
        ReadOnlySeStringSpan text, float wrapWidth = 0, int tabSize = 0,
        ImTextLayoutFlags flags = ImTextLayoutFlags.None)
    {
        using (this.Push())
            return ImGui.CreateTextLayout(text, wrapWidth, tabSize, flags);
    }

    /// <summary>Gets the font of this style, starting to load its family in the background if needed.</summary>
    /// <param name="retry">Whether to load again if an earlier attempt failed or left fonts of the family out.</param>
    /// <returns>The font, at once. <see cref="ImFontRef.LoadTask"/> completes when it is loaded.</returns>
    /// <remarks>It does not wait. Keep the result to keep the font loaded.</remarks>
    public readonly ImFontRef GetFont(bool retry = false) => Service<FontResolver>.Get().Get(this, retry);

    /// <summary>Gets the font of this style once it is loaded, loading its family in the background if needed.
    /// </summary>
    /// <param name="retry">Whether to load again if an earlier attempt failed or left fonts of the family out.</param>
    /// <returns>The font. Fonts of the family that are not found or fail to load are left out, and listed in
    /// <see cref="ImFontRef.LoadErrors"/>.</returns>
    /// <exception cref="FontLoadException">If none of the fonts of the family could be loaded. Its inner exception
    /// says why, as one <see cref="FontLoadException"/> per font, in an <see cref="AggregateException"/> if there
    /// are several.</exception>
    /// <remarks>Pushing and drawing never fail this way. A font that could not be loaded is drawn with the default
    /// font.</remarks>
    public readonly async Task<ImFontRef> GetFontAsync(bool retry = false)
    {
        var font = this.GetFont(retry);
        await font.LoadTask.ConfigureAwait(false);
        return font;
    }
}
