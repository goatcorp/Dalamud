using Dalamud.Game.ClientState.Aetherytes;

using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget for displaying aetheryte table.
/// </summary>
internal class AetherytesWidget : IDataWindowWidget
{
    /// <inheritdoc/>
    public bool Ready { get; set; }

    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = { "aetherytes" };
    
    /// <inheritdoc/>
    public string DisplayName { get; init; } = "Aetherytes"; 

    /// <inheritdoc/>
    public void Load()
    {
        this.Ready = true;
    }

    /// <inheritdoc/>
    public void Draw()
    {
        if (!ImGui.BeginTable("##aetheryteTable", 11, ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders))
            return;

        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableSetupColumn("Idx", ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("ID", ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Zone", ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Ward", ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Plot", ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Sub", ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Gil", ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Fav", ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Shared", ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Apartment", ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableHeadersRow();

        var tpList = Service<AetheryteList>.Get();

        for (var i = 0; i < tpList.Length; i++)
        {
            var info = tpList[i];
            if (info == null)
                continue;

            ImGui.TableNextColumn(); // Idx
            ImGui.TextUnformatted($"{i}");

            ImGui.TableNextColumn(); // Name
            ImGui.TextUnformatted($"{info.AetheryteData.GameData?.PlaceName.Value?.Name}");

            ImGui.TableNextColumn(); // ID
            ImGui.TextUnformatted($"{info.AetheryteId}");

            ImGui.TableNextColumn(); // Zone
            ImGui.TextUnformatted($"{info.TerritoryId}");

            ImGui.TableNextColumn(); // Ward
            ImGui.TextUnformatted($"{info.Ward}");

            ImGui.TableNextColumn(); // Plot
            ImGui.TextUnformatted($"{info.Plot}");

            ImGui.TableNextColumn(); // Sub
            ImGui.TextUnformatted($"{info.SubIndex}");

            ImGui.TableNextColumn(); // Gil
            ImGui.TextUnformatted($"{info.GilCost}");

            ImGui.TableNextColumn(); // Favourite
            ImGui.TextUnformatted($"{info.IsFavourite}");

            ImGui.TableNextColumn(); // Shared
            ImGui.TextUnformatted($"{info.IsSharedHouse}");

            ImGui.TableNextColumn(); // Apartment
            ImGui.TextUnformatted($"{info.IsAppartment}");
        }

        ImGui.EndTable();
    }
}
