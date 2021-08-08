using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using CheapLoc;
using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Plugin.Internal;
using Serilog;

namespace Dalamud.Interface.Internal
{
    /// <summary>
    /// Class handling Dalamud core commands.
    /// </summary>
    internal class DalamudCommands
    {
        private readonly CommandManager commandManager;
        private readonly Dalamud dalamud;
        private readonly PluginManager pluginManager;
        private readonly InterfaceManager interfaceManager;
        private readonly Framework framework;
        private readonly DalamudInterface dalamudInterface;

        /// <summary>
        /// Initializes a new instance of the <see cref="DalamudCommands"/> class.
        /// </summary>
        public DalamudCommands()
        {
            this.dalamud = Service<Dalamud>.Get();
            this.framework = Service<Framework>.Get();
            this.pluginManager = Service<PluginManager>.Get();
            this.commandManager = Service<CommandManager>.Get();
            this.interfaceManager = Service<InterfaceManager>.Get();
            this.dalamudInterface = Service<DalamudInterface>.Get();
        }

        /// <summary>
        /// Register all command handlers with the Dalamud instance.
        /// </summary>
        public void SetupCommands()
        {
            this.commandManager.AddHandler("/xldclose", new CommandInfo(this.OnUnloadCommand)
            {
                HelpMessage = Loc.Localize("DalamudUnloadHelp", "Unloads XIVLauncher in-game addon."),
                ShowInHelp = false,
            });

            this.commandManager.AddHandler("/xldreloadplugins", new CommandInfo(this.OnPluginReloadCommand)
            {
                HelpMessage = Loc.Localize("DalamudPluginReloadHelp", "Reloads all plugins."),
                ShowInHelp = false,
            });

            this.commandManager.AddHandler("/xlhelp", new CommandInfo(this.OnHelpCommand)
            {
                HelpMessage = Loc.Localize("DalamudCmdInfoHelp", "Shows list of commands available."),
            });

            this.commandManager.AddHandler("/xlmute", new CommandInfo(this.OnBadWordsAddCommand)
            {
                HelpMessage = Loc.Localize("DalamudMuteHelp", "Mute a word or sentence from appearing in chat. Usage: /xlmute <word or sentence>"),
            });

            this.commandManager.AddHandler("/xlmutelist", new CommandInfo(this.OnBadWordsListCommand)
            {
                HelpMessage = Loc.Localize("DalamudMuteListHelp", "List muted words or sentences."),
            });

            this.commandManager.AddHandler("/xlunmute", new CommandInfo(this.OnBadWordsRemoveCommand)
            {
                HelpMessage = Loc.Localize("DalamudUnmuteHelp", "Unmute a word or sentence. Usage: /xlunmute <word or sentence>"),
            });

            this.commandManager.AddHandler("/ll", new CommandInfo(this.OnLastLinkCommand)
            {
                HelpMessage = Loc.Localize("DalamudLastLinkHelp", "Open the last posted link in your default browser."),
            });

            this.commandManager.AddHandler("/xlbgmset", new CommandInfo(this.OnBgmSetCommand)
            {
                HelpMessage = Loc.Localize("DalamudBgmSetHelp", "Set the Game background music. Usage: /xlbgmset <BGM ID>"),
            });

            this.commandManager.AddHandler("/xldev", new CommandInfo(this.OnDebugDrawDevMenu)
            {
                HelpMessage = Loc.Localize("DalamudDevMenuHelp", "Draw dev menu DEBUG"),
                ShowInHelp = false,
            });

            this.commandManager.AddHandler("/xldata", new CommandInfo(this.OnDebugDrawDataMenu)
            {
                HelpMessage = Loc.Localize("DalamudDevDataMenuHelp", "Draw dev data menu DEBUG. Usage: /xldata [Data Dropdown Type]"),
                ShowInHelp = false,
            });

            this.commandManager.AddHandler("/xllog", new CommandInfo(this.OnOpenLog)
            {
                HelpMessage = Loc.Localize("DalamudDevLogHelp", "Open dev log DEBUG"),
                ShowInHelp = false,
            });

            this.commandManager.AddHandler("/xlplugins", new CommandInfo(this.OnOpenInstallerCommand)
            {
                HelpMessage = Loc.Localize("DalamudInstallerHelp", "Open the plugin installer"),
            });

            this.commandManager.AddHandler("/xlcredits", new CommandInfo(this.OnOpenCreditsCommand)
            {
                HelpMessage = Loc.Localize("DalamudCreditsHelp", "Opens the credits for dalamud."),
            });

            this.commandManager.AddHandler("/xllanguage", new CommandInfo(this.OnSetLanguageCommand)
            {
                HelpMessage =
                    Loc.Localize(
                        "DalamudLanguageHelp",
                        "Set the language for the in-game addon and plugins that support it. Available languages: ") +
                        Localization.ApplicableLangCodes.Aggregate("en", (current, code) => current + ", " + code),
            });

            this.commandManager.AddHandler("/xlsettings", new CommandInfo(this.OnOpenSettingsCommand)
            {
                HelpMessage = Loc.Localize(
                        "DalamudSettingsHelp",
                        "Change various In-Game-Addon settings like chat channels and the discord bot setup."),
            });

            this.commandManager.AddHandler("/imdebug", new CommandInfo(this.OnDebugImInfoCommand)
            {
                HelpMessage = "ImGui DEBUG",
                ShowInHelp = false,
            });
        }

