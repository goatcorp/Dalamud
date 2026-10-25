using Lumina.Text.ReadOnly;

namespace Dalamud.Interface.Fonts;

/// <summary>Decides how SeStrings are drawn, for text in fonts with <see cref="TextMarkup.SeString"/> or
/// <see cref="TextMarkup.MacroString"/>.</summary>
/// <remarks>Asked as <see cref="ITextMarkupCallback"/> is.</remarks>
public interface ISeStringCallback : ITextMarkupCallback
{
    /// <summary>Gets the theme index, or <see langword="null"/> to pass on to the next callback, and eventually use
    /// the default.</summary>
    int? ThemeIndex => null;

    /// <summary>Decides whether a macro payload applies.</summary>
    /// <param name="payload">The payload.</param>
    /// <returns><see langword="true"/> to apply it, <see langword="false"/> to ignore it, or <see langword="null"/> to
    /// pass on to the next callback, and eventually to do nothing.</returns>
    bool? ShouldHandle(ReadOnlySePayloadSpan payload) => null;
}
