using System.Collections.Generic;
using System.Numerics;

using CheapLoc;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

namespace Dalamud.Interface.Internal.DesignSystem;

/// <summary>
/// Private ImGui widgets for use inside Dalamud.
/// </summary>
internal static partial class DalamudComponents
{
    /// <summary>
    /// Draw a standard error display with a child for displaying the exception and optional buttons.
    /// </summary>
    /// <param name="headerMessage">The error message shown at the top.</param>
    /// <param name="error">The exception to display, or null if details are unavailable.</param>
    /// <param name="buttons"> Pairs of (label, action) for the action buttons drawn below the header.</param>
    internal static void DrawErrorDisplay(
        string headerMessage,
        Exception? error,
        IReadOnlyList<(string Label, Action Action)> buttons)
    {
        ImGui.TextColoredWrapped(ImGuiColors.ErrorForeground, headerMessage);
        ImGuiHelpers.ScaledDummy(5);

        var isFirst = true;
        foreach (var (label, action) in buttons)
        {
            if (!isFirst)
                ImGui.SameLine();
            isFirst = false;

            if (ImGui.Button(label))
                action();
        }

        if (error != null)
        {
            ImGuiHelpers.ScaledDummy(10);

            using var child = ImRaii.Child("##ErrorDetails", new Vector2(0, 200 * ImGuiHelpers.GlobalScale), true);
            using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudGrey))
            {
                ImGui.TextWrapped(Loc.Localize("ErrorDisplayDetails", "Error Details:"));
                ImGui.Separator();
                ImGui.TextWrapped(error.ToString());
            }

            if (child.Success)
            {
                var windowSize = ImGui.GetWindowSize();
                var scrollbarWidth = ImGui.GetScrollMaxY() > 0 ? ImGui.GetStyle().ScrollbarSize : 0f;

                var copyText = Loc.Localize("ErrorDisplayCopy", "Copy");
                var buttonWidth = ImGuiComponents.GetIconButtonWithTextWidth(FontAwesomeIcon.Copy, copyText);
                ImGui.SetCursorPos(new Vector2(
                                       windowSize.X - scrollbarWidth - buttonWidth - ImGui.GetStyle().FramePadding.X,
                                       ImGui.GetStyle().FramePadding.Y));
                if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Copy, copyText))
                    ImGui.SetClipboardText(error.ToString());
            }
        }
    }
}
