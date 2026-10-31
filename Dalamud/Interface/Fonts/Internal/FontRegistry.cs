using Dalamud.Interface.Fonts.Internal.DirectWrite;
using Dalamud.Interface.Fonts.Internal.GameFonts;
using Dalamud.Interface.Fonts.Internal.Sources;

namespace Dalamud.Interface.Fonts.Internal;

/// <summary>The font sources of the font engine.</summary>
/// <remarks>It is looked up when fonts are made, never per frame.</remarks>
internal sealed class FontRegistry : IDisposable
{
    private readonly GameFontSources gameFonts = new();
    private readonly DirectWriteFontSources directWriteFonts = new();

    /// <summary>Gets the font of a font id from the source that serves it.</summary>
    /// <param name="id">The font id.</param>
    /// <param name="locale">The locale to draw text in.</param>
    /// <returns>The font, or null if no source serves the id.</returns>
    /// <remarks>A font that is there but fails to load throws.</remarks>
    public Font? GetSourceFont(IFontFaceId id, string locale) =>
        this.gameFonts.GetFont(id) ?? this.directWriteFonts.GetFont(id, locale);

    /// <inheritdoc cref="DirectWriteFontSources.GetSystemFallback"/>
    public Font GetSystemFallback(string locale) => this.directWriteFonts.GetSystemFallback(locale);

    /// <inheritdoc/>
    public void Dispose() => this.directWriteFonts.Dispose();
}
