using System.Numerics;

using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

using Dalamud.Bindings.ImGui;

namespace Dalamud.Interface.Internal.DesignSystem;

/// <summary>
/// Private ImGui widgets for use inside Dalamud.
/// </summary>
internal static partial class DalamudComponents
{
    private static readonly Vector2 ButtonPadding = new(8 * ImGuiHelpers.GlobalScale, 6 * ImGuiHelpers.GlobalScale);
    private static readonly Vector4 SecondaryButtonBackground = new(0, 0, 0, 0);

    private static Vector4 PrimaryButtonBackground => ImGuiColors.TankBlue;

    /// <summary>
    /// Draw a "primary style" button.
    /// </summary>
    /// <param name="text">The text to show.</param>
    /// <returns>True if the button was clicked.</returns>
    internal static bool PrimaryButton(string text)
    {
        using (ImRaii.PushColor(ImGuiCol.Button, PrimaryButtonBackground))
        {
            return Button(text);
        }
    }

    /// <summary>
    /// Draw a "secondary style" button.
    /// </summary>
    /// <param name="text">The text to show.</param>
    /// <returns>True if the button was clicked.</returns>
    internal static bool SecondaryButton(string text)
    {
        using (ImRaii.PushStyle(ImGuiStyleVar.FrameBorderSize, 1))
        using (var buttonColor = ImRaii.PushColor(ImGuiCol.Button, SecondaryButtonBackground))
        {
            buttonColor.Push(ImGuiCol.Border, ImGuiColors.DalamudGrey3);
            return Button(text);
        }
    }

    private static bool Button(string text)
    {
        using (ImRaii.PushStyle(ImGuiStyleVar.FramePadding, ButtonPadding))
        {
            return ImGui.Button(text);
        }
    }
}
