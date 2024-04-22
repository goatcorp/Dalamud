using ImGuiNET;

namespace Dalamud.Interface.Components;

/// <summary>
/// Class containing various methods providing ImGui components.
/// </summary>
public static partial class ImGuiComponents
{
    /// <summary>
    /// HelpMarker component to add a help icon with text on hover.
    /// </summary>
    /// <param name="helpText">The text to display on hover.</param>
    public static void HelpMarker(string helpText) => HelpMarker(helpText, FontAwesomeIcon.InfoCircle);

    /// <summary>
    /// HelpMarker component to add a custom icon with text on hover.
    /// </summary>
    /// <param name="helpText">The text to display on hover.</param>
    /// <param name="icon">The icon to use.</param>
    public static void HelpMarker(string helpText, FontAwesomeIcon icon)
    {
        ImGui.SameLine();
        ImGui.PushFont(UiBuilder.IconFont);
        ImGui.TextDisabled(icon.ToIconString());
        ImGui.PopFont();
        if (!ImGui.IsItemHovered()) return;
        ImGui.BeginTooltip();
        ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35.0f);
        ImGui.TextUnformatted(helpText);
        ImGui.PopTextWrapPos();
        ImGui.EndTooltip();
    }
}
