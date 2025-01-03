namespace Dalamud.Game.Command;

/// <summary>
/// Interface representing a registered command.
/// </summary>
public interface IReadOnlyCommandInfo
{
    /// <summary>
    /// The function to be executed when the command is dispatched.
    /// </summary>
    /// <param name="command">The command itself.</param>
    /// <param name="arguments">The arguments supplied to the command, ready for parsing.</param>
    public delegate void HandlerDelegate(string command, string arguments);

    /// <summary>
    /// Gets a <see cref="HandlerDelegate"/> which will be called when the command is dispatched.
    /// </summary>
    HandlerDelegate Handler { get; }

    /// <summary>
    /// Gets the help message for this command.
    /// </summary>
    string HelpMessage { get; }

    /// <summary>
    /// Gets a value indicating whether if this command should be shown in the help output.
    /// </summary>
    bool ShowInHelp { get; }

    /// <summary>
    /// Gets the display order of this command.
    /// </summary>
    int DisplayOrder { get; }
}

/// <summary>
/// This class describes a registered command.
/// </summary>
public sealed class CommandInfo : IReadOnlyCommandInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandInfo"/> class.
    /// Create a new CommandInfo with the provided handler.
    /// </summary>
    /// <param name="handler">The method to call when the command is run.</param>
    public CommandInfo(IReadOnlyCommandInfo.HandlerDelegate handler)
    {
        this.Handler = handler;
    }

    /// <inheritdoc/>
    public IReadOnlyCommandInfo.HandlerDelegate Handler { get; }

    /// <inheritdoc/>
    public string HelpMessage { get; set; } = string.Empty;

    /// <inheritdoc/>
    public bool ShowInHelp { get; set; } = true;

    /// <inheritdoc/>
    public int DisplayOrder { get; set; } = -1;
}
