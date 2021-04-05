using ImGuiNET;

namespace Dalamud.Interface
{
    /// <summary>
    /// Class containing various helper methods for use with ImGui inside Dalamud.
    /// </summary>
    public static class ImGuiHelpers
    {
        private static uint mainViewportId;

        /// <summary>
        /// Force this ImGui window to stay inside the main game window.
        /// </summary>
        public static void ForceMainWindow() => ImGui.SetNextWindowViewport(GetMainViewportId());

        /// <summary>
        /// Get the ID of the main game window viewport.
        /// </summary>
        /// <returns>The ID of the main game window viewport.</returns>
        public static uint GetMainViewportId()
        {
            if (mainViewportId == 0)
                mainViewportId = ImGui.GetMainViewport().ID;

            return mainViewportId;
        }
    }
}
