using System.Buffers.Binary;
using System.Linq;
using System.Numerics;

using Dalamud.Data;
using Dalamud.Interface.ImGuiSeStringRenderer.Internal;
using Dalamud.Storage.Assets;

using ImGuiNET;

using Lumina.Excel.GeneratedSheets;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget for displaying all UI Colors from Lumina.
/// </summary>
internal class UiColorWidget : IDataWindowWidget
{
    private UIColor[]? colors;

    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = ["uicolor"];

    /// <inheritdoc/>
    public string DisplayName { get; init; } = "UIColor";

    /// <inheritdoc/>
    public bool Ready { get; set; }

    /// <inheritdoc/>
    public void Load()
    {
        this.Ready = true;
        this.colors = null;
    }

    /// <inheritdoc/>
    public unsafe void Draw()
    {
        this.colors ??= Service<DataManager>.Get().GetExcelSheet<UIColor>()?.ToArray();
        if (this.colors is null) return;

        ImGui.TextUnformatted("Color notation is #RRGGBB.");
        if (!ImGui.BeginTable("UIColor", 5))
            return;

        ImGui.TableSetupScrollFreeze(0, 1);
        var basew = ImGui.CalcTextSize("9").X;
        ImGui.TableSetupColumn("Row ID", ImGuiTableColumnFlags.WidthFixed, basew * 7);
        ImGui.TableSetupColumn("Dark", ImGuiTableColumnFlags.WidthFixed, basew * 17);
        ImGui.TableSetupColumn("Light", ImGuiTableColumnFlags.WidthFixed, basew * 17);
        ImGui.TableSetupColumn("Classic FF", ImGuiTableColumnFlags.WidthFixed, basew * 17);
        ImGui.TableSetupColumn("Clear Blue", ImGuiTableColumnFlags.WidthFixed, basew * 17);
        ImGui.TableHeadersRow();

        var clipper = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
        clipper.Begin(this.colors.Length);
        while (clipper.Step())
        {
            for (var i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
            {
                var id = this.colors[i].RowId;
                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted($"{id}");

                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                ImGui.PushID($"row{id}_col1");
                DrawColorColumn(this.colors[i].UIForeground);
                if (id is >= 500 and < 580)
                    DrawEdgePreview(id, this.colors[i].UIForeground, this.colors[i + 1].UIForeground);
                ImGui.PopID();

                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                ImGui.PushID($"row{id}_col2");
                DrawColorColumn(this.colors[i].UIGlow);
                if (id is >= 500 and < 580)
                    DrawEdgePreview(id, this.colors[i].UIGlow, this.colors[i + 1].UIGlow);
                ImGui.PopID();

                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                ImGui.PushID($"row{id}_col3");
                DrawColorColumn(this.colors[i].Unknown2);
                if (id is >= 500 and < 580)
                    DrawEdgePreview(id, this.colors[i].Unknown2, this.colors[i + 1].Unknown2);
                ImGui.PopID();

                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                ImGui.PushID($"row{id}_col4");
                DrawColorColumn(this.colors[i].Unknown3);
                if (id is >= 500 and < 580)
                    DrawEdgePreview(id, this.colors[i].Unknown3, this.colors[i + 1].Unknown3);
                ImGui.PopID();
            }
        }

        clipper.Destroy();
        ImGui.EndTable();
    }

    private static void DrawColorColumn(uint sheetColor)
    {
        sheetColor = BinaryPrimitives.ReverseEndianness(sheetColor);
        ImGui.Image(
            Service<DalamudAssetManager>.Get().White4X4.ImGuiHandle,
            new(ImGui.GetFrameHeight()),
            Vector2.Zero,
            Vector2.One,
            ImGui.ColorConvertU32ToFloat4(sheetColor | 0xFF000000u));
        ImGui.SameLine();
        ImGui.TextUnformatted($"#{sheetColor & 0xFF:X02}{(sheetColor >> 8) & 0xFF:X02}{(sheetColor >> 16) & 0xFF:X02}");
    }

    private static void DrawEdgePreview(uint id, uint sheetColor, uint sheetColor2)
    {
        ImGui.SameLine();
        if (Service<SeStringRenderer>.Get().Draw(
                new("+E"u8),
                new()
                {
                    Edge = true,
                    Color = BinaryPrimitives.ReverseEndianness(sheetColor) | 0xFF000000u,
                    EdgeColor = BinaryPrimitives.ReverseEndianness(sheetColor2) | 0xFF000000u,
                },
                "+E"u8).Clicked)
            ImGui.SetClipboardText($"<colortype({id})><edgecolortype({id + 1})>+E<edgecolortype(0)><colortype(0)>");
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip($"<colortype({id})><edgecolortype({id + 1})>+E<edgecolortype(0)><colortype(0)>");

        ImGui.SameLine();
        ImGui.AlignTextToFramePadding();
        if (Service<SeStringRenderer>.Get().Draw(
                new("+F"u8),
                new()
                {
                    Edge = true,
                    Color = BinaryPrimitives.ReverseEndianness(sheetColor2) | 0xFF000000u,
                    EdgeColor = BinaryPrimitives.ReverseEndianness(sheetColor) | 0xFF000000u,
                },
                "+F"u8).Clicked)
            ImGui.SetClipboardText($"<colortype({id + 1})><edgecolortype({id})>+E<edgecolortype(0)><colortype(0)>");
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip($"<colortype({id + 1})><edgecolortype({id})>+E<edgecolortype(0)><colortype(0)>");
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
