using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

using Dalamud.Configuration.Internal;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.Command;
using Dalamud.Game.Internal;
using Dalamud.Game.Network.Internal;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking.Internal;
using Dalamud.Interface.Internal;
using Dalamud.IoC;
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

        private readonly DalamudStartInfo startInfo;

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
            Service<Dalamud>.Set(this);

            this.startInfo = Service<DalamudStartInfo>.Set(info);
            this.LogLevelSwitch = loggingLevelSwitch;
            this.Configuration = configuration;

            // this.baseDirectory = info.WorkingDirectory;

            this.unloadSignal = new ManualResetEvent(false);
            this.unloadSignal.Reset();

            this.finishUnloadSignal = finishSignal;
            this.finishUnloadSignal.Reset();
        }

        #region Dalamud Subsystems

        /// <summary>
        /// Gets Localization subsystem facilitating localization for Dalamud and plugins.
        /// </summary>
        internal Localization LocalizationManager { get; private set; }

        #endregion

        #region Helpers

        /// <summary>
        /// Gets LoggingLevelSwitch for Dalamud and Plugin logs.
        /// </summary>
        internal LoggingLevelSwitch LogLevelSwitch { get; private set; }

        /// <summary>
        /// Gets Configuration object facilitating save and load of Dalamud configuration.
        /// </summary>
        internal DalamudConfiguration Configuration { get; private set; }

        #endregion

        #region Dalamud Core functionality

        /// <summary>
        /// Gets Dalamud chat commands.
        /// </summary>
        private DalamudCommands DalamudCommands { get; set; }

        /// <summary>
        /// Gets Dalamud chat-based features.
        /// </summary>
        internal ChatHandlers ChatHandlers { get; private set; }

        /// <summary>
        /// Gets subsystem responsible for adding the Dalamud menu items to the game's system menu.
        /// </summary>
        private DalamudSystemMenu SystemMenu { get; set; }

        #endregion

        /// <summary>
        /// Gets a value indicating whether Dalamud was successfully loaded.
        /// </summary>
        internal bool IsReady { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the plugin system is loaded.
        /// </summary>
        internal bool IsLoadedPluginSystem => Service<PluginManager>.GetNullable() != null;

        /// <summary>
        /// Gets location of stored assets.
        /// </summary>
        internal DirectoryInfo AssetDirectory => new(Service<DalamudStartInfo>.Get().AssetDirectory);

        /// <summary>
        /// Runs tier 1 of the Dalamud initialization process.
        /// </summary>
        public void LoadTier1()
        {
            try
            {
                // Initialize the process information.
                Service<SigScanner>.Set(new SigScanner(true));
                Service<HookManager>.Set();

                // Initialize game subsystem
                var framework = Service<Framework>.Set();
                Log.Information("[T1] Framework OK!");

                framework.Enable();
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
                var antiDebug = Service<AntiDebug>.Set();
                if (this.Configuration.IsAntiAntiDebugEnabled)
                    antiDebug.Enable();
#if DEBUG
                if (!antiDebug.IsEnabled)
                    antiDebug.Enable();
#endif
                Log.Information("[T2] AntiDebug OK!");

                Service<WinSockHandlers>.Set();
                Log.Information("[T2] WinSock OK!");

                Service<NetworkHandlers>.Set();
                Log.Information("[T2] NH OK!");

                Service<ClientState>.Set();
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
                        var interfaceManager = Service<InterfaceManager>.Set();

                        interfaceManager.Enable();

                        Log.Information("[T2] IM OK!");
                    }
                    catch (Exception e)
                    {
                        Log.Information(e, "Could not init interface.");
                    }
                }

                var data = Service<DataManager>.Set();
                try
                {
                    data.Initialize(this.AssetDirectory.FullName);
                }
                catch (Exception e)
                {
                    Log.Error(e, "Could not initialize DataManager.");
                    this.Unload();
                    return;
                }

                Log.Information("[T2] Data OK!");

                Service<SeStringManager>.Set();
                MemoryHelper.Initialize();  // For SeString handling

                Log.Information("[T2] SeString OK!");

                // Initialize managers. Basically handlers for the logic
                Service<CommandManager>.Set();

                this.DalamudCommands = new DalamudCommands();
                this.DalamudCommands.SetupCommands();

                Log.Information("[T2] CM OK!");

                this.ChatHandlers = new ChatHandlers();

                Log.Information("[T2] CH OK!");

                Service<ClientState>.Get().Enable();
                Log.Information("[T2] CS ENABLE!");

                this.SystemMenu = new DalamudSystemMenu();
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
                        var pluginManager = Service<PluginManager>.Set();
                        pluginManager.OnInstalledPluginsChanged += () =>
                            Troubleshooting.LogTroubleshooting();

                        Log.Information("[T3] PM OK!");

                        pluginManager.CleanupPlugins();
                        Log.Information("[T3] PMC OK!");

                        pluginManager.LoadAllPlugins();
                        Log.Information("[T3] PML OK!");
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Plugin load failed.");
                    }
                }

                Service<DalamudInterface>.Set();
                Log.Information("[T3] DUI OK!");

                Troubleshooting.LogTroubleshooting();

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
            Service<InterfaceManager>.GetNullable()?.Dispose();

            Service<DalamudInterface>.GetNullable()?.Dispose();

            Service<PluginManager>.GetNullable()?.Dispose();
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

                Service<Framework>.GetNullable()?.Dispose();
                Service<ClientState>.GetNullable()?.Dispose();

                this.unloadSignal?.Dispose();

                Service<WinSockHandlers>.GetNullable()?.Dispose();
                Service<DataManager>.GetNullable()?.Dispose();
                Service<AntiDebug>.GetNullable()?.Dispose();
                Service<DalamudSystemMenu>.GetNullable()?.Dispose();
                Service<HookManager>.GetNullable()?.Dispose();
                Service<SigScanner>.GetNullable()?.Dispose();

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
            var releaseFilter = Service<SigScanner>.Get().ScanText("40 55 53 56 48 8D AC 24 ?? ?? ?? ?? B8 ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 2B E0 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 85 ?? ?? ?? ?? 48 83 3D ?? ?? ?? ?? ??");
            Log.Debug($"SE debug filter at {releaseFilter.ToInt64():X}");

            var oldFilter = NativeFunctions.SetUnhandledExceptionFilter(releaseFilter);
            Log.Debug("Reset ExceptionFilter, old: {0}", oldFilter);
        }
    }
}
