using System.Numerics;

using Dalamud.Data;

using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget for displaying all UI Colors from Lumina.
/// </summary>
internal class UIColorWidget : IDataWindowWidget
{
    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = { "uicolor" };
    
    /// <inheritdoc/>
    public string DisplayName { get; init; } = "UIColor"; 

    /// <inheritdoc/>
    public bool Ready { get; set; }

    /// <inheritdoc/>
    public void Load()
    {
        this.Ready = true;
    }

    /// <inheritdoc/>
    public void Draw()
    {
        var colorSheet = Service<DataManager>.Get().GetExcelSheet<UIColor>();
        if (colorSheet is null) return;

        foreach (var color in colorSheet)
        {
            this.DrawUiColor(color);
        }
    }
    
    private void DrawUiColor(UIColor color)
    {
        ImGui.Text($"[{color.RowId:D3}] ");
        ImGui.SameLine();
        ImGui.TextColored(this.ConvertToVector4(color.Unknown2), $"Unknown2 ");
        ImGui.SameLine();
        ImGui.TextColored(this.ConvertToVector4(color.UIForeground), "UIForeground ");
        ImGui.SameLine();
        ImGui.TextColored(this.ConvertToVector4(color.Unknown3), "Unknown3 ");
        ImGui.SameLine();
        ImGui.TextColored(this.ConvertToVector4(color.UIGlow), "UIGlow");
    }
    
    private Vector4 ConvertToVector4(uint color)
    {
        var r = (byte)(color >> 24);
        var g = (byte)(color >> 16);
        var b = (byte)(color >> 8);
        var a = (byte)color;

        return new Vector4(r / 255.0f, g / 255.0f, b / 255.0f, a / 255.0f);
    }
}
