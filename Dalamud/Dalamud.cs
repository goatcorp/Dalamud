using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

using Dalamud.Configuration.Internal;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game.Gui.Internal;
using Dalamud.Game.Internal;
using Dalamud.Game.Network;
using Dalamud.Game.Network.Internal;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking.Internal;
using Dalamud.Interface.Internal;
using Dalamud.IoC.Internal;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Internal;
using Dalamud.Plugin.Ipc.Internal;
using Dalamud.Support;
using Dalamud.Utility;
using Serilog;
using Serilog.Core;
using Serilog.Events;

#if DEBUG
[assembly: InternalsVisibleTo("Dalamud.CorePlugin")]
#endif

[assembly: InternalsVisibleTo("Dalamud.Test")]
[assembly: InternalsVisibleTo("Dalamud.DevHelpers")]

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
        private MonoMod.RuntimeDetour.Hook processMonoHook;
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
            this.ApplyProcessPatch();

            Service<Dalamud>.Set(this);
            Service<DalamudStartInfo>.Set(info);
            Service<DalamudConfiguration>.Set(configuration);

            this.LogLevelSwitch = loggingLevelSwitch;

            this.unloadSignal = new ManualResetEvent(false);
            this.unloadSignal.Reset();

            this.finishUnloadSignal = finishSignal;
            this.finishUnloadSignal.Reset();
        }

        /// <summary>
        /// Gets LoggingLevelSwitch for Dalamud and Plugin logs.
        /// </summary>
        internal LoggingLevelSwitch LogLevelSwitch { get; private set; }

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
                SerilogEventSink.Instance.LogLine += SerilogOnLogLine;

                Service<ServiceContainer>.Set();

                // Initialize the process information.
                Service<SigScanner>.Set(new SigScanner(true));
                Service<HookManager>.Set();

                // Initialize FFXIVClientStructs function resolver
                FFXIVClientStructs.Resolver.Initialize();
                Log.Information("[T1] FFXIVClientStructs initialized!");

                // Initialize game subsystem
                var framework = Service<Framework>.Set();
                Log.Information("[T1] Framework OK!");

#if DEBUG
                Service<TaskTracker>.Set();
                Log.Information("[T1] TaskTracker OK!");
