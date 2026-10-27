using System.Globalization;

using Dalamud.Bindings.ImGui;
using Dalamud.Data;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Text;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget for displaying coordinates and map links.
/// </summary>
internal class CoordinatesWidget : IDataWindowWidget
{
    private uint levelRowId;

    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = [];

    /// <inheritdoc/>
    public string DisplayName { get; init; } = "Coordinates";

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
        var objectTable = Service<ObjectTable>.GetNullable();

        if (objectTable?.LocalPlayer != null)
        {
            ImGui.Text("ObjectTable.LocalPlayer:"u8);
            ImGui.SameLine();
            ImGuiHelpers.SeStringWrapped(SeString.CreateMapLink(objectTable.LocalPlayer));

            ImGui.Text("ObjectTable.LocalPlayer (WorldCoordinate):"u8);
            ImGui.SameLine();
            DrawCoordinate(objectTable.LocalPlayer.GetWorldCoordinate());

            ImGui.Text("ObjectTable.LocalPlayer (MapCoordinate):"u8);
            ImGui.SameLine();
            DrawCoordinate(objectTable.LocalPlayer.GetMapCoordinate());

            ImGui.Text("ObjectTable.LocalPlayer (MapCoordinate w/o Z):"u8);
            ImGui.SameLine();
            DrawCoordinate(objectTable.LocalPlayer.GetMapCoordinate() with { Z = null });

            ImGui.Text("ObjectTable.LocalPlayer (MapCoordinate->WorldCoordinate):"u8);
            ImGui.SameLine();
            DrawCoordinate(objectTable.LocalPlayer.GetMapCoordinate().ToWorldCoordinate());

            ImGui.Text("ObjectTable.LocalPlayer (MapCoordinate->WorldCoordinate->MapCoordinate):"u8);
            ImGui.SameLine();
            DrawCoordinate(objectTable.LocalPlayer.GetMapCoordinate().ToWorldCoordinate().ToMapCoordinate());
        }
        else
        {
            ImGui.Text("LocalPlayer not found"u8);
        }

        ImGui.Separator();

        using var tabbar = ImRaii.TabBar("LevelExamples"u8);
        if (!tabbar) return;

        using (var tab = ImRaii.TabItem("Examples"))
        {
            if (tab)
            {
                this.DrawLevel(6940301);
                this.DrawLevel(3777844);
            }
        }

