using Dalamud.Interface.Fonts.Internal.SeStrings;
using Dalamud.Logging.Internal;

namespace Dalamud.Interface.Fonts.Internal.Markup;

/// <summary>The styler of each markup.</summary>
/// <remarks>They are made on first use. One that fails to be made is logged and left out.</remarks>
internal static class TextStylers
{
    private static readonly ModuleLog Log = new(typeof(TextStylers));

    // By markup.
    private static readonly TextStyler?[] Stylers =
    [
        Create(static () => new PlainTextStyler()),
        Create(static () => new SeStringStyler()),
        Create(static () => new MacroStringStyler()),
    ];

    /// <summary>Gets the styler of a markup.</summary>
    /// <param name="markup">The markup.</param>
    /// <returns>The styler, or null to lay the text out as UTF-8.</returns>
    public static TextStyler? Get(TextMarkup markup) =>
        (uint)markup < (uint)Stylers.Length ? Stylers[(int)markup] : null;

    private static TextStyler? Create(Func<TextStyler> create)
    {
        try
        {
            return create();
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to make a text styler; ignoring");
            return null;
        }
    }
}
