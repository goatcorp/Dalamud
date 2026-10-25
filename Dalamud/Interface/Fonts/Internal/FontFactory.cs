using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

using Dalamud.Bindings.ImGui;
using Dalamud.Configuration.Internal;
using Dalamud.Data;
using Dalamud.Interface.Fonts.Internal.Sources;
using Dalamud.Interface.Internal;
using Dalamud.Logging.Internal;
using Dalamud.Storage.Assets;
using Dalamud.Utility;

namespace Dalamud.Interface.Fonts.Internal;

/// <summary>Makes the fonts of font families.</summary>
/// <seealso cref="FontRegistry"/>
[ServiceManager.EarlyLoadedService]
internal sealed class FontFactory : IInternalDisposableService
{
    private static readonly ModuleLog Log = ModuleLog.Create<FontFactory>();

    private readonly FontRegistry registry;
    private readonly DefaultFontChain defaultChainMaker;
    private readonly Localization localization;

    // The font ids warned about as not found or failing to load, so that each is warned about once. By their text, so
    // that it keeps no ids or their data alive.
    private readonly ConcurrentDictionary<string, bool> warnedFonts = new();

    // By locale. Replaced as a whole on invalidation, so that invalidating never waits for a chain being made. Each is
    // also the lock for making its chains.
    private Dictionary<string, CompositeFont> defaultChains = [];

    // The font sources take these services while they are made, and the text stylers when they are first used.
    [ServiceManager.ServiceConstructor]
    private FontFactory(DataManager dataManager, DalamudAssetManager dalamudAssetManager, Localization localization)
    {
        this.registry = new();
        this.localization = localization;
        this.defaultChainMaker = new(this, this.registry);
        ImGui.InherentFont = FontSizes.InherentPush;
    }

    /// <summary>Raised when the default font chain changed, such as by settings or the UI language.</summary>
    /// <remarks>Fonts that end with the default font, or take the locale of the UI language, must be made again.
    /// </remarks>
    public event Action? DefaultChainChanged;

    /// <summary>Gets the default font style that is used if none is configured.</summary>
    public static TextStyle BuiltInDefaultTextStyle { get; } =
        TextStyle.From(GameFontFamily.Axis, StyleLength.Px(InterfaceManager.DefaultFontSizePx), fallback: false);

    /// <summary>Gets or sets the default font style to use instead of the configured one.</summary>
    public TextStyle? DefaultTextStyleOverride { get; set; }

    /// <summary>Gets the default font style. Only its family, size, size basis and line height are used.</summary>
    public TextStyle DefaultTextStyle =>
        this.DefaultTextStyleOverride
        ?? Service<DalamudConfiguration>.Get().DefaultTextStyle
        ?? BuiltInDefaultTextStyle;

    /// <summary>Gets the default font size in pixels (before global scaling).</summary>
    public float DefaultSizePx =>
        this.DefaultTextStyle.Size is { Unit: StyleUnit.Px, Value: > 0 } size
            ? size.Value
            : InterfaceManager.DefaultFontSizePx;

    /// <summary>Gets the locale of the UI language, which fonts are made in.</summary>
    /// <remarks>It is read when a font or a default chain is made.</remarks>
    public string InferredLocale => LocaleOf(this.localization.LanguageCode);

    /// <summary>Gets the face of a family that best matches a style.</summary>
    /// <param name="family">The family.</param>
    /// <param name="style">The style.</param>
    /// <returns>The family itself if it is a face, or else its face.</returns>
    public static IFontFaceId FaceOf(IFontFamilyId family, FontStyle style) =>
        family as IFontFaceId ?? (family is SystemFontFamilyId s
                                      ? new SystemFontFaceId(s, style)
                                      : family.Fonts[family.FindBestMatch(style)]);

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService() => this.registry.Dispose();

    /// <summary>Forgets the default font chains, and tells everything that uses them to make its fonts again.</summary>
    public void InvalidateDefaultChain()
    {
        Volatile.Write(ref this.defaultChains, []);
        this.DefaultChainChanged.InvokeSafely();
    }

    /// <summary>Gets the font a font face id stands for.</summary>
    /// <param name="id">The font id.</param>
    /// <param name="locale">The locale to draw text in.</param>
    /// <returns>The font, or null if no font source serves the id, such as for a font that is not installed.</returns>
    /// <remarks>The game symbols are AXIS while nothing serves them, as in the default chain.</remarks>
    public Font? GetSourceFont(IFontFaceId id, string locale) =>
        id is DalamudDefaultFontId
            ? this.GetDefaultChain(locale)
            : this.registry.GetSourceFont(id, locale)
              ?? (id.Equals(DefaultFontChain.GameSymbolsId)
                      ? this.registry.GetSourceFont(DefaultFontChain.GameSymbolsStandInId, locale)
                      : null);

