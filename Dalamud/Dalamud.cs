using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
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
using Dalamud.Interface;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.Internal;
using Dalamud.IoC.Internal;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Internal;
using Dalamud.Plugin.Ipc.Internal;
using Dalamud.Support;
using Dalamud.Utility;
using Dalamud.Utility.Timing;
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
    internal sealed class Dalamud : IDisposable, IProvidedServiceObject
    {
        #region Internals

        private readonly ManualResetEvent unloadSignal;
        private readonly ManualResetEvent finishUnloadSignal;
        private readonly IntPtr mainThreadContinueEvent;
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
        /// <param name="mainThreadContinueEvent">Event used to signal the main thread to continue.</param>
        public Dalamud(DalamudStartInfo info, LoggingLevelSwitch loggingLevelSwitch, ManualResetEvent finishSignal, DalamudConfiguration configuration, IntPtr mainThreadContinueEvent)
        {
            this.LogLevelSwitch = loggingLevelSwitch;

            this.unloadSignal = new ManualResetEvent(false);
            this.unloadSignal.Reset();

            this.finishUnloadSignal = finishSignal;
            this.finishUnloadSignal.Reset();

            SerilogEventSink.Instance.LogLine += SerilogOnLogLine;

            Service<Dalamud>.Provide(this);
            Service<DalamudStartInfo>.Provide(info);
            Service<DalamudConfiguration>.Provide(configuration);
            ServiceManager.InitializeEarlyLoadableServices();

            this.mainThreadContinueEvent = mainThreadContinueEvent;
        }

        /// <summary>
        /// Gets LoggingLevelSwitch for Dalamud and Plugin logs.
        /// </summary>
        internal LoggingLevelSwitch LogLevelSwitch { get; private set; }

        /// <summary>
        /// Gets location of stored assets.
        /// </summary>
        internal DirectoryInfo AssetDirectory => new(Service<DalamudStartInfo>.Get().AssetDirectory!);

        /// <summary>
        /// Runs tier 3 of the Dalamud initialization process.
        /// </summary>
        /// <returns>Whether or not the load succeeded.</returns>
        public bool LoadTier3()
        {
            using var tier3Timing = Timings.Start("Tier 3 Init");

            ThreadSafety.AssertMainThread();

            try
            {
                Log.Information("[T3] START!");

                PluginManager pluginManager;

                Troubleshooting.LogTroubleshooting();

                Log.Information("Dalamud is ready");
                Timings.Event("Dalamud ready");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Tier 3 load failed");
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

                var sigScanner = Service<SigScanner>.Get();
                sigScanner.Save();
                sigScanner.Dispose();

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
    }
}
