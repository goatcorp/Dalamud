using Dalamud.Console;

namespace Dalamud.Game.Command;

/// <summary>
/// Interface representing a command.
/// </summary>
internal abstract class BaseCommand(IConsoleCommand command)
{
    /// <summary>
    /// Gets or sets the help message for this command.
    /// </summary>
    public string? HelpMessage { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether if this command should be shown in the help output.
    /// </summary>
    public bool ShowInHelp { get; set; }

    /// <summary>
    /// Gets or sets the display order of this command. Defaults to alphabetical ordering.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Gets or sets the console entry associated with this command.
    /// </summary>
    public IConsoleCommand ConsoleEntry { get; set; } = command;

    /// <summary>
    /// Gets or sets the WorkingPluginId of the plugin that registered this command.
    /// </summary>
    public Guid? OwnerPluginGuid { get; set; }
}
