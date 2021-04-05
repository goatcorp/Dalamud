using System.Numerics;
using ImGuiNET;
using Serilog;

namespace Dalamud.Interface
{
    /// <summary>
    /// Class containing various helper methods for use with ImGui inside Dalamud.
    /// </summary>
    public static class ImGuiHelpers
    {
        private static uint mainViewportId;

        /// <summary>
        /// Gets the global Dalamud scale.
        /// </summary>
        public static float GlobalScale { get; private set; }

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

        public static Vector2 MainWindowPos { get; set; }

        /// <summary>
        /// Create a dummy scaled by the global Dalamud scale.
        /// </summary>
        /// <param name="size">The size of the dummy.</param>
        public static void ScaledDummy(Vector2 size) => ImGui.Dummy(size * GlobalScale);

        /// <summary>
        /// Use a relative ImGui.SameLine() from your current cursor position, scaled by the Dalamud global scale.
        /// </summary>
        /// <param name="offset">The offset from your current cursor position.</param>
        /// <param name="spacing">The spacing to use.</param>
        public static void ScaledRelativeSameLine(float offset, float spacing = -1.0f) =>
            ImGui.SameLine(ImGui.GetCursorPosX() + (offset * GlobalScale));

        /// <summary>
        /// Get data needed for each new frame.
        /// </summary>
        internal static void NewFrame()
        {
            GlobalScale = ImGui.GetIO().FontGlobalScale;
            MainWindowPos = ImGui.GetMainViewport().Pos;
        }
    }
}
