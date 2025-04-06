using Dalamud.Interface.Utility;

using Dalamud.Bindings.ImGui;

namespace Dalamud.Interface.Internal.Windows.Data;

/// <summary>
/// Common utilities used in Widgets.
/// </summary>
internal class WidgetUtil
{
    /// <summary>
    /// Draws text that can be copied on click.
    /// </summary>
    /// <param name="text">The text shown and to be copied.</param>
    /// <param name="tooltipText">The text in the tooltip.</param>
    internal static void DrawCopyableText(string text, string tooltipText = "Copy")
    {
        ImGuiHelpers.SafeTextWrapped(text);

        if (ImGui.IsItemHovered())
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            ImGui.BeginTooltip();
            ImGui.TextUnformatted(tooltipText);
            ImGui.EndTooltip();
        }

        if (ImGui.IsItemClicked())
        {
            ImGui.SetClipboardText(text);
        }
    }
}
