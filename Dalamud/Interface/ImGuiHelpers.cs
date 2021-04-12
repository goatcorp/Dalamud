using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;

namespace Dalamud.Interface
{
    /// <summary>
    /// Class containing various helper methods for use with ImGui inside Dalamud.
    /// </summary>
    public static class ImGuiHelpers
    {
        /// <summary>
        /// Gets the main viewport.
        /// </summary>
        public static ImGuiViewportPtr MainViewport { get; internal set; }

        /// <summary>
        /// Gets the global Dalamud scale.
        /// </summary>
        public static float GlobalScale { get; private set; }

        /// <summary>
        /// Force the next ImGui window to stay inside the main game window.
        /// </summary>
        public static void ForceNextWindowMainViewport() => ImGui.SetNextWindowViewport(MainViewport.ID);

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
        /// Set the position of the next window relative to the main viewport.
        /// </summary>
        /// <param name="position">The position of the next window.</param>
        /// <param name="condition">When to set the position.</param>
        /// <param name="pivot">The pivot to set the position around.</param>
        public static void SetNextWindowPosRelativeMainViewport(
            Vector2 position, ImGuiCond condition = ImGuiCond.None, Vector2 pivot = default)
            => ImGui.SetNextWindowPos(position + MainViewport.Pos, condition, pivot);

        /// <summary>
        /// Set the position of a window relative to the main viewport.
        /// </summary>
        /// <param name="name">The name/ID of the window.</param>
        /// <param name="position">The position of the window.</param>
        /// <param name="condition">When to set the position.</param>
        public static void SetWindowPosRelativeMainViewport(
            string name, Vector2 position, ImGuiCond condition = ImGuiCond.None)
            => ImGui.SetWindowPos(position + MainViewport.Pos, condition);

        /// <summary>
        /// Creates default color palette for use with color pickers.
        /// </summary>
        /// <param name="swatchCount">The total number of swatches to use.</param>
        /// <returns>Default color palette.</returns>
        public static List<Vector4> DefaultColorPalette(int swatchCount = 32)
        {
            var colorPalette = new List<Vector4>();
            for (var i = 0; i < swatchCount; i++)
            {
                ImGui.ColorConvertHSVtoRGB(i / 31.0f, 0.7f, 0.8f, out var r, out var g, out var b);
                colorPalette.Add(new Vector4(r, g, b, 1.0f));
            }

            return colorPalette;
        }

        /// <summary>
        /// Get data needed for each new frame.
        /// </summary>
        internal static void NewFrame()
        {
            GlobalScale = ImGui.GetIO().FontGlobalScale;
        }
    }
}
