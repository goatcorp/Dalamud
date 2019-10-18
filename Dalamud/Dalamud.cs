using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Dalamud.DiscordBot;
using Dalamud.Game;
using Dalamud.Game.Chat;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Game.ClientState.Actors.Types.NonPlayer;
using Dalamud.Game.Command;
using Dalamud.Game.Internal;
using Dalamud.Game.Internal.Gui;
using Dalamud.Game.Network;
using Dalamud.Plugin;
using Dalamud.Settings;
using Serilog;
using XIVLauncher.Dalamud;

namespace Dalamud {
    public sealed class Dalamud : IDisposable {
        private readonly string baseDirectory;

        private readonly ManualResetEvent unloadSignal;

        private readonly ProcessModule targetModule;
        private readonly SigScanner sigScanner;

        public Framework Framework { get; }

        public CommandManager CommandManager { get; }
        public ChatHandlers ChatHandlers { get; }
        public NetworkHandlers NetworkHandlers { get; }

        public readonly DiscordBotManager BotManager;

        public readonly PluginManager PluginManager;

        public readonly ClientState ClientState;

        public readonly DalamudStartInfo StartInfo;

        public readonly IconReplacer IconReplacer;

        public readonly DalamudConfiguration Configuration;

        public Dalamud(DalamudStartInfo info) {
            this.StartInfo = info;
            this.Configuration = DalamudConfiguration.Load(info.ConfigurationPath);
            
            this.baseDirectory = info.WorkingDirectory;

            this.unloadSignal = new ManualResetEvent(false);

            // Initialize the process information.
            this.targetModule = Process.GetCurrentProcess().MainModule;
            this.sigScanner = new SigScanner(this.targetModule);

            // Initialize game subsystem
            Framework = new Framework(this.sigScanner, this);

            // Initialize managers. Basically handlers for the logic
            CommandManager = new CommandManager(this, info.Language);
            SetupCommands();

            ChatHandlers = new ChatHandlers(this);
            NetworkHandlers = new NetworkHandlers(this, this.Configuration.OptOutMbCollection);

            this.ClientState = new ClientState(this, info, this.sigScanner, this.targetModule);

            this.BotManager = new DiscordBotManager(this, this.Configuration.DiscordFeatureConfig);

            this.PluginManager = new PluginManager(this, info.PluginDirectory, info.DefaultPluginDirectory);

            this.IconReplacer = new IconReplacer(this, this.sigScanner);

            try {
                this.PluginManager.LoadPlugins();
            } catch (Exception ex) {
                Framework.Gui.Chat.PrintError(
                    "[XIVLAUNCHER] There was an error loading additional plugins. Please check the log for more details.");
                Log.Error(ex, "Plugin load failed.");
            }
        }

        public void Start() {
            Framework.Enable();

            this.BotManager.Start();

            this.IconReplacer.Enable();
        }

        public void Unload() {
            this.unloadSignal.Set();
        }

        public void WaitForUnload() {
            this.unloadSignal.WaitOne();
        }

        public void Dispose() {
            Framework.Dispose();

            this.BotManager.Dispose();

            this.unloadSignal.Dispose();

            this.IconReplacer.Dispose();
        }

        private void SetupCommands() {
            CommandManager.AddHandler("/xldclose", new CommandInfo(OnUnloadCommand) {
                HelpMessage = "Unloads XIVLauncher in-game addon.",
                ShowInHelp = false
            });

            CommandManager.AddHandler("/xldreloadplugins", new CommandInfo(OnPluginReloadCommand) {
                HelpMessage = "Reloads all plugins.",
                ShowInHelp = false
            });

            CommandManager.AddHandler("/xldsay", new CommandInfo(OnCommandDebugSay) {
                HelpMessage = "Print to chat.",
                ShowInHelp = false
            });

            CommandManager.AddHandler("/xldcombo", new CommandInfo(OnCommandDebugCombo) {
                HelpMessage = "COMBO debug",
                ShowInHelp = false
            });

            CommandManager.AddHandler("/xlhelp", new CommandInfo(OnCommandHelp) {
                HelpMessage = "Shows list of commands available."
            });

            CommandManager.AddHandler("/fatewatchadd", new CommandInfo(OnFateWatchAdd) {
                HelpMessage = "Add a fate to your watch list by name. Usage: /fatewatchadd <name of fate>"
            });

            CommandManager.AddHandler("/fatewatchlist", new CommandInfo(OnFateWatchList) {
                HelpMessage = "List fates you're currently watching."
            });

            CommandManager.AddHandler("/fatewatchremove", new CommandInfo(OnFateWatchRemove) {
                HelpMessage = "Remove a fate from your watch list. Usage: /fatewatchremove <name of fate>"
            });

            CommandManager.AddHandler("/xlmute", new CommandInfo(OnBadWordsAdd) {
                HelpMessage = "Mute a word or sentence from appearing in chat. Usage: /xlmute <word or sentence>"
            });

            CommandManager.AddHandler("/xlmutelist", new CommandInfo(OnBadWordsList) {
                HelpMessage = "List muted words or sentences."
            });

            CommandManager.AddHandler("/xlunmute", new CommandInfo(OnBadWordsRemove) {
                HelpMessage = "Unmute a word or sentence. Usage: /fatewatchremove <word or sentence>"
            });

            CommandManager.AddHandler("/xldactortable", new CommandInfo(OnDebugActorTable) {
                HelpMessage = "Actor table operations",
                ShowInHelp = false
            });

            CommandManager.AddHandler("/ll", new CommandInfo(OnLastLinkCommand) {
                HelpMessage = "Open the last posted link in your default browser."
            });

            CommandManager.AddHandler("/xlbotjoin", new CommandInfo(OnBotJoinCommand) {
                HelpMessage = "Add the XIVLauncher discord bot you set up to your server."
            });
        }

