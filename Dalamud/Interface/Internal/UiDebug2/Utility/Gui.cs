using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

using FFXIVClientStructs.FFXIV.Client.Graphics;

using static Dalamud.Bindings.ImGui.ImGuiCol;
using static Dalamud.Interface.ColorHelpers;

namespace Dalamud.Interface.Internal.UiDebug2.Utility;

/// <summary>
/// Miscellaneous ImGui tools used by <see cref="UiDebug2"/>.
/// </summary>
internal static class Gui
{
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
        var grey60 = new Vector4(0.6f, 0.6f, 0.6f, 1);
        if (copy)
        {
            ImGuiHelpers.ClickToCopyText(value, default, grey60);
        }
        else
        {
            ImGui.TextColored(grey60, value);
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
        using (ImRaii.PushColor(Text, Luminosity(color) < 0.5f ? new Vector4(1) : new(0, 0, 0, 1))
                     .Push(Button, color)
                     .Push(ButtonActive, color)
                     .Push(ButtonHovered, color))
        {
            ImGui.SmallButton(fmt);
        }

        return;

        static double Luminosity(Vector4 vector4) =>
            Math.Pow(
                (Math.Pow(vector4.X, 2) * 0.299f) +
                (Math.Pow(vector4.Y, 2) * 0.587f) +
                (Math.Pow(vector4.Z, 2) * 0.114f),
                0.5f) * vector4.W;
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

        using var tt = ImRaii.Tooltip();

        if (tt.Success)
        {
            ImGui.Text(tooltips[index]);
        }

        return true;
    }

    /// <summary>
    /// Draws a separator with some padding above and below.
    /// </summary>
    /// <param name="mask">Governs whether to pad above, below, or both.</param>
    /// <param name="padding">The amount of padding.</param>
    internal static void PaddedSeparator(uint mask = 0b11, float padding = 5f)
    {
        if ((mask & 0b10) > 0)
        {
            ImGui.Dummy(new(padding * ImGui.GetIO().FontGlobalScale));
        }

        ImGui.Separator();
        if ((mask & 0b01) > 0)
        {
            ImGui.Dummy(new(padding * ImGui.GetIO().FontGlobalScale));
        }
    }
}
