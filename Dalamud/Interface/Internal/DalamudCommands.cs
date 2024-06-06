using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using CheapLoc;
using Dalamud.Configuration.Internal;
using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Internal;
using Dalamud.Utility;
using Serilog;

namespace Dalamud.Interface.Internal;

/// <summary>
/// Class handling Dalamud core commands.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal class DalamudCommands : IServiceType
{
    [ServiceManager.ServiceConstructor]
    private DalamudCommands(CommandManager commandManager)
    {
        commandManager.AddHandler("/xldclose", new CommandInfo(this.OnUnloadCommand)
        {
            HelpMessage = Loc.Localize("DalamudUnloadHelp", "Unloads XIVLauncher in-game addon. For debug use only!"),
            ShowInHelp = false,
        });

        commandManager.AddHandler("/xlkill", new CommandInfo(this.OnKillCommand)
        {
            HelpMessage = "Kill the game. For debug use only!",
            ShowInHelp = false,
        });

        commandManager.AddHandler("/xlrestart", new CommandInfo(this.OnRestartCommand)
        {
            HelpMessage = "Restart the game. For debug use only!",
            ShowInHelp = false,
        });

        commandManager.AddHandler("/xlhelp", new CommandInfo(this.OnHelpCommand)
        {
            HelpMessage = Loc.Localize("DalamudCmdInfoHelp", "Shows list of commands available. If an argument is provided, shows help for that command."),
        });

        commandManager.AddHandler("/xlmute", new CommandInfo(this.OnBadWordsAddCommand)
        {
            HelpMessage = Loc.Localize("DalamudMuteHelp", "Mute a word or sentence from appearing in chat. Usage: /xlmute <word or sentence>"),
        });

        commandManager.AddHandler("/xlmutelist", new CommandInfo(this.OnBadWordsListCommand)
        {
            HelpMessage = Loc.Localize("DalamudMuteListHelp", "List muted words or sentences."),
        });

        commandManager.AddHandler("/xlunmute", new CommandInfo(this.OnBadWordsRemoveCommand)
        {
            HelpMessage = Loc.Localize("DalamudUnmuteHelp", "Unmute a word or sentence. Usage: /xlunmute <word or sentence>"),
        });

        commandManager.AddHandler("/ll", new CommandInfo(this.OnLastLinkCommand)
        {
            HelpMessage = Loc.Localize("DalamudLastLinkHelp", "Open the last posted link in your default browser."),
        });

        commandManager.AddHandler("/xlbgmset", new CommandInfo(this.OnBgmSetCommand)
        {
            HelpMessage = Loc.Localize("DalamudBgmSetHelp", "Set the Game background music. Usage: /xlbgmset <BGM ID>"),
        });

        commandManager.AddHandler("/xldev", new CommandInfo(this.OnDebugDrawDevMenu)
        {
            HelpMessage = Loc.Localize("DalamudDevMenuHelp", "Draw dev menu DEBUG"),
            ShowInHelp = false,
        });

        commandManager.AddHandler("/xlstats", new CommandInfo(this.OnTogglePluginStats)
        {
            HelpMessage = Loc.Localize("DalamudPluginStats", "Draw plugin statistics window"),
        });

        commandManager.AddHandler("/xlbranch", new CommandInfo(this.OnToggleBranchSwitcher)
        {
            HelpMessage = Loc.Localize("DalamudBranchSwitcher", "Open the branch switcher"),
        });

        commandManager.AddHandler("/xldata", new CommandInfo(this.OnDebugDrawDataMenu)
        {
            HelpMessage = Loc.Localize("DalamudDevDataMenuHelp", "Draw dev data menu DEBUG. Usage: /xldata [Data Dropdown Type]"),
            ShowInHelp = false,
        });

        commandManager.AddHandler("/xllog", new CommandInfo(this.OnOpenLog)
        {
            HelpMessage = Loc.Localize("DalamudDevLogHelp", "Open the plugin log window/console"),
        });

        commandManager.AddHandler("/xlplugins", new CommandInfo(this.OnOpenInstallerCommand)
        {
            HelpMessage = Loc.Localize("DalamudInstallerHelp", "Open the plugin installer"),
        });

        commandManager.AddHandler("/xllanguage", new CommandInfo(this.OnSetLanguageCommand)
        {
            HelpMessage =
                Loc.Localize(
                    "DalamudLanguageHelp",
                    "Set the language for Dalamud and plugins that support it. Available languages: ") +
                Localization.ApplicableLangCodes.Aggregate("en", (current, code) => current + ", " + code),
        });

        commandManager.AddHandler("/xlsettings", new CommandInfo(this.OnOpenSettingsCommand)
        {
            HelpMessage = Loc.Localize(
                "DalamudSettingsHelp",
                "Change various In-Game-Addon settings like chat channels and the discord bot setup."),
        });

        commandManager.AddHandler("/xlversion", new CommandInfo(this.OnVersionInfoCommand)
        {
            HelpMessage = "Dalamud version info",
        });

        commandManager.AddHandler("/xlui", new CommandInfo(this.OnUiCommand)
        {
            HelpMessage = Loc.Localize(
                "DalamudUiModeHelp",
                "Toggle Dalamud UI display modes. Native UI modifications may also be affected by this, but that depends on the plugin."),
        });

        commandManager.AddHandler("/xlprofiler", new CommandInfo(this.OnOpenProfilerCommand)
        {
            HelpMessage = Loc.Localize(
                "DalamudProfilerHelp",
                "Open Dalamud's startup timing profiler."),
        });

        commandManager.AddHandler("/imdebug", new CommandInfo(this.OnDebugImInfoCommand)
        {
            HelpMessage = "ImGui DEBUG",
            ShowInHelp = false,
        });
    }

    private void OnUnloadCommand(string command, string arguments)
    {
        Service<ChatGui>.Get().Print("Unloading...");
        Service<Dalamud>.Get().Unload();
    }

    private void OnKillCommand(string command, string arguments)
    {
        Process.GetCurrentProcess().Kill();
    }

    private void OnRestartCommand(string command, string arguments)
    {
        Dalamud.RestartGame();
    }

    private void OnHelpCommand(string command, string arguments)
    {
        var chatGui = Service<ChatGui>.Get();
        var commandManager = Service<CommandManager>.Get();

        if (arguments.IsNullOrWhitespace())
        {
            chatGui.Print(Loc.Localize("DalamudCmdHelpAvailable", "Available commands:"));
            foreach (var cmd in commandManager.Commands)
            {
                if (!cmd.Value.ShowInHelp)
                    continue;

                chatGui.Print($"{cmd.Key}: {cmd.Value.HelpMessage}");
            }
        }
        else
        {
            var trimmedArguments = arguments.Trim();
            var targetCommandText = trimmedArguments[0] == '/' ? trimmedArguments : $"/{trimmedArguments}";
            chatGui.Print(commandManager.Commands.TryGetValue(targetCommandText, out var targetCommand)
                              ? $"{targetCommandText}: {targetCommand.HelpMessage}"
                              : Loc.Localize("DalamudCmdHelpNotFound", "Command not found."));
        }
    }

    private void OnBadWordsAddCommand(string command, string arguments)
    {
        var chatGui = Service<ChatGui>.Get();
        var configuration = Service<DalamudConfiguration>.Get();

        configuration.BadWords ??= new List<string>();

        if (string.IsNullOrEmpty(arguments))
        {
            chatGui.Print(Loc.Localize("DalamudMuteNoArgs", "Please provide a word to mute."));
            return;
        }

        configuration.BadWords.Add(arguments);

        configuration.QueueSave();

        chatGui.Print(string.Format(Loc.Localize("DalamudMuted", "Muted \"{0}\"."), arguments));
    }

    private void OnBadWordsListCommand(string command, string arguments)
    {
        var chatGui = Service<ChatGui>.Get();
        var configuration = Service<DalamudConfiguration>.Get();

        configuration.BadWords ??= new List<string>();

        if (configuration.BadWords.Count == 0)
        {
            chatGui.Print(Loc.Localize("DalamudNoneMuted", "No muted words or sentences."));
            return;
        }

        configuration.QueueSave();

        foreach (var word in configuration.BadWords)
            chatGui.Print($"\"{word}\"");
    }

    private void OnBadWordsRemoveCommand(string command, string arguments)
    {
        var chatGui = Service<ChatGui>.Get();
        var configuration = Service<DalamudConfiguration>.Get();

        configuration.BadWords ??= new List<string>();

        configuration.BadWords.RemoveAll(x => x == arguments);

        configuration.QueueSave();

        chatGui.Print(string.Format(Loc.Localize("DalamudUnmuted", "Unmuted \"{0}\"."), arguments));
    }

    private void OnLastLinkCommand(string command, string arguments)
    {
        var chatHandlers = Service<ChatHandlers>.Get();
        var chatGui = Service<ChatGui>.Get();

        if (string.IsNullOrEmpty(chatHandlers.LastLink))
        {
            chatGui.Print(Loc.Localize("DalamudNoLastLink", "No last link..."));
            return;
        }

        chatGui.Print(string.Format(Loc.Localize("DalamudOpeningLink", "Opening {0}"), chatHandlers.LastLink));
        Process.Start(new ProcessStartInfo(chatHandlers.LastLink)
        {
            UseShellExecute = true,
        });
    }

    private void OnBgmSetCommand(string command, string arguments)
    {
        var gameGui = Service<GameGui>.Get();

        if (ushort.TryParse(arguments, out var value))
        {
            gameGui.SetBgm(value);
        }
        else
        {
            // Revert to the original BGM by specifying an invalid one
            gameGui.SetBgm(9999);
        }
    }

    private void OnDebugDrawDevMenu(string command, string arguments)
    {
        Service<DalamudInterface>.Get().ToggleDevMenu();
    }

    private void OnTogglePluginStats(string command, string arguments)
    {
        Service<DalamudInterface>.Get().TogglePluginStatsWindow();
    }

    private void OnToggleBranchSwitcher(string command, string arguments)
    {
        Service<DalamudInterface>.Get().ToggleBranchSwitcher();
    }

    private void OnDebugDrawDataMenu(string command, string arguments)
    {
        var dalamudInterface = Service<DalamudInterface>.Get();

        if (string.IsNullOrEmpty(arguments))
            dalamudInterface.ToggleDataWindow();
        else
            dalamudInterface.ToggleDataWindow(arguments);
    }

    private void OnOpenLog(string command, string arguments)
    {
        Service<DalamudInterface>.Get().ToggleLogWindow();
    }

    private void OnDebugImInfoCommand(string command, string arguments)
    {
        var io = Service<InterfaceManager>.Get().LastImGuiIoPtr;
        var info = $"WantCaptureKeyboard: {io.WantCaptureKeyboard}\n";
        info += $"WantCaptureMouse: {io.WantCaptureMouse}\n";
        info += $"WantSetMousePos: {io.WantSetMousePos}\n";
        info += $"WantTextInput: {io.WantTextInput}\n";
        info += $"WantSaveIniSettings: {io.WantSaveIniSettings}\n";
        info += $"BackendFlags: {(int)io.BackendFlags}\n";
        info += $"DeltaTime: {io.DeltaTime}\n";
        info += $"DisplaySize: {io.DisplaySize.X} {io.DisplaySize.Y}\n";
        info += $"Framerate: {io.Framerate}\n";
        info += $"MetricsActiveWindows: {io.MetricsActiveWindows}\n";
        info += $"MetricsRenderWindows: {io.MetricsRenderWindows}\n";
        info += $"MousePos: {io.MousePos.X} {io.MousePos.Y}\n";
        info += $"MouseClicked: {io.MouseClicked}\n";
        info += $"MouseDown: {io.MouseDown}\n";
        info += $"NavActive: {io.NavActive}\n";
        info += $"NavVisible: {io.NavVisible}\n";

        Log.Information(info);
    }

    private void OnVersionInfoCommand(string command, string arguments)
    {
        var chatGui = Service<ChatGui>.Get();

        chatGui.Print(new SeStringBuilder()
                      .AddItalics("Dalamud:")
                      .AddText($" D{Util.AssemblyVersion}({Util.GetGitHash()}")
                      .Build());

        chatGui.Print(new SeStringBuilder()
                      .AddItalics("FFXIVCS:")
                      .AddText($" {Util.GetGitHashClientStructs()}")
                      .Build());
    }

    private void OnOpenInstallerCommand(string command, string arguments)
    {
        var configuration = Service<DalamudConfiguration>.Get();
        Service<DalamudInterface>.Get().TogglePluginInstallerWindowTo(configuration.PluginInstallerOpen);
    }

    private void OnSetLanguageCommand(string command, string arguments)
    {
        var chatGui = Service<ChatGui>.Get();
        var configuration = Service<DalamudConfiguration>.Get();
        var localization = Service<Localization>.Get();

        if (Localization.ApplicableLangCodes.Contains(arguments.ToLower()) || arguments.ToLower() == "en")
        {
            localization.SetupWithLangCode(arguments.ToLower());
            configuration.LanguageOverride = arguments.ToLower();

            chatGui.Print(string.Format(Loc.Localize("DalamudLanguageSetTo", "Language set to {0}"), arguments));
        }
        else
        {
            localization.SetupWithUiCulture();
            configuration.LanguageOverride = null;

            chatGui.Print(string.Format(Loc.Localize("DalamudLanguageSetTo", "Language set to {0}"), "default"));
        }

        configuration.QueueSave();
    }

    private void OnOpenSettingsCommand(string command, string arguments)
    {
        Service<DalamudInterface>.Get().ToggleSettingsWindow();
    }

    private void OnUiCommand(string command, string arguments)
    {
        var im = Service<InterfaceManager>.Get();

        im.IsDispatchingEvents = arguments switch
        {
            "show" => true,
            "hide" => false,
            _ => !im.IsDispatchingEvents,
        };

        var pm = Service<PluginManager>.Get();

        foreach (var plugin in pm.InstalledPlugins)
        {
            if (im.IsDispatchingEvents)
            {
                plugin.DalamudInterface?.UiBuilder.NotifyShowUi();
            }
            else
            {
                plugin.DalamudInterface?.UiBuilder.NotifyHideUi();
            }
        }
    }

    private void OnOpenProfilerCommand(string command, string arguments)
    {
        Service<DalamudInterface>.Get().ToggleProfilerWindow();
    }
}
