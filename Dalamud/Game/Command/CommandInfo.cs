namespace Dalamud.Game.Command;

/// <summary>
/// This class describes a registered command.
/// </summary>
public sealed class CommandInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandInfo"/> class.
    /// Create a new CommandInfo with the provided handler.
    /// </summary>
    /// <param name="handler">The method to call when the command is run.</param>
    public CommandInfo(HandlerDelegate handler)
    {
        this.Handler = handler;
    }

    /// <summary>
    /// The function to be executed when the command is dispatched.
    /// </summary>
    /// <param name="command">The command itself.</param>
    /// <param name="arguments">The arguments supplied to the command, ready for parsing.</param>
    public delegate void HandlerDelegate(string command, string arguments);

    /// <summary>
    /// Gets a <see cref="HandlerDelegate"/> which will be called when the command is dispatched.
    /// </summary>
    public HandlerDelegate Handler { get; }

    /// <summary>
    /// Gets or sets the help message for this command.
    /// </summary>
    public string HelpMessage { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether if this command should be shown in the help output.
    /// </summary>
    public bool ShowInHelp { get; set; } = true;

    /// <summary>
    /// Gets or sets the name of the assembly responsible for this command.
    /// </summary>
    internal string LoaderAssemblyName { get; set; } = string.Empty;
}
