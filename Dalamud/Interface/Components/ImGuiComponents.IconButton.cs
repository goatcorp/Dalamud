using System.Numerics;

using ImGuiNET;

namespace Dalamud.Interface.Components
{
    /// <summary>
    /// Class containing various methods providing ImGui components.
    /// </summary>
    public static partial class ImGuiComponents
    {
        /// <summary>
        /// IconButton component to use an icon as a button.
        /// </summary>
        /// <param name="id">The ID of the button.</param>
        /// <param name="icon">The icon for the button.</param>
        /// <returns>Indicator if button is clicked.</returns>
        public static bool IconButton(int id, FontAwesomeIcon icon)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, Vector4.Zero);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, Vector4.Zero);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Vector4.Zero);
            ImGui.PushFont(UiBuilder.IconFont);
            var button = ImGui.Button($"{icon.ToIconString()}{id}");
            ImGui.PopFont();
            ImGui.PopStyleColor(3);
            return button;
        }

        /// <summary>
        /// IconButton component to use an icon as a button with color options.
        /// </summary>
        /// <param name="id">The ID of the button.</param>
        /// <param name="icon">The icon for the button.</param>
        /// <param name="defaultColor">The default color of the button.</param>
        /// <param name="activeColor">The color of the button when active.</param>
        /// <param name="hoveredColor">The color of the button when hovered.</param>
        /// <returns>Indicator if button is clicked.</returns>
        public static bool IconButton(int id, FontAwesomeIcon icon, Vector4 defaultColor, Vector4 activeColor, Vector4 hoveredColor)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, defaultColor);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, activeColor);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, hoveredColor);
            ImGui.PushFont(UiBuilder.IconFont);
            var button = ImGui.Button($"{icon.ToIconString()}{id}");
            ImGui.PopFont();
            ImGui.PopStyleColor(3);
            return button;
        }
    }
}
