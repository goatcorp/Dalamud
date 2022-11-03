using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Serilog;

namespace Dalamud.Game.Command;

/// <summary>
/// This class manages registered in-game slash commands.
/// </summary>
[PluginInterface]
[InterfaceVersion("1.0")]
[ServiceManager.BlockingEarlyLoadedService]
public sealed class CommandManager : IServiceType, IDisposable
{
    private readonly Dictionary<string, CommandInfo> commandMap = new();
    private readonly Regex commandRegexEn = new(@"^The command (?<command>.+) does not exist\.$", RegexOptions.Compiled);
    private readonly Regex commandRegexJp = new(@"^そのコマンドはありません。： (?<command>.+)$", RegexOptions.Compiled);
    private readonly Regex commandRegexDe = new(@"^„(?<command>.+)“ existiert nicht als Textkommando\.$", RegexOptions.Compiled);
    private readonly Regex commandRegexFr = new(@"^La commande texte “(?<command>.+)” n'existe pas\.$", RegexOptions.Compiled);
    private readonly Regex commandRegexCn = new(@"^^(“|「)(?<command>.+)(”|」)(出现问题：该命令不存在|出現問題：該命令不存在)。$", RegexOptions.Compiled);
    private readonly Regex currentLangCommandRegex;

    [ServiceManager.ServiceDependency]
    private readonly ChatGui chatGui = Service<ChatGui>.Get();

    [ServiceManager.ServiceConstructor]
    private CommandManager(DalamudStartInfo startInfo)
    {
        this.currentLangCommandRegex = startInfo.Language switch
        {
            ClientLanguage.Japanese => this.commandRegexJp,
            ClientLanguage.English => this.commandRegexEn,
            ClientLanguage.German => this.commandRegexDe,
            ClientLanguage.French => this.commandRegexFr,
            _ => this.currentLangCommandRegex,
        };

        this.chatGui.CheckMessageHandled += this.OnCheckMessageHandled;
    }

    /// <summary>
    /// Gets a read-only list of all registered commands.
    /// </summary>
    public ReadOnlyDictionary<string, CommandInfo> Commands => new(this.commandMap);

    /// <summary>
    /// Process a command in full.
    /// </summary>
    /// <param name="content">The full command string.</param>
    /// <returns>True if the command was found and dispatched.</returns>
    public bool ProcessCommand(string content)
    {
        string command;
        string argument;

        var separatorPosition = content.IndexOf(' ');
        if (separatorPosition == -1 || separatorPosition + 1 >= content.Length)
        {
            // If no space was found or ends with the space. Process them as a no argument
            if (separatorPosition + 1 >= content.Length)
            {
                // Remove the trailing space
                command = content.Substring(0, separatorPosition);
            }
            else
            {
                command = content;
            }

            argument = string.Empty;
        }
        else
        {
            // e.g.)
            // /testcommand arg1
            // => Total of 17 chars
            // => command: 0-12 (12 chars)
            // => argument: 13-17 (4 chars)
            // => content.IndexOf(' ') == 12
            command = content.Substring(0, separatorPosition);

            var argStart = separatorPosition + 1;
            argument = content[argStart..];
        }

        if (!this.commandMap.TryGetValue(command, out var handler)) // Commad was not found.
            return false;

        this.DispatchCommand(command, argument, handler);
        return true;
    }

    /// <summary>
    /// Dispatch the handling of a command.
    /// </summary>
    /// <param name="command">The command to dispatch.</param>
    /// <param name="argument">The provided arguments.</param>
    /// <param name="info">A <see cref="CommandInfo"/> object describing this command.</param>
    public void DispatchCommand(string command, string argument, CommandInfo info)
    {
        try
        {
            info.Handler(command, argument);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while dispatching command {CommandName} (Argument: {Argument})", command, argument);
        }
    }

    /// <summary>
    /// Add a command handler, which you can use to add your own custom commands to the in-game chat.
    /// </summary>
    /// <param name="command">The command to register.</param>
    /// <param name="info">A <see cref="CommandInfo"/> object describing the command.</param>
    /// <returns>If adding was successful.</returns>
    public bool AddHandler(string command, CommandInfo info)
    {
        if (info == null)
            throw new ArgumentNullException(nameof(info), "Command handler is null.");

        try
        {
            this.commandMap.Add(command, info);
            return true;
        }
        catch (ArgumentException)
        {
            Log.Error("Command {CommandName} is already registered.", command);
            return false;
        }
    }

    /// <summary>
    /// Remove a command from the command handlers.
    /// </summary>
    /// <param name="command">The command to remove.</param>
    /// <returns>If the removal was successful.</returns>
    public bool RemoveHandler(string command)
    {
        return this.commandMap.Remove(command);
    }

    /// <inheritdoc/>
    void IDisposable.Dispose()
    {
        this.chatGui.CheckMessageHandled -= this.OnCheckMessageHandled;
    }

    private void OnCheckMessageHandled(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
    {
        if (type == XivChatType.ErrorMessage && senderId == 0)
        {
            var cmdMatch = this.currentLangCommandRegex.Match(message.TextValue).Groups["command"];
            if (cmdMatch.Success)
            {
                // Yes, it's a chat command.
                var command = cmdMatch.Value;
                if (this.ProcessCommand(command)) isHandled = true;
            }
            else
            {
                // Always match for china, since they patch in language files without changing the ClientLanguage.
                cmdMatch = this.commandRegexCn.Match(message.TextValue).Groups["command"];
                if (cmdMatch.Success)
                {
                    // Yes, it's a Chinese fallback chat command.
                    var command = cmdMatch.Value;
                    if (this.ProcessCommand(command)) isHandled = true;
                }
            }
        }
    }
}
