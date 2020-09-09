using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CheapLoc;
using Dalamud.Data;
using Dalamud.DiscordBot;
using Dalamud.Game;
using Dalamud.Game.Chat;
using Dalamud.Game.Chat.SeStringHandling;
using Dalamud.Game.ClientState;
using Dalamud.Game.Command;
using Dalamud.Game.Internal;
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

        public CommandManager CommandManager { get; private set; }

        public ChatHandlers ChatHandlers { get; private set; }

        public NetworkHandlers NetworkHandlers { get; private set; }

        public DiscordBotManager BotManager { get; private set; }

        internal PluginManager PluginManager { get; private set; }
        internal PluginRepository PluginRepository { get; private set; }

        public readonly ClientState ClientState;

        public readonly DalamudStartInfo StartInfo;
        private readonly LoggingLevelSwitch loggingLevelSwitch;

        internal readonly DalamudConfiguration Configuration;

        private readonly WinSockHandlers WinSock2;

        internal InterfaceManager InterfaceManager { get; private set; }

        public DataManager Data { get; private set; }

        internal SeStringManager SeStringManager { get; private set; }


        internal Localization LocalizationManager;

        public bool IsReady { get; private set; }

        public Dalamud(DalamudStartInfo info, LoggingLevelSwitch loggingLevelSwitch) {
            this.StartInfo = info;
            this.loggingLevelSwitch = loggingLevelSwitch;

            this.Configuration = DalamudConfiguration.Load(info.ConfigurationPath);

            this.baseDirectory = info.WorkingDirectory;

            this.unloadSignal = new ManualResetEvent(false);

            // Initialize the process information.
            this.targetModule = Process.GetCurrentProcess().MainModule;
            this.SigScanner = new SigScanner(this.targetModule, true);

            // Initialize game subsystem
            this.Framework = new Framework(this.SigScanner, this);

            this.ClientState = new ClientState(this, info, this.SigScanner);

            this.WinSock2 = new WinSockHandlers();

            Task.Run(async () => {
                try {
                    var res = await AssetManager.EnsureAssets(this.baseDirectory);

                    if (!res) {
                        Log.Error("One or more assets failed to download.");
                        Unload();
                        return;
                    }
                } catch (Exception e) {
                    Log.Error(e, "Error in asset task.");
                    Unload();
                    return;
                }

                this.LocalizationManager = new Localization(this.StartInfo.WorkingDirectory);
                if (!string.IsNullOrEmpty(this.Configuration.LanguageOverride))
                    this.LocalizationManager.SetupWithLangCode(this.Configuration.LanguageOverride);
                else
                    this.LocalizationManager.SetupWithUiCulture();

                var pluginDir = this.StartInfo.PluginDirectory;
                if (this.Configuration.DoPluginTest)
                    pluginDir = Path.Combine(pluginDir, "..", "testPlugins");

                PluginRepository = new PluginRepository(this, pluginDir, this.StartInfo.GameVersion);

                var isInterfaceLoaded = false;
                if (!bool.Parse(Environment.GetEnvironmentVariable("DALAMUD_NOT_HAVE_INTERFACE") ?? "false")) {
                    try
                    {
                        InterfaceManager = new InterfaceManager(this, this.SigScanner);
                        InterfaceManager.OnDraw += BuildDalamudUi;

                        InterfaceManager.Enable();
                        isInterfaceLoaded = true;
                    }
                    catch (Exception e)
                    {
                        Log.Information(e, "Could not init interface.");
                    }
                }

                Data = new DataManager(this.StartInfo.Language);
                try {
                    await Data.Initialize(this.baseDirectory);
                } catch (Exception e) {
                    Log.Error(e, "Could not initialize DataManager.");
                    Unload();
                    return;
                }

                SeStringManager = new SeStringManager(Data);

                NetworkHandlers = new NetworkHandlers(this, this.Configuration.OptOutMbCollection);

                // Initialize managers. Basically handlers for the logic
                CommandManager = new CommandManager(this, info.Language);
                SetupCommands();

                ChatHandlers = new ChatHandlers(this);
                // Discord Bot Manager
                BotManager = new DiscordBotManager(this, this.Configuration.DiscordFeatureConfig);
                BotManager.Start();

                if (!bool.Parse(Environment.GetEnvironmentVariable("DALAMUD_NOT_HAVE_PLUGINS") ?? "false")) {
                    try
                    {
                        PluginRepository.CleanupPlugins();

                        PluginManager = new PluginManager(this, pluginDir, this.StartInfo.DefaultPluginDirectory);
                        PluginManager.LoadPlugins();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Plugin load failed.");
                    }
                }

                this.Framework.Enable();
                this.ClientState.Enable();

                IsReady = true;

                Troubleshooting.LogTroubleshooting(this, isInterfaceLoaded);
            });
        }

        public void Start() {
#if DEBUG
            //ReplaceExceptionHandler();
#endif
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
            this.InterfaceManager?.Dispose();

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
            
            this.Data.Dispose();
        }

