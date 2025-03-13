using System.Buffers.Binary;
using System.Numerics;
using System.Text;

using Dalamud.Data;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.ImGuiNotification.Internal;
using Dalamud.Interface.ImGuiSeStringRenderer.Internal;

using ImGuiNET;

using Lumina.Excel.Sheets;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget for displaying all UI Colors from Lumina.
/// </summary>
internal class UiColorWidget : IDataWindowWidget
{
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
    }

    /// <inheritdoc/>
    public unsafe void Draw()
    {
        var colors = Service<DataManager>.GetNullable()?.GetExcelSheet<UIColor>()
            ?? throw new InvalidOperationException("UIColor sheet not loaded.");

        Service<SeStringRenderer>.Get().CompileAndDrawWrapped(
            "· Color notation is #" +
            "<edgecolor(0xFFEEEE)><color(0xFF0000)>RR<color(stackcolor)><edgecolor(stackcolor)>" +
            "<edgecolor(0xEEFFEE)><color(0x00FF00)>GG<color(stackcolor)><edgecolor(stackcolor)>" +
            "<edgecolor(0xEEEEFF)><color(0x0000FF)>BB<color(stackcolor)><edgecolor(stackcolor)>.<br>" +
            "· Click on a color to copy the color code.<br>" +
            "· Hover on a color to preview the text with edge, when the next color has been used together.");
        if (!ImGui.BeginTable("UIColor", 5))
            return;

        ImGui.TableSetupScrollFreeze(0, 1);
        var rowidw = ImGui.CalcTextSize("9999999").X;
        var colorw = ImGui.CalcTextSize("#999999").X;
        colorw = Math.Max(colorw, ImGui.CalcTextSize("#AAAAAA").X);
        colorw = Math.Max(colorw, ImGui.CalcTextSize("#BBBBBB").X);
        colorw = Math.Max(colorw, ImGui.CalcTextSize("#CCCCCC").X);
        colorw = Math.Max(colorw, ImGui.CalcTextSize("#DDDDDD").X);
        colorw = Math.Max(colorw, ImGui.CalcTextSize("#EEEEEE").X);
        colorw = Math.Max(colorw, ImGui.CalcTextSize("#FFFFFF").X);
        colorw += ImGui.GetFrameHeight() + ImGui.GetStyle().FramePadding.X;
        ImGui.TableSetupColumn("Row ID", ImGuiTableColumnFlags.WidthFixed, rowidw);
        ImGui.TableSetupColumn("Dark", ImGuiTableColumnFlags.WidthFixed, colorw);
        ImGui.TableSetupColumn("Light", ImGuiTableColumnFlags.WidthFixed, colorw);
        ImGui.TableSetupColumn("Classic FF", ImGuiTableColumnFlags.WidthFixed, colorw);
        ImGui.TableSetupColumn("Clear Blue", ImGuiTableColumnFlags.WidthFixed, colorw);
        ImGui.TableHeadersRow();

        var clipper = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
        clipper.Begin(colors.Count, ImGui.GetFrameHeightWithSpacing());
        while (clipper.Step())
        {
            for (var i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
            {
                var row = colors.GetRowAt(i);
                UIColor? adjacentRow = null;
                if (i + 1 < colors.Count)
                {
                    var adjRow = colors.GetRowAt(i + 1);
                    if (adjRow.RowId == row.RowId + 1)
                    {
                        adjacentRow = adjRow;
                    }
                }

                var id = row.RowId;

                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted($"{id}");

                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                ImGui.PushID($"row{id}_col1");
                if (this.DrawColorColumn(row.UIForeground) &&
                    adjacentRow.HasValue)
                    DrawEdgePreview(id, row.UIForeground, adjacentRow.Value.UIForeground);
                ImGui.PopID();

                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                ImGui.PushID($"row{id}_col2");
                if (this.DrawColorColumn(row.UIGlow) &&
                    adjacentRow.HasValue)
                    DrawEdgePreview(id, row.UIGlow, adjacentRow.Value.UIGlow);
                ImGui.PopID();

                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                ImGui.PushID($"row{id}_col3");
                if (this.DrawColorColumn(row.Unknown0) &&
                    adjacentRow.HasValue)
                    DrawEdgePreview(id, row.Unknown0, adjacentRow.Value.Unknown0);
                ImGui.PopID();

                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                ImGui.PushID($"row{id}_col4");
                if (this.DrawColorColumn(row.Unknown1) &&
                    adjacentRow.HasValue)
                    DrawEdgePreview(id, row.Unknown1, adjacentRow.Value.Unknown1);
                ImGui.PopID();
            }
        }

        clipper.Destroy();
        ImGui.EndTable();
    }

    private static void DrawEdgePreview(uint id, uint sheetColor, uint sheetColor2)
    {
        ImGui.BeginTooltip();
        Span<byte> buf = stackalloc byte[256];
        var ptr = 0;
        ptr += Encoding.UTF8.GetBytes("<colortype(", buf[ptr..]);
        id.TryFormat(buf[ptr..], out var bytesWritten);
        ptr += bytesWritten;
        ptr += Encoding.UTF8.GetBytes(")><edgecolortype(", buf[ptr..]);
        (id + 1).TryFormat(buf[ptr..], out bytesWritten);
        ptr += bytesWritten;
        ptr += Encoding.UTF8.GetBytes(")>", buf[ptr..]);
        Service<SeStringRenderer>.Get().Draw(
            buf[..ptr],
            new()
            {
                Edge = true,
                Color = BinaryPrimitives.ReverseEndianness(sheetColor) | 0xFF000000u,
                EdgeColor = BinaryPrimitives.ReverseEndianness(sheetColor2) | 0xFF000000u,
                WrapWidth = float.PositiveInfinity,
            });

        ptr = 0;
        ptr += Encoding.UTF8.GetBytes("<colortype(", buf[ptr..]);
        (id + 1).TryFormat(buf[ptr..], out bytesWritten);
        ptr += bytesWritten;
        ptr += Encoding.UTF8.GetBytes(")><edgecolortype(", buf[ptr..]);
        id.TryFormat(buf[ptr..], out bytesWritten);
        ptr += bytesWritten;
        ptr += Encoding.UTF8.GetBytes(")>", buf[ptr..]);
        Service<SeStringRenderer>.Get().Draw(
            buf[..ptr],
            new()
            {
                Edge = true,
                Color = BinaryPrimitives.ReverseEndianness(sheetColor2) | 0xFF000000u,
                EdgeColor = BinaryPrimitives.ReverseEndianness(sheetColor) | 0xFF000000u,
                WrapWidth = float.PositiveInfinity,
            });
        ImGui.EndTooltip();
    }

    private bool DrawColorColumn(uint sheetColor)
    {
        sheetColor = BinaryPrimitives.ReverseEndianness(sheetColor);
        var rgbtext = $"#{sheetColor & 0xFF:X02}{(sheetColor >> 8) & 0xFF:X02}{(sheetColor >> 16) & 0xFF:X02}";
        var size = new Vector2(ImGui.GetFrameHeight());
        size.X += ImGui.CalcTextSize(rgbtext).X + ImGui.GetStyle().FramePadding.X;

        var off = ImGui.GetCursorScreenPos();
        ImGui.GetWindowDrawList().AddRectFilled(
            off,
            off + new Vector2(size.Y),
            sheetColor | 0xFF000000u);
        ImGui.GetWindowDrawList().AddText(
            off + ImGui.GetStyle().FramePadding + new Vector2(size.Y, 0),
            ImGui.GetColorU32(ImGuiCol.Text),
            rgbtext);

        if (ImGui.InvisibleButton("##copy", size))
        {
            ImGui.SetClipboardText(rgbtext);
            Service<NotificationManager>.Get().AddNotification(
                new()
                {
                    Content = $"Copied \"{rgbtext}\".",
                    Title = this.DisplayName,
                    Type = NotificationType.Success,
                });
        }

        return ImGui.IsItemHovered();
    }
}
