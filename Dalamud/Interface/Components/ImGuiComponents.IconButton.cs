using System.Numerics;

using ImGuiNET;

namespace Dalamud.Interface.Components;

/// <summary>
/// Class containing various methods providing ImGui components.
/// </summary>
public static partial class ImGuiComponents
{
    /// <summary>
    /// IconButton component to use an icon as a button.
    /// </summary>
    /// <param name="icon">The icon for the button.</param>
    /// <returns>Indicator if button is clicked.</returns>
    public static bool IconButton(FontAwesomeIcon icon)
        => IconButton(icon, null, null, null);

    /// <summary>
    /// IconButton component to use an icon as a button.
    /// </summary>
    /// <param name="id">The ID of the button.</param>
    /// <param name="icon">The icon for the button.</param>
    /// <returns>Indicator if button is clicked.</returns>
    public static bool IconButton(int id, FontAwesomeIcon icon)
        => IconButton(id, icon, null, null, null);

    /// <summary>
    /// IconButton component to use an icon as a button.
    /// </summary>
    /// <param name="iconText">Text already containing the icon string.</param>
    /// <returns>Indicator if button is clicked.</returns>
    public static bool IconButton(string iconText)
        => IconButton(iconText, null, null, null);

    /// <summary>
    /// IconButton component to use an icon as a button.
    /// </summary>
    /// <param name="icon">The icon for the button.</param>
    /// <param name="defaultColor">The default color of the button.</param>
    /// <param name="activeColor">The color of the button when active.</param>
    /// <param name="hoveredColor">The color of the button when hovered.</param>
    /// <returns>Indicator if button is clicked.</returns>
    public static bool IconButton(FontAwesomeIcon icon, Vector4? defaultColor = null, Vector4? activeColor = null, Vector4? hoveredColor = null)
        => IconButton($"{icon.ToIconString()}", defaultColor, activeColor, hoveredColor);

    /// <summary>
    /// IconButton component to use an icon as a button with color options.
    /// </summary>
    /// <param name="id">The ID of the button.</param>
    /// <param name="icon">The icon for the button.</param>
    /// <param name="defaultColor">The default color of the button.</param>
    /// <param name="activeColor">The color of the button when active.</param>
    /// <param name="hoveredColor">The color of the button when hovered.</param>
    /// <returns>Indicator if button is clicked.</returns>
    public static bool IconButton(int id, FontAwesomeIcon icon, Vector4? defaultColor = null, Vector4? activeColor = null, Vector4? hoveredColor = null)
        => IconButton($"{icon.ToIconString()}{id}", defaultColor, activeColor, hoveredColor);

    /// <summary>
    /// IconButton component to use an icon as a button with color options.
    /// </summary>
    /// <param name="iconText">Text already containing the icon string.</param>
    /// <param name="defaultColor">The default color of the button.</param>
    /// <param name="activeColor">The color of the button when active.</param>
    /// <param name="hoveredColor">The color of the button when hovered.</param>
    /// <returns>Indicator if button is clicked.</returns>
    public static bool IconButton(string iconText, Vector4? defaultColor = null, Vector4? activeColor = null, Vector4? hoveredColor = null)
    {
        var numColors = 0;

        if (defaultColor.HasValue)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, defaultColor.Value);
            numColors++;
        }

        if (activeColor.HasValue)
        {
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, activeColor.Value);
            numColors++;
        }

        if (hoveredColor.HasValue)
        {
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, hoveredColor.Value);
            numColors++;
        }

        ImGui.PushFont(UiBuilder.IconFont);

        var button = ImGui.Button(iconText);

        ImGui.PopFont();

        if (numColors > 0)
            ImGui.PopStyleColor(numColors);

        return button;
    }
}
