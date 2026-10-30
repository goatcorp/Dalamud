using Dalamud.Bindings.ImGui;

namespace Dalamud.Interface.Fonts;

/// <summary>Customizes entities and links when laying out and drawing markup text.</summary>
/// <remarks><ul>
/// <li>Invoked on the main thread only.</li>
/// <li>Callbacks are queried from most recently pushed to oldest, and the first to answer wins. Default interface
/// implementations pass to the next callback. Markup-specific interfaces such as <see cref="ISeStringCallback"/>
/// follow the same stack.</li>
/// <li><see cref="FindLinks"/> is the exception. Every active callback is invoked, and links from more recently pushed
/// callbacks take precedence when ranges overlap.</li>
/// <li>A callback pushed with <c>inherit</c> set to <see langword="false"/> hides callbacks pushed before it until
/// popped.</li>
/// <li>Return values are cached with the layout, except <see cref="TryActivateLink"/> and non-layout fields of
/// <see cref="TryGetLinkStyle"/> such as colors and lines, which are queried each draw. Call
/// <see cref="ImGuiTextExtensions.extension(ImGui).InvalidateTextCallback"/> when cached results change.</li>
/// <li>Entities and links are skipped when rendering with a transform.</li>
/// </ul></remarks>
/// <seealso cref="ImGuiTextExtensions.extension(ImGui).PushTextCallback"/>
public interface ITextMarkupCallback
{
    /// <summary>Tries to replace bytes at a source offset with an inline custom-drawn <see cref="TextEntity"/>.</summary>
    /// <param name="context">The markup source and style context at the candidate offset.</param>
    /// <param name="entity">The replacement entity when returning <see langword="true"/>.</param>
    /// <returns><see langword="true"/> to replace the bytes with <paramref name="entity"/>, or
    /// <see langword="false"/> to pass to earlier callbacks.</returns>
    /// <remarks>When the returned size changes, call
    /// <see cref="ImGuiTextExtensions.extension(ImGui).InvalidateTextCallback"/>.</remarks>
    bool TryGetEntity(in TextEntityContext context, out TextEntity entity)
    {
        entity = default;
        return false;
    }

    /// <summary>Tries to customize the <see cref="TextStyle"/> used to render a link.</summary>
    /// <param name="link">The link and its current interaction state.</param>
    /// <param name="style">The baseline style for this state. For <see cref="TextLinkState.Hovered"/> and
    /// <see cref="TextLinkState.Held"/>, this is the style resolved for <see cref="TextLinkState.Normal"/>.</param>
    /// <param name="result">The style to apply. Set to <paramref name="style"/> to keep it unchanged.</param>
    /// <returns><see langword="true"/> to use <paramref name="result"/>, or <see langword="false"/> to pass to earlier
    /// callbacks.</returns>
    bool TryGetLinkStyle(in TextLinkContext link, in TextStyle style, out TextStyle result)
    {
        result = style;
        return false;
    }

    /// <summary>Acts on a click on a link.</summary>
    /// <param name="link">The link.</param>
    /// <param name="button">The mouse button. The extra buttons are 3 and 4.</param>
    /// <returns><see langword="true"/> if it acted, or <see langword="false"/> to pass.</returns>
    /// <remarks>A click is a press and a release of a button on the link. It is called as the text is drawn, and
    /// <see cref="ImGuiTextExtensions.TryGetTextInteraction"/> still reports it.</remarks>
    bool TryActivateLink(in TextLinkContext link, ImGuiMouseButton button) => false;

    /// <summary>Finds links such as URLs in a plain-text segment produced by markup.</summary>
    /// <param name="context">The UTF-8 text segment to scan.</param>
    /// <param name="links">Collector receiving detected links in ascending offset order.</param>
    /// <remarks><ul>
    /// <li>Unlike the other methods, every active callback is invoked and their links are combined. Links overlapping
    /// one from a more recently pushed callback are ignored.</li>
    /// <li>Also called for <see cref="TextMarkup.Plain"/> text while any callback is pushed, along with link styling
    /// and activation for any links found.</li>
    /// <li>Detected links behave like markup links and set <see cref="TextLinkContext.Detected"/> to
    /// <see langword="true"/>.</li>
    /// <li>A link interrupted by markup payloads, such as a color macro mid-URL, spans separate segments and is not
    /// matched across them.</li>
    /// </ul></remarks>
    /// <seealso cref="TextMarkupCallbacks.Urls"/>
    void FindLinks(in TextLinkSearchContext context, TextLinkSearchResults links)
    {
    }
}