        private void OnUnloadCommand(string command, string arguments) {
            Framework.Gui.Chat.Print("Unloading...");
            Unload();
        }

        private void OnCommandHelp(string command, string arguments) {
            var showDebug = arguments.Contains("debug");

            Framework.Gui.Chat.Print("Available commands:");
            foreach (var cmd in CommandManager.Commands) {
                if (!cmd.Value.ShowInHelp && !showDebug)
                    continue;

                Framework.Gui.Chat.Print($"{cmd.Key}: {cmd.Value.HelpMessage}");
            }
        }

        private void OnCommandDebugSay(string command, string arguments) {
            var parts = arguments.Split();

            var chatType = (XivChatType) int.Parse(parts[0]);
            var msg = string.Join(" ", parts.Take(1).ToArray());

            Framework.Gui.Chat.PrintChat(new XivChatEntry {
                Message = msg,
                Name = "Xiv Launcher",
                Type = chatType
            });
        }

        private void OnPluginReloadCommand(string command, string arguments) {
            Framework.Gui.Chat.Print("Reloading...");

            try {
                this.PluginManager.UnloadPlugins();
                this.PluginManager.LoadPlugins();

                Framework.Gui.Chat.Print("OK");
            } catch (Exception ex) {
                Framework.Gui.Chat.PrintError("Reload failed.");
                Log.Error(ex, "Plugin reload failed.");
            }
        }

        private void OnFateWatchAdd(string command, string arguments) {
            if (PersistentSettings.Instance.Fates == null)
                PersistentSettings.Instance.Fates = new List<PersistentSettings.FateInfo>();

            dynamic candidates = XivApi.Search(arguments, "Fate").GetAwaiter().GetResult();

            if (candidates.Results.Count == 0) {
                Framework.Gui.Chat.Print("No fates found using that name.");
                return;
            }

            var fateInfo = new PersistentSettings.FateInfo {
                Id = candidates.Results[0].ID,
                Name = candidates.Results[0].Name
            };

            PersistentSettings.Instance.Fates.Add(fateInfo);

            Framework.Gui.Chat.Print($"Added fate \"{fateInfo.Name}\".");
        }

        private void OnFateWatchList(string command, string arguments) {
            if (PersistentSettings.Instance.Fates == null)
                PersistentSettings.Instance.Fates = new List<PersistentSettings.FateInfo>();

            if (PersistentSettings.Instance.Fates.Count == 0) {
                Framework.Gui.Chat.Print("No fates on your watchlist.");
                return;
            }

            foreach (var fate in PersistentSettings.Instance.Fates)
                Framework.Gui.Chat.Print($"Fate {fate.Id}: {fate.Name}");
        }

        private void OnFateWatchRemove(string command, string arguments) {
            if (PersistentSettings.Instance.Fates == null)
                PersistentSettings.Instance.Fates = new List<PersistentSettings.FateInfo>();

            dynamic candidates = XivApi.Search(arguments, "Fate").GetAwaiter().GetResult();

            if (candidates.Results.Count == 0) {
                Framework.Gui.Chat.Print("No fates found using that name.");
                return;
            }

            PersistentSettings.Instance.Fates.RemoveAll(x => x.Id == candidates.Results[0].ID);

            Framework.Gui.Chat.Print($"Removed fate \"{candidates.Results[0].Name}\".");
        }