        using (var tab = ImRaii.TabItem("Custom"))
        {
            if (tab)
            {
                ImGui.Text("Level RowId:");
                ImGui.SameLine();
                ImGui.InputUInt("##LevelRowId", ref this.levelRowId);
                this.DrawLevel(this.levelRowId);
            }
        }
    }

    private static void DrawCoordinate(WorldCoordinate wc)
    {
        ImGuiHelpers.SeStringWrapped(SeString.CreateMapLink(wc));

        using (ImRaii.PushIndent())
        {
            ImGui.Text("TerritoryId: "u8);
            ImGui.SameLine(0, 0);
            WidgetUtil.DrawCopyableText($"{wc.TerritoryId}");
            ImGui.SameLine(0, 0);
            ImGui.Text(" | MapId: "u8);
            ImGui.SameLine(0, 0);
            WidgetUtil.DrawCopyableText($"{wc.MapId}");
            ImGui.SameLine(0, 0);
            ImGui.Text(" | InstanceId: "u8);
            ImGui.SameLine(0, 0);
            WidgetUtil.DrawCopyableText($"{wc.InstanceId}");
            ImGui.SameLine(0, 0);
            ImGui.Text(" | X: "u8);
            ImGui.SameLine(0, 0);
            WidgetUtil.DrawCopyableText($"{wc.X.ToString(CultureInfo.InvariantCulture)}");
            ImGui.SameLine(0, 0);
            ImGui.Text(" | Y: "u8);
            ImGui.SameLine(0, 0);
            WidgetUtil.DrawCopyableText($"{wc.Y.ToString(CultureInfo.InvariantCulture)}");
            ImGui.SameLine(0, 0);
            ImGui.Text(" | Z: "u8);
            ImGui.SameLine(0, 0);
            WidgetUtil.DrawCopyableText($"{wc.Z.ToString(CultureInfo.InvariantCulture)}");
        }
    }

    private static void DrawCoordinate(MapCoordinate mc)
    {
        ImGuiHelpers.SeStringWrapped(SeString.CreateMapLink(mc));

        using (ImRaii.PushIndent())
        {
            ImGui.Text("TerritoryId: "u8);
            ImGui.SameLine(0, 0);
            WidgetUtil.DrawCopyableText($"{mc.TerritoryId}");
            ImGui.SameLine(0, 0);
            ImGui.Text(" | MapId: "u8);
            ImGui.SameLine(0, 0);
            WidgetUtil.DrawCopyableText($"{mc.MapId}");
            ImGui.SameLine(0, 0);
            ImGui.Text(" | InstanceId: "u8);
            ImGui.SameLine(0, 0);
            WidgetUtil.DrawCopyableText($"{mc.InstanceId}");
            ImGui.SameLine(0, 0);
            ImGui.Text(" | X: "u8);
            ImGui.SameLine(0, 0);
            WidgetUtil.DrawCopyableText($"{mc.X.ToString(CultureInfo.InvariantCulture)}");
            ImGui.SameLine(0, 0);
            ImGui.Text(" | Y: "u8);
            ImGui.SameLine(0, 0);
            WidgetUtil.DrawCopyableText($"{mc.Y.ToString(CultureInfo.InvariantCulture)}");
            ImGui.SameLine(0, 0);
            ImGui.Text(" | Z: "u8);
            ImGui.SameLine(0, 0);
            WidgetUtil.DrawCopyableText($"{mc.Z?.ToString(CultureInfo.InvariantCulture)}");
        }
    }

    private void DrawLevel(uint levelRowId)
    {
        var dataManager = Service<DataManager>.GetNullable();
        if (dataManager == null)
            return;

        using var treeNode = ImRaii.TreeNode($"Level#{levelRowId}##Level{levelRowId}", ImGuiTreeNodeFlags.SpanAvailWidth | ImGuiTreeNodeFlags.DefaultOpen);
        if (!treeNode) return;

        if (!dataManager.GetExcelSheet<Excel.Sheets.Level>().TryGetRow(levelRowId, out var levelRow))
        {
            ImGui.Text("Row not found"u8);
            return;
        }

        if (dataManager.GetExcelSheet<Excel.Sheets.TerritoryTypeTransient>().TryGetRow(levelRow.Territory.RowId, out var transientRow))
        {
            ImGui.Text($"TerritoryTypeTransient.OffsetZ = {transientRow.OffsetZ}");

            if (transientRow.OffsetZ == MapUtil.NoOffsetZ)
            {
                ImGui.SameLine();
                ImGui.Text("(MapUtil.NoOffsetZ)"u8);
            }
        }
        else
        {
            ImGui.Text("No TerritoryTypeTransient row"u8);
        }

        ImGui.Text("WorldCoordinate:"u8);
        ImGui.SameLine();
        DrawCoordinate(levelRow.ToWorldCoordinate());

        ImGui.Text("MapCoordinate:"u8);
        ImGui.SameLine();
        DrawCoordinate(levelRow.ToMapCoordinate());

        ImGui.Text("MapCoordinate->WorldCoordinate:"u8);
        ImGui.SameLine();
        DrawCoordinate(levelRow.ToMapCoordinate().ToWorldCoordinate());

        ImGui.Text("MapCoordinate->WorldCoordinate->MapCoordinate:"u8);
        ImGui.SameLine();
        DrawCoordinate(levelRow.ToMapCoordinate().ToWorldCoordinate().ToMapCoordinate());

        ImGui.Separator();
    }
}