#endif
                Service<GameNetwork>.Set();
                Service<GameGui>.Set();

                framework.Enable();

                Log.Information("[T1] Load complete!");
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
        /// <returns>Whether or not the load succeeded.</returns>
        public bool LoadTier2()
        {
            try
            {
                var configuration = Service<DalamudConfiguration>.Get();

                var antiDebug = Service<AntiDebug>.Set();
                if (!antiDebug.IsEnabled)
                {
#if DEBUG
                    antiDebug.Enable();
#else
                    if (configuration.IsAntiAntiDebugEnabled)
                        antiDebug.Enable();
#endif
                }

                Log.Information("[T2] AntiDebug OK!");

                Service<WinSockHandlers>.Set();
                Log.Information("[T2] WinSock OK!");

                Service<NetworkHandlers>.Set();
                Log.Information("[T2] NH OK!");

                try
                {
                    Service<DataManager>.Set().Initialize(this.AssetDirectory.FullName);
                }
                catch (Exception e)
                {
                    Log.Error(e, "Could not initialize DataManager.");
                    this.Unload();
                    return false;
                }

                Log.Information("[T2] Data OK!");

                var clientState = Service<ClientState>.Set();
                Log.Information("[T2] CS OK!");

                var localization = Service<Localization>.Set(new Localization(Path.Combine(this.AssetDirectory.FullName, "UIRes", "loc", "dalamud"), "dalamud_"));
                if (!string.IsNullOrEmpty(configuration.LanguageOverride))
                {
                    localization.SetupWithLangCode(configuration.LanguageOverride);
                }
                else
                {
                    localization.SetupWithUiCulture();
                }

                Log.Information("[T2] LOC OK!");

                // This is enabled in ImGuiScene setup
                Service<DalamudIME>.Set();
                Log.Information("[T2] IME OK!");

                Service<InterfaceManager>.Set().Enable();
                Log.Information("[T2] IM OK!");

#pragma warning disable CS0618 // Type or member is obsolete
                Service<SeStringManager>.Set();
#pragma warning restore CS0618 // Type or member is obsolete

                Log.Information("[T2] SeString OK!");

                // Initialize managers. Basically handlers for the logic
                Service<CommandManager>.Set();

                Service<DalamudCommands>.Set().SetupCommands();

                Log.Information("[T2] CM OK!");

                Service<ChatHandlers>.Set();

                Log.Information("[T2] CH OK!");

                clientState.Enable();
                Log.Information("[T2] CS ENABLE!");

                Service<DalamudAtkTweaks>.Set().Enable();

                Log.Information("[T2] Load complete!");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Tier 2 load failed.");
                this.Unload();
                return false;
            }

            return true;
        }

        /// <summary>
        /// Runs tier 3 of the Dalamud initialization process.
        /// </summary>
        /// <returns>Whether or not the load succeeded.</returns>
        public bool LoadTier3()
        {
            try
            {
                Log.Information("[T3] START!");

                var pluginManager = Service<PluginManager>.Set();
                Service<CallGate>.Set();

                try
                {
                    _ = pluginManager.SetPluginReposFromConfigAsync(false);

                    pluginManager.OnInstalledPluginsChanged += Troubleshooting.LogTroubleshooting;

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

                Service<DalamudInterface>.Set();
                Log.Information("[T3] DUI OK!");

                Troubleshooting.LogTroubleshooting();

                Log.Information("Dalamud is ready.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Tier 3 load failed.");
                this.Unload();

                return false;
            }

            return true;
        }

        /// <summary>
        /// Queue an unload of Dalamud when it gets the chance.
        /// </summary>
        public void Unload()
        {
            Log.Information("Trigger unload");
            this.unloadSignal.Set();
        }

        /// <summary>
        /// Wait for an unload request to start.
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

            // this must be done before unloading interface manager, in order to do rebuild
            // the correct cascaded WndProc (IME -> RawDX11Scene -> Game). Otherwise the game
            // will not receive any windows messages
            Service<DalamudIME>.GetNullable()?.Dispose();

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

                Service<Framework>.GetNullable()?.ExplicitDispose();
                Service<ClientState>.GetNullable()?.ExplicitDispose();

                this.unloadSignal?.Dispose();

                Service<WinSockHandlers>.GetNullable()?.Dispose();
                Service<DataManager>.GetNullable()?.ExplicitDispose();
                Service<AntiDebug>.GetNullable()?.Dispose();
                Service<DalamudAtkTweaks>.GetNullable()?.Dispose();
                Service<HookManager>.GetNullable()?.Dispose();
                Service<SigScanner>.GetNullable()?.Dispose();

                SerilogEventSink.Instance.LogLine -= SerilogOnLogLine;

                this.processMonoHook?.Dispose();

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
            var releaseSig = "40 55 53 56 48 8D AC 24 ?? ?? ?? ?? B8 ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 2B E0 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 85 ?? ?? ?? ?? 48 83 3D ?? ?? ?? ?? ??";
            var releaseFilter = Service<SigScanner>.Get().ScanText(releaseSig);
            Log.Debug($"SE debug filter at {releaseFilter.ToInt64():X}");

            var oldFilter = NativeFunctions.SetUnhandledExceptionFilter(releaseFilter);
            Log.Debug("Reset ExceptionFilter, old: {0}", oldFilter);
        }

        private static void SerilogOnLogLine(object? sender, (string Line, LogEventLevel Level, DateTimeOffset TimeStamp, Exception? Exception) e)
        {
            if (e.Exception == null)
                return;

            Troubleshooting.LogException(e.Exception, e.Line);
        }

        /// <summary>
        /// Patch method for the class Process.Handle. This patch facilitates fixing Reloaded so that it
        /// uses pseudo-handles to access memory, to prevent permission errors.
        /// It should never be called manually.
        /// </summary>
        /// <param name="orig">A delegate that acts as the original method.</param>
        /// <param name="self">The equivalent of `this`.</param>
        /// <returns>A pseudo-handle for the current process, or the result from the original method.</returns>
        private static IntPtr ProcessHandlePatch(Func<Process, IntPtr> orig, Process self)
        {
            var result = orig(self);

            if (self.Id == Environment.ProcessId)
            {
                result = (IntPtr)0xFFFFFFFF;
            }

            // Log.Verbose($"Process.Handle // {self.ProcessName} // {result:X}");
            return result;
        }

        private void ApplyProcessPatch()
        {
            var targetType = typeof(Process);

            var handleTarget = targetType.GetProperty(nameof(Process.Handle)).GetGetMethod();
            var handlePatch = typeof(Dalamud).GetMethod(nameof(Dalamud.ProcessHandlePatch), BindingFlags.NonPublic | BindingFlags.Static);
            this.processMonoHook = new MonoMod.RuntimeDetour.Hook(handleTarget, handlePatch);
        }
    }
}
