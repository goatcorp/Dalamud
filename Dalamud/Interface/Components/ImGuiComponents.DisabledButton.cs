using System.Numerics;

using ImGuiNET;

namespace Dalamud.Interface.Components;

/// <summary>
/// Class containing various methods providing ImGui components.
/// </summary>
public static partial class ImGuiComponents
{
    /// <summary>
    /// Alpha modified IconButton component to use an icon as a button with alpha and color options.
    /// </summary>
    /// <param name="icon">The icon for the button.</param>
    /// <param name="id">The ID of the button.</param>
    /// <param name="defaultColor">The default color of the button.</param>
    /// <param name="activeColor">The color of the button when active.</param>
    /// <param name="hoveredColor">The color of the button when hovered.</param>
    /// <param name="alphaMult">A multiplier for the current alpha levels.</param>
    /// <returns>Indicator if button is clicked.</returns>
    public static bool DisabledButton(FontAwesomeIcon icon, int? id = null, Vector4? defaultColor = null, Vector4? activeColor = null, Vector4? hoveredColor = null, float alphaMult = .5f)
    {
        ImGui.PushFont(UiBuilder.IconFont);

        var text = icon.ToIconString();
        if (id.HasValue)
            text = $"{text}##{id}";

        var button = DisabledButton(text, defaultColor, activeColor, hoveredColor, alphaMult);

        ImGui.PopFont();

        return button;
    }

    /// <summary>
    /// Alpha modified Button component to use as a disabled button with alpha and color options.
    /// </summary>
    /// <param name="labelWithId">The button label with ID.</param>
    /// <param name="defaultColor">The default color of the button.</param>
    /// <param name="activeColor">The color of the button when active.</param>
    /// <param name="hoveredColor">The color of the button when hovered.</param>
    /// <param name="alphaMult">A multiplier for the current alpha levels.</param>
    /// <returns>Indicator if button is clicked.</returns>
    public static bool DisabledButton(string labelWithId, Vector4? defaultColor = null, Vector4? activeColor = null, Vector4? hoveredColor = null, float alphaMult = .5f)
    {
        if (defaultColor.HasValue)
            ImGui.PushStyleColor(ImGuiCol.Button, defaultColor.Value);

        if (activeColor.HasValue)
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, activeColor.Value);

        if (hoveredColor.HasValue)
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, hoveredColor.Value);

        var style = ImGui.GetStyle();
        ImGui.PushStyleVar(ImGuiStyleVar.Alpha, style.Alpha * alphaMult);

        var button = ImGui.Button(labelWithId);

        ImGui.PopStyleVar();

        if (defaultColor.HasValue)
            ImGui.PopStyleColor();

        if (activeColor.HasValue)
            ImGui.PopStyleColor();

        if (hoveredColor.HasValue)
            ImGui.PopStyleColor();

        return button;
    }
}
