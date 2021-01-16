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

        public AntiDebug AntiDebug { get; set; }

        internal PluginManager PluginManager { get; private set; }
        internal PluginRepository PluginRepository { get; private set; }

        public readonly ClientState ClientState;

        public readonly DalamudStartInfo StartInfo;
        internal LoggingLevelSwitch LogLevelSwitch { get; }

        internal readonly DalamudConfiguration Configuration;

        private readonly WinSockHandlers WinSock2;

        internal InterfaceManager InterfaceManager { get; private set; }

        internal DalamudInterface DalamudUi { get; private set; }

        public DataManager Data { get; private set; }

        internal SeStringManager SeStringManager { get; private set; }


        internal Localization LocalizationManager;

        public bool IsReady { get; private set; }

        public Dalamud(DalamudStartInfo info, LoggingLevelSwitch loggingLevelSwitch) {
            this.StartInfo = info;
            this.LogLevelSwitch = loggingLevelSwitch;

            this.Configuration = DalamudConfiguration.Load(info.ConfigurationPath);

            this.baseDirectory = info.WorkingDirectory;

            this.unloadSignal = new ManualResetEvent(false);

            // Initialize the process information.
            this.targetModule = Process.GetCurrentProcess().MainModule;
            this.SigScanner = new SigScanner(this.targetModule, true);

            // Initialize game subsystem
            this.Framework = new Framework(this.SigScanner, this);

            this.WinSock2 = new WinSockHandlers();

            NetworkHandlers = new NetworkHandlers(this, info.OptOutMbCollection);

            this.ClientState = new ClientState(this, info, this.SigScanner);

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

                PluginRepository = new PluginRepository(this, this.StartInfo.PluginDirectory, this.StartInfo.GameVersion);

                DalamudUi = new DalamudInterface(this);

                var isInterfaceLoaded = false;
                if (!bool.Parse(Environment.GetEnvironmentVariable("DALAMUD_NOT_HAVE_INTERFACE") ?? "false")) {
                    try
                    {
                        InterfaceManager = new InterfaceManager(this, this.SigScanner);
                        InterfaceManager.OnDraw += DalamudUi.Draw;

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

#if DEBUG
                AntiDebug = new AntiDebug(this.SigScanner);
                AntiDebug.Enable();
#endif

                // Initialize managers. Basically handlers for the logic
                CommandManager = new CommandManager(this, info.Language);
                SetupCommands();

                ChatHandlers = new ChatHandlers(this);

                if (!bool.Parse(Environment.GetEnvironmentVariable("DALAMUD_NOT_HAVE_PLUGINS") ?? "false")) {
                    try
                    {
                        PluginRepository.CleanupPlugins();

                        PluginManager = new PluginManager(this, this.StartInfo.PluginDirectory, this.StartInfo.DefaultPluginDirectory);
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

            this.unloadSignal.Dispose();

            this.WinSock2.Dispose();

            this.SigScanner.Dispose();
            
            this.Data.Dispose();

            this.AntiDebug?.Dispose();
        }

        internal void ReplaceExceptionHandler() {
            var semd = this.SigScanner.ScanText(
                "40 55 53 56 48 8D AC 24 ?? ?? ?? ?? B8 ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 2B E0 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 85 ?? ?? ?? ?? 48 83 3D ?? ?? ?? ?? ??");
            Log.Debug($"SE debug filter at {semd.ToInt64():X}");

            var oldFilter = NativeFunctions.SetUnhandledExceptionFilter(semd);
            Log.Debug("Reset ExceptionFilter, old: {0}", oldFilter);
        }

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

            CommandManager.AddHandler("/xllog", new CommandInfo(OnOpenLog) {
                HelpMessage = Loc.Localize("DalamudDevLogHelp", "Open dev log DEBUG"),
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
                PluginManager.ReloadPlugins();

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
            this.DalamudUi.IsDevMenu = !this.DalamudUi.IsDevMenu;
        }

        private void OnOpenLog(string command, string arguments) {
            this.DalamudUi.OpenLog();
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

        private void OnOpenInstallerCommand(string command, string arguments)
        {
            this.DalamudUi.OpenPluginInstaller();
        }

        private void OnOpenCreditsCommand(string command, string arguments) {
            DalamudUi.OpenCredits();
        }

        private void OnSetLanguageCommand(string command, string arguments)
        {
            if (Localization.ApplicableLangCodes.Contains(arguments.ToLower()) || arguments.ToLower() == "en") {
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
            this.DalamudUi.OpenSettings();
        }
    }
}