        private void OnBadWordsAdd(string command, string arguments) {
            if (PersistentSettings.Instance.BadWords == null)
                PersistentSettings.Instance.BadWords = new List<string>();

            PersistentSettings.Instance.BadWords.Add(arguments);

            Framework.Gui.Chat.Print($"Muted \"{arguments}\".");
        }

        private void OnBadWordsList(string command, string arguments) {
            if (PersistentSettings.Instance.BadWords == null)
                PersistentSettings.Instance.BadWords = new List<string>();

            if (PersistentSettings.Instance.BadWords.Count == 0) {
                Framework.Gui.Chat.Print("No muted words or sentences.");
                return;
            }

            foreach (var word in PersistentSettings.Instance.BadWords) Framework.Gui.Chat.Print($"\"{word}\"");
        }

        private void OnBadWordsRemove(string command, string arguments) {
            if (PersistentSettings.Instance.BadWords == null)
                PersistentSettings.Instance.BadWords = new List<string>();

            PersistentSettings.Instance.BadWords.RemoveAll(x => x == arguments);

            Framework.Gui.Chat.Print($"Unmuted \"{arguments}\".");
        }

        private void OnLastLinkCommand(string command, string arguments) {
            if (string.IsNullOrEmpty(ChatHandlers.LastLink)) {
                Framework.Gui.Chat.Print("No last link...");
                return;
            }

            Framework.Gui.Chat.Print("Opening " + ChatHandlers.LastLink);
            Process.Start(ChatHandlers.LastLink);
        }

        private void OnDebugActorTable(string command, string arguments) {
            Framework.Gui.Chat.Print(this.ClientState.Actors.Length + " entries");
            Framework.Gui.Chat.Print(this.ClientState.LocalPlayer.Name);
            Framework.Gui.Chat.Print(this.ClientState.LocalPlayer.CurrentWorld.Name);
            Framework.Gui.Chat.Print(this.ClientState.LocalContentId.ToString("X"));

            for (var i = 0; i < this.ClientState.Actors.Length; i++) {
                var actor = this.ClientState.Actors[i];

                Log.Debug(actor.Name);
                Framework.Gui.Chat.Print(
                    $"{i} - {actor.Name} - {actor.Position.X} {actor.Position.Y} {actor.Position.Z}");

                if (actor is Npc npc)
                    Framework.Gui.Chat.Print($"DataId: {npc.DataId}");

                if (actor is Chara chara)
                    Framework.Gui.Chat.Print(
                        $"Level: {chara.Level} ClassJob: {chara.ClassJob.Name} CHP: {chara.CurrentHp} MHP: {chara.MaxHp} CMP: {chara.CurrentMp} MMP: {chara.MaxMp}");
            }
        }

        private void OnCommandDebugCombo(string command, string arguments) {
            var argumentsParts = arguments.Split();

            switch (argumentsParts[0]) {
                case "setall": {
                    foreach (var value in Enum.GetValues(typeof(CustomComboPreset)).Cast<CustomComboPreset>()) {
                        if (value == CustomComboPreset.None)
                            continue;

                        this.Configuration.ComboPresets |= value;
                    }

                    Framework.Gui.Chat.Print("all SET");
                }
                    break;
                case "unsetall": {
                    foreach (var value in Enum.GetValues(typeof(CustomComboPreset)).Cast<CustomComboPreset>()) {
                        this.Configuration.ComboPresets &= value;
                    }

                    Framework.Gui.Chat.Print("all UNSET");
                }
                    break;
                case "set": {
                    foreach (var value in Enum.GetValues(typeof(CustomComboPreset)).Cast<CustomComboPreset>()) {
                        if (value.ToString().ToLower() != argumentsParts[1].ToLower())
                            continue;

                        this.Configuration.ComboPresets |= value;
                        Framework.Gui.Chat.Print(argumentsParts[1] + " SET");
                    }
                }
                    break;
                case "list": {
                    foreach (var value in Enum.GetValues(typeof(CustomComboPreset)).Cast<CustomComboPreset>()) {
                        if (this.Configuration.ComboPresets.HasFlag(value))
                            Framework.Gui.Chat.Print(value.ToString());
                    }
                }
                    break;

                default: Framework.Gui.Chat.Print("Unknown");
                    break;
            }

            this.Configuration.Save(this.StartInfo.ConfigurationPath);
        }

        private void OnBotJoinCommand(string command, string arguments) {
            if (this.BotManager != null && this.BotManager.IsConnected)
                Process.Start(
                    $"https://discordapp.com/oauth2/authorize?client_id={this.BotManager.UserId}&scope=bot&permissions=117760");
            else
                Framework.Gui.Chat.Print(
                    "The XIVLauncher discord bot was not set up correctly or could not connect to discord. Please check the settings and the FAQ.");
        }
    }
}
