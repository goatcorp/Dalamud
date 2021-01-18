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
        internal readonly Framework Framework;

        /// <summary>
        /// Anti-Debug detection prevention system
        /// </summary>
        internal readonly AntiDebug AntiDebug;

        /// <summary>
        /// WinSock optimization subsystem
        /// </summary>
        internal readonly WinSockHandlers WinSock2;

        /// <summary>
        /// ImGui Interface subsystem
        /// </summary>
        internal readonly InterfaceManager InterfaceManager;

        /// <summary>
        /// ClientState subsystem
        /// </summary>
        public readonly ClientState ClientState;

        #endregion

        #region Dalamud Subsystems

        /// <summary>
        /// Plugin Manager subsystem
        /// </summary>
        internal readonly PluginManager PluginManager;

        /// <summary>
        /// Plugin Repository subsystem
        /// </summary>
        internal readonly PluginRepository PluginRepository;

        /// <summary>
        /// Data provider subsystem
        /// </summary>
        internal readonly DataManager Data;

        /// <summary>
        /// Command Manager subsystem
        /// </summary>
        internal readonly CommandManager CommandManager;

        /// <summary>
        /// Localization subsystem facilitating localization for Dalamud and plugins
        /// </summary>
        internal readonly Localization LocalizationManager;

        #endregion

        #region Helpers

        /// <summary>
        /// SeStringManager subsystem facilitating string parsing
        /// </summary>
        internal readonly SeStringManager SeStringManager;

        /// <summary>
        /// Copy-enabled SigScanner for target module
        /// </summary>
        internal readonly SigScanner SigScanner;

        /// <summary>
        /// LoggingLevelSwitch for Dalamud and Plugin logs
        /// </summary>
        internal readonly LoggingLevelSwitch LogLevelSwitch;

        /// <summary>
        /// StartInfo object passed from injector
        /// </summary>
        internal readonly DalamudStartInfo StartInfo;

        /// <summary>
        /// Configuration object facilitating save and load of Dalamud configuration
        /// </summary>
        internal readonly DalamudConfiguration Configuration;

        #endregion

        #region Dalamud Core functionality

        /// <summary>
        /// Dalamud base UI
        /// </summary>
        internal readonly DalamudInterface DalamudUi;

        /// <summary>
        /// Dalamud chat commands
        /// </summary>
        internal readonly DalamudCommands DalamudCommands;

        /// <summary>
        /// Dalamud chat-based features
        /// </summary>
        internal readonly ChatHandlers ChatHandlers;

        /// <summary>
        /// Dalamud network-based features
        /// </summary>
        internal readonly NetworkHandlers NetworkHandlers;

        #endregion

        #region Internals

        private readonly ManualResetEvent unloadSignal;

        private readonly ManualResetEvent finishUnloadSignal;

        private readonly string baseDirectory;

        #endregion

        /// <summary>
        /// Injected process module
        /// </summary>
        internal readonly ProcessModule TargetModule;

        /// <summary>
        /// Value indicating if Dalamud was successfully loaded
        /// </summary>
        internal bool IsReady { get; private set; }

        /// <summary>
        /// Location of stored assets
        /// </summary>
        internal DirectoryInfo AssetDirectory => new DirectoryInfo(this.StartInfo.AssetDirectory);

        public Dalamud(DalamudStartInfo info, LoggingLevelSwitch loggingLevelSwitch, ManualResetEvent finishSignal) {
            this.StartInfo = info;
            this.LogLevelSwitch = loggingLevelSwitch;

            this.baseDirectory = info.WorkingDirectory;

            this.unloadSignal = new ManualResetEvent(false);
            this.finishUnloadSignal = finishSignal;

            this.Configuration = DalamudConfiguration.Load(info.ConfigurationPath);

            // Initialize the process information.
            this.TargetModule = Process.GetCurrentProcess().MainModule;
            this.SigScanner = new SigScanner(this.TargetModule, true);

            this.AntiDebug = new AntiDebug(this.SigScanner);

            // Initialize game subsystem
            this.Framework = new Framework(this.SigScanner, this);

            this.WinSock2 = new WinSockHandlers();

            this.NetworkHandlers = new NetworkHandlers(this, info.OptOutMbCollection);

            this.ClientState = new ClientState(this, info, this.SigScanner);

            this.LocalizationManager = new Localization(AssetDirectory.FullName);
            if (!string.IsNullOrEmpty(this.Configuration.LanguageOverride))
                this.LocalizationManager.SetupWithLangCode(this.Configuration.LanguageOverride);
            else
                this.LocalizationManager.SetupWithUiCulture();

            this.PluginRepository = new PluginRepository(this, this.StartInfo.PluginDirectory, this.StartInfo.GameVersion);

            this.DalamudUi = new DalamudInterface(this);

            var isInterfaceLoaded = false;
            if (!bool.Parse(Environment.GetEnvironmentVariable("DALAMUD_NOT_HAVE_INTERFACE") ?? "false"))
            {
                try
                {
                    this.InterfaceManager = new InterfaceManager(this, this.SigScanner);
                    this.InterfaceManager.OnDraw += this.DalamudUi.Draw;

                    this.InterfaceManager.Enable();
                    isInterfaceLoaded = true;
                }
                catch (Exception e)
                {
                    Log.Information(e, "Could not init interface.");
                }
            }

            this.Data = new DataManager(this.StartInfo.Language);
            try
            {
                this.Data.Initialize(AssetDirectory.FullName);
            }
            catch (Exception e)
            {
                Log.Error(e, "Could not initialize DataManager.");
                Unload();
                return;
            }

            this.SeStringManager = new SeStringManager(this.Data);

#if DEBUG
            this.AntiDebug = new AntiDebug(this.SigScanner);
#endif

            // Initialize managers. Basically handlers for the logic
            this.CommandManager = new CommandManager(this, info.Language);
            this.DalamudCommands = new DalamudCommands(this);
            this.DalamudCommands.SetupCommands();

            this.ChatHandlers = new ChatHandlers(this);

            if (!bool.Parse(Environment.GetEnvironmentVariable("DALAMUD_NOT_HAVE_PLUGINS") ?? "false"))
            {
                try
                {
                    this.PluginRepository.CleanupPlugins();

                    this.PluginManager =
                        new PluginManager(this, this.StartInfo.PluginDirectory, this.StartInfo.DefaultPluginDirectory);
                    this.PluginManager.LoadPlugins();
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
        }

        public void Start() {
#if DEBUG
            AntiDebug.Enable();
            //ReplaceExceptionHandler();
#endif
        }

        public void Unload() {
            this.unloadSignal.Set();
        }

        public void WaitForUnload() {
            this.unloadSignal.WaitOne();
        }

        public void WaitForUnloadFinish() {
            this.finishUnloadSignal.WaitOne();
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

            Log.Debug("Dalamud::Dispose OK!");
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