        private void OnUnloadCommand(string command, string arguments)
        {
            this.framework.Gui.Chat.Print("Unloading...");
            this.dalamud.Unload();
        }

        private void OnHelpCommand(string command, string arguments)
        {
            var showDebug = arguments.Contains("debug");

            this.framework.Gui.Chat.Print(Loc.Localize("DalamudCmdHelpAvailable", "Available commands:"));
            foreach (var cmd in this.commandManager.Commands)
            {
                if (!cmd.Value.ShowInHelp && !showDebug)
                    continue;

                this.framework.Gui.Chat.Print($"{cmd.Key}: {cmd.Value.HelpMessage}");
            }
        }

        private void OnPluginReloadCommand(string command, string arguments)
        {
            this.framework.Gui.Chat.Print("Reloading...");

            try
            {
                this.pluginManager.ReloadAllPlugins();

                this.framework.Gui.Chat.Print("OK");
            }
            catch (Exception ex)
            {
                this.framework.Gui.Chat.PrintError("Reload failed.");
                Log.Error(ex, "Plugin reload failed.");
            }
        }

        private void OnBadWordsAddCommand(string command, string arguments)
        {
            this.dalamud.Configuration.BadWords ??= new List<string>();

            if (string.IsNullOrEmpty(arguments))
            {
                this.framework.Gui.Chat.Print(
                    Loc.Localize("DalamudMuteNoArgs", "Please provide a word to mute."));
                return;
            }

            this.dalamud.Configuration.BadWords.Add(arguments);

            this.dalamud.Configuration.Save();

            this.framework.Gui.Chat.Print(
                string.Format(Loc.Localize("DalamudMuted", "Muted \"{0}\"."), arguments));
        }

        private void OnBadWordsListCommand(string command, string arguments)
        {
            this.dalamud.Configuration.BadWords ??= new List<string>();

            if (this.dalamud.Configuration.BadWords.Count == 0)
            {
                this.framework.Gui.Chat.Print(Loc.Localize("DalamudNoneMuted", "No muted words or sentences."));
                return;
            }

            this.dalamud.Configuration.Save();

            foreach (var word in this.dalamud.Configuration.BadWords)
                this.framework.Gui.Chat.Print($"\"{word}\"");
        }

        private void OnBadWordsRemoveCommand(string command, string arguments)
        {
            this.dalamud.Configuration.BadWords ??= new List<string>();

            this.dalamud.Configuration.BadWords.RemoveAll(x => x == arguments);

            this.dalamud.Configuration.Save();

            this.framework.Gui.Chat.Print(
                string.Format(Loc.Localize("DalamudUnmuted", "Unmuted \"{0}\"."), arguments));
        }

        private void OnLastLinkCommand(string command, string arguments)
        {
            if (string.IsNullOrEmpty(this.dalamud.ChatHandlers.LastLink))
            {
                this.framework.Gui.Chat.Print(Loc.Localize("DalamudNoLastLink", "No last link..."));
                return;
            }

            this.framework.Gui.Chat.Print(string.Format(Loc.Localize("DalamudOpeningLink", "Opening {0}"), this.dalamud.ChatHandlers.LastLink));
            Process.Start(this.dalamud.ChatHandlers.LastLink);
        }

        private void OnBgmSetCommand(string command, string arguments)
        {
            if (ushort.TryParse(arguments, out var value))
            {
                this.framework.Gui.SetBgm(value);
            }
            else
            {
                // Revert to the original BGM by specifying an invalid one
                this.framework.Gui.SetBgm(9999);
            }
        }

        private void OnDebugDrawDevMenu(string command, string arguments)
        {
            this.dalamudInterface.ToggleDevMenu();
        }

        private void OnDebugDrawDataMenu(string command, string arguments)
        {
            if (string.IsNullOrEmpty(arguments))
                this.dalamudInterface.ToggleDataWindow();
            else
                this.dalamudInterface.OpenDataWindow(arguments);
        }

        private void OnOpenLog(string command, string arguments)
        {
            this.dalamudInterface.ToggleLogWindow();
        }

        private void OnDebugImInfoCommand(string command, string arguments)
        {
            var io = this.interfaceManager.LastImGuiIoPtr;
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

        private void OnOpenInstallerCommand(string command, string arguments)
        {
            this.dalamudInterface.TogglePluginInstallerWindow();
        }

        private void OnOpenCreditsCommand(string command, string arguments)
        {
            this.dalamudInterface.ToggleCreditsWindow();
        }

        private void OnSetLanguageCommand(string command, string arguments)
        {
            if (Localization.ApplicableLangCodes.Contains(arguments.ToLower()) || arguments.ToLower() == "en")
            {
                this.dalamud.LocalizationManager.SetupWithLangCode(arguments.ToLower());
                this.dalamud.Configuration.LanguageOverride = arguments.ToLower();

                this.framework.Gui.Chat.Print(
                    string.Format(Loc.Localize("DalamudLanguageSetTo", "Language set to {0}"), arguments));
            }
            else
            {
                this.dalamud.LocalizationManager.SetupWithUiCulture();
                this.dalamud.Configuration.LanguageOverride = null;

                this.framework.Gui.Chat.Print(
                    string.Format(Loc.Localize("DalamudLanguageSetTo", "Language set to {0}"), "default"));
            }

            this.dalamud.Configuration.Save();
        }

        private void OnOpenSettingsCommand(string command, string arguments)
        {
            this.dalamudInterface.ToggleSettingsWindow();
        }
    }
}
