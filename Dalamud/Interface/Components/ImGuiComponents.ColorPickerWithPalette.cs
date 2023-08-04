using System.Numerics;

using Dalamud.Interface.Utility;
using ImGuiNET;

namespace Dalamud.Interface.Components;

/// <summary>
/// Class containing various methods providing ImGui components.
/// </summary>
public static partial class ImGuiComponents
{
    /// <summary>
    /// ColorPicker with palette.
    /// </summary>
    /// <param name="id">Id for the color picker.</param>
    /// <param name="description">The description of the color picker.</param>
    /// <param name="originalColor">The current color.</param>
    /// <returns>Selected color.</returns>
    public static Vector4 ColorPickerWithPalette(int id, string description, Vector4 originalColor)
    {
        const ImGuiColorEditFlags flags = ImGuiColorEditFlags.NoSidePreview | ImGuiColorEditFlags.NoSmallPreview;
        return ColorPickerWithPalette(id, description, originalColor, flags);
    }

    /// <summary>
    /// ColorPicker with palette with color picker options.
    /// </summary>
    /// <param name="id">Id for the color picker.</param>
    /// <param name="description">The description of the color picker.</param>
    /// <param name="originalColor">The current color.</param>
    /// <param name="flags">Flags to customize color picker.</param>
    /// <returns>Selected color.</returns>
    public static Vector4 ColorPickerWithPalette(int id, string description, Vector4 originalColor, ImGuiColorEditFlags flags)
    {
        var existingColor = originalColor;
        var selectedColor = originalColor;
        var colorPalette = ImGuiHelpers.DefaultColorPalette(36);
        if (ImGui.ColorButton($"{description}###ColorPickerButton{id}", originalColor))
        {
            ImGui.OpenPopup($"###ColorPickerPopup{id}");
        }

        if (ImGui.BeginPopup($"###ColorPickerPopup{id}"))
        {
            if (ImGui.ColorPicker4($"###ColorPicker{id}", ref existingColor, flags))
            {
                selectedColor = existingColor;
            }

            for (var i = 0; i < 4; i++)
            {
                ImGui.Spacing();
                for (var j = i * 9; j < (i * 9) + 9; j++)
                {
                    if (ImGui.ColorButton($"###ColorPickerSwatch{id}{i}{j}", colorPalette[j]))
                    {
                        selectedColor = colorPalette[j];
                    }

                    ImGui.SameLine();
                }
            }

            ImGui.EndPopup();
        }

        return selectedColor;
    }
}
