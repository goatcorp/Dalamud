namespace Dalamud.Bindings.ImAnim;

public enum ImAnimAnchorSpace
{
    /// <summary>
    /// ImGui::GetContentRegionAvail()
    /// </summary>
    WindowContent,

    /// <summary>
    /// ImGui::GetWindowSize()
    /// </summary>
    Window,

    /// <summary>
    /// ImGui::GetWindowViewport()->Size
    /// </summary>
    Viewport,

    /// <summary>
    /// ImGui::GetItemRectSize()
    /// </summary>
    LastItem,
}
