using System.Numerics;

using Dalamud.Interface.Utility;
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
    /// <param name="id">The ID of the button.</param>
    /// <param name="icon">The icon for the button.</param>
    /// <returns>Indicator if button is clicked.</returns>
    public static bool IconButton(string id, FontAwesomeIcon icon)
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
        => IconButton($"{icon.ToIconString()}##{id}", defaultColor, activeColor, hoveredColor);

    /// <summary>
    /// IconButton component to use an icon as a button with color options.
    /// </summary>
    /// <param name="id">The ID of the button.</param>
    /// <param name="icon">The icon for the button.</param>
    /// <param name="defaultColor">The default color of the button.</param>
    /// <param name="activeColor">The color of the button when active.</param>
    /// <param name="hoveredColor">The color of the button when hovered.</param>
    /// <returns>Indicator if button is clicked.</returns>
    public static bool IconButton(string id, FontAwesomeIcon icon, Vector4? defaultColor = null, Vector4? activeColor = null, Vector4? hoveredColor = null)
        => IconButton($"{icon.ToIconString()}##{id}", defaultColor, activeColor, hoveredColor);

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

        var icon = iconText;
        if (icon.Contains("#"))
            icon = icon[..icon.IndexOf("#", StringComparison.Ordinal)];

        ImGui.PushID(iconText);

        ImGui.PushFont(UiBuilder.IconFont);
        var iconSize = ImGui.CalcTextSize(icon);
        ImGui.PopFont();
        
        var dl = ImGui.GetWindowDrawList();
        var cursor = ImGui.GetCursorScreenPos();
        
        // Draw an ImGui button with the icon and text
        var buttonWidth = iconSize.X + (ImGui.GetStyle().FramePadding.X * 2);
        var buttonHeight = ImGui.GetFrameHeight();
        var button = ImGui.Button(string.Empty, new Vector2(buttonWidth, buttonHeight));
        
        // Draw the icon on the window drawlist
        var iconPos = new Vector2(cursor.X + ImGui.GetStyle().FramePadding.X, cursor.Y + ImGui.GetStyle().FramePadding.Y);
        
        ImGui.PushFont(UiBuilder.IconFont);
        dl.AddText(iconPos, ImGui.GetColorU32(ImGuiCol.Text), icon);
        ImGui.PopFont();

        ImGui.PopID();

        if (numColors > 0)
            ImGui.PopStyleColor(numColors);

        return button;
    }

    /// <summary>
    /// IconButton component to use an icon as a button with color options.
    /// </summary>
    /// <param name="icon">Icon to show.</param>
    /// <param name="text">Text to show.</param>
    /// <param name="defaultColor">The default color of the button.</param>
    /// <param name="activeColor">The color of the button when active.</param>
    /// <param name="hoveredColor">The color of the button when hovered.</param>
    /// <returns>Indicator if button is clicked.</returns>
    public static bool IconButtonWithText(FontAwesomeIcon icon, string text, Vector4? defaultColor = null, Vector4? activeColor = null, Vector4? hoveredColor = null)
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

        ImGui.PushID(text);

        ImGui.PushFont(UiBuilder.IconFont);
        var iconSize = ImGui.CalcTextSize(icon.ToIconString());
        ImGui.PopFont();
        
        var textSize = ImGui.CalcTextSize(text);
        var dl = ImGui.GetWindowDrawList();
        var cursor = ImGui.GetCursorScreenPos();

        var iconPadding = 3 * ImGuiHelpers.GlobalScale;
        
        // Draw an ImGui button with the icon and text
        var buttonWidth = iconSize.X + textSize.X + (ImGui.GetStyle().FramePadding.X * 2) + iconPadding;
        var buttonHeight = ImGui.GetFrameHeight();
        var button = ImGui.Button(string.Empty, new Vector2(buttonWidth, buttonHeight));
        
        // Draw the icon on the window drawlist
        var iconPos = new Vector2(cursor.X + ImGui.GetStyle().FramePadding.X, cursor.Y + ImGui.GetStyle().FramePadding.Y);
        
        ImGui.PushFont(UiBuilder.IconFont);
        dl.AddText(iconPos, ImGui.GetColorU32(ImGuiCol.Text), icon.ToIconString());
        ImGui.PopFont();
        
        // Draw the text on the window drawlist
        var textPos = new Vector2(iconPos.X + iconSize.X + iconPadding, cursor.Y + ImGui.GetStyle().FramePadding.Y);
        dl.AddText(textPos, ImGui.GetColorU32(ImGuiCol.Text), text);

        ImGui.PopID();

        if (numColors > 0)
            ImGui.PopStyleColor(numColors);

        return button;
    }

    /// <summary>
    /// Get width of IconButtonWithText component.
    /// </summary>
    /// <param name="icon">Icon to use.</param>
    /// <param name="text">Text to use.</param>
    /// <returns>Width.</returns>
    internal static float GetIconButtonWithTextWidth(FontAwesomeIcon icon, string text)
    {
        ImGui.PushFont(UiBuilder.IconFont);
        var iconSize = ImGui.CalcTextSize(icon.ToIconString());
        ImGui.PopFont();
        
        var textSize = ImGui.CalcTextSize(text);
        var dl = ImGui.GetWindowDrawList();
        var cursor = ImGui.GetCursorScreenPos();

        var iconPadding = 3 * ImGuiHelpers.GlobalScale;
        
        return iconSize.X + textSize.X + (ImGui.GetStyle().FramePadding.X * 2) + iconPadding;
    }
}
