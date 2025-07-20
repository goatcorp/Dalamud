using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Aetherytes;

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
        if (!ImGui.BeginTable("##aetheryteTable"u8, 11, ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders))
            return;

        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableSetupColumn("Idx"u8, ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Name"u8, ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("ID"u8, ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Zone"u8, ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Ward"u8, ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Plot"u8, ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Sub"u8, ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Gil"u8, ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Fav"u8, ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Shared"u8, ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Apartment"u8, ImGuiTableColumnFlags.WidthFixed);
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
            ImGui.TextUnformatted($"{info.AetheryteData.ValueNullable?.PlaceName.ValueNullable?.Name}");

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
            ImGui.TextUnformatted($"{info.IsApartment}");
        }

        ImGui.EndTable();
    }
}
