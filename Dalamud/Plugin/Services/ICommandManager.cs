using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

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
    public ReadOnlyDictionary<string, IReadOnlyCommandInfo> Commands { get; }

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
    public void DispatchCommand(string command, string argument, IReadOnlyCommandInfo info);

    /// <summary>
    /// Add a command handler, which you can use to add your own custom commands to the in-game chat.
    /// </summary>
    /// <param name="command">The command to register.</param>
    /// <param name="info">A <see cref="CommandInfo"/> object describing the command.</param>
    /// <returns>If adding was successful.</returns>
    public bool AddHandler(string command, CommandInfo info);

    /// <summary>
    /// Register a chat command. Arguments to the command are parsed for you, based on the arguments to the function
    /// passed into the <paramref name="func"/> parameter.
    /// </summary>
    /// <param name="commandName">The name of the command.</param>
    /// <param name="helpMessage">The help message shown to users in chat or the installer.</param>
    /// <param name="func">The function to be called when the chat command is executed. Arguments to the command are derived from the parameters of this function.</param>
    /// <param name="showInHelp">Whether or not this command should be shown to users.</param>
    /// <param name="displayOrder">The display order of this command. Defaults to alphabetical ordering.</param>
    /// <returns>If adding was successful.</returns>
    [Experimental("Dalamud001")]
    public bool AddCommand(string commandName, string helpMessage, Delegate func, bool showInHelp = true, int displayOrder = -1);

    /// <summary>
    /// Remove a command from the command handlers.
    /// </summary>
    /// <param name="command">The command to remove.</param>
    /// <returns>If the removal was successful.</returns>
    public bool RemoveHandler(string command);
}
