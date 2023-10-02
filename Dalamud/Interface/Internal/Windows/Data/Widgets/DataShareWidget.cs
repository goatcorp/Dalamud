using Dalamud.Interface.Utility;
using Dalamud.Plugin.Ipc.Internal;
using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget for displaying plugin data share modules.
/// </summary>
internal class DataShareWidget : IDataWindowWidget
{
    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = { "datashare" };
    
    /// <inheritdoc/>
    public string DisplayName { get; init; } = "Data Share"; 

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
        if (!ImGui.BeginTable("###DataShareTable", 4, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg))
            return;

        try
        {
            ImGui.TableSetupColumn("Shared Tag");
            ImGui.TableSetupColumn("Creator Assembly");
            ImGui.TableSetupColumn("#", ImGuiTableColumnFlags.WidthFixed, 30 * ImGuiHelpers.GlobalScale);
            ImGui.TableSetupColumn("Consumers");
            ImGui.TableHeadersRow();
            foreach (var share in Service<DataShare>.Get().GetAllShares())
            {
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(share.Tag);
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(share.CreatorAssembly);
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(share.Users.Length.ToString());
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(string.Join(", ", share.Users));
            }
        }
        finally
        {
            ImGui.EndTable();
        }
    }
}