    /// <summary>Makes the font of a family, in <see cref="InferredLocale"/>.</summary>
    /// <param name="family">The family.</param>
    /// <param name="report">Receives the fonts of the family that were loaded and left out, or null.</param>
    /// <returns>A new font, with shared sources.</returns>
    /// <remarks>A font of the family that is not found or fails to load is left out, with a warning once per font id.
    /// </remarks>
    public CompositeFont CreateFont(IFontFamilyId family, LoadReport? report = null)
    {
        if (family is DalamudDefaultFontId)
            return this.GetDefaultChain(this.InferredLocale);
        var elements = new List<CompositeFont.Element>();
        this.Append(elements, new(family), this.InferredLocale, false, report);
        return new(family.ToString() ?? nameof(CompositeFont), CollectionsMarshal.AsSpan(elements));
    }

    /// <summary>Adds the element of a family in a chain.</summary>
    /// <param name="target">The elements to add to.</param>
    /// <param name="element">The element.</param>
    /// <param name="locale">The locale.</param>
    /// <param name="skipDefault">Leave out the default font, for making the default font itself.</param>
    /// <param name="report">Receives the fonts that were loaded and left out, or null.</param>
    public void Append(
        List<CompositeFont.Element> target, ChainFamilyId.Element element, string locale, bool skipDefault,
        LoadReport? report = null)
    {
        Font? font;
        Exception? error = null;
        var family = element.Family;
        try
        {
            if (family is DalamudDefaultFontId)
            {
                if (skipDefault)
                    return;
                font = this.GetDefaultChain(locale);
            }
            else if (family is ChainFamilyId chain)
            {
                var inner = new List<CompositeFont.Element>();
                foreach (var e in chain.Elements)
                    this.Append(inner, e, locale, skipDefault, report);
                font = new CompositeFont(chain.ToString(), CollectionsMarshal.AsSpan(inner));
            }
            else
            {
                font = this.GetSourceFont(FaceOf(family, default), locale);
            }
        }
        catch (Exception e)
        {
            font = null;
            error = e;
        }

        // A font that is not found or fails to load is left out, so that the rest of the family still draws.
        if (font is null)
        {
            if (this.warnedFonts.TryAdd(family.ToString() ?? string.Empty, true))
            {
                if (error is null)
                    Log.Verbose("Font {Id} was not found; leaving it out", family);
                else
                    Log.Verbose(error, "Font {Id} failed to load; leaving it out", family);
            }

            report?.Errors.Add(new(family, error ?? new FileNotFoundException($"Font {family} was not found.")));
            return;
        }

        if (report is not null && family is not (DalamudDefaultFontId or ChainFamilyId))
            report.Loaded++;

        var offset = element.GlyphOffsetUnit == StyleUnit.Px;
        var spacing = element.LetterSpacing;
        var hasAdvance = element.Advance.Unit != StyleUnit.Default;
        target.Add(
            new(
                font.WithStyle(new() { Features = element.Features }),
                element.CodepointRanges.IsEmpty ? null : CompositeFont.ToRanges(element.CodepointRanges.AsSpan()),
                element.SizeScale,
                offset ? default : element.GlyphOffset,
                offset ? element.GlyphOffset : default,
                spacing.Unit == StyleUnit.Em ? spacing.Value : 0,
                spacing.Unit == StyleUnit.Px ? spacing.Value : 0,
                hasAdvance ? element.Advance : element.GlyphMinAdvanceX,
                hasAdvance ? element.Advance : element.GlyphMaxAdvanceX,
                FitToSquare: element.FitToSquare,
                Terminal: family is DalamudDefaultFontId));
    }

    // The locale of a Dalamud UI language, such as "ko-KR" for "ko". It is "en-US" for English, null, and languages not
    // in Localization.ApplicableLangCodes.
    private static string LocaleOf(string? language) =>
        language?.ToLowerInvariant() switch
        {
            "de" => "de-DE",
            "ja" => "ja-JP",
            "fr" => "fr-FR",
            "it" => "it-IT",
            "es" => "es-ES",
            "ko" => "ko-KR",
            "no" => "nb-NO",
            "ru" => "ru-RU",
            "zh" => "zh-CN",
            "tw" => "zh-TW",
            _ => "en-US",
        };

    // The default font with all its fallbacks, in a locale. It is made once per locale, until InvalidateDefaultChain.
    private CompositeFont GetDefaultChain(string locale)
    {
        var chains = Volatile.Read(ref this.defaultChains);
        lock (chains)
        {
            if (!chains.TryGetValue(locale, out var chain))
                chains.Add(locale, chain = this.defaultChainMaker.Create(this.DefaultTextStyle, locale));
            return chain;
        }
    }

    /// <summary>What became of the fonts of a family while its font was made.</summary>
    /// <remarks>The default font a family falls back to is not counted.</remarks>
    public sealed class LoadReport
    {
        /// <summary>Gets or sets the number of fonts of the family that were loaded.</summary>
        public int Loaded { get; set; }

        /// <summary>Gets the fonts of the family that were left out, each with the family it is for.</summary>
        public List<FontLoadException> Errors { get; } = [];

        /// <summary>Gets a value indicating whether the family has fonts of its own, and none of them were loaded.
        /// </summary>
        public bool NoneLoaded => this.Loaded == 0 && this.Errors.Count != 0;
    }
}
