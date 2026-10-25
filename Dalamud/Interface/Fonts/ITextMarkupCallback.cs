using Dalamud.Bindings.ImGui;

namespace Dalamud.Interface.Fonts;

/// <summary>Decides how text in fonts with <see cref="TextStyle.Markup"/> is drawn.</summary>
/// <remarks><ul>
/// <li>Called from the main thread.</li>
/// <li>Callbacks are asked from the most recently pushed one down, and the first with an answer decides. The defaults
/// do nothing with it. A markup also asks the callbacks of its own interface, such as
/// <see cref="ISeStringCallback"/>.</li>
/// <li>A callback pushed with <c>inherit</c> set to <see langword="false"/> cuts off the ones pushed before it, until it
/// is popped.</li>
/// <li>Return values are part of the cached layout, except for <see cref="TryActivateLink"/>, and for the fields of
/// <see cref="TryGetLinkStyle"/> that do not change the layout, such as colors and lines. Those are asked for each
/// time the text is drawn. Call <see cref="ImGuiTextExtensions.extension(ImGui).InvalidateTextCallback"/> if a cached
/// return value would change.</li>
/// <li>Entities and links are not drawn or hit-tested when the text is drawn with a transform.</li>
/// </ul></remarks>
/// <seealso cref="ImGuiTextExtensions.extension(ImGui).PushTextCallback"/>
public interface ITextMarkupCallback
{
    /// <summary>Gets something to draw instead of the text at a byte offset.</summary>
    /// <param name="context">Where it is.</param>
    /// <param name="entity">The entity.</param>
    /// <returns><see langword="true"/> if there is one, or <see langword="false"/> to pass.</returns>
    /// <remarks>When the returned size changes, call
    /// <see cref="ImGuiTextExtensions.extension(ImGui).InvalidateTextCallback"/>.</remarks>
    bool TryGetEntity(in TextEntityContext context, out TextEntity entity)
    {
        entity = default;
        return false;
    }

    /// <summary>Gets the style to draw a link with.</summary>
    /// <param name="link">The link, and what is happening to it.</param>
    /// <param name="style">The style the link is drawn with otherwise. For <see cref="TextLinkState.Hovered"/> and
    /// <see cref="TextLinkState.Held"/>, it is the one given for <see cref="TextLinkState.Normal"/>.</param>
    /// <param name="result">The style. Set it to <paramref name="style"/> to draw the link as it is.</param>
    /// <returns><see langword="true"/> if there is one, or <see langword="false"/> to pass.</returns>
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
}
