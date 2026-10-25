using Dalamud.Bindings.ImGui;

using Lumina.Text.ReadOnly;

namespace Dalamud.Interface.Fonts;

/// <summary>Acts on a click on a link of a SeString.</summary>
/// <param name="link">The link payload.</param>
/// <param name="button">The mouse button. The extra buttons are 3 and 4.</param>
/// <seealso cref="TextMarkupCallbacks.CreateSeStringLinkHandler"/>
public delegate void SeStringLinkHandler(ReadOnlySePayloadSpan link, ImGuiMouseButton button);
