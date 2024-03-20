using System.Linq;

using Dalamud.Game.Command;
using Dalamud.Interface.Utility.Raii;

using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget for displaying command info.
/// </summary>
internal class CommandWidget : IDataWindowWidget
{
    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = { "command" };
    
    /// <inheritdoc/>
    public string DisplayName { get; init; } = "Command"; 

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
        var commandManager = Service<CommandManager>.Get();

        var tableFlags = ImGuiTableFlags.ScrollY | ImGuiTableFlags.Borders | ImGuiTableFlags.SizingStretchProp |
                         ImGuiTableFlags.Sortable | ImGuiTableFlags.SortTristate;
        using var table = ImRaii.Table("CommandList", 4, tableFlags);
        if (table)
        {
            ImGui.TableSetupScrollFreeze(0, 1);

            ImGui.TableSetupColumn("Command");
            ImGui.TableSetupColumn("Plugin");
            ImGui.TableSetupColumn("HelpMessage", ImGuiTableColumnFlags.NoSort);
            ImGui.TableSetupColumn("In Help?", ImGuiTableColumnFlags.NoSort);
            ImGui.TableHeadersRow();
            
            var sortSpecs = ImGui.TableGetSortSpecs();
            var commands = commandManager.Commands.ToArray();

            if (sortSpecs.SpecsCount != 0)
            {
                commands = sortSpecs.Specs.ColumnIndex switch
                {
                    0 => sortSpecs.Specs.SortDirection == ImGuiSortDirection.Ascending
                             ? commands.OrderBy(kv => kv.Key).ToArray()
                             : commands.OrderByDescending(kv => kv.Key).ToArray(),
                    1 => sortSpecs.Specs.SortDirection == ImGuiSortDirection.Ascending
                             ? commands.OrderBy(kv => kv.Value.LoaderAssemblyName).ToArray()
                             : commands.OrderByDescending(kv => kv.Value.LoaderAssemblyName).ToArray(),
                    _ => commands,
                };
            }

            foreach (var command in commands)
            {
                ImGui.TableNextRow();
            
                ImGui.TableSetColumnIndex(0);
                ImGui.Text(command.Key);
            
                ImGui.TableNextColumn();
                ImGui.Text(command.Value.LoaderAssemblyName);
            
                ImGui.TableNextColumn();
                ImGui.TextWrapped(command.Value.HelpMessage);
            
                ImGui.TableNextColumn();
                ImGui.Text(command.Value.ShowInHelp ? "Yes" : "No");
            }
        }
    }
}
