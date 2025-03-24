using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using CheapLoc;
using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using Serilog;

namespace Dalamud.Plugin.Internal.Profiles;

/// <summary>
/// Service responsible for profile-related chat commands.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal class PluginManagementCommandHandler : IInternalDisposableService
{
#pragma warning disable SA1600
    public const string CommandEnableProfile = "/xlenablecollection";
    public const string CommandDisableProfile = "/xldisablecollection";
    public const string CommandToggleProfile = "/xltogglecollection";
    
    public const string CommandEnablePlugin = "/xlenableplugin";
    public const string CommandDisablePlugin = "/xldisableplugin";
    public const string CommandTogglePlugin = "/xltoggleplugin";
#pragma warning restore SA1600
    
    private static readonly string LegacyCommandEnable = CommandEnableProfile.Replace("collection", "profile");
    private static readonly string LegacyCommandDisable = CommandDisableProfile.Replace("collection", "profile");
    private static readonly string LegacyCommandToggle = CommandToggleProfile.Replace("collection", "profile");
    
    private readonly CommandManager cmd;
    private readonly ProfileManager profileManager;
    private readonly PluginManager pluginManager;
    private readonly ChatGui chat;
    private readonly Framework framework;

    private List<(Target Target, PluginCommandOperation Operation)> commandQueue = new();
    
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginManagementCommandHandler"/> class.
    /// </summary>
    /// <param name="cmd">Command handler.</param>
    /// <param name="profileManager">Profile manager.</param>
    /// <param name="pluginManager">Plugin manager.</param>
    /// <param name="chat">Chat handler.</param>
    /// <param name="framework">Framework.</param>
    [ServiceManager.ServiceConstructor]
    public PluginManagementCommandHandler(
        CommandManager cmd,
        ProfileManager profileManager,
        PluginManager pluginManager,
        ChatGui chat,
        Framework framework)
    {
        this.cmd = cmd;
        this.profileManager = profileManager;
        this.pluginManager = pluginManager;
        this.chat = chat;
        this.framework = framework;

        this.cmd.AddHandler(CommandEnableProfile, new CommandInfo(this.OnEnableProfile)
        {
            HelpMessage = Loc.Localize("ProfileCommandsEnableHint", "Enable a collection. Usage: /xlenablecollection \"Collection Name\""),
            ShowInHelp = true,
        });

        this.cmd.AddHandler(CommandDisableProfile, new CommandInfo(this.OnDisableProfile)
        {
            HelpMessage = Loc.Localize("ProfileCommandsDisableHint", "Disable a collection. Usage: /xldisablecollection \"Collection Name\""),
            ShowInHelp = true,
        });

        this.cmd.AddHandler(CommandToggleProfile, new CommandInfo(this.OnToggleProfile)
        {
            HelpMessage = Loc.Localize("ProfileCommandsToggleHint", "Toggle a collection. Usage: /xltogglecollection \"Collection Name\""),
            ShowInHelp = true,
        });
        
        this.cmd.AddHandler(LegacyCommandEnable, new CommandInfo(this.OnEnableProfile)
        {
            ShowInHelp = false,
        });

        this.cmd.AddHandler(LegacyCommandDisable, new CommandInfo(this.OnDisableProfile)
        {
            ShowInHelp = false,
        });

        this.cmd.AddHandler(LegacyCommandToggle, new CommandInfo(this.OnToggleProfile)
        {
            ShowInHelp = false,
        });
        
        this.cmd.AddHandler(CommandEnablePlugin, new CommandInfo(this.OnEnablePlugin)
        {
            HelpMessage = Loc.Localize("PluginCommandsEnableHint", "Enable a plugin. Usage: /xlenableplugin \"Plugin Name\""),
            ShowInHelp = true,
        });
        
        this.cmd.AddHandler(CommandDisablePlugin, new CommandInfo(this.OnDisablePlugin)
        {
            HelpMessage = Loc.Localize("PluginCommandsDisableHint", "Disable a plugin. Usage: /xldisableplugin \"Plugin Name\""),
            ShowInHelp = true,
        });
        
        this.cmd.AddHandler(CommandTogglePlugin, new CommandInfo(this.OnTogglePlugin)
        {
            HelpMessage = Loc.Localize("PluginCommandsToggleHint", "Toggle a plugin. Usage: /xltoggleplugin \"Plugin Name\""),
            ShowInHelp = true,
        });

        this.framework.Update += this.FrameworkOnUpdate;
    }

    private enum PluginCommandOperation
    {
        Enable,
        Disable,
        Toggle,
    }
    
    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        this.cmd.RemoveHandler(CommandEnableProfile);
        this.cmd.RemoveHandler(CommandDisableProfile);
        this.cmd.RemoveHandler(CommandToggleProfile);
        this.cmd.RemoveHandler(LegacyCommandEnable);
        this.cmd.RemoveHandler(LegacyCommandDisable);
        this.cmd.RemoveHandler(LegacyCommandToggle);

        this.framework.Update += this.FrameworkOnUpdate;
    }
    
    private void HandleProfileOperation(string profileName, PluginCommandOperation operation)
    {
        var profile = this.profileManager.Profiles.FirstOrDefault(
            x => x.Name == profileName);
        if (profile == null || profile.IsDefaultProfile)
            return;

        switch (operation)
        {
            case PluginCommandOperation.Enable:
                if (!profile.IsEnabled)
                    Task.Run(() => profile.SetStateAsync(true, false)).GetAwaiter().GetResult();
                break;
            case PluginCommandOperation.Disable:
                if (profile.IsEnabled)
                    Task.Run(() => profile.SetStateAsync(false, false)).GetAwaiter().GetResult();
                break;
            case PluginCommandOperation.Toggle:
                Task.Run(() => profile.SetStateAsync(!profile.IsEnabled, false)).GetAwaiter().GetResult();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(operation), operation, null);
        }

        this.chat.Print(
            profile.IsEnabled
                ? Loc.Localize("ProfileCommandsEnabling", "Enabling collection \"{0}\"...").Format(profile.Name)
                : Loc.Localize("ProfileCommandsDisabling", "Disabling collection \"{0}\"...").Format(profile.Name));

        Task.Run(this.profileManager.ApplyAllWantStatesAsync).ContinueWith(t =>
        {
            if (!t.IsCompletedSuccessfully && t.Exception != null)
            {
                Log.Error(t.Exception, "Could not apply profiles through commands");
                this.chat.PrintError(Loc.Localize("ProfileCommandsApplyFailed", "Failed to apply your collections. Please check the console for errors."));
            }
            else
            {
                this.chat.Print(Loc.Localize("ProfileCommandsApplySuccess", "Collections applied."));
            }
        });
    }
    
    private bool HandlePluginOperation(Guid workingPluginId, PluginCommandOperation operation)
    {
        var plugin = this.pluginManager.InstalledPlugins.FirstOrDefault(x => x.EffectiveWorkingPluginId == workingPluginId);
        if (plugin == null)
            return true;

        switch (plugin.State)
        {
            // Ignore if the plugin is in a fail state
            case PluginState.LoadError or PluginState.UnloadError:
                this.chat.Print(Loc.Localize("PluginCommandsFailed", "Plugin \"{0}\" has previously failed to load/unload, not continuing.").Format(plugin.Name));
                return true;

            case PluginState.Loaded when operation == PluginCommandOperation.Enable:
                this.chat.Print(Loc.Localize("PluginCommandsAlreadyEnabled", "Plugin \"{0}\" is already enabled.").Format(plugin.Name));
                return true;
            case PluginState.Unloaded when operation == PluginCommandOperation.Disable:
                this.chat.Print(Loc.Localize("PluginCommandsAlreadyDisabled", "Plugin \"{0}\" is already disabled.").Format(plugin.Name));
                return true;

            // Defer if this plugin is busy right now
            case PluginState.Loading or PluginState.Unloading:
                return false;
        }

        void Continuation(Task t, string onSuccess, string onError)
        {
            if (!t.IsCompletedSuccessfully && t.Exception != null)
            {
                Log.Error(t.Exception, "Plugin command operation failed for plugin {PluginName}", plugin.Name);
                this.chat.PrintError(onError);
                return;
            }
            
            this.chat.Print(onSuccess);
        }

        if (operation is PluginCommandOperation.Toggle)
        {
            return HandlePluginOperation(workingPluginId, plugin.State == PluginState.Loaded ? PluginCommandOperation.Disable : PluginCommandOperation.Enable);
        }
            
        switch (operation)
        {
            case PluginCommandOperation.Enable:
                this.chat.Print(Loc.Localize("PluginCommandsEnabling", "Enabling plugin \"{0}\"...").Format(plugin.Name));
                Task.Run(() => plugin.LoadAsync(PluginLoadReason.Installer))
                    .ContinueWith(t => Continuation(t, 
                                                    Loc.Localize("PluginCommandsEnableSuccess", "Plugin \"{0}\" enabled.").Format(plugin.Name), 
                                                    Loc.Localize("PluginCommandsEnableFailed", "Failed to enable plugin \"{0}\". Please check the console for errors.").Format(plugin.Name)))
                    .ConfigureAwait(false);
                break;
            case PluginCommandOperation.Disable:
                this.chat.Print(Loc.Localize("PluginCommandsDisabling", "Disabling plugin \"{0}\"...").Format(plugin.Name));
                Task.Run(() => plugin.UnloadAsync())
                    .ContinueWith(t => Continuation(t,
                                      Loc.Localize("PluginCommandsDisableSuccess", "Plugin \"{0}\" disabled.").Format(plugin.Name),
                                      Loc.Localize("PluginCommandsDisableFailed", "Failed to disable plugin \"{0}\". Please check the console for errors.").Format(plugin.Name)))
                    .ConfigureAwait(false);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(operation), operation, null);
        }

        return true;
    }
    
    private void FrameworkOnUpdate(IFramework framework1)
    {
        if (this.profileManager.IsBusy)
        {
            return;
        }
        
        if (this.commandQueue.Count > 0)
        {
            var op = this.commandQueue[0];
            
            var remove = true;
            switch (op.Target)
            {
                case PluginTarget pluginTarget:
                    remove = this.HandlePluginOperation(pluginTarget.WorkingPluginId, op.Operation);
                    break;
                case ProfileTarget profileTarget:
                    this.HandleProfileOperation(profileTarget.ProfileName, op.Operation);
                    break;
            }
            
            if (remove)
            {
                this.commandQueue.RemoveAt(0);
            }
        }
    }

    private void OnEnableProfile(string command, string arguments)
    {
        var name = this.ValidateProfileName(arguments);
        if (name == null)
            return;

        var target = new ProfileTarget(name);
        this.commandQueue = this.commandQueue.Where(x => x.Target != target).ToList();
        this.commandQueue.Add((target, PluginCommandOperation.Enable));
    }

    private void OnDisableProfile(string command, string arguments)
    {
        var name = this.ValidateProfileName(arguments);
        if (name == null)
            return;

        var target = new ProfileTarget(name);
        this.commandQueue = this.commandQueue.Where(x => x.Target != target).ToList();
        this.commandQueue.Add((target, PluginCommandOperation.Disable));
    }

    private void OnToggleProfile(string command, string arguments)
    {
        var name = this.ValidateProfileName(arguments);
        if (name == null)
            return;

        var target = new ProfileTarget(name);
        this.commandQueue.Add((target, PluginCommandOperation.Toggle));
    }
    
    private void OnEnablePlugin(string command, string arguments)
    {
        var plugin = this.ValidatePluginName(arguments);
        if (plugin == null)
            return;

        var target = new PluginTarget(plugin.EffectiveWorkingPluginId);
        this.commandQueue
            .RemoveAll(x => x.Target == target);
        this.commandQueue.Add((target, PluginCommandOperation.Enable));
    }
    
    private void OnDisablePlugin(string command, string arguments)
    {
        var plugin = this.ValidatePluginName(arguments);
        if (plugin == null)
            return;
        
        var target = new PluginTarget(plugin.EffectiveWorkingPluginId);
        this.commandQueue
            .RemoveAll(x => x.Target == target);
        this.commandQueue.Add((target, PluginCommandOperation.Disable));
    }
    
    private void OnTogglePlugin(string command, string arguments)
    {
        var plugin = this.ValidatePluginName(arguments);
        if (plugin == null)
            return;

        var target = new PluginTarget(plugin.EffectiveWorkingPluginId);
        this.commandQueue
            .RemoveAll(x => x.Target == target);
        this.commandQueue.Add((target, PluginCommandOperation.Toggle));
    }

    private string? ValidateProfileName(string arguments)
    {
        var name = arguments.Replace("\"", string.Empty);
        if (this.profileManager.Profiles.All(x => x.Name != name))
        {
            this.chat.PrintError(Loc.Localize("ProfileCommandsNotFound", "Collection \"{0}\" not found.").Format(name));
            return null;
        }

        return name;
    }

    private LocalPlugin? ValidatePluginName(string arguments)
    {
        var name = arguments.Replace("\"", string.Empty);
        var targetPlugin =
            this.pluginManager.InstalledPlugins.FirstOrDefault(x => x.InternalName == name || x.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase));
        
        if (targetPlugin == null)
        {
            this.chat.PrintError(Loc.Localize("PluginCommandsNotFound", "Plugin \"{0}\" not found.").Format(name));
            return null;
        }

        if (!this.profileManager.IsInDefaultProfile(targetPlugin.EffectiveWorkingPluginId))
        {
            this.chat.PrintError(Loc.Localize("PluginCommandsNotInDefaultProfile", "Plugin \"{0}\" is in a collection and can't be managed through commands. Manage the collection instead.")
                                    .Format(targetPlugin.Name));
        }

        return targetPlugin;
    }

    private abstract record Target;

    private record PluginTarget(Guid WorkingPluginId) : Target;

    private record ProfileTarget(string ProfileName) : Target;
}
