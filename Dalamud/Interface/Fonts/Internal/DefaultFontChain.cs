using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

using Dalamud.Game.Text;
using Dalamud.Interface.Fonts.Internal.Sources;
using Dalamud.Logging.Internal;

namespace Dalamud.Interface.Fonts.Internal;

/// <summary>Makes the default font chain.</summary>
/// <remarks>The game symbols come first, the family of the default font style from Dalamud Settings next, and then
/// Windows' system font fallback.</remarks>
internal sealed class DefaultFontChain
{
    /// <summary>The font id of the game symbol font, from the local Dalamud assets.</summary>
    public static readonly DalamudAssetFontId GameSymbolsId = new(DalamudAsset.LodestoneGameSymbol);

    /// <summary>The font id that stands in for the game symbol font while it is missing and fills its gaps.</summary>
    public static readonly GameFontId GameSymbolsStandInId = new(GameFontFamily.Axis);

    private const string GameSymbolsFamilyName = "XIV AXIS Std ATK";

    // The em of the game symbol font, relative to that of the default font. A 12 px em matches AXIS at 16 px.
    private const float GameSymbolsSizeRatio = 0.75f;

    private static readonly ModuleLog Log = ModuleLog.Create<DefaultFontChain>();

    // The font id of the game symbol font, when it is installed.
    private static readonly SystemFontFaceId GameSymbolsInstalledId = new(new(GameSymbolsFamilyName), default);

    private static readonly (uint Low, uint High) GameSymbolsRange =
        ((uint)Enum.GetValues<SeIconChar>().Min(), (uint)Enum.GetValues<SeIconChar>().Max());

    private readonly FontFactory factory;

    /// <summary>Initializes a new instance of the <see cref="DefaultFontChain"/> class.</summary>
    /// <param name="factory">The factory to add the default font family with, and to get the other fonts from.</param>
    public DefaultFontChain(FontFactory factory) => this.factory = factory;

    /// <summary>Makes the default font chain.</summary>
    /// <param name="userStyle">The default font style.</param>
    /// <param name="locale">The locale.</param>
    /// <returns>The chain.</returns>
    /// <remarks>A step that throws is logged and left out.</remarks>
    public CompositeFont Create(TextStyle userStyle, string locale)
    {
        var elements = new List<CompositeFont.Element>();
        try
        {
            this.AddGameSymbols(elements, locale);
        }
        catch (Exception e)
        {
            Log.Verbose(e, "Failed to add the game symbols to the default font; ignoring");
        }

        var count = elements.Count;
        this.TryAppend(elements, userStyle, locale);
        if (elements.Count == count)
            this.TryAppend(elements, FontFactory.BuiltInDefaultTextStyle, locale);

        try
        {
            elements.Add(new(this.factory.GetSystemFallback(locale)));
        }
        catch (Exception e)
        {
            Log.Verbose(e, "Failed to add the system font fallback for {Locale}; ignoring", locale);
        }

        return new($"Default ({locale})", CollectionsMarshal.AsSpan(elements));
    }

    private void AddGameSymbols(List<CompositeFont.Element> elements, string locale)
    {
        var symbols = (DirectWriteUtilities.IsFamilyInstalled(GameSymbolsFamilyName)
                           ? this.TryGetFont(GameSymbolsInstalledId, locale)
                           : null)
                      ?? this.TryGetFont(GameSymbolsId, locale);
        if (symbols is not null)
            elements.Add(new(symbols, [GameSymbolsRange], GameSymbolsSizeRatio, Supplement: true));

        // Lodestone font lacks the boxed outlined numbers.
        if (this.TryGetFont(GameSymbolsStandInId, locale) is { } axis)
            elements.Add(new(axis, [GameSymbolsRange], Supplement: true));
    }

    private Font? TryGetFont(IFontFaceId id, string locale)
    {
        try
        {
            return this.factory.GetServedFont(id, locale);
        }
        catch (Exception e)
        {
            Log.Verbose(e, "Failed to load the game symbols from {Id}; ignoring", id);
            return null;
        }
    }

    // Adds the elements of the family of a style, in its faces, with its letter spacing split into ems of each element
    // and pixels, as FontComposer and CompositeFont.Flatten split it.
    private void TryAppend(List<CompositeFont.Element> elements, TextStyle style, string locale)
    {
        if (style.FamilyId is not { } family)
            return;
        var composer = new FontComposer(this.factory, locale, true, null);
        family.AppendTo(composer);

        var spacing = style.LetterSpacing;
        var em = spacing.Unit == StyleUnit.Em ? spacing.Value : 0;
        var px = spacing.Unit == StyleUnit.Px ? spacing.Value : 0;
        foreach (var e in composer.Elements)
        {
            elements.Add(
                e with
                {
                    Font = e.Font.WithStyle(style.FaceKey),
                    ExtraAdvanceX = e.ExtraAdvanceX + (em / e.SizeRatio),
                    ExtraAdvanceXPx = e.ExtraAdvanceXPx + px,
                });
        }
    }
}
