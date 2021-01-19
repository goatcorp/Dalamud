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

        #region Native Game Subsystems

        /// <summary>
        /// Game framework subsystem
        /// </summary>
        internal Framework Framework { get; private set; }

        /// <summary>
        /// Anti-Debug detection prevention system
        /// </summary>
        internal AntiDebug AntiDebug { get; private set; }

        /// <summary>
        /// WinSock optimization subsystem
        /// </summary>
        internal WinSockHandlers WinSock2 { get; private set; }

        /// <summary>
        /// ImGui Interface subsystem
        /// </summary>
        internal InterfaceManager InterfaceManager { get; private set; }

        /// <summary>
        /// ClientState subsystem
        /// </summary>
        public ClientState ClientState { get; private set; }

        #endregion

        #region Dalamud Subsystems

        /// <summary>
        /// Plugin Manager subsystem
        /// </summary>
        internal PluginManager PluginManager { get; private set; }

        /// <summary>
        /// Plugin Repository subsystem
        /// </summary>
        internal PluginRepository PluginRepository { get; private set; }

        /// <summary>
        /// Data provider subsystem
        /// </summary>
        internal DataManager Data { get; private set; }

        /// <summary>
        /// Command Manager subsystem
        /// </summary>
        internal CommandManager CommandManager { get; private set; }

        /// <summary>
        /// Localization subsystem facilitating localization for Dalamud and plugins
        /// </summary>
        internal Localization LocalizationManager { get; private set; }

        #endregion

        #region Helpers

        /// <summary>
        /// SeStringManager subsystem facilitating string parsing
        /// </summary>
        internal SeStringManager SeStringManager { get; private set; }

        /// <summary>
        /// Copy-enabled SigScanner for target module
        /// </summary>
        internal SigScanner SigScanner { get; private set; }

        /// <summary>
        /// LoggingLevelSwitch for Dalamud and Plugin logs
        /// </summary>
        internal LoggingLevelSwitch LogLevelSwitch { get; private set; }

        /// <summary>
        /// StartInfo object passed from injector
        /// </summary>
        internal DalamudStartInfo StartInfo { get; private set; }

        /// <summary>
        /// Configuration object facilitating save and load of Dalamud configuration
        /// </summary>
        internal DalamudConfiguration Configuration { get; private set; }

        #endregion

        #region Dalamud Core functionality

        /// <summary>
        /// Dalamud base UI
        /// </summary>
        internal DalamudInterface DalamudUi { get; private set; }

        /// <summary>
        /// Dalamud chat commands
        /// </summary>
        internal DalamudCommands DalamudCommands { get; private set; }

        /// <summary>
        /// Dalamud chat-based features
        /// </summary>
        internal ChatHandlers ChatHandlers { get; private set; }

        /// <summary>
        /// Dalamud network-based features
        /// </summary>
        internal NetworkHandlers NetworkHandlers { get; private set; }

        #endregion

        #region Internals

        private readonly ManualResetEvent unloadSignal;

        private readonly ManualResetEvent finishUnloadSignal;

        private readonly string baseDirectory;

        #endregion

        /// <summary>
        /// Injected process module
        /// </summary>
        internal ProcessModule TargetModule { get; private set; }

        /// <summary>
        /// Value indicating if Dalamud was successfully loaded
        /// </summary>
        internal bool IsReady { get; private set; }

        /// <summary>
        /// Location of stored assets
        /// </summary>
        internal DirectoryInfo AssetDirectory => new DirectoryInfo(this.StartInfo.AssetDirectory);

        public Dalamud(DalamudStartInfo info, LoggingLevelSwitch loggingLevelSwitch, ManualResetEvent finishSignal) {
            StartInfo = info;
            LogLevelSwitch = loggingLevelSwitch;

            this.baseDirectory = info.WorkingDirectory;

            this.unloadSignal = new ManualResetEvent(false);
            this.unloadSignal.Reset();

            this.finishUnloadSignal = finishSignal;
            this.unloadSignal.Reset();
        }

        public void Start() {
            Configuration = DalamudConfiguration.Load(StartInfo.ConfigurationPath);

            // Initialize the process information.
            TargetModule = Process.GetCurrentProcess().MainModule;
            SigScanner = new SigScanner(TargetModule, true);

            AntiDebug = new AntiDebug(SigScanner);
#if DEBUG
            AntiDebug.Enable();
#endif

            // Initialize game subsystem
            Framework = new Framework(SigScanner, this);

            WinSock2 = new WinSockHandlers();

            NetworkHandlers = new NetworkHandlers(this, StartInfo.OptOutMbCollection);

            ClientState = new ClientState(this, StartInfo, SigScanner);

            LocalizationManager = new Localization(AssetDirectory.FullName);
            if (!string.IsNullOrEmpty(Configuration.LanguageOverride))
                LocalizationManager.SetupWithLangCode(Configuration.LanguageOverride);
            else
                LocalizationManager.SetupWithUiCulture();

            PluginRepository = new PluginRepository(this, StartInfo.PluginDirectory, StartInfo.GameVersion);

            DalamudUi = new DalamudInterface(this);

            var isInterfaceLoaded = false;
            if (!bool.Parse(Environment.GetEnvironmentVariable("DALAMUD_NOT_HAVE_INTERFACE") ?? "false")) {
                try {
                    InterfaceManager = new InterfaceManager(this, SigScanner);
                    InterfaceManager.OnDraw += DalamudUi.Draw;

                    InterfaceManager.Enable();
                    isInterfaceLoaded = true;
                } catch (Exception e) {
                    Log.Information(e, "Could not init interface.");
                }
            }

            Data = new DataManager(StartInfo.Language);
            try {
                Data.Initialize(AssetDirectory.FullName);
            } catch (Exception e) {
                Log.Error(e, "Could not initialize DataManager.");
                Unload();
                return;
            }

            SeStringManager = new SeStringManager(Data);

            // Initialize managers. Basically handlers for the logic
            CommandManager = new CommandManager(this, StartInfo.Language);
            DalamudCommands = new DalamudCommands(this);
            DalamudCommands.SetupCommands();

            ChatHandlers = new ChatHandlers(this);

            if (!bool.Parse(Environment.GetEnvironmentVariable("DALAMUD_NOT_HAVE_PLUGINS") ?? "false"))
            {
                try
                {
                    PluginRepository.CleanupPlugins();

                    PluginManager =
                        new PluginManager(this, StartInfo.PluginDirectory, StartInfo.DefaultPluginDirectory);
                    PluginManager.LoadPlugins();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Plugin load failed.");
                }
            }

            Framework.Enable();
            ClientState.Enable();

            IsReady = true;

            Troubleshooting.LogTroubleshooting(this, isInterfaceLoaded);

            Log.Information("Dalamud is ready.");
        }

        public void Unload() {
            Log.Information("Trigger unload");
            this.unloadSignal.Set();
        }

        public void WaitForUnload() {
            this.unloadSignal.WaitOne();
        }

        public void WaitForUnloadFinish() {
            this.finishUnloadSignal.WaitOne();
        }

        public void Dispose() {
            try {
                // this must be done before unloading plugins, or it can cause a race condition
                // due to rendering happening on another thread, where a plugin might receive
                // a render call after it has been disposed, which can crash if it attempts to
                // use any resources that it freed in its own Dispose method
                InterfaceManager?.Dispose();

                try {
                    PluginManager.UnloadPlugins();
                } catch (Exception ex) {
                    Log.Error(ex, "Plugin unload failed.");
                }

                Framework.Dispose();
                ClientState.Dispose();

                this.unloadSignal.Dispose();

                WinSock2.Dispose();

                SigScanner.Dispose();

                Data.Dispose();

                AntiDebug.Dispose();

                Log.Debug("Dalamud::Dispose OK!");
            } catch (Exception ex) {
                Log.Error(ex, "skjdgjjkodsfg");
            }
        }

        internal void ReplaceExceptionHandler() {
            var semd = this.SigScanner.ScanText(
                "40 55 53 56 48 8D AC 24 ?? ?? ?? ?? B8 ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 2B E0 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 85 ?? ?? ?? ?? 48 83 3D ?? ?? ?? ?? ??");
            Log.Debug($"SE debug filter at {semd.ToInt64():X}");

            var oldFilter = NativeFunctions.SetUnhandledExceptionFilter(semd);
            Log.Debug("Reset ExceptionFilter, old: {0}", oldFilter);
        }
    }
}
