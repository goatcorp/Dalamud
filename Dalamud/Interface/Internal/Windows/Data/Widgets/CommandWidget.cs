using Dalamud.Game.Command;
using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.Data;

/// <summary>
/// Widget for displaying command info.
/// </summary>
internal class CommandWidget : IDataWindowWidget
{
    /// <inheritdoc/>
    public DataKind DataKind { get; init; } = DataKind.Command;

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

        foreach (var command in commandManager.Commands)
        {
            ImGui.Text($"{command.Key}\n    -> {command.Value.HelpMessage}\n    -> In help: {command.Value.ShowInHelp}\n\n");
        }
    }
}
