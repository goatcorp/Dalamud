using ImGuiNET;

namespace Dalamud.Interface
{
    /// <summary>
    /// Class containing various helper methods for use with ImGui inside Dalamud.
    /// </summary>
    public static class ImGuiHelpers
    {
        /// <summary>
        /// Force this ImGui window to stay inside the main game window.
        /// </summary>
        public static void ForceMainWindow() => ImGui.SetNextWindowViewport(ImGui.GetMainViewport().ID);
    }
}
