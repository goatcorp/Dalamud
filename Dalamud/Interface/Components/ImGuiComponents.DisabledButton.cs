using System.Numerics;

using Dalamud.Interface.Utility.Raii;

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
        bool button;
        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            var text = icon.ToIconString();
            if (id.HasValue)
            {
                text = $"{text}##{id}";
            }

            button = DisabledButton(text, defaultColor, activeColor, hoveredColor, alphaMult);
        }

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
        using var col = new ImRaii.Color();

        if (defaultColor.HasValue)
        {
            col.Push(ImGuiCol.Button, defaultColor.Value);
        }

        if (activeColor.HasValue)
        {
            col.Push(ImGuiCol.ButtonActive, activeColor.Value);
        }

        if (hoveredColor.HasValue)
        {
            col.Push(ImGuiCol.ButtonHovered, hoveredColor.Value);
        }

        var style = ImGui.GetStyle();

        using (ImRaii.PushStyle(ImGuiStyleVar.Alpha, style.Alpha * alphaMult))
        {
            return ImGui.Button(labelWithId);
        }
    }
}
