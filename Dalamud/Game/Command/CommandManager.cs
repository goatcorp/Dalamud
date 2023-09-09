using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Plugin.Services;
using Serilog;

namespace Dalamud.Game.Command;

/// <summary>
/// This class manages registered in-game slash commands.
/// </summary>
[PluginInterface]
[InterfaceVersion("1.0")]
[ServiceManager.BlockingEarlyLoadedService]
#pragma warning disable SA1015
[ResolveVia<ICommandManager>]
#pragma warning restore SA1015
public sealed class CommandManager : IServiceType, IDisposable, ICommandManager
{
    private readonly ConcurrentDictionary<string, CommandInfo> commandMap = new();
    private readonly Regex commandRegexEn = new(@"^The command (?<command>.+) does not exist\.$", RegexOptions.Compiled);
    private readonly Regex commandRegexJp = new(@"^そのコマンドはありません。： (?<command>.+)$", RegexOptions.Compiled);
    private readonly Regex commandRegexDe = new(@"^„(?<command>.+)“ existiert nicht als Textkommando\.$", RegexOptions.Compiled);
    private readonly Regex commandRegexFr = new(@"^La commande texte “(?<command>.+)” n'existe pas\.$", RegexOptions.Compiled);
    private readonly Regex commandRegexCn = new(@"^^(“|「)(?<command>.+)(”|」)(出现问题：该命令不存在|出現問題：該命令不存在)。$", RegexOptions.Compiled);
    private readonly Regex commandRegexKo = new(@"^(?<command>.+)[은는] 존재하지 않는 명령어입니다\.$", RegexOptions.Compiled);
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
            ClientLanguage.Korean => this.commandRegexKo,
            _ => this.currentLangCommandRegex,
        };

        this.chatGui.CheckMessageHandled += this.OnCheckMessageHandled;
    }

    /// <inheritdoc/>
    public ReadOnlyDictionary<string, CommandInfo> Commands => new(this.commandMap);

    /// <inheritdoc/>
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

        if (!this.commandMap.TryGetValue(command, out var handler)) // Command was not found.
            return false;

        this.DispatchCommand(command, argument, handler);
        return true;
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public bool AddHandler(string command, CommandInfo info)
    {
        if (info == null)
            throw new ArgumentNullException(nameof(info), "Command handler is null.");

        if (!this.commandMap.TryAdd(command, info))
        {
            Log.Error("Command {CommandName} is already registered.", command);
            return false;
        }

        return true;
    }

    /// <inheritdoc/>
    public bool RemoveHandler(string command)
    {
        return this.commandMap.Remove(command, out _);
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
