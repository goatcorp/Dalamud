using Dalamud.Bindings.ImGui;

namespace Dalamud.Interface.Fonts;

/// <summary>Acts on a click on a URL found in text.</summary>
/// <param name="url">The URL.</param>
/// <param name="button">The mouse button. The extra buttons are 3 and 4.</param>
/// <seealso cref="TextMarkupCallbacks.CreateUrlsWithHandler"/>
public delegate void UrlClickHandler(string url, ImGuiMouseButton button);
