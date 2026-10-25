using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

using Dalamud.Bindings.ImGui;
using Dalamud.Configuration.Internal;
using Dalamud.Data;
using Dalamud.Interface.Fonts.Internal.DirectWrite;
using Dalamud.Interface.Fonts.Internal.GameFonts;
using Dalamud.Interface.Fonts.Internal.Sources;
using Dalamud.Interface.Internal;
using Dalamud.Storage.Assets;
using Dalamud.Utility;

namespace Dalamud.Interface.Fonts.Internal;

/// <summary>Makes the fonts of font families.</summary>
/// <remarks>It owns the font sources, which are looked up when fonts are made, never per frame.</remarks>
[ServiceManager.EarlyLoadedService]
internal sealed class FontFactory : IInternalDisposableService
{
    private readonly GameFontSources gameFonts = new();
    private readonly DirectWriteFontSources directWriteFonts = new();
    private readonly DefaultFontChain defaultChainMaker;
    private readonly Localization localization;

    // By locale. Replaced as a whole on invalidation, so that invalidating never waits for a chain being made. Each is
    // also the lock for making its chains.
    private Dictionary<string, CompositeFont> defaultChains = [];

    // Unused parameters ensure ServiceManager initializes DataManager and DalamudAssetManager before FontFactory.
    [ServiceManager.ServiceConstructor]
    private FontFactory(DataManager dataManager, DalamudAssetManager dalamudAssetManager, Localization localization)
    {
        this.localization = localization;
        this.defaultChainMaker = new(this);
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

    /// <summary>Gets the BCP-47 locale mapped from the current Dalamud UI language.</summary>
    /// <remarks>It is read when a font or a default chain is made.</remarks>
    public string InferredLocale => LocaleOf(this.localization.LanguageCode);

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService() => this.directWriteFonts.Dispose();

    /// <summary>Clears cached default font chains and notifies dependents to rebuild their fonts.</summary>
    public void InvalidateDefaultChain()
    {
        Volatile.Write(ref this.defaultChains, []);
        this.DefaultChainChanged.InvokeSafely();
    }

    /// <summary>Drops cached system fallbacks and rebuilds fonts after refreshing installed system fonts.</summary>
    /// <remarks>The new fonts have new ids, so that no layout or pick of the old fallbacks is used.</remarks>
    /// <seealso cref="DirectWriteUtilities.ListFamilies"/>
    public void OnSystemFontsRefreshed()
    {
        this.directWriteFonts.DropSystemFallbacks();

        // It may be called on any thread. The rebuild invalidates the chains before a frame.
        if (Service<InterfaceManager>.GetNullable() is { } interfaceManager)
            interfaceManager.RebuildFonts();
        else
            this.InvalidateDefaultChain();
    }

    /// <summary>Gets the font of a font face id from the font source that serves it.</summary>
    /// <param name="id">The font id.</param>
    /// <param name="locale">The locale to draw text in.</param>
    /// <returns>The font, or null if no source serves the id.</returns>
    /// <remarks>Throws if a matching source fails to load. Does not fall back to the default chain or stand-ins.</remarks>
    public Font? GetServedFont(IFontFaceId id, string locale) =>
        this.gameFonts.GetFont(id) ?? this.directWriteFonts.GetFont(id, locale);

    /// <inheritdoc cref="DirectWriteFontSources.GetSystemFallback"/>
    public Font GetSystemFallback(string locale) => this.directWriteFonts.GetSystemFallback(locale);

    /// <summary>Makes the font of a family, in <see cref="InferredLocale"/>.</summary>
    /// <param name="family">The family.</param>
    /// <param name="report">Receives the fonts of the family that were loaded and left out, or null.</param>
    /// <returns>A new font, with shared sources.</returns>
    /// <remarks>A font of the family that is not found or fails to load is left out, with a warning once per file or
    /// installed family.</remarks>
    public CompositeFont CreateFont(IFontFamilyId family, LoadReport? report = null)
    {
        if (family is DalamudDefaultFontId)
            return this.GetDefaultChain(this.InferredLocale);
        var composer = new FontComposer(this, this.InferredLocale, false, report);
        foreach (var e in family is ChainFamilyId c ? c.Elements : [family])
            e.AppendTo(composer);
        return new(family.ToString() ?? nameof(CompositeFont), CollectionsMarshal.AsSpan(composer.Elements));
    }

    /// <summary>Gets the default font with all its fallbacks, in a locale.</summary>
    /// <param name="locale">The locale.</param>
    /// <returns>The font. It is made once per locale, until <see cref="InvalidateDefaultChain"/>.</returns>
    public CompositeFont GetDefaultChain(string locale)
    {
        var chains = Volatile.Read(ref this.defaultChains);
        lock (chains)
        {
            if (!chains.TryGetValue(locale, out var chain))
                chains.Add(locale, chain = this.defaultChainMaker.Create(this.DefaultTextStyle, locale));
            return chain;
        }
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

    /// <summary>Tracks loaded and failed member fonts while composing a family.</summary>
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
