using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Configuration.Internal;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.Gui.Internal;
using Dalamud.Game.Internal;
using Dalamud.Game.Network.Internal;
using Dalamud.Hooking.Internal;
using Dalamud.Interface.Internal;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Internal;
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
    internal sealed class Dalamud : IServiceType
    {
        #region Internals

        private readonly ManualResetEvent unloadSignal;
        private bool hasDisposedPlugins = false;

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="Dalamud"/> class.
        /// </summary>
        /// <param name="info">DalamudStartInfo instance.</param>
        /// <param name="loggingLevelSwitch">LoggingLevelSwitch to control Serilog level.</param>
        /// <param name="configuration">The Dalamud configuration.</param>
        /// <param name="mainThreadContinueEvent">Event used to signal the main thread to continue.</param>
        public Dalamud(DalamudStartInfo info, LoggingLevelSwitch loggingLevelSwitch, DalamudConfiguration configuration, IntPtr mainThreadContinueEvent)
        {
            this.LogLevelSwitch = loggingLevelSwitch;

            this.unloadSignal = new ManualResetEvent(false);
            this.unloadSignal.Reset();

            ServiceManager.InitializeProvidedServicesAndClientStructs(this, info, configuration);

            if (!configuration.IsResumeGameAfterPluginLoad)
            {
                NativeFunctions.SetEvent(mainThreadContinueEvent);
                try
                {
                    _ = ServiceManager.InitializeEarlyLoadableServices();
                }
                catch (Exception e)
                {
                    Log.Error(e, "Service initialization failure");
                }
            }
            else
            {
                Task.Run(async () =>
                {
                    try
                    {
                        var tasks = new[]
                        {
                            ServiceManager.InitializeEarlyLoadableServices(),
                            ServiceManager.BlockingResolved,
                        };

                        await Task.WhenAny(tasks);
                        var faultedTasks = tasks.Where(x => x.IsFaulted).Select(x => (Exception)x.Exception!).ToArray();
                        if (faultedTasks.Any())
                            throw new AggregateException(faultedTasks);

                        NativeFunctions.SetEvent(mainThreadContinueEvent);

                        await Task.WhenAll(tasks);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, "Service initialization failure");
                    }
                    finally
                    {
                        NativeFunctions.SetEvent(mainThreadContinueEvent);
                    }
                });
            }
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
    }
}
