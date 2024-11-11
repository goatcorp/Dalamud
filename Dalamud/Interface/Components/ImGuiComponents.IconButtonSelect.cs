// <copyright file="ImGuiComponents.IconButtonSelect.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Interface.Utility;

using ImGuiNET;

namespace Dalamud.Interface.Components;

public static partial class ImGuiComponents
{
   /// <summary>
    /// A radio-like input that uses icon buttons.
    /// </summary>
    /// <typeparam name="T">The type of the value being set.</typeparam>
    /// <param name="label">Text that will be used to generate individual labels for the buttons.</param>
    /// <param name="val">The value to set.</param>
    /// <param name="optionIcons">The icons that will be displayed on each button.</param>
    /// <param name="optionValues">The options that each button will apply.</param>
    /// <param name="columns">Arranges the buttons in a grid with the given number of columns. 0 = ignored (all buttons drawn in one row).</param>
    /// <param name="buttonSize">Sets the size of all buttons. If either dimension is set to 0, that dimension will conform to the size of the icon.</param>
    /// <param name="defaultColor">The default color of the button range.</param>
    /// <param name="activeColor">The color of the actively-selected button.</param>
    /// <param name="hoveredColor">The color of the buttons when hovered.</param>
    /// <returns>True if any button is clicked.</returns>
    internal static bool IconButtonSelect<T>(string label, ref T val, IEnumerable<FontAwesomeIcon> optionIcons, IEnumerable<T> optionValues, uint columns = 0, Vector2? buttonSize = null, Vector4? defaultColor = null, Vector4? activeColor = null, Vector4? hoveredColor = null)
    {
        var options = optionIcons.Zip(optionValues, static (icon,value) => new KeyValuePair<FontAwesomeIcon,T>(icon,value));
        return IconButtonSelect(label, ref val, options, columns, buttonSize, defaultColor, activeColor, hoveredColor);
    }

    /// <summary>
    /// A radio-like input that uses icon buttons.
    /// </summary>
    /// <typeparam name="T">The type of the value being set.</typeparam>
    /// <param name="label">Text that will be used to generate individual labels for the buttons.</param>
    /// <param name="val">The value to set.</param>
    /// <param name="options">A list of all icon/option pairs.</param>
    /// <param name="columns">Arranges the buttons in a grid with the given number of columns. 0 = ignored (all buttons drawn in one row).</param>
    /// <param name="buttonSize">Sets the size of all buttons. If either dimension is set to 0, that dimension will conform to the size of the icon.</param>
    /// <param name="defaultColor">The default color of the button range.</param>
    /// <param name="activeColor">The color of the actively-selected button.</param>
    /// <param name="hoveredColor">The color of the buttons when hovered.</param>
    /// <returns>True if any button is clicked.</returns>
    internal static unsafe bool IconButtonSelect<T>(string label, ref T val, IEnumerable<KeyValuePair<FontAwesomeIcon, T>> options, uint columns = 0, Vector2? buttonSize = null, Vector4? defaultColor = null, Vector4? activeColor = null, Vector4? hoveredColor = null)
    {
        defaultColor ??= *ImGui.GetStyleColorVec4(ImGuiCol.Button);
        activeColor ??= *ImGui.GetStyleColorVec4(ImGuiCol.ButtonActive);
        hoveredColor ??= *ImGui.GetStyleColorVec4(ImGuiCol.ButtonHovered);

        var result = false;

        var innerSpacing = ImGui.GetStyle().ItemInnerSpacing;
        var y = ImGui.GetCursorPosY();

        var optArr = options.ToArray();
        for (var i = 0; i < optArr.Length; i++)
        {
            if (i > 0)
            {
                if (columns == 0 || i % columns != 0)
                {
                    ImGui.SameLine(0, innerSpacing.X);
                }
                else
                {
                    y += (buttonSize is { Y: not 0 } ? buttonSize.Value.Y * ImGuiHelpers.GlobalScale : ImGui.GetFrameHeight()) + innerSpacing.Y;

                    ImGui.SetCursorPosY(y);
                }
            }

            optArr[i].Deconstruct(out var icon, out var option);

            var selected = val is not null && val.Equals(option);

            if (IconButton($"{label}{option}{i}", icon, selected ? activeColor : defaultColor, activeColor, hoveredColor, buttonSize))
            {
                val = option;
                result = true;
            }
        }

        return result;
    }
}
