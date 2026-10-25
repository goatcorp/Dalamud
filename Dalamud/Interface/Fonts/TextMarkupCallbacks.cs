using Dalamud.Bindings.ImGui;
using Lumina.Text.Payloads;
using Lumina.Text.ReadOnly;

namespace Dalamud.Interface.Fonts;

/// <summary>Ready-made <see cref="ITextMarkupCallback"/>s.</summary>
public static class TextMarkupCallbacks
{
    /// <summary>Gets a callback that keeps the edge color of the font, ignoring <see cref="MacroCode.EdgeColor"/> and
    /// <see cref="MacroCode.EdgeColorType"/>.</summary>
    public static ISeStringCallback IgnoreEdgeColor { get; } = new IgnoreEdgeColorCallback();

    /// <summary>Creates a callback that acts on clicks on links of SeStrings.</summary>
    /// <param name="handler">Called with the link payload when a link is clicked.</param>
    /// <returns>The callback. Keep it, rather than creating one every frame.</returns>
    /// <remarks>It covers links of <see cref="TextMarkup.SeString"/> and <see cref="TextMarkup.MacroString"/> text.
    /// </remarks>
    public static ITextMarkupCallback CreateSeStringLinkHandler(SeStringLinkHandler handler) =>
        new SeStringLinkCallback(handler ?? throw new ArgumentNullException(nameof(handler)));

    private sealed class IgnoreEdgeColorCallback : ISeStringCallback
    {
        public bool? ShouldHandle(ReadOnlySePayloadSpan payload) =>
            payload.MacroCode is MacroCode.EdgeColor or MacroCode.EdgeColorType ? false : null;
    }

    private sealed class SeStringLinkCallback(SeStringLinkHandler handler) : ITextMarkupCallback
    {
        public bool TryActivateLink(in TextLinkContext link, ImGuiMouseButton button)
        {
            if (!link.TryGetSePayload(out var payload))
                return false;
            handler(payload, button);
            return true;
        }
    }
}
