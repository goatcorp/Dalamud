using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

using Dalamud.Configuration.Internal;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.Addon;
using Dalamud.Game.ClientState;
using Dalamud.Game.Command;
using Dalamud.Game.Internal;
using Dalamud.Game.Network;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking.Internal;
using Dalamud.Interface.Internal;
using Dalamud.Memory;
using Dalamud.Plugin.Internal;
using Serilog;
using Serilog.Core;

#if DEBUG
// This allows for rapid prototyping of Dalamud modules with access to internal objects.
[assembly: InternalsVisibleTo("Dalamud.CorePlugin")]
#endif

namespace Dalamud
{
    /// <summary>
    /// The main Dalamud class containing all subsystems.
    /// </summary>
    internal sealed class Dalamud : IDisposable
    {
        #region Internals

        private readonly ManualResetEvent unloadSignal;

        private readonly ManualResetEvent finishUnloadSignal;

        private bool hasDisposedPlugins = false;

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="Dalamud"/> class.
        /// </summary>
        /// <param name="info">DalamudStartInfo instance.</param>
        /// <param name="loggingLevelSwitch">LoggingLevelSwitch to control Serilog level.</param>
        /// <param name="finishSignal">Signal signalling shutdown.</param>
        /// <param name="configuration">The Dalamud configuration.</param>
        public Dalamud(DalamudStartInfo info, LoggingLevelSwitch loggingLevelSwitch, ManualResetEvent finishSignal, DalamudConfiguration configuration)
        {
#if DEBUG
            Instance = this;
#endif
            this.StartInfo = info;
            this.LogLevelSwitch = loggingLevelSwitch;
            this.Configuration = configuration;

            // this.baseDirectory = info.WorkingDirectory;

            this.unloadSignal = new ManualResetEvent(false);
            this.unloadSignal.Reset();

            this.finishUnloadSignal = finishSignal;
            this.finishUnloadSignal.Reset();
        }

#if DEBUG
        /// <summary>
        /// Gets the Dalamud singleton instance.
        /// </summary>
        internal static Dalamud Instance { get; private set; }
#endif

        #region Native Game Subsystems

        /// <summary>
        /// Gets game framework subsystem.
        /// </summary>
        internal Framework Framework { get; private set; }

        /// <summary>
        /// Gets Anti-Debug detection prevention subsystem.
        /// </summary>
        internal AntiDebug AntiDebug { get; private set; }

        /// <summary>
        /// Gets WinSock optimization subsystem.
        /// </summary>
        internal WinSockHandlers WinSock2 { get; private set; }

        /// <summary>
        /// Gets Hook management subsystem.
        /// </summary>
        internal HookManager HookManager { get; private set; }

        /// <summary>
        /// Gets ImGui Interface subsystem.
        /// </summary>
        internal InterfaceManager InterfaceManager { get; private set; }

        /// <summary>
        /// Gets ClientState subsystem.
        /// </summary>
        internal ClientState ClientState { get; private set; }

        #endregion

        #region Dalamud Subsystems

        /// <summary>
        /// Gets Plugin Manager subsystem.
        /// </summary>
        internal PluginManager PluginManager { get; private set; }

        /// <summary>
        /// Gets Data provider subsystem.
        /// </summary>
        internal DataManager Data { get; private set; }

        /// <summary>
        /// Gets Command Manager subsystem.
        /// </summary>
        internal CommandManager CommandManager { get; private set; }

        /// <summary>
        /// Gets Localization subsystem facilitating localization for Dalamud and plugins.
        /// </summary>
        internal Localization LocalizationManager { get; private set; }

        #endregion

        #region Helpers

        /// <summary>
        /// Gets SeStringManager subsystem facilitating string parsing.
        /// </summary>
        internal SeStringManager SeStringManager { get; private set; }

        /// <summary>
        /// Gets copy-enabled SigScanner for target module.
        /// </summary>
        internal SigScanner SigScanner { get; private set; }

        /// <summary>
        /// Gets LoggingLevelSwitch for Dalamud and Plugin logs.
        /// </summary>
        internal LoggingLevelSwitch LogLevelSwitch { get; private set; }

        /// <summary>
        /// Gets StartInfo object passed from injector.
        /// </summary>
        internal DalamudStartInfo StartInfo { get; private set; }

        /// <summary>
        /// Gets Configuration object facilitating save and load of Dalamud configuration.
        /// </summary>
        internal DalamudConfiguration Configuration { get; private set; }

