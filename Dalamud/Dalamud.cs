using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Common;
using Dalamud.Configuration.Internal;
using Dalamud.Game;
using Dalamud.Plugin.Internal;
using Dalamud.Storage;
using Dalamud.Utility;
using Dalamud.Utility.Timing;
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
[ServiceManager.ProvidedService]
internal sealed class Dalamud : IServiceType
{
    #region Internals

    private static int shownServiceError = 0;
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
        this.StartInfo = info;
        
        this.unloadSignal = new ManualResetEvent(false);
        this.unloadSignal.Reset();
        
        // Directory resolved signatures(CS, our own) will be cached in
        var cacheDir = new DirectoryInfo(Path.Combine(this.StartInfo.WorkingDirectory!, "cachedSigs"));
        if (!cacheDir.Exists)
            cacheDir.Create();
        
        // Set up the SigScanner for our target module
        TargetSigScanner scanner;
        using (Timings.Start("SigScanner Init"))
        {
            scanner = new TargetSigScanner(
                true, new FileInfo(Path.Combine(cacheDir.FullName, $"{this.StartInfo.GameVersion}.json")));
        }

        ServiceManager.InitializeProvidedServices(this, fs, configuration, scanner);
        
        // Set up FFXIVClientStructs
        this.SetupClientStructsResolver(cacheDir);
        
        void KickoffGameThread()
        {
            Log.Verbose("=============== GAME THREAD KICKOFF ===============");
            Timings.Event("Game thread kickoff");
            NativeFunctions.SetEvent(mainThreadContinueEvent);
        }

        void HandleServiceInitFailure(Task t)
        {
            Log.Error(t.Exception!, "Service initialization failure");
            
            if (Interlocked.CompareExchange(ref shownServiceError, 1, 0) != 0)
                return;

            Util.Fatal(
                "Dalamud failed to load all necessary services.\n\nThe game will continue, but you may not be able to use plugins.",
                "Dalamud", false);
        }

        ServiceManager.InitializeEarlyLoadableServices()
                      .ContinueWith(
                          t =>
                          {
                              if (t.IsCompletedSuccessfully)
                                  return;

                              HandleServiceInitFailure(t);
                          });

        ServiceManager.BlockingResolved.ContinueWith(
            t =>
            {
                if (t.IsCompletedSuccessfully)
                {
                    KickoffGameThread();
                    return;
                }

                HandleServiceInitFailure(t);
            });

        this.DefaultExceptionFilter = NativeFunctions.SetUnhandledExceptionFilter(nint.Zero);
        NativeFunctions.SetUnhandledExceptionFilter(this.DefaultExceptionFilter);
        Log.Debug($"SE default exception filter at {this.DefaultExceptionFilter.ToInt64():X}");

        var debugSig = "40 55 53 56 48 8D AC 24 ?? ?? ?? ?? B8 ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 2B E0 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 85 ?? ?? ?? ?? 48 83 3D ?? ?? ?? ?? ??";
        this.DebugExceptionFilter = Service<TargetSigScanner>.Get().ScanText(debugSig);
        Log.Debug($"SE debug exception filter at {this.DebugExceptionFilter.ToInt64():X}");
    }
    
    /// <summary>
    /// Gets the start information for this Dalamud instance.
    /// </summary>
    internal DalamudStartInfo StartInfo { get; private set; }

    /// <summary>
    /// Gets location of stored assets.
    /// </summary>
    internal DirectoryInfo AssetDirectory => new(this.StartInfo.AssetDirectory!);

    /// <summary>
    /// Gets the in-game default exception filter.
    /// </summary>
    private nint DefaultExceptionFilter { get; }

    /// <summary>
    /// Gets the in-game debug exception filter.
    /// </summary>
    private nint DebugExceptionFilter { get; }

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
    /// Replace the current exception handler with the default one.
    /// </summary>
    internal void UseDefaultExceptionHandler() => 
        this.SetExceptionHandler(this.DefaultExceptionFilter);

    /// <summary>
    /// Replace the current exception handler with a debug one.
    /// </summary>
    internal void UseDebugExceptionHandler() =>
        this.SetExceptionHandler(this.DebugExceptionFilter);

    /// <summary>
    /// Disable the current exception handler.
    /// </summary>
    internal void UseNoExceptionHandler() =>
        this.SetExceptionHandler(nint.Zero);

    /// <summary>
    /// Helper function to set the exception handler.
    /// </summary>
    private void SetExceptionHandler(nint newFilter)
    {
        var oldFilter = NativeFunctions.SetUnhandledExceptionFilter(newFilter);
        Log.Debug("Set ExceptionFilter to {0}, old: {1}", newFilter, oldFilter);
    }

    private void SetupClientStructsResolver(DirectoryInfo cacheDir)
    {
        using (Timings.Start("CS Resolver Init"))
        {
            FFXIVClientStructs.Interop.Resolver.GetInstance.SetupSearchSpace(Service<TargetSigScanner>.Get().SearchBase, new FileInfo(Path.Combine(cacheDir.FullName, $"{this.StartInfo.GameVersion}_cs.json")));
            FFXIVClientStructs.Interop.Resolver.GetInstance.Resolve();
        }
    }
}
