using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using Dalamud.Console;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Plugin.Services;
using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.Shell;

namespace Dalamud.Game.Command;

/// <summary>
/// This class manages registered in-game slash commands.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal sealed unsafe class CommandManager : IInternalDisposableService, ICommandManager
{
    private static readonly ModuleLog Log = new("Command");

    private readonly ConcurrentDictionary<string, BaseChatCommand> commandMap = new();

    private readonly Hook<ShellCommands.Delegates.TryInvokeDebugCommand>? tryInvokeDebugCommandHook;

    [ServiceManager.ServiceDependency]
    private readonly ConsoleManager console = Service<ConsoleManager>.Get();

    [ServiceManager.ServiceConstructor]
    private CommandManager(Dalamud dalamud)
    {
        this.tryInvokeDebugCommandHook = Hook<ShellCommands.Delegates.TryInvokeDebugCommand>.FromAddress(
            (nint)ShellCommands.MemberFunctionPointers.TryInvokeDebugCommand,
            this.OnTryInvokeDebugCommand);
        this.tryInvokeDebugCommandHook.Enable();
    }

    /// <inheritdoc/>
    [Api13ToDo("Make this sensible. Don't use exposed API for internal structures.")]
    public ReadOnlyDictionary<string, IReadOnlyCommandInfo> Commands => this.commandMap.ToDictionary(x => x.Key,
        x =>
        {
            return x.Value switch
            {
                IReadOnlyCommandInfo commandInfo => commandInfo,
                ConsoleBackedChatCommand consoleEntry => new CommandInfo(null!)
                {
                    HelpMessage = consoleEntry.HelpMessage ?? string.Empty,
                    ShowInHelp = consoleEntry.ShowInHelp,
                    DisplayOrder = consoleEntry.DisplayOrder,
                },
                _ => throw new Exception("Unknown command type"),
            };
        }).AsReadOnly();

    /// <summary>
    /// Gets a read-only dictionary of all registered commands.
    /// </summary>
    [Api13ToDo("Make this sensible. Don't use exposed API for internal structures.")]
    public ReadOnlyDictionary<string, BaseChatCommand> CommandsNew => new(this.commandMap);

    /// <inheritdoc/>
    public bool ProcessCommand(string content)
    {
        string command;
        string argument;

        var separatorPosition = content.IndexOf(' ');
        if (separatorPosition == -1 || separatorPosition + 1 >= content.Length)
        {
            // If no space was found or ends with the space. Process them as a no argument
            // Remove the trailing space
            command = separatorPosition + 1 >= content.Length ?
                          content[..separatorPosition] :
                          content;

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
            command = content[..separatorPosition];

            var argStart = separatorPosition + 1;
            argument = content[argStart..];
        }

        if (!this.commandMap.TryGetValue(command, out var handler)) // Command was not found.
            return false;

        switch (handler)
        {
            case ConsoleBackedChatCommand:
                try
                {
                    // TODO: Localize, print errors to chat
                    this.console.ProcessCommand(content, diagnostic => diagnostic.WriteToLog());
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error while dispatching command {CommandName} (Argument: {Argument})", command, argument);
                }

                break;

            case LegacyHandlerChatCommand legacyHandler:
                this.DispatchCommand(command, argument, legacyHandler);

                break;
            default:
                Log.Error("Unknown command type for {CommandName}", command);
                return false;
        }

        return true;
    }

    /// <inheritdoc/>
    public void DispatchCommand(string command, string argument, IReadOnlyCommandInfo info)
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
    /// <param name="ownerPluginGuid">WorkingPluginId of the plugin that added this command.</param>
    /// <returns>If adding was successful.</returns>
    public bool AddHandler(string command, CommandInfo info, Guid? ownerPluginGuid)
    {
        if (info == null)
            throw new ArgumentNullException(nameof(info), "Command handler is null.");

        IConsoleCommand debugCommand;
        try
        {
            debugCommand = this.console.AddCommand(
                command,
                info.HelpMessage,
                (string args) => { this.DispatchCommand(command, args, info); });
        }
        catch (InvalidOperationException)
        {
            Log.Error("Could not register debug command for legacy command {CommandName}", command);
            return false;
        }

        var legacyCommandInfo = new LegacyHandlerChatCommand(info, debugCommand)
        {
            OwnerPluginGuid = ownerPluginGuid,
        };

        if (!this.commandMap.TryAdd(command, legacyCommandInfo))
        {
            Log.Error("Command {CommandName} is already registered", command);
            return false;
        }

        return true;
    }

    /// <inheritdoc/>
    public bool AddHandler(string command, CommandInfo info)
    {
        return this.AddHandler(command, info, null);
    }

    /// <inheritdoc/>
    [Experimental("Dalamud001")]
    public bool AddCommand(string commandName, string helpMessage, Delegate func, bool showInHelp = true, int displayOrder = -1)
    {
        return this.AddCommandInternal(commandName, helpMessage, func, showInHelp, displayOrder, null);
    }

    /// <inheritdoc/>
    [Api13ToDo("Rename to Remove().")]
    public bool RemoveHandler(string command)
    {
        var removed = this.commandMap.Remove(command, out var commandInfo);
        if (removed)
        {
            this.console.RemoveEntry(commandInfo.ConsoleEntry);
        }

        return removed;
    }

    /// <summary>
    /// Returns a list of commands given a specified WorkingPluginId.
    /// </summary>
    /// <param name="ownerPluginGuid">The WorkingPluginId of the plugin.</param>
    /// <returns>A list of commands and their associated activation string.</returns>
    public List<(string Command, BaseChatCommand CommandInfo)> GetHandlersByWorkingPluginId(
        Guid ownerPluginGuid)
    {
        return this.commandMap.Where(c => c.Value.OwnerPluginGuid == ownerPluginGuid)
                   .Select(x => (x.Key, x.Value))
                   .ToList();
    }

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        this.tryInvokeDebugCommandHook?.Dispose();
    }

    /// <summary>
    /// Register a chat command. Arguments to the command are parsed for you, based on the arguments to the function
    /// passed into the <paramref name="func"/> parameter.
    /// </summary>
    /// <param name="commandName">The name of the command.</param>
    /// <param name="helpMessage">The help message shown to users in chat or the installer.</param>
    /// <param name="func">The function to be called when the chat command is executed. Arguments to the command are derived from the parameters of this function.</param>
    /// <param name="showInHelp">Whether this command should be shown to users.</param>
    /// <param name="displayOrder">The display order of this command. Defaults to alphabetical ordering.</param>
    /// <param name="ownerPluginGuid">WorkingPluginId of the plugin that added this command.</param>
    /// <returns>If adding was successful.</returns>
    internal bool AddCommandInternal(string commandName, string helpMessage, Delegate func, bool showInHelp, int displayOrder, Guid? ownerPluginGuid)
    {
        var command = this.console.AddCommand(commandName, helpMessage, func);
        var commandInfo = new ConsoleBackedChatCommand(command)
        {
            HelpMessage = helpMessage,
            ShowInHelp = showInHelp,
            DisplayOrder = displayOrder,
            OwnerPluginGuid = ownerPluginGuid,
        };

        try
        {
            this.console.AddEntry(command);
        }
        catch (InvalidOperationException)
        {
            Log.Error("Command {CommandName} is already registered.", commandName);
            return false;
        }

        if (!this.commandMap.TryAdd(commandName, commandInfo))
        {
            this.console.RemoveEntry(command);
            Log.Error("Command {CommandName} is already registered.", commandName);
            return false;
        }

        return true;
    }

    private int OnTryInvokeDebugCommand(ShellCommands* self, Utf8String* command, UIModule* uiModule)
    {
        var result = this.tryInvokeDebugCommandHook!.OriginalDisposeSafe(self, command, uiModule);
        if (result != -1) return result;

        return this.ProcessCommand(command->ToString()) ? 0 : result;
    }

    private class ConsoleBackedChatCommand(IConsoleCommand consoleCommand) : BaseChatCommand(consoleCommand)
    {
    }

    private class LegacyHandlerChatCommand(IReadOnlyCommandInfo.HandlerDelegate handler, IConsoleCommand command)
        : BaseChatCommand(command), IReadOnlyCommandInfo
    {
        public LegacyHandlerChatCommand(CommandInfo commandInfo, IConsoleCommand command)
            : this(commandInfo.Handler, command)
        {
            this.HelpMessage = commandInfo.HelpMessage;
            this.ShowInHelp = commandInfo.ShowInHelp;
            this.DisplayOrder = commandInfo.DisplayOrder;
        }

        /// <inheritdoc/>
        public IReadOnlyCommandInfo.HandlerDelegate Handler { get; set; } = handler;
    }
}

