using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CheapLoc;
using Dalamud.Data;
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
using Dalamud.Interface;
using Dalamud.Plugin;
using ImGuiNET;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Dalamud {
    public sealed class Dalamud : IDisposable {
        private readonly string baseDirectory;

        private readonly ManualResetEvent unloadSignal;

        private readonly ProcessModule targetModule;

        public readonly SigScanner SigScanner;

        public readonly Framework Framework;

        public readonly CommandManager CommandManager;

        public readonly ChatHandlers ChatHandlers;

        public readonly NetworkHandlers NetworkHandlers;

        public readonly DiscordBotManager BotManager;

        public PluginManager PluginManager { get; private set; }
        public PluginRepository PluginRepository { get; private set; }

        public readonly ClientState ClientState;

        public readonly DalamudStartInfo StartInfo;
        private readonly LoggingLevelSwitch loggingLevelSwitch;

        public readonly DalamudConfiguration Configuration;

        private readonly WinSockHandlers WinSock2;

        public readonly InterfaceManager InterfaceManager;

        public readonly DataManager Data;

        private AntiDebug antiDebug;


        private Localization localizationMgr;


        private readonly string assemblyVersion = Assembly.GetAssembly(typeof(ChatHandlers)).GetName().Version.ToString();

        public Dalamud(DalamudStartInfo info, LoggingLevelSwitch loggingLevelSwitch) {
            this.StartInfo = info;
            this.loggingLevelSwitch = loggingLevelSwitch;

            this.Configuration = DalamudConfiguration.Load(info.ConfigurationPath);
            this.localizationMgr = new Localization(this.StartInfo.WorkingDirectory);

            if (!string.IsNullOrEmpty(this.Configuration.LanguageOverride)) {
                this.localizationMgr.SetupWithLangCode(this.Configuration.LanguageOverride);
            } else {
                this.localizationMgr.SetupWithUiCulture();
            }

            this.baseDirectory = info.WorkingDirectory;

            this.unloadSignal = new ManualResetEvent(false);

            // Initialize the process information.
            this.targetModule = Process.GetCurrentProcess().MainModule;
            this.SigScanner = new SigScanner(this.targetModule, true);

            // Initialize game subsystem
            this.Framework = new Framework(this.SigScanner, this);

            // Initialize managers. Basically handlers for the logic
            this.CommandManager = new CommandManager(this, info.Language);
            SetupCommands();

            this.ChatHandlers = new ChatHandlers(this);
            this.NetworkHandlers = new NetworkHandlers(this, this.Configuration.OptOutMbCollection);

            this.Data = new DataManager(this.StartInfo.Language);
            this.Data.Initialize();

            this.ClientState = new ClientState(this, info, this.SigScanner);

            this.BotManager = new DiscordBotManager(this, this.Configuration.DiscordFeatureConfig);

            this.WinSock2 = new WinSockHandlers();

            try {
                this.InterfaceManager = new InterfaceManager(this, this.SigScanner);
                this.InterfaceManager.OnDraw += BuildDalamudUi;
            } catch (Exception e) {
                Log.Information(e, "Could not init interface.");
            }
        }

        public void Start() {
            try {
                this.InterfaceManager?.Enable();
            } catch (Exception e) {
                Log.Information("Could not enable interface.");
            }

            this.Framework.Enable();
            this.ClientState.Enable();

            this.BotManager.Start();

            try {
                this.PluginManager = new PluginManager(this, this.StartInfo.PluginDirectory, this.StartInfo.DefaultPluginDirectory);
                this.PluginManager.LoadPlugins();

                PluginRepository = new PluginRepository(PluginManager, this.StartInfo.PluginDirectory, this.StartInfo.GameVersion);
            } catch (Exception ex) {
                Log.Error(ex, "Plugin load failed.");
            }
        }

        public void Unload() {
            this.unloadSignal.Set();
        }

        public void WaitForUnload() {
            this.unloadSignal.WaitOne();
        }

        public void Dispose() {
            // this must be done before unloading plugins, or it can cause a race condition
            // due to rendering happening on another thread, where a plugin might receive
            // a render call after it has been disposed, which can crash if it attempts to
            // use any resources that it freed in its own Dispose method
            this.InterfaceManager.Dispose();

            try
            {
                this.PluginManager.UnloadPlugins();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Plugin unload failed.");
            }

            this.Framework.Dispose();
            this.ClientState.Dispose();

            this.BotManager.Dispose();

            this.unloadSignal.Dispose();

            this.WinSock2.Dispose();

            this.SigScanner.Dispose();

            this.antiDebug?.Dispose();
        }

        #region Interface

        private bool isImguiDrawDemoWindow = false;

#if DEBUG
        private bool isImguiDrawDevMenu = true;
#else
        private bool isImguiDrawDevMenu = false;
#endif

        private bool isAntiDebugEnabled = false;

        private bool isImguiDrawLogWindow = false;
        private bool isImguiDrawDataWindow = false;
        private bool isImguiDrawPluginWindow = false;
        private bool isImguiDrawCreditsWindow = false;

        private DalamudLogWindow logWindow;
        private DalamudDataWindow dataWindow;
        private DalamudCreditsWindow creditsWindow;
        private PluginInstallerWindow pluginWindow;

        private void BuildDalamudUi()
        {
            if (this.isImguiDrawDevMenu)
            {
                if (ImGui.BeginMainMenuBar())
                {
                    if (ImGui.BeginMenu("Dalamud"))
                    {
                        ImGui.MenuItem("Draw Dalamud dev menu", "", ref this.isImguiDrawDevMenu);
                        ImGui.Separator();
                        if (ImGui.MenuItem("Open Log window"))
                        {
                            this.logWindow = new DalamudLogWindow();
                            this.isImguiDrawLogWindow = true;
                        }
                        if (ImGui.BeginMenu("Set log level..."))
                        {
                            foreach (var logLevel in Enum.GetValues(typeof(LogEventLevel)).Cast<LogEventLevel>()) {
                                if (ImGui.MenuItem(logLevel + "##logLevelSwitch", "", this.loggingLevelSwitch.MinimumLevel == logLevel))
                                {
                                    this.loggingLevelSwitch.MinimumLevel = logLevel;
                                }
                            }

                            ImGui.EndMenu();
                        }
                        ImGui.Separator();
                        if (ImGui.MenuItem("Open Data window"))
                        {
                            this.dataWindow = new DalamudDataWindow(this);
                            this.isImguiDrawDataWindow = true;
                        }
                        if (ImGui.MenuItem("Open Credits window")) {
                            var logoGraphic =
                                this.InterfaceManager.LoadImage(
                                    Path.Combine(this.StartInfo.WorkingDirectory, "UIRes", "logo.png"));
                            this.creditsWindow = new DalamudCreditsWindow(logoGraphic, this.Framework);
                            this.isImguiDrawCreditsWindow = true;
                        }
                        ImGui.MenuItem("Draw ImGui demo", "", ref this.isImguiDrawDemoWindow);
                        ImGui.Separator();
                        if (ImGui.MenuItem("Enable AntiDebug", "", ref this.isAntiDebugEnabled)) {
                            if (this.isAntiDebugEnabled) {
                                this.antiDebug = new AntiDebug();
                                this.antiDebug.Enable();
                            } else {
                                this.antiDebug?.Dispose();
                            }
                        }
                        ImGui.Separator();
                        if (ImGui.MenuItem("Unload Dalamud"))
                        {
                            Unload();
                        }
                        if (ImGui.MenuItem("Kill game"))
                        {
                            Process.GetCurrentProcess().Kill();
                        }
                        ImGui.Separator();
                        ImGui.MenuItem(this.assemblyVersion, false);
                        ImGui.MenuItem(this.StartInfo.GameVersion, false);

                        ImGui.EndMenu();
                    }

                    if (ImGui.BeginMenu("Plugins"))
                    {
                        if (ImGui.MenuItem("Open Plugin installer"))
                        {
                            this.pluginWindow = new PluginInstallerWindow(this.PluginManager, this.PluginRepository, this.StartInfo.GameVersion);
                            this.isImguiDrawPluginWindow = true;
                        }
                        if (ImGui.MenuItem("Print plugin info")) {
                            foreach (var plugin in this.PluginManager.Plugins) {
                                // TODO: some more here, state maybe?
                                Log.Information($"{plugin.Plugin.Name}");
                            }
                        }
                        if (ImGui.MenuItem("Reload plugins"))
                        {
                            OnPluginReloadCommand(string.Empty, string.Empty);
                        }
                        ImGui.EndMenu();
                    }

                    if (ImGui.BeginMenu("Localization"))
                    {
                        if (ImGui.MenuItem("Export localizable"))
                        {
                            Loc.ExportLocalizable();
                        }

                        if (ImGui.BeginMenu("Load language..."))
                        {
                            if (ImGui.MenuItem("From Fallbacks"))
                            {
                                Loc.SetupWithFallbacks();
                            }

                            if (ImGui.MenuItem("From UICulture")) {
                                this.localizationMgr.SetupWithUiCulture();
                            }

                            foreach (var applicableLangCode in Localization.ApplicableLangCodes) {
                                if (ImGui.MenuItem($"Applicable: {applicableLangCode}"))
                                {
                                    this.localizationMgr.SetupWithLangCode(applicableLangCode);
                                }
                            }

                            ImGui.EndMenu();
                        }
                        ImGui.EndMenu();
                    }

                    ImGui.EndMainMenuBar();
                }
            }

            if (this.isImguiDrawLogWindow)
            {
                this.isImguiDrawLogWindow = this.logWindow != null && this.logWindow.Draw();

                if (this.isImguiDrawLogWindow == false)
                {
                    this.logWindow?.Dispose();
                    this.logWindow = null;
                }
            }

            if (this.isImguiDrawDataWindow)
            {
                this.isImguiDrawDataWindow = this.dataWindow != null && this.dataWindow.Draw();
            }

            if (this.isImguiDrawPluginWindow)
            {
                this.isImguiDrawPluginWindow = this.pluginWindow != null && this.pluginWindow.Draw();
            }

            if (this.isImguiDrawItemSearchWindow)
            {
                this.isImguiDrawItemSearchWindow = this.itemSearchCommandWindow != null && this.itemSearchCommandWindow.Draw();

                if (this.isImguiDrawItemSearchWindow == false)
                {
                    this.itemSearchCommandWindow?.Dispose();
                    this.itemSearchCommandWindow = null;
                }
            }

            if (this.isImguiDrawCreditsWindow)
            {
                this.isImguiDrawCreditsWindow = this.creditsWindow != null && this.creditsWindow.Draw();

                if (this.isImguiDrawCreditsWindow == false)
                {
                    this.creditsWindow?.Dispose();
                    this.creditsWindow = null;
                }
            }

            if (this.isImguiDrawDemoWindow)
                ImGui.ShowDemoWindow();
        }

        #endregion

        private void SetupCommands() {
            CommandManager.AddHandler("/xldclose", new CommandInfo(OnUnloadCommand) {
                HelpMessage = Loc.Localize("DalamudUnloadHelp", "Unloads XIVLauncher in-game addon."),
                ShowInHelp = false
            });

            CommandManager.AddHandler("/xldreloadplugins", new CommandInfo(OnPluginReloadCommand) {
                HelpMessage = Loc.Localize("DalamudPluginReloadHelp", "Reloads all plugins."),
                                           ShowInHelp = false
            });

            CommandManager.AddHandler("/xldsay", new CommandInfo(OnCommandDebugSay) {
                HelpMessage = Loc.Localize("DalamudPrintChatHelp", "Print to chat."),
                                           ShowInHelp = false
            });

            CommandManager.AddHandler("/xlhelp", new CommandInfo(OnHelpCommand) {
                HelpMessage = Loc.Localize("DalamudCmdInfoHelp", "Shows list of commands available.")
            });

            CommandManager.AddHandler("/xlmute", new CommandInfo(OnBadWordsAddCommand) {
                HelpMessage = Loc.Localize("DalamudMuteHelp", "Mute a word or sentence from appearing in chat. Usage: /xlmute <word or sentence>")
            });

            CommandManager.AddHandler("/xlmutelist", new CommandInfo(OnBadWordsListCommand) {
                HelpMessage = Loc.Localize("DalamudMuteListHelp", "List muted words or sentences.")
            });

            CommandManager.AddHandler("/xlunmute", new CommandInfo(OnBadWordsRemoveCommand) {
                HelpMessage = Loc.Localize("DalamudUnmuteHelp", "Unmute a word or sentence. Usage: /xlunmute <word or sentence>")
            });

            CommandManager.AddHandler("/ll", new CommandInfo(OnLastLinkCommand) {
                HelpMessage = Loc.Localize("DalamudLastLinkHelp", "Open the last posted link in your default browser.")
            });

            CommandManager.AddHandler("/xlbotjoin", new CommandInfo(OnBotJoinCommand) {
                HelpMessage = Loc.Localize("DalamudBotJoinHelp", "Add the XIVLauncher discord bot you set up to your server.")
            });

            CommandManager.AddHandler("/xlbgmset", new CommandInfo(OnBgmSetCommand)
            {
                HelpMessage = Loc.Localize("DalamudBgmSetHelp", "Set the Game background music. Usage: /xlbgmset <BGM ID>")
            });

            CommandManager.AddHandler("/xlitem", new CommandInfo(OnItemLinkCommand)
            {
                HelpMessage = Loc.Localize("DalamudItemLinkHelp", "Link an item by name. Usage: /xlitem <Item name>.  For matching an item exactly, use /xlitem +<Item name>")
            });

#if DEBUG
            CommandManager.AddHandler("/xldzpi", new CommandInfo(OnDebugZoneDownInjectCommand)
            {
                HelpMessage = "Inject zone down channel",
                ShowInHelp = false
            });
#endif

            CommandManager.AddHandler("/xlbonus", new CommandInfo(OnRouletteBonusNotifyCommand)
            {
                HelpMessage = Loc.Localize("DalamudBonusHelp", "Notify when a roulette has a bonus you specified. Run without parameters for more info. Usage: /xlbonus <roulette name> <role name>")
            });

            CommandManager.AddHandler("/xldev", new CommandInfo(OnDebugDrawDevMenu) {
                HelpMessage = Loc.Localize("DalamudDevMenuHelp", "Draw dev menu DEBUG"),
                                           ShowInHelp = false
            });

            CommandManager.AddHandler("/xlplugins", new CommandInfo(OnOpenInstallerCommand)
            {
                HelpMessage = Loc.Localize("DalamudInstallerHelp", "Open the plugin installer")
            });

            this.CommandManager.AddHandler("/xlcredits", new CommandInfo(OnOpenCreditsCommand) {
                HelpMessage = Loc.Localize("DalamudCreditsHelp", "Opens the credits for dalamud.")
            });

            this.CommandManager.AddHandler("/xllanguage", new CommandInfo(OnSetLanguageCommand)
            {
                HelpMessage = Loc.Localize("DalamudLanguageHelp", "Set the language for the in-game addon and plugins that support it.")
            });
        }

        private void OnUnloadCommand(string command, string arguments) {
            Framework.Gui.Chat.Print("Unloading...");
            Unload();
        }

        private void OnHelpCommand(string command, string arguments) {
            var showDebug = arguments.Contains("debug");

            Framework.Gui.Chat.Print(Loc.Localize("DalamudCmdHelpAvailable", "Available commands:"));
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
                MessageBytes = Encoding.UTF8.GetBytes(msg),
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

        private void OnBadWordsAddCommand(string command, string arguments) {
            if (this.Configuration.BadWords == null)
                this.Configuration.BadWords = new List<string>();

            this.Configuration.BadWords.Add(arguments);

            this.Configuration.Save(this.StartInfo.ConfigurationPath);

            Framework.Gui.Chat.Print(string.Format(Loc.Localize("DalamudMuted", "Muted \"{0}\"."), arguments));
        }

        private void OnBadWordsListCommand(string command, string arguments) {
            if (this.Configuration.BadWords == null)
                this.Configuration.BadWords = new List<string>();

            if (this.Configuration.BadWords.Count == 0) {
                Framework.Gui.Chat.Print(Loc.Localize("DalamudNoneMuted", "No muted words or sentences."));
                return;
            }

            this.Configuration.Save(this.StartInfo.ConfigurationPath);

            foreach (var word in this.Configuration.BadWords) Framework.Gui.Chat.Print($"\"{word}\"");
        }

        private void OnBadWordsRemoveCommand(string command, string arguments) {
            if (this.Configuration.BadWords == null)
                this.Configuration.BadWords = new List<string>();

            this.Configuration.BadWords.RemoveAll(x => x == arguments);

            this.Configuration.Save(this.StartInfo.ConfigurationPath);

            Framework.Gui.Chat.Print(string.Format(Loc.Localize("DalamudUnmuted", "Unmuted \"{0}\"."), arguments));
        }

        private void OnLastLinkCommand(string command, string arguments) {
            if (string.IsNullOrEmpty(ChatHandlers.LastLink)) {
                Framework.Gui.Chat.Print(Loc.Localize("DalamudNoLastLink", "No last link..."));
                return;
            }

            Framework.Gui.Chat.Print(string.Format(Loc.Localize("DalamudOpeningLink", "Opening {0}"), ChatHandlers.LastLink));
            Process.Start(ChatHandlers.LastLink);
        }

        private void OnBotJoinCommand(string command, string arguments) {
            if (this.BotManager != null && this.BotManager.IsConnected)
                Process.Start(
                    $"https://discordapp.com/oauth2/authorize?client_id={this.BotManager.UserId}&scope=bot&permissions=117760");
            else
                Framework.Gui.Chat.Print(
                    Loc.Localize("DalamudBotNotSetup", "The XIVLauncher discord bot was not set up correctly or could not connect to discord. Please check the settings and the FAQ."));
        }

        private void OnBgmSetCommand(string command, string arguments)
        {
            Framework.Gui.SetBgm(ushort.Parse(arguments));
        }

        private ItemSearchWindow itemSearchCommandWindow;
        private bool isImguiDrawItemSearchWindow;

        private void OnItemLinkCommand(string command, string arguments) {
            this.itemSearchCommandWindow = new ItemSearchWindow(this.Data, new UiBuilder(this.InterfaceManager, "ItemSearcher"));
            this.itemSearchCommandWindow.OnItemChosen += (sender, item) => {
                var hexData = new byte[] {
                    0x02, 0x13, 0x06, 0xFE, 0xFF, 0xF3, 0xF3, 0xF3, 0x03, 0x02, 0x27, 0x07, 0x03, 0xF2, 0x3A, 0x2F,
                    0x02, 0x01, 0x03, 0x02, 0x13, 0x06, 0xFE, 0xFF, 0xFF, 0x7B, 0x1A, 0x03, 0xEE, 0x82, 0xBB, 0x02,
                    0x13, 0x02, 0xEC, 0x03
                };

                var endTag = new byte[] {
                    0x02, 0x27, 0x07, 0xCF, 0x01, 0x01, 0x01, 0xFF, 0x01, 0x03, 0x02, 0x13, 0x02, 0xEC, 0x03
                };

                BitConverter.GetBytes((short) item.RowId).Reverse().ToArray().CopyTo(hexData, 14);

                hexData = hexData.Concat(Encoding.UTF8.GetBytes(item.Name)).Concat(endTag).ToArray();

                this.Framework.Gui.Chat.PrintChat(new XivChatEntry {
                    MessageBytes = hexData
                });
            };
            this.isImguiDrawItemSearchWindow = true;
        }

#if DEBUG
        private void OnDebugZoneDownInjectCommand(string command, string arguments) {
            var data = File.ReadAllBytes(arguments);

            Framework.Network.InjectZoneProtoPacket(data);
            Framework.Gui.Chat.Print($"{arguments} OK with {data.Length} bytes");
        }
#endif

        private void OnRouletteBonusNotifyCommand(string command, string arguments)
        {
            if (this.Configuration.DiscordFeatureConfig.CfPreferredRoleChannel == null)
                Framework.Gui.Chat.PrintError(Loc.Localize("DalamudChannelNotSetup", "You have not set up a discord channel for these notifications - you will only receive them in chat. To do this, please use the XIVLauncher in-game settings."));

            if (string.IsNullOrEmpty(arguments))
                goto InvalidArgs;

            var argParts = arguments.Split();
            if (argParts.Length < 2)
                goto InvalidArgs;


            if (this.Configuration.PreferredRoleReminders == null)
                this.Configuration.PreferredRoleReminders = new Dictionary<int, DalamudConfiguration.PreferredRole>();

            var rouletteIndex = RouletteSlugToKey(argParts[0]);

            if (rouletteIndex == 0)
                goto InvalidArgs;

            if (!Enum.TryParse(argParts[1].First().ToString().ToUpper() + argParts[1].ToLower().Substring(1), out DalamudConfiguration.PreferredRole role))
                goto InvalidArgs;

            if (this.Configuration.PreferredRoleReminders.ContainsKey(rouletteIndex))
                this.Configuration.PreferredRoleReminders[rouletteIndex] = role;
            else
                this.Configuration.PreferredRoleReminders.Add(rouletteIndex, role);

            Framework.Gui.Chat.Print($"Set bonus notifications for {argParts[0]}({rouletteIndex}) to {role}");
            Framework.Gui.Chat.Print(string.Format(Loc.Localize("DalamudBonusSet", "Set bonus notifications for {0}({1}) to {2}"), argParts[0], rouletteIndex, role));
            this.Configuration.Save(this.StartInfo.ConfigurationPath);

            return;

            InvalidArgs:
            Framework.Gui.Chat.PrintError(Loc.Localize("DalamudInvalidArguments", "Unrecognized arguments."));
            Framework.Gui.Chat.Print(Loc.Localize("DalamudBonusPossibleValues", "Possible values for roulette: leveling, 506070, msq, guildhests, expert, trials, mentor, alliance, normal\nPossible values for role: tank, dps, healer, all, none/reset"));
        }

        private void OnDebugDrawDevMenu(string command, string arguments) {
            this.isImguiDrawDevMenu = true;
        }

        private void OnOpenInstallerCommand(string command, string arguments) {
            this.pluginWindow = new PluginInstallerWindow(this.PluginManager, PluginRepository, this.StartInfo.GameVersion);
            this.isImguiDrawPluginWindow = true;
        }

        private void OnOpenCreditsCommand(string command, string arguments)
        {
            var logoGraphic =
                this.InterfaceManager.LoadImage(
                    Path.Combine(this.StartInfo.WorkingDirectory, "UIRes", "logo.png"));
            this.creditsWindow = new DalamudCreditsWindow(logoGraphic, this.Framework);
            this.isImguiDrawCreditsWindow = true;
        }

        private void OnSetLanguageCommand(string command, string arguments)
        {
            if (Localization.ApplicableLangCodes.Contains(arguments.ToLower())) {
                this.localizationMgr.SetupWithLangCode(arguments.ToLower());
                this.Configuration.LanguageOverride = arguments.ToLower();
            } else {
                this.localizationMgr.SetupWithUiCulture();
                this.Configuration.LanguageOverride = null;
            }

            this.Configuration.Save(this.StartInfo.ConfigurationPath);
        }

        private int RouletteSlugToKey(string slug) => slug.ToLower() switch {
            "leveling" => 1,
            "506070" => 2,
            "msq" => 3,
            "guildhests" => 4,
            "expert" => 5,
            "trials" => 6,
            "mentor" => 8,
            "alliance" => 9,
            "normal" => 10,
            _ => 0
        };

        private DalamudConfiguration.PreferredRole RoleNameToPreferredRole(string name) => name.ToLower() switch
        {
            "Tank" => DalamudConfiguration.PreferredRole.Tank,
            "Healer" => DalamudConfiguration.PreferredRole.Healer,
            "Dps" => DalamudConfiguration.PreferredRole.Dps,
            "All" => DalamudConfiguration.PreferredRole.All,
            _ => DalamudConfiguration.PreferredRole.None
        };
    }
}
