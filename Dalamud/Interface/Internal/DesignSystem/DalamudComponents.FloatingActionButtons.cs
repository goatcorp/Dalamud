using System.Numerics;

using CheapLoc;

using Dalamud.Bindings.ImGui;
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
    /// Draw floating "Close without saving" and "Save" action buttons anchored to the bottom-right
    /// of the current window.
    /// </summary>
    /// <param name="saveClicked">Set to true if the save button was clicked this frame.</param>
    /// <param name="saveDisabled">Whether the save button should be rendered as disabled.</param>
    /// <param name="saveDisabledTooltip">Tooltip shown when the save button is disabled.</param>
    /// <returns>True, if the window should be closed.</returns>
    internal static bool DrawFloatingSaveDiscardButtons(
        out bool saveClicked,
        bool saveDisabled = false,
        string? saveDisabledTooltip = null)
    {
        saveClicked = false;

        var windowSize = ImGui.GetWindowSize();
        var saveText = Loc.Localize("DalamudSaveButton", "Save");
        var saveButtonWidth = ImGuiComponents.GetIconButtonWithTextWidth(FontAwesomeIcon.Save, saveText);

        float closeButtonWidth;
        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            var iconSize = ImGui.CalcTextSize(FontAwesomeIcon.Times.ToIconString());
            closeButtonWidth = iconSize.X + (ImGui.GetStyle().FramePadding.X * 2);
        }

        var totalWidth = closeButtonWidth + ImGui.GetStyle().ItemSpacing.X + saveButtonWidth;
        ImGui.SetCursorPos(windowSize - new Vector2(totalWidth + ImGuiHelpers.ScaledVector2(74).X, ImGuiHelpers.ScaledVector2(74).Y));

        using var buttonChild = ImRaii.Child("###dalamudFloatingActionButtons"u8);
        if (!buttonChild)
            return false;

        var closeWindow = false;
        using (ImRaii.PushStyle(ImGuiStyleVar.FramePadding, new Vector2(16, 12)))
        using (ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 100f))
        {
            if (ImGuiComponents.IconButton(FontAwesomeIcon.Times))
                closeWindow = true;

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(Loc.Localize("DalamudCloseWithoutSaving", "Close without saving"));

            ImGui.SameLine();

            using (ImRaii.Disabled(saveDisabled))
            {
                if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Save, saveText))
                {
                    saveClicked = true;
                    closeWindow = !ImGui.IsKeyDown(ImGuiKey.ModShift);
                }
            }

            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            {
                if (saveDisabled && saveDisabledTooltip != null)
                {
                    ImGui.SetTooltip(saveDisabledTooltip);
                }
                else if (!saveDisabled)
                {
                    ImGui.SetTooltip(!ImGui.IsKeyDown(ImGuiKey.ModShift)
                                         ? Loc.Localize("DalamudSaveAndClose", "Save and close")
                                         : Loc.Localize("DalamudSave", "Save"));
                }
            }
        }

        return closeWindow;
    }
}
