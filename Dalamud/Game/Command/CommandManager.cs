using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

    private readonly ConcurrentDictionary<string, IReadOnlyCommandInfo> commandMap = new();
    private readonly ConcurrentDictionary<(string, IReadOnlyCommandInfo), string> commandAssemblyNameMap = new();

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

        this.console.Invoke += this.ConsoleOnInvoke;
    }

    /// <summary>
    /// Published whenever a command is registered
    /// </summary>
    public event EventHandler<CommandEventArgs>? CommandAdded;

    /// <summary>
    /// Published whenever a command is unregistered
    /// </summary>
    public event EventHandler<CommandEventArgs>? CommandRemoved;

    /// <inheritdoc/>
    public ReadOnlyDictionary<string, IReadOnlyCommandInfo> Commands => new(this.commandMap);

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
            command = content[..separatorPosition];

            var argStart = separatorPosition + 1;
            argument = content[argStart..];
        }

        if (!this.commandMap.TryGetValue(command, out var handler)) // Command was not found.
            return false;

        this.DispatchCommand(command, argument, handler);
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
    /// <param name="loaderAssemblyName">Assembly name of the plugin that added this command.</param>
    /// <returns>If adding was successful.</returns>
    public bool AddHandler(string command, CommandInfo info, string loaderAssemblyName)
    {
        if (info == null)
            throw new ArgumentNullException(nameof(info), "Command handler is null.");

        if (!this.commandMap.TryAdd(command, info))
        {
            Log.Error("Command {CommandName} is already registered", command);
            return false;
        }

        this.CommandAdded?.Invoke(this, new CommandEventArgs
        {
            Command = command,
            CommandInfo = info,
        });

        if (!this.commandAssemblyNameMap.TryAdd((command, info), loaderAssemblyName))
        {
            this.commandMap.Remove(command, out _);
            Log.Error("Command {CommandName} is already registered in the assembly name map", command);
            return false;
        }

        return true;
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

        this.CommandAdded?.Invoke(this, new CommandEventArgs
        {
            Command = command,
            CommandInfo = info,
        });

        return true;
    }

    /// <inheritdoc/>
    public bool RemoveHandler(string command)
    {
        if (this.commandAssemblyNameMap.FindFirst(c => c.Key.Item1 == command, out var assemblyKeyValuePair))
        {
            this.commandAssemblyNameMap.TryRemove(assemblyKeyValuePair.Key, out _);
        }

        var removed = this.commandMap.Remove(command, out var info);
        if (removed)
        {
            this.CommandRemoved?.Invoke(this, new CommandEventArgs
            {
                Command = command,
                CommandInfo = info,
            });
        }

        return removed;
    }

    /// <summary>
    /// Returns the assembly name from which the command was added or blank if added internally.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <param name="commandInfo">A ICommandInfo object.</param>
    /// <returns>The name of the assembly.</returns>
    public string GetHandlerAssemblyName(string command, IReadOnlyCommandInfo commandInfo)
    {
        if (this.commandAssemblyNameMap.TryGetValue((command, commandInfo), out var assemblyName))
        {
            return assemblyName;
        }

        return string.Empty;
    }

    /// <summary>
    /// Returns a list of commands given a specified assembly name.
    /// </summary>
    /// <param name="assemblyName">The name of the assembly.</param>
    /// <returns>A list of commands and their associated activation string.</returns>
    public List<KeyValuePair<(string Command, IReadOnlyCommandInfo CommandInfo), string>> GetHandlersByAssemblyName(
        string assemblyName)
    {
        return this.commandAssemblyNameMap.Where(c => c.Value == assemblyName).ToList();
    }

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        this.console.Invoke -= this.ConsoleOnInvoke;
        this.tryInvokeDebugCommandHook?.Dispose();
    }

    private bool ConsoleOnInvoke(string arg)
    {
        return arg.StartsWith('/') && this.ProcessCommand(arg);
    }

    private int OnTryInvokeDebugCommand(ShellCommands* self, Utf8String* command, UIModule* uiModule)
    {
        var result = this.tryInvokeDebugCommandHook!.OriginalDisposeSafe(self, command, uiModule);
        if (result != -1) return result;

        return this.ProcessCommand(command->ToString()) ? 0 : result;
    }

    /// <inheritdoc />
    public class CommandEventArgs : EventArgs
    {
        /// <summary>
        ///  Gets the command string.
        /// </summary>
        public string Command { get; init; }

        /// <summary>
        ///  Gets the command info.
        /// </summary>
        public IReadOnlyCommandInfo CommandInfo { get; init; }
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
            if (this.commandManagerService.AddHandler(command, info, this.pluginInfo.InternalName))
            {
                this.pluginRegisteredCommands.Add(command);
                return true;
            }
        }
        else
        {
            Log.Error("Command {Command} is already registered.", command);
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
            Log.Error("Command {Command} not found.", command);
        }

        return false;
    }
}