#region Interface

        private bool isImguiDrawDemoWindow = false;

#if DEBUG
        private bool isImguiDrawDevMenu = true;
#else
        private bool isImguiDrawDevMenu = false;
#endif

        private bool isImguiDrawLogWindow = false;
        private bool isImguiDrawDataWindow = false;
        private bool isImguiDrawPluginWindow = false;
        private bool isImguiDrawCreditsWindow = false;
        private bool isImguiDrawSettingsWindow = false;
        private bool isImguiDrawPluginStatWindow = false;
        private bool isImguiDrawChangelogWindow = false;

        private DalamudLogWindow logWindow;
        private DalamudDataWindow dataWindow;
        private DalamudCreditsWindow creditsWindow;
        private DalamudSettingsWindow settingsWindow;
        private PluginInstallerWindow pluginWindow;
        private DalamudPluginStatWindow pluginStatWindow;
        private DalamudChangelogWindow changelogWindow;

        private void BuildDalamudUi()
        {
            if (!this.isImguiDrawDevMenu && !ClientState.Condition.Any())
            {
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 0, 0, 0));
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0, 0, 0, 0));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0, 0, 0, 0));
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0, 0, 0, 1));
                ImGui.PushStyleColor(ImGuiCol.TextSelectedBg, new Vector4(0, 0, 0, 1));
                ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(0, 0, 0, 1));
                ImGui.PushStyleColor(ImGuiCol.BorderShadow, new Vector4(0, 0, 0, 1));
                ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0, 0, 0, 1));

                ImGui.SetNextWindowPos(new Vector2(0, 0), ImGuiCond.Always);
                ImGui.SetNextWindowBgAlpha(1);

                if (ImGui.Begin("DevMenu Opener", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoSavedSettings))
                {
                    if (ImGui.Button("###devMenuOpener", new Vector2(40, 25)))
                        this.isImguiDrawDevMenu = true;

                    ImGui.End();
                }

                ImGui.PopStyleColor(5);
            }

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
                            this.logWindow = new DalamudLogWindow(CommandManager);
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
                            OnOpenCreditsCommand(null, null);
                        }
                        if (ImGui.MenuItem("Open Settings window"))
                        {
                            OnOpenSettingsCommand(null, null);
                        }
                        if (ImGui.MenuItem("Open Changelog window"))
                        {
                            OpenChangelog();
                        }
                        ImGui.MenuItem("Draw ImGui demo", "", ref this.isImguiDrawDemoWindow);
                        if (ImGui.MenuItem("Dump ImGui info"))
                            OnDebugImInfoCommand(null, null);
                        ImGui.Separator();
                        if (ImGui.MenuItem("Unload Dalamud"))
                        {
                            Unload();
                        }
                        if (ImGui.MenuItem("Kill game"))
                        {
                            Process.GetCurrentProcess().Kill();
                        }
                        if (ImGui.MenuItem("Cause AccessViolation")) {
                            var a = Marshal.ReadByte(IntPtr.Zero);
                        }
                        ImGui.Separator();
                        ImGui.MenuItem(Util.AssemblyVersion, false);
                        ImGui.MenuItem(this.StartInfo.GameVersion, false);

                        ImGui.EndMenu();
                    }

                    if (ImGui.BeginMenu("Game")) {
                        if (ImGui.MenuItem("Replace ExceptionHandler")) {
                            ReplaceExceptionHandler();
                        }

                        ImGui.EndMenu();
                    }

                    if (ImGui.BeginMenu("Plugins"))
                    {
                        if (ImGui.MenuItem("Open Plugin installer"))
                        {
                            this.pluginWindow = new PluginInstallerWindow(this, this.StartInfo.GameVersion);
                            this.isImguiDrawPluginWindow = true;
                        }
                        if (ImGui.MenuItem("Open Plugin Stats")) {
                            if (!this.isImguiDrawPluginStatWindow) {
                                this.pluginStatWindow = new DalamudPluginStatWindow(this.PluginManager);
                                this.isImguiDrawPluginStatWindow = true;
                            }
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

                        ImGui.Separator();
                        ImGui.MenuItem("API Level:" + PluginManager.DALAMUD_API_LEVEL, false);
                        ImGui.MenuItem("Loaded plugins:" + PluginManager?.Plugins.Count, false);
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
                                this.LocalizationManager.SetupWithUiCulture();
                            }

                            foreach (var applicableLangCode in Localization.ApplicableLangCodes) {
                                if (ImGui.MenuItem($"Applicable: {applicableLangCode}"))
                                {
                                    this.LocalizationManager.SetupWithLangCode(applicableLangCode);
                                }
                            }

                            ImGui.EndMenu();
                        }
                        ImGui.EndMenu();
                    }

                    if (this.Framework.Gui.GameUiHidden)
                        ImGui.BeginMenu("UI is hidden...", false);

                    ImGui.EndMainMenuBar();
                }
            }

            if (this.Framework.Gui.GameUiHidden)
                return;

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

            if (this.isImguiDrawCreditsWindow)
            {
                this.isImguiDrawCreditsWindow = this.creditsWindow != null && this.creditsWindow.Draw();

                if (this.isImguiDrawCreditsWindow == false)
                {
                    this.creditsWindow?.Dispose();
                    this.creditsWindow = null;
                }
            }

            if (this.isImguiDrawSettingsWindow)
            {
                this.isImguiDrawSettingsWindow = this.settingsWindow != null && this.settingsWindow.Draw();
            }

            if (this.isImguiDrawDemoWindow)
                ImGui.ShowDemoWindow();

            if (this.isImguiDrawPluginStatWindow) {
                this.isImguiDrawPluginStatWindow = this.pluginStatWindow != null && this.pluginStatWindow.Draw();
                if (!this.isImguiDrawPluginStatWindow) {
                    this.pluginStatWindow?.Dispose();
                    this.pluginStatWindow = null;
                }
            }

            if (this.isImguiDrawChangelogWindow)
            {
                this.isImguiDrawChangelogWindow = this.changelogWindow != null && this.changelogWindow.Draw();
            }
        }
        internal void OpenPluginInstaller() {
            this.pluginWindow = new PluginInstallerWindow(this, this.StartInfo.GameVersion);
            this.isImguiDrawPluginWindow = true;
        }

        internal void OpenChangelog() {
            this.changelogWindow = new DalamudChangelogWindow(this);
            this.isImguiDrawChangelogWindow = true;
        }

        private void ReplaceExceptionHandler() {
            var semd = this.SigScanner.ScanText(
                "40 55 53 56 48 8D AC 24 ?? ?? ?? ?? B8 ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 2B E0 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 85 ?? ?? ?? ?? 48 83 3D ?? ?? ?? ?? ??");
            Log.Debug($"SE debug filter at {semd.ToInt64():X}");

            var oldFilter = NativeFunctions.SetUnhandledExceptionFilter(semd);
            Log.Debug("Reset ExceptionFilter, old: {0}", oldFilter);
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

            CommandManager.AddHandler("/xlbgmset", new CommandInfo(OnBgmSetCommand) {
                HelpMessage = Loc.Localize("DalamudBgmSetHelp", "Set the Game background music. Usage: /xlbgmset <BGM ID>")
            });

#if DEBUG
            CommandManager.AddHandler("/xldzpi", new CommandInfo(OnDebugZoneDownInjectCommand) {
                HelpMessage = "Inject zone down channel",
                ShowInHelp = false
            });
#endif

            CommandManager.AddHandler("/xldev", new CommandInfo(OnDebugDrawDevMenu) {
                HelpMessage = Loc.Localize("DalamudDevMenuHelp", "Draw dev menu DEBUG"),
                ShowInHelp = false
            });

            CommandManager.AddHandler("/xlplugins", new CommandInfo(OnOpenInstallerCommand) {
                HelpMessage = Loc.Localize("DalamudInstallerHelp", "Open the plugin installer")
            });

            CommandManager.AddHandler("/xlcredits", new CommandInfo(OnOpenCreditsCommand) {
                HelpMessage = Loc.Localize("DalamudCreditsHelp", "Opens the credits for dalamud.")
            });

            CommandManager.AddHandler("/xllanguage", new CommandInfo(OnSetLanguageCommand) {
                HelpMessage = Loc.Localize("DalamudLanguageHelp", "Set the language for the in-game addon and plugins that support it. Available languages: ") + Localization.ApplicableLangCodes.Aggregate("en", (current, code) => current + ", " + code)
            });

            CommandManager.AddHandler("/xlsettings", new CommandInfo(OnOpenSettingsCommand) {
                HelpMessage = Loc.Localize("DalamudSettingsHelp", "Change various In-Game-Addon settings like chat channels and the discord bot setup.")
            });

            CommandManager.AddHandler("/xlbugreport", new CommandInfo(OnBugReportCommand) {
                HelpMessage = Loc.Localize("DalamudBugReport", "Upload a log to be analyzed by our professional development team."),
                ShowInHelp = false
            });

            CommandManager.AddHandler("/imdebug", new CommandInfo(OnDebugImInfoCommand) {
                HelpMessage = "ImGui DEBUG",
                ShowInHelp = false
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

            if (string.IsNullOrEmpty(arguments)) {
                Framework.Gui.Chat.Print(Loc.Localize("DalamudMuteNoArgs", "Please provide a word to mute."));
                return;
            }

            this.Configuration.BadWords.Add(arguments);

            this.Configuration.Save();

            Framework.Gui.Chat.Print(string.Format(Loc.Localize("DalamudMuted", "Muted \"{0}\"."), arguments));
        }

        private void OnBadWordsListCommand(string command, string arguments) {
            if (this.Configuration.BadWords == null)
                this.Configuration.BadWords = new List<string>();

            if (this.Configuration.BadWords.Count == 0) {
                Framework.Gui.Chat.Print(Loc.Localize("DalamudNoneMuted", "No muted words or sentences."));
                return;
            }

            this.Configuration.Save();

            foreach (var word in this.Configuration.BadWords) Framework.Gui.Chat.Print($"\"{word}\"");
        }

        private void OnBadWordsRemoveCommand(string command, string arguments) {
            if (this.Configuration.BadWords == null)
                this.Configuration.BadWords = new List<string>();

            this.Configuration.BadWords.RemoveAll(x => x == arguments);

            this.Configuration.Save();

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

#if DEBUG
        private void OnDebugZoneDownInjectCommand(string command, string arguments) {
            var data = File.ReadAllBytes(arguments);

            Framework.Network.InjectZoneProtoPacket(data);
            Framework.Gui.Chat.Print($"{arguments} OK with {data.Length} bytes");
        }
#endif

        private void OnDebugDrawDevMenu(string command, string arguments) {
            this.isImguiDrawDevMenu = !this.isImguiDrawDevMenu;
        }

        private void OnDebugImInfoCommand(string command, string arguments) {
            var io = this.InterfaceManager.LastImGuiIoPtr;
            var info = $"WantCaptureKeyboard: {io.WantCaptureKeyboard}\n";
            info += $"WantCaptureMouse: {io.WantCaptureMouse}\n";
            info += $"WantSetMousePos: {io.WantSetMousePos}\n";
            info += $"WantTextInput: {io.WantTextInput}\n";
            info += $"WantSaveIniSettings: {io.WantSaveIniSettings}\n"; 
            info += $"BackendFlags: {(int) io.BackendFlags}\n";
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

        private void OnOpenInstallerCommand(string command, string arguments) {
            OpenPluginInstaller();
        }

        private void OnOpenCreditsCommand(string command, string arguments)
        {
            var logoGraphic =
                this.InterfaceManager.LoadImage(
                    Path.Combine(this.StartInfo.WorkingDirectory, "UIRes", "logo.png"));
            this.creditsWindow = new DalamudCreditsWindow(this, logoGraphic, this.Framework);
            this.isImguiDrawCreditsWindow = true;
        }

        private void OnSetLanguageCommand(string command, string arguments)
        {
            if (Localization.ApplicableLangCodes.Contains(arguments.ToLower())) {
                this.LocalizationManager.SetupWithLangCode(arguments.ToLower());
                this.Configuration.LanguageOverride = arguments.ToLower();

                this.Framework.Gui.Chat.Print(string.Format(Loc.Localize("DalamudLanguageSetTo", "Language set to {0}"), arguments));
            } else {
                this.LocalizationManager.SetupWithUiCulture();
                this.Configuration.LanguageOverride = null;

                this.Framework.Gui.Chat.Print(string.Format(Loc.Localize("DalamudLanguageSetTo", "Language set to {0}"), "default"));
            }

            this.Configuration.Save();
        }

        private void OnOpenSettingsCommand(string command, string arguments)
        {
            this.settingsWindow = new DalamudSettingsWindow(this);
            this.isImguiDrawSettingsWindow = true;
        }

        private void OnBugReportCommand(string command, string arguments) {
            Task.Run(() => {
                try {
                    using var file = new FileStream(Path.Combine(this.StartInfo.WorkingDirectory, "dalamud.txt"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var reader = new StreamReader(file, Encoding.UTF8);

                    using var client = new WebClient();
                    var response = client.UploadString("https://dalamud-bugbait.herokuapp.com/catch", reader.ReadToEnd());

                    this.Framework.Gui.Chat.PrintError(
                        "Your bug report was submitted. A certified technical support specialist will be with you shortly. Please tell them this number: " +
                        response);
                } catch (Exception ex) {
                    Log.Error(ex, "Bug report failed.");
                    this.Framework.Gui.Chat.PrintError("Could not submit bug report");
                }
            });
        }
    }
}
