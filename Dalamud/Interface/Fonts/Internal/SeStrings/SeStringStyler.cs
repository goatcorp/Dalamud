using Dalamud.Interface.Fonts.Internal.Layout;
using Dalamud.Interface.Fonts.Internal.Markup;

namespace Dalamud.Interface.Fonts.Internal.SeStrings;

/// <summary>Reads SeStrings.</summary>
/// <remarks>Layouts draw their icons and entities, and make their links react.</remarks>
internal class SeStringStyler : TextStyler
{
    private readonly SeStringParser parser = new();

    // Populated by GetExtraKeys immediately before ParseSeString runs on a cache miss.
    private int keyTheme;

    /// <inheritdoc/>
    public override TextMarkup Markup => TextMarkup.SeString;

    /// <inheritdoc/>
    protected override int ExtraKeyCount => 2;

    /// <inheritdoc/>
    protected override void Parse(
        FontImpl font, in TextLayoutDesc desc, ReadOnlySpan<byte> text, ITextMarkupCallback[] callbacks, bool nested,
        StyledText output) =>
        this.ParseSeString(font, desc, [.. text], callbacks, nested, output);

    /// <inheritdoc/>
    /// <remarks>The theme the callbacks ask for, and the gamepad icons. Global numbers are not, as they may change
    /// often. So a text with <c>&lt;color(gnum1)&gt;</c> keeps the color of when it was laid out, while its layout is
    /// cached.</remarks>
    protected override void GetExtraKeys(ReadOnlySpan<ITextMarkupCallback> callbacks, Span<long> keys)
    {
        keys[0] = this.keyTheme = ThemeIndexOf(callbacks);
        keys[1] = SeStringGameData.IconRemapKey();
    }

    /// <summary>Reads a SeString.</summary>
    /// <param name="font">The font, whose style the text starts with.</param>
    /// <param name="desc">Layout parameters.</param>
    /// <param name="seString">The SeString, which becomes <see cref="StyledText.Source"/>.</param>
    /// <param name="callbacks">The callbacks, in order.</param>
    /// <param name="nested">Whether the parser of this styler is in use.</param>
    /// <param name="output">Receives the styled text, cleared.</param>
    protected void ParseSeString(
        FontImpl font, in TextLayoutDesc desc, byte[] seString, ITextMarkupCallback[] callbacks, bool nested,
        StyledText output)
    {
        // Without callbacks, as after a failed parse, the theme is the game's.
        var theme = callbacks.Length == 0 ? SeStringGameData.GameThemeIndex() : this.keyTheme;
        var reader = nested ? new() : this.parser;
        reader.Parse(seString, this.Markup, font, desc, callbacks, theme, output);
    }

    // The theme the callbacks ask for, in order, or that the game is set to. A callback that throws has no say.
    private static int ThemeIndexOf(ReadOnlySpan<ITextMarkupCallback> callbacks)
    {
        foreach (var callback in callbacks)
        {
            if (callback is ISeStringCallback seStringCallback && AskThemeIndex(seStringCallback) is { } theme)
                return theme;
        }

        return SeStringGameData.GameThemeIndex();
    }

    // Outlined to keep ThemeIndexOf free of try/catch overhead when no ISeStringCallback is pushed.
    private static int? AskThemeIndex(ISeStringCallback callback)
    {
        try
        {
            return callback.ThemeIndex;
        }
        catch (Exception e)
        {
            StyledTextHook.ReportCallbackFailure(callback.GetType(), e);
            return null;
        }
    }
}
