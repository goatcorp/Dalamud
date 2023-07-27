using System.Collections.ObjectModel;

using Dalamud.Game.Command;

namespace Dalamud.Plugin.Services;

/// <summary>
/// This class manages registered in-game slash commands.
/// </summary>
public interface ICommandManager
{
    /// <summary>
    /// Gets a read-only list of all registered commands.
    /// </summary>
    public ReadOnlyDictionary<string, CommandInfo> Commands { get; }

    /// <summary>
    /// Process a command in full.
    /// </summary>
    /// <param name="content">The full command string.</param>
    /// <returns>True if the command was found and dispatched.</returns>
    public bool ProcessCommand(string content);

    /// <summary>
    /// Dispatch the handling of a command.
    /// </summary>
    /// <param name="command">The command to dispatch.</param>
    /// <param name="argument">The provided arguments.</param>
    /// <param name="info">A <see cref="CommandInfo"/> object describing this command.</param>
    public void DispatchCommand(string command, string argument, CommandInfo info);

    /// <summary>
    /// Add a command handler, which you can use to add your own custom commands to the in-game chat.
    /// </summary>
    /// <param name="command">The command to register.</param>
    /// <param name="info">A <see cref="CommandInfo"/> object describing the command.</param>
    /// <returns>If adding was successful.</returns>
    public bool AddHandler(string command, CommandInfo info);

    /// <summary>
    /// Remove a command from the command handlers.
    /// </summary>
    /// <param name="command">The command to remove.</param>
    /// <returns>If the removal was successful.</returns>
    public bool RemoveHandler(string command);
}
