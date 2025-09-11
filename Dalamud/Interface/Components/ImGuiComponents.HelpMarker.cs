using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Common.Math;

#pragma warning disable CS0618 // Type or member is obsolete. To be fixed with API14.

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
    public static void HelpMarker(string helpText, FontAwesomeIcon icon, System.Numerics.Vector4 color)
    {
        using var col = new ImRaii.Color();
        col.Push(ImGuiCol.TextDisabled, color);

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

    /// <summary>
    /// HelpMarker component to add a custom icon with text on hover.
    /// </summary>
    /// <param name="helpText">The text to display on hover.</param>
    /// <param name="icon">The icon to use.</param>
    /// <param name="color">The color of the icon.</param>
    [Api14ToDo(Api14ToDoAttribute.Remove)]
    [Obsolete("CS type is deprecated. Use System.Numerics.Vector4 instead.")]
    public static void HelpMarker(string helpText, FontAwesomeIcon icon, Vector4? color = null)
    {
        if (color.HasValue)
        {
            HelpMarker(helpText, icon, color.Value);
            return;
        }

        // FIXME: Code duplication is easier than splitting up the Nullable in a way that doesn't break the API.
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
