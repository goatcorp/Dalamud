using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Common.Math;

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
    /// <param name="color">The color of the icon.</param>
    public static void HelpMarker(string helpText, FontAwesomeIcon icon, Vector4? color = null)
    {
        using var col = new ImRaii.Color();

        if (color.HasValue)
        {
            col.Push(ImGuiCol.TextDisabled, color.Value);
        }

        ImGui.SameLine();

        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            ImGui.TextDisabled(icon.ToIconString());
        }

        if (ImGui.IsItemHovered())
        {
            using (ImRaii.Tooltip())
            {
                using (ImRaii.TextWrapPos(ImGui.GetFontSize() * 35.0f))
                {
                    ImGui.Text(helpText);
                }
            }
        }
    }
}
