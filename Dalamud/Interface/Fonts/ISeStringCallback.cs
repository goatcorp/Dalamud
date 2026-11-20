using Lumina.Text.ReadOnly;

namespace Dalamud.Interface.Fonts;

/// <summary>Customizes how <see cref="TextMarkup.SeString"/> and <see cref="TextMarkup.MacroString"/> payloads are
/// evaluated and drawn.</summary>
/// <remarks>Follows the same stack order and fallback rules as <see cref="ITextMarkupCallback"/>.</remarks>
public interface ISeStringCallback : ITextMarkupCallback
{
    /// <summary>Gets the theme index, or <see langword="null"/> to pass on to the next callback, and eventually use
    /// the default.</summary>
    int? ThemeIndex => null;

    /// <summary>Decides whether a macro payload should be applied.</summary>
    /// <param name="payload">The payload.</param>
    /// <returns><see langword="true"/> to apply it, <see langword="false"/> to ignore it, or <see langword="null"/> to
    /// pass on to the next callback and default to <see langword="true"/>.</returns>
    bool? ShouldHandle(ReadOnlySePayloadSpan payload) => null;
}