/// <summary>
/// Plugin-scoped version of a AddonLifecycle service.
/// </summary>
[PluginInterface]
[ServiceManager.ScopedService]
#pragma warning disable SA1015
[ResolveVia<ICommandManager>]
#pragma warning restore SA1015
internal class CommandManagerPluginScoped : IInternalDisposableService, ICommandManager
{
    private static readonly ModuleLog Log = new("Command");

    [ServiceManager.ServiceDependency]
    private readonly CommandManager commandManagerService = Service<CommandManager>.Get();

    private readonly List<string> pluginRegisteredCommands = new();
    private readonly LocalPlugin pluginInfo;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandManagerPluginScoped"/> class.
    /// </summary>
    /// <param name="localPlugin">Info for the plugin that requests this service.</param>
    public CommandManagerPluginScoped(LocalPlugin localPlugin)
    {
        this.pluginInfo = localPlugin;
    }

    /// <inheritdoc/>
    public ReadOnlyDictionary<string, IReadOnlyCommandInfo> Commands => this.commandManagerService.Commands;

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        foreach (var command in this.pluginRegisteredCommands)
        {
            this.commandManagerService.RemoveHandler(command);
        }

        this.pluginRegisteredCommands.Clear();
    }

    /// <inheritdoc/>
    public bool ProcessCommand(string content)
        => this.commandManagerService.ProcessCommand(content);

    /// <inheritdoc/>
    public void DispatchCommand(string command, string argument, IReadOnlyCommandInfo info)
        => this.commandManagerService.DispatchCommand(command, argument, info);

    /// <inheritdoc/>
    public bool AddHandler(string command, CommandInfo info)
    {
        if (!this.pluginRegisteredCommands.Contains(command))
        {
            if (this.commandManagerService.AddHandler(command, info, this.pluginInfo.EffectiveWorkingPluginId))
            {
                this.pluginRegisteredCommands.Add(command);
                return true;
            }
        }
        else
        {
            Log.Error($"Command {command} is already registered.");
        }

        return false;
    }

    /// <inheritdoc/>
    [Experimental("Dalamud001")]
    public bool AddCommand(string commandName, string helpMessage, Delegate func, bool showInHelp = true, int displayOrder = -1)
    {
        if (!this.pluginRegisteredCommands.Contains(commandName))
        {
            if (this.commandManagerService.AddCommandInternal(commandName, helpMessage, func, showInHelp, displayOrder, this.pluginInfo.EffectiveWorkingPluginId))
            {
                this.pluginRegisteredCommands.Add(commandName);
                return true;
            }
        }
        else
        {
            Log.Error($"Command {commandName} is already registered.");
        }

        return false;
    }

    /// <inheritdoc/>
    public bool RemoveHandler(string command)
    {
        if (this.pluginRegisteredCommands.Contains(command))
        {
            if (this.commandManagerService.RemoveHandler(command))
            {
                this.pluginRegisteredCommands.Remove(command);
                return true;
            }
        }
        else
        {
            Log.Error($"Command {command} not found.");
        }

        return false;
    }
}
