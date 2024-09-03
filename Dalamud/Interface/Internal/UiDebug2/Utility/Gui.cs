using System.Collections.Generic;
using System.Numerics;

using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using FFXIVClientStructs.FFXIV.Client.Graphics;
using ImGuiNET;

using static Dalamud.Interface.ColorHelpers;
using static ImGuiNET.ImGuiCol;

namespace Dalamud.Interface.Internal.UiDebug2.Utility;

/// <summary>
/// Miscellaneous ImGui tools used by <see cref="UiDebug2"/>.
/// </summary>
internal static class Gui
{
    /// <summary>
    /// Begins a tree node that also displays a colored line to the left while open.
    /// </summary>
    /// <param name="label">The label of the tree.</param>
    /// <param name="color">The color of the heading text.</param>
    /// <param name="lineStart">A value representing where to begin drawing the left-side line.</param>
    /// <param name="defOpen">Whether this tree should default to being open.</param>
    /// <returns>true if the tree is open.</returns>
    internal static bool NestedTreePush(string label, Vector4 color, out Vector2 lineStart, bool defOpen = false)
    {
        ImGui.PushStyleColor(Text, color);
        var result = NestedTreePush(label, out lineStart, defOpen);
        ImGui.PopStyleColor();
        return result;
    }

    /// <inheritdoc cref="NestedTreePush(string, Vector4, out Vector2, bool)"/>
    internal static bool NestedTreePush(string label, out Vector2 lineStart, bool defOpen = false)
    {
        var imGuiTreeNodeFlags = ImGuiTreeNodeFlags.SpanFullWidth;

        if (defOpen)
        {
            imGuiTreeNodeFlags |= ImGuiTreeNodeFlags.DefaultOpen;
        }

        var treeNodeEx = ImGui.TreeNodeEx(label, imGuiTreeNodeFlags);
        lineStart = ImGui.GetCursorScreenPos() + new Vector2(-10, 2);
        return treeNodeEx;
    }

    /// <summary>
    /// Completes a NestedTree.
    /// </summary>
    /// <param name="lineStart">The starting position calculated when the tree was pushed.</param>
    /// <param name="color">The color of the left-side line.</param>
    internal static void NestedTreePop(Vector2 lineStart, Vector4? color = null)
    {
        var lineEnd = lineStart with { Y = ImGui.GetCursorScreenPos().Y - 7 };

        if (lineStart.Y < lineEnd.Y)
        {
            ImGui.GetWindowDrawList().AddLine(lineStart, lineEnd, RgbaVector4ToUint(color ?? new(1)), 1);
        }

        ImGui.TreePop();
    }

    /// <summary>
    /// A radio-button-esque input that uses Fontawesome icon buttons.
    /// </summary>
    /// <typeparam name="T">The type of value being set.</typeparam>
    /// <param name="label">The label for the inputs.</param>
    /// <param name="val">The value being set.</param>
    /// <param name="options">A list of all options to create buttons for.</param>
    /// <param name="icons">A list of the icons to use for each option.</param>
    /// <returns>true if a button is clicked.</returns>
    internal static unsafe bool IconSelectInput<T>(string label, ref T val, List<T> options, List<FontAwesomeIcon> icons)
    {
        var ret = false;
        for (var i = 0; i < options.Count; i++)
        {
            var option = options[i];
            var icon = icons[i];

            if (i > 0)
            {
                ImGui.SameLine();
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() - ((ImGui.GetFontSize() / -6f) + 7f));
            }

            var color = *ImGui.GetStyleColorVec4(val is not null && val.Equals(option) ? ButtonActive : Button);

            if (ImGuiComponents.IconButton($"{label}{option}{i}", icon, color))
            {
                val = option;
                ret = true;
            }
        }

        return ret;
    }

    /// <summary>
    /// Prints field name and its value.
    /// </summary>
    /// <param name="fieldName">The name of the field.</param>
    /// <param name="value">The value of the field.</param>
    /// <param name="copy">Whether to enable click-to-copy.</param>
    internal static void PrintFieldValuePair(string fieldName, string value, bool copy = true)
    {
        ImGui.Text($"{fieldName}:");
        ImGui.SameLine();
        if (copy)
        {
            ClickToCopyText(value);
        }
        else
        {
            ImGui.TextColored(new(0.6f, 0.6f, 0.6f, 1), value);
        }
    }

    /// <summary>
    /// Prints a set of fields and their values.
    /// </summary>
    /// <param name="pairs">Tuples of fieldnames and values to display.</param>
    internal static void PrintFieldValuePairs(params (string FieldName, string Value)[] pairs)
    {
        for (var i = 0; i < pairs.Length; i++)
        {
            if (i != 0)
            {
                ImGui.SameLine();
            }

            PrintFieldValuePair(pairs[i].FieldName, pairs[i].Value, false);
        }
    }

    /// <inheritdoc cref="PrintColor(Vector4,string)"/>
    internal static void PrintColor(ByteColor color, string fmt) => PrintColor(RgbaUintToVector4(color.RGBA), fmt);

    /// <inheritdoc cref="PrintColor(Vector4,string)"/>
    internal static void PrintColor(Vector3 color, string fmt) => PrintColor(new Vector4(color, 1), fmt);

    /// <summary>
    /// Prints a text string representing a color, with a backdrop in that color.
    /// </summary>
    /// <param name="color">The color value.</param>
    /// <param name="fmt">The text string to print.</param>
    /// <remarks>Colors the text itself either white or black, depending on the luminosity of the background color.</remarks>
    internal static void PrintColor(Vector4 color, string fmt)
    {
        static double Luminosity(Vector4 vector4) =>
            Math.Pow(
                (Math.Pow(vector4.X, 2) * 0.299f) +
                (Math.Pow(vector4.Y, 2) * 0.587f) +
                (Math.Pow(vector4.Z, 2) * 0.114f),
                0.5f) * vector4.W;

        ImGui.PushStyleColor(Text, Luminosity(color) < 0.5f ? new Vector4(1) : new(0, 0, 0, 1));
        ImGui.PushStyleColor(Button, color);
        ImGui.PushStyleColor(ButtonActive, color);
        ImGui.PushStyleColor(ButtonHovered, color);
        ImGui.SmallButton(fmt);
        ImGui.PopStyleColor(4);
    }

    /// <inheritdoc cref="ImGuiHelpers.ClickToCopyText"/>
    internal static void ClickToCopyText(string text, string? textCopy = null)
    {
        ImGui.PushStyleColor(Text, new Vector4(0.6f, 0.6f, 0.6f, 1));
        ImGuiHelpers.ClickToCopyText(text, textCopy);
        ImGui.PopStyleColor();

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip($"{textCopy ?? text}");
        }
    }

    /// <summary>
    /// Draws a tooltip that changes based on the cursor's x-position within the hovered item.
    /// </summary>
    /// <param name="tooltips">The text for each section.</param>
    /// <returns>true if the item is hovered.</returns>
    internal static bool SplitTooltip(params string[] tooltips)
    {
        if (!ImGui.IsItemHovered())
        {
            return false;
        }

        var mouseX = ImGui.GetMousePos().X;
        var minX = ImGui.GetItemRectMin().X;
        var maxX = ImGui.GetItemRectMax().X;
        var prog = (mouseX - minX) / (maxX - minX);

        var index = (int)Math.Floor(prog * tooltips.Length);

        ImGui.SetTooltip(tooltips[index]);

        return true;
    }
}
