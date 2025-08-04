using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;

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
        ImGui.TextWrapped(text);

        if (ImGui.IsItemHovered())
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            ImGui.BeginTooltip();
            ImGui.Text(tooltipText);
            ImGui.EndTooltip();
        }

        if (ImGui.IsItemClicked())
        {
            ImGui.SetClipboardText(text);
        }
    }
}
