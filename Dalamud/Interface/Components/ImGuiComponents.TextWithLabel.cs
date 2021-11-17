using ImGuiNET;

namespace Dalamud.Interface.Components;

/// <summary>
/// Class containing various methods providing ImGui components.
/// </summary>
public static partial class ImGuiComponents
{
    /// <summary>
    /// TextWithLabel component to show labeled text.
    /// </summary>
    /// <param name="label">The label for text.</param>
    /// <param name="value">The text value.</param>
    /// <param name="hint">The hint to show on hover.</param>
    public static void TextWithLabel(string label, string value, string hint = "")
    {
        ImGui.Text(label + ": ");
        ImGui.SameLine();
        if (string.IsNullOrEmpty(hint))
        {
            ImGui.Text(value);
        }
        else
        {
            ImGui.Text(value + "*");
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(hint);
        }
    }
}