        #endregion

        #region Dalamud Core functionality

        /// <summary>
        /// Gets Dalamud base UI.
        /// </summary>
        internal DalamudInterface DalamudUi { get; private set; }

        /// <summary>
        /// Gets Dalamud chat commands.
        /// </summary>
        internal DalamudCommands DalamudCommands { get; private set; }

        /// <summary>
        /// Gets Dalamud chat-based features.
        /// </summary>
        internal ChatHandlers ChatHandlers { get; private set; }

        /// <summary>
        /// Gets Dalamud network-based features.
        /// </summary>
        internal NetworkHandlers NetworkHandlers { get; private set; }

        /// <summary>
        /// Gets subsystem responsible for adding the Dalamud menu items to the game's system menu.
        /// </summary>
        internal DalamudSystemMenu SystemMenu { get; private set; }

        #endregion

        /// <summary>
        /// Gets Injected process module.
        /// </summary>
        internal ProcessModule TargetModule { get; private set; }

        /// <summary>
        /// Gets a value indicating whether Dalamud was successfully loaded.
        /// </summary>
        internal bool IsReady { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the plugin system is loaded.
        /// </summary>
        internal bool IsLoadedPluginSystem => this.PluginManager != null;

        /// <summary>
        /// Gets location of stored assets.
        /// </summary>
        internal DirectoryInfo AssetDirectory => new(this.StartInfo.AssetDirectory);

        /// <summary>
        /// Runs tier 1 of the Dalamud initialization process.
        /// </summary>
        public void LoadTier1()
        {
            try
            {
                // Initialize the process information.
                this.TargetModule = Process.GetCurrentProcess().MainModule;
                this.SigScanner = new SigScanner(this.TargetModule, true);
                this.HookManager = new HookManager(this);

                // Initialize game subsystem
                this.Framework = new Framework(this.SigScanner, this);

                Log.Information("[T1] Framework OK!");

                this.Framework.Enable();
                Log.Information("[T1] Framework ENABLE!");

                // Initialize FFXIVClientStructs function resolver
                FFXIVClientStructs.Resolver.Initialize();
                Log.Information("[T1] FFXIVClientStructs initialized!");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Tier 1 load failed.");
                this.Unload();
            }
        }

        /// <summary>
        /// Runs tier 2 of the Dalamud initialization process.
        /// </summary>
        public void LoadTier2()
        {
            try
            {
                this.AntiDebug = new AntiDebug(this.SigScanner);
                if (this.Configuration.IsAntiAntiDebugEnabled)
                    this.AntiDebug.Enable();
#if DEBUG
                if (!this.AntiDebug.IsEnabled)
                    this.AntiDebug.Enable();
#endif

                Log.Information("[T2] AntiDebug OK!");

                this.WinSock2 = new WinSockHandlers();

                Log.Information("[T2] WinSock OK!");

                this.NetworkHandlers = new NetworkHandlers(this, this.StartInfo.OptOutMbCollection);

                Log.Information("[T2] NH OK!");

                this.ClientState = new ClientState(this, this.StartInfo, this.SigScanner);

                Log.Information("[T2] CS OK!");

                this.LocalizationManager = new Localization(Path.Combine(this.AssetDirectory.FullName, "UIRes", "loc", "dalamud"), "dalamud_");
                if (!string.IsNullOrEmpty(this.Configuration.LanguageOverride))
                    this.LocalizationManager.SetupWithLangCode(this.Configuration.LanguageOverride);
                else
                    this.LocalizationManager.SetupWithUiCulture();

                Log.Information("[T2] LOC OK!");

                if (!bool.Parse(Environment.GetEnvironmentVariable("DALAMUD_NOT_HAVE_INTERFACE") ?? "false"))
                {
                    try
                    {
                        this.InterfaceManager = new InterfaceManager(this, this.SigScanner);

                        this.InterfaceManager.Enable();

                        Log.Information("[T2] IM OK!");
                    }
                    catch (Exception e)
                    {
                        Log.Information(e, "Could not init interface.");
                    }
                }

                this.Data = new DataManager(this.StartInfo.Language, this.InterfaceManager);
                try
                {
                    this.Data.Initialize(this.AssetDirectory.FullName);
                }
                catch (Exception e)
                {
                    Log.Error(e, "Could not initialize DataManager.");
                    this.Unload();
                    return;
                }

                Log.Information("[T2] Data OK!");

                this.SeStringManager = new SeStringManager(this.Data);
                MemoryHelper.Initialize(this);  // For SeString handling

                Log.Information("[T2] SeString OK!");

                // Initialize managers. Basically handlers for the logic
                this.CommandManager = new CommandManager(this, this.StartInfo.Language);
                this.DalamudCommands = new DalamudCommands(this);
                this.DalamudCommands.SetupCommands();

                Log.Information("[T2] CM OK!");

                this.ChatHandlers = new ChatHandlers(this);

                Log.Information("[T2] CH OK!");

                this.ClientState.Enable();
                Log.Information("[T2] CS ENABLE!");

                this.SystemMenu = new DalamudSystemMenu(this);
                this.SystemMenu.Enable();

                this.IsReady = true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Tier 2 load failed.");
                this.Unload();
            }
        }

        /// <summary>
        /// Runs tier 3 of the Dalamud initialization process.
        /// </summary>
        public void LoadTier3()
        {
            try
            {
                Log.Information("[T3] START!");

                if (!bool.Parse(Environment.GetEnvironmentVariable("DALAMUD_NOT_HAVE_PLUGINS") ?? "false"))
                {
                    try
                    {
                        this.PluginManager = new PluginManager(this);
                        this.PluginManager.OnInstalledPluginsChanged += () =>
                            Troubleshooting.LogTroubleshooting(this, this.InterfaceManager.IsReady);

                        Log.Information("[T3] PM OK!");

                        this.PluginManager.CleanupPlugins();
                        Log.Information("[T3] PMC OK!");

                        this.PluginManager.LoadAllPlugins();
                        Log.Information("[T3] PML OK!");
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Plugin load failed.");
                    }
                }

                this.DalamudUi = new DalamudInterface(this);
                Log.Information("[T3] DUI OK!");

                Troubleshooting.LogTroubleshooting(this, this.InterfaceManager.IsReady);

                Log.Information("Dalamud is ready.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Tier 3 load failed.");
                this.Unload();
            }
        }

        /// <summary>
        ///     Queue an unload of Dalamud when it gets the chance.
        /// </summary>
        public void Unload()
        {
            Log.Information("Trigger unload");
            this.unloadSignal.Set();
        }

        /// <summary>
        ///     Wait for an unload request to start.
        /// </summary>
        public void WaitForUnload()
        {
            this.unloadSignal.WaitOne();
        }

        /// <summary>
        /// Wait for a queued unload to be finalized.
        /// </summary>
        public void WaitForUnloadFinish()
        {
            this.finishUnloadSignal?.WaitOne();
        }

        /// <summary>
        /// Dispose subsystems related to plugin handling.
        /// </summary>
        public void DisposePlugins()
        {
            this.hasDisposedPlugins = true;

            // this must be done before unloading plugins, or it can cause a race condition
            // due to rendering happening on another thread, where a plugin might receive
            // a render call after it has been disposed, which can crash if it attempts to
            // use any resources that it freed in its own Dispose method
            this.InterfaceManager?.Dispose();

            this.DalamudUi?.Dispose();

            this.PluginManager?.Dispose();
        }

        /// <summary>
        /// Dispose Dalamud subsystems.
        /// </summary>
        public void Dispose()
        {
            try
            {
                if (!this.hasDisposedPlugins)
                {
                    this.DisposePlugins();
                    Thread.Sleep(100);
                }

                this.Framework?.Dispose();

                this.ClientState?.Dispose();

                this.unloadSignal?.Dispose();

                this.WinSock2?.Dispose();

                this.Data?.Dispose();

                this.AntiDebug?.Dispose();

                this.SystemMenu?.Dispose();

                this.HookManager?.Dispose();

                this.SigScanner?.Dispose();

                Log.Debug("Dalamud::Dispose() OK!");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Dalamud::Dispose() failed.");
            }
        }

        /// <summary>
        /// Replace the built-in exception handler with a debug one.
        /// </summary>
        internal void ReplaceExceptionHandler()
        {
            var releaseFilter = this.SigScanner.ScanText("40 55 53 56 48 8D AC 24 ?? ?? ?? ?? B8 ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 2B E0 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 85 ?? ?? ?? ?? 48 83 3D ?? ?? ?? ?? ??");
            Log.Debug($"SE debug filter at {releaseFilter.ToInt64():X}");

            var oldFilter = NativeFunctions.SetUnhandledExceptionFilter(releaseFilter);
            Log.Debug("Reset ExceptionFilter, old: {0}", oldFilter);
        }
    }
}
