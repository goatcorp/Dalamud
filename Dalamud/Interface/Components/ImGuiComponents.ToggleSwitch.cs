using System.Numerics;

using ImGuiNET;

namespace Dalamud.Interface.Components;

/// <summary>
/// Component for toggle buttons.
/// </summary>
public static partial class ImGuiComponents
{
    /// <summary>
    /// Draw a toggle button.
    /// </summary>
    /// <param name="id">The id of the button.</param>
    /// <param name="v">The state of the switch.</param>
    /// <returns>If the button has been interacted with this frame.</returns>
    public static bool ToggleButton(string id, ref bool v)
    {
        var colors = ImGui.GetStyle().Colors;
        var p = ImGui.GetCursorScreenPos();
        var drawList = ImGui.GetWindowDrawList();

        var height = ImGui.GetFrameHeight();
        var width = height * 1.55f;
        var radius = height * 0.50f;

        // TODO: animate

        var changed = false;
        ImGui.InvisibleButton(id, new Vector2(width, height));
        if (ImGui.IsItemClicked())
        {
            v = !v;
            changed = true;
        }

        if (ImGui.IsItemHovered())
            drawList.AddRectFilled(p, new Vector2(p.X + width, p.Y + height), ImGui.GetColorU32(!v ? colors[(int)ImGuiCol.ButtonActive] : new Vector4(0.78f, 0.78f, 0.78f, 1.0f)), height * 0.5f);
        else
            drawList.AddRectFilled(p, new Vector2(p.X + width, p.Y + height), ImGui.GetColorU32(!v ? colors[(int)ImGuiCol.Button] * 0.6f : new Vector4(0.35f, 0.35f, 0.35f, 1.0f)), height * 0.50f);
        drawList.AddCircleFilled(new Vector2(p.X + radius + ((v ? 1 : 0) * (width - (radius * 2.0f))), p.Y + radius), radius - 1.5f, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 1)));

        return changed;
    }

    /// <summary>
    /// Draw a disabled toggle button.
    /// </summary>
    /// <param name="id">The id of the button.</param>
    /// <param name="v">The state of the switch.</param>
    public static void DisabledToggleButton(string id, bool v)
    {
        var colors = ImGui.GetStyle().Colors;
        var p = ImGui.GetCursorScreenPos();
        var drawList = ImGui.GetWindowDrawList();

        var height = ImGui.GetFrameHeight();
        var width = height * 1.55f;
        var radius = height * 0.50f;

        // TODO: animate
        ImGui.InvisibleButton(id, new Vector2(width, height));

        var dimFactor = 0.5f;

        drawList.AddRectFilled(p, new Vector2(p.X + width, p.Y + height), ImGui.GetColorU32(v ? colors[(int)ImGuiCol.Button] * dimFactor : new Vector4(0.55f, 0.55f, 0.55f, 1.0f) * dimFactor), height * 0.50f);
        drawList.AddCircleFilled(new Vector2(p.X + radius + ((v ? 1 : 0) * (width - (radius * 2.0f))), p.Y + radius), radius - 1.5f, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 1) * dimFactor));
    }
}
