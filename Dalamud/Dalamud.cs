using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Configuration.Internal;
using Dalamud.Game;
using Dalamud.Game.Gui.Internal;
using Dalamud.Interface.Internal;
using Dalamud.Plugin.Internal;
using Dalamud.Storage;
using Dalamud.Utility;
using PInvoke;
using Serilog;

#if DEBUG
[assembly: InternalsVisibleTo("Dalamud.CorePlugin")]
#endif

[assembly: InternalsVisibleTo("Dalamud.Test")]
[assembly: InternalsVisibleTo("Dalamud.DevHelpers")]

namespace Dalamud;

/// <summary>
/// The main Dalamud class containing all subsystems.
/// </summary>
[ServiceManager.Service]
internal sealed class Dalamud : IServiceType
{
    #region Internals

    private readonly ManualResetEvent unloadSignal;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="Dalamud"/> class.
    /// </summary>
    /// <param name="info">DalamudStartInfo instance.</param>
    /// <param name="fs">ReliableFileStorage instance.</param>
    /// <param name="configuration">The Dalamud configuration.</param>
    /// <param name="mainThreadContinueEvent">Event used to signal the main thread to continue.</param>
    public Dalamud(DalamudStartInfo info, ReliableFileStorage fs, DalamudConfiguration configuration, IntPtr mainThreadContinueEvent)
    {
        this.unloadSignal = new ManualResetEvent(false);
        this.unloadSignal.Reset();

        ServiceManager.InitializeProvidedServicesAndClientStructs(this, info, fs, configuration);

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
                    Util.Fatal("Dalamud could not initialize correctly. Please report this error. \n\nThe game will continue, but you may not be able to use plugins.", "Dalamud", false);
                }
                finally
                {
                    NativeFunctions.SetEvent(mainThreadContinueEvent);
                }
            });
        }
    }

    /// <summary>
    /// Gets location of stored assets.
    /// </summary>
    internal DirectoryInfo AssetDirectory => new(Service<DalamudStartInfo>.Get().AssetDirectory!);
    
    /// <summary>
    /// Signal to the crash handler process that we should restart the game.
    /// </summary>
    public static void RestartGame()
    {
        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern void RaiseException(uint dwExceptionCode, uint dwExceptionFlags, uint nNumberOfArguments, IntPtr lpArguments);

        RaiseException(0x12345678, 0, 0, IntPtr.Zero);
        Process.GetCurrentProcess().Kill();
    }

    /// <summary>
    /// Queue an unload of Dalamud when it gets the chance.
    /// </summary>
    public void Unload()
    {
        Log.Information("Trigger unload");

        var reportCrashesSetting = Service<DalamudConfiguration>.GetNullable()?.ReportShutdownCrashes ?? true;
        var pmHasDevPlugins = Service<PluginManager>.GetNullable()?.InstalledPlugins.Any(x => x.IsDev) ?? false;
        if (!reportCrashesSetting && !pmHasDevPlugins)
        {
            // Leaking on purpose for now
            var attribs = Kernel32.SECURITY_ATTRIBUTES.Create();
            Kernel32.CreateMutex(attribs, false, "DALAMUD_CRASHES_NO_MORE");
        }

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
        var releaseFilter = Service<TargetSigScanner>.Get().ScanText(releaseSig);
        Log.Debug($"SE debug filter at {releaseFilter.ToInt64():X}");

        var oldFilter = NativeFunctions.SetUnhandledExceptionFilter(releaseFilter);
        Log.Debug("Reset ExceptionFilter, old: {0}", oldFilter);
    }
}
