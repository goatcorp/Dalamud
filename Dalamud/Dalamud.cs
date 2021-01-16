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
        private DalamudCommands DalamudCommands { get; set; }

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

        public DirectoryInfo AssetDirectory => new DirectoryInfo(this.StartInfo.AssetDirectory);

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

            this.LocalizationManager = new Localization(AssetDirectory.FullName);
            if (!string.IsNullOrEmpty(this.Configuration.LanguageOverride))
                this.LocalizationManager.SetupWithLangCode(this.Configuration.LanguageOverride);
            else
                this.LocalizationManager.SetupWithUiCulture();

            PluginRepository = new PluginRepository(this, this.StartInfo.PluginDirectory, this.StartInfo.GameVersion);

            DalamudUi = new DalamudInterface(this);

            var isInterfaceLoaded = false;
            if (!bool.Parse(Environment.GetEnvironmentVariable("DALAMUD_NOT_HAVE_INTERFACE") ?? "false"))
            {
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
            try
            {
                Data.Initialize(AssetDirectory.FullName);
            }
            catch (Exception e)
            {
                Log.Error(e, "Could not initialize DataManager.");
                Unload();
                return;
            }

            SeStringManager = new SeStringManager(Data);

#if DEBUG
            AntiDebug = new AntiDebug(this.SigScanner);
#endif

            // Initialize managers. Basically handlers for the logic
            CommandManager = new CommandManager(this, info.Language);
            DalamudCommands = new DalamudCommands(this);
            DalamudCommands.SetupCommands();

            ChatHandlers = new ChatHandlers(this);

            if (!bool.Parse(Environment.GetEnvironmentVariable("DALAMUD_NOT_HAVE_PLUGINS") ?? "false"))
            {
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
    }
}
