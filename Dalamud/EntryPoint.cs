using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Common;
using Dalamud.Configuration.Internal;
using Dalamud.Interface.Internal.Windows;
using Dalamud.Logging.Internal;
using Dalamud.Logging.Retention;
using Dalamud.Plugin.Internal;
using Dalamud.Storage;
using Dalamud.Support;
using Dalamud.Utility;
using Newtonsoft.Json;
using PInvoke;
using Serilog;
using Serilog.Core;
using Serilog.Events;

using static Dalamud.NativeFunctions;

namespace Dalamud;

/// <summary>
/// The main entrypoint for the Dalamud system.
/// </summary>
public sealed class EntryPoint
{
    /// <summary>
    /// Log level switch for runtime log level change.
    /// </summary>
    public static readonly LoggingLevelSwitch LogLevelSwitch = new(LogEventLevel.Verbose);

    /// <summary>
    /// A delegate used during initialization of the CLR from Dalamud.Boot.
    /// </summary>
    /// <param name="infoPtr">Pointer to a serialized <see cref="DalamudStartInfo"/> data.</param>
    /// <param name="mainThreadContinueEvent">Event used to signal the main thread to continue.</param>
    public delegate void InitDelegate(IntPtr infoPtr, IntPtr mainThreadContinueEvent);

    /// <summary>
    /// A delegate used from VEH handler on exception which CoreCLR will fast fail by default.
    /// </summary>
    /// <returns>HGLOBAL for message.</returns>
    public delegate IntPtr VehDelegate();

    /// <summary>
    /// Initialize Dalamud.
    /// </summary>
    /// <param name="infoPtr">Pointer to a serialized <see cref="DalamudStartInfo"/> data.</param>
    /// <param name="mainThreadContinueEvent">Event used to signal the main thread to continue.</param>
    public static void Initialize(IntPtr infoPtr, IntPtr mainThreadContinueEvent)
    {
        var infoStr = Marshal.PtrToStringUTF8(infoPtr)!;
        var info = JsonConvert.DeserializeObject<DalamudStartInfo>(infoStr)!;

        if ((info.BootWaitMessageBox & 4) != 0)
            MessageBoxW(IntPtr.Zero, "Press OK to continue (BeforeDalamudConstruct)", "Dalamud Boot", MessageBoxType.Ok);

        new Thread(() => RunThread(info, mainThreadContinueEvent)).Start();
    }

    /// <summary>
    /// Returns stack trace.
    /// </summary>
    /// <returns>HGlobal to wchar_t* stack trace c-string.</returns>
    public static IntPtr VehCallback()
    {
        try
        {
            return Marshal.StringToHGlobalUni(new StackTrace(1).ToString());
        }
        catch (Exception e)
        {
            return Marshal.StringToHGlobalUni("Fail: " + e);
        }
    }

    /// <summary>
    /// Sets up logging.
    /// </summary>
    /// <param name="baseDirectory">Base directory.</param>
    /// <param name="logConsole">Whether to log to console.</param>
    /// <param name="logSynchronously">Log synchronously.</param>
    /// <param name="logName">Name that should be appended to the log file.</param>
    internal static void InitLogging(string baseDirectory, bool logConsole, bool logSynchronously, string? logName)
    {
        var logFileName = logName.IsNullOrEmpty() ? "dalamud" : $"dalamud-{logName}";

        var logPath = new FileInfo(Path.Combine(baseDirectory, $"{logFileName}.log"));
        var oldPath = new FileInfo(Path.Combine(baseDirectory, $"{logFileName}.old.log"));

        Log.CloseAndFlush();

        RetentionBehaviour behaviour;
#if DEBUG
        behaviour = new DebugRetentionBehaviour();
#else
        behaviour = new ReleaseRetentionBehaviour();
#endif

        behaviour.Apply(logPath, oldPath);

        var config = new LoggerConfiguration()
                     .WriteTo.Sink(SerilogEventSink.Instance)
                     .MinimumLevel.ControlledBy(LogLevelSwitch);

        const long maxLogSize = 100 * 1024 * 1024; // 100MB
        if (logSynchronously)
        {
            config = config.WriteTo.File(logPath.FullName, fileSizeLimitBytes: maxLogSize);
        }
        else
        {
            config = config.WriteTo.Async(a => a.File(
                                              logPath.FullName,
                                              fileSizeLimitBytes: maxLogSize,
                                              buffered: false,
                                              flushToDiskInterval: TimeSpan.FromSeconds(1)));
        }

        if (logConsole)
            config = config.WriteTo.Console();

        Log.Logger = config.CreateLogger();
    }

    /// <summary>
    /// Initialize all Dalamud subsystems and start running on the main thread.
    /// </summary>
    /// <param name="info">The <see cref="DalamudStartInfo"/> containing information needed to initialize Dalamud.</param>
    /// <param name="mainThreadContinueEvent">Event used to signal the main thread to continue.</param>
    private static void RunThread(DalamudStartInfo info, IntPtr mainThreadContinueEvent)
    {
        // Setup logger
        InitLogging(info.LogPath!, info.BootShowConsole, true, info.LogName);
        SerilogEventSink.Instance.LogLine += SerilogOnLogLine;

        // Load configuration first to get some early persistent state, like log level
        var fs = new ReliableFileStorage(Path.GetDirectoryName(info.ConfigurationPath)!);
        var configuration = DalamudConfiguration.Load(info.ConfigurationPath!, fs);

        // Set the appropriate logging level from the configuration
        if (!configuration.LogSynchronously)
            InitLogging(info.LogPath!, info.BootShowConsole, configuration.LogSynchronously, info.LogName);
        LogLevelSwitch.MinimumLevel = configuration.LogLevel;

        // Log any unhandled exception.
        switch (info.UnhandledException)
        {
            case UnhandledExceptionHandlingMode.Default:
                AppDomain.CurrentDomain.UnhandledException += OnUnhandledExceptionDefault;
                break;
            case UnhandledExceptionHandlingMode.StallDebug:
                AppDomain.CurrentDomain.UnhandledException += OnUnhandledExceptionStallDebug;
                break;
        }

        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        var unloadFailed = false;

        try
        {
            if (info.DelayInitializeMs > 0)
            {
                Log.Information(string.Format("Waiting for {0}ms before starting a session.", info.DelayInitializeMs));
                Thread.Sleep(info.DelayInitializeMs);
            }

            Log.Information(new string('-', 80));
            Log.Information("Initializing a session..");

            if (string.IsNullOrEmpty(info.WorkingDirectory))
                throw new Exception("Working directory was invalid");

            Reloaded.Hooks.Tools.Utilities.FasmBasePath = new DirectoryInfo(info.WorkingDirectory);

            // Apply common fixes for culture issues
            CultureFixes.Apply();

            if (!Util.IsWine())
                InitSymbolHandler(info);

            var dalamud = new Dalamud(info, fs, configuration, mainThreadContinueEvent);
            Log.Information("This is Dalamud - Core: {GitHash}, CS: {CsGitHash} [{CsVersion}]",
                            Util.GetScmVersion(),
                            Util.GetGitHashClientStructs(),
                            FFXIVClientStructs.ThisAssembly.Git.Commits);

            dalamud.WaitForUnload();

            try
            {
                ServiceManager.UnloadAllServices();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Could not unload services.");
                unloadFailed = true;
            }
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Unhandled exception on main thread.");
        }
        finally
        {
            TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
            switch (info.UnhandledException)
            {
                case UnhandledExceptionHandlingMode.Default:
                    AppDomain.CurrentDomain.UnhandledException -= OnUnhandledExceptionDefault;
                    break;
                case UnhandledExceptionHandlingMode.StallDebug:
                    AppDomain.CurrentDomain.UnhandledException -= OnUnhandledExceptionStallDebug;
                    break;
            }

            Log.Information("Session has ended.");
            Log.CloseAndFlush();
            SerilogEventSink.Instance.LogLine -= SerilogOnLogLine;
        }

        // If we didn't unload services correctly, we need to kill the process.
        // We will never signal to Framework.
        if (unloadFailed)
            Environment.Exit(-1);
    }

    private static void SerilogOnLogLine(object? sender, (string Line, LogEvent LogEvent) ev)
    {
        if (!LoadingDialog.IsGloballyHidden)
            LoadingDialog.NewLogEntries.Enqueue(ev);
        ConsoleWindow.NewLogEntries.Enqueue(ev);

        if (ev.LogEvent.Exception == null)
            return;

        // Don't pass verbose/debug/info exceptions to the troubleshooter, as the developer is probably doing
        // something intentionally (or this is known).
        if (ev.LogEvent.Level < LogEventLevel.Warning)
            return;

        Troubleshooting.LogException(ev.LogEvent.Exception, ev.Line);
    }

    private static void InitSymbolHandler(DalamudStartInfo info)
    {
        try
        {
            if (string.IsNullOrEmpty(info.AssetDirectory))
                return;

            var symbolPath = Path.Combine(info.AssetDirectory, "UIRes", "pdb");
            var searchPath = $".;{symbolPath}";

            // Remove any existing Symbol Handler and Init a new one with our search path added
            SymCleanup(GetCurrentProcess());

            if (!SymInitialize(GetCurrentProcess(), searchPath, true))
                throw new Win32Exception();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "SymbolHandler Initialize Failed.");
        }
    }

    private static void OnUnhandledExceptionDefault(object sender, UnhandledExceptionEventArgs args)
    {
        switch (args.ExceptionObject)
        {
            case Exception ex:
                Log.Fatal(ex, "Unhandled exception on AppDomain");
                Troubleshooting.LogException(ex, "DalamudUnhandled");

                var info = "Further information could not be obtained";
                if (ex.TargetSite != null && ex.TargetSite.DeclaringType != null)
                {
                    info = $"{ex.TargetSite.DeclaringType.Assembly.GetName().Name}, {ex.TargetSite.DeclaringType.FullName}::{ex.TargetSite.Name}";
                }

                var pluginInfo = string.Empty;
                var supportText = ", please visit us on Discord for more help";
                try
                {
                    var pm = Service<PluginManager>.GetNullable();
                    var plugin = pm?.FindCallingPlugin(new StackTrace(ex));
                    if (plugin != null)
                    {
                        pluginInfo = $"Plugin that caused this:\n{plugin.Name}\n\nClick \"Yes\" and remove it.\n\n";

                        if (plugin.IsThirdParty)
                            supportText = string.Empty;
                    }
                }
                catch
                {
                    // ignored
                }

                const MessageBoxType flags = NativeFunctions.MessageBoxType.YesNo | NativeFunctions.MessageBoxType.IconError | NativeFunctions.MessageBoxType.SystemModal;
                var result = MessageBoxW(
                    Process.GetCurrentProcess().MainWindowHandle,
                    $"An internal error in a Dalamud plugin occurred.\nThe game must close.\n\n{ex.GetType().Name}\n{info}\n\n{pluginInfo}More information has been recorded separately{supportText}.\n\nDo you want to disable all plugins the next time you start the game?",
                    "Dalamud",
                    flags);

                if (result == (int)User32.MessageBoxResult.IDYES)
                {
                    Log.Information("User chose to disable plugins on next launch...");
                    var config = Service<DalamudConfiguration>.Get();
                    config.PluginSafeMode = true;
                    config.QueueSave();
                }

                Log.CloseAndFlush();
                Environment.Exit(-1);
                break;
            default:
                Log.Fatal("Unhandled SEH object on AppDomain: {Object}", args.ExceptionObject);

                Log.CloseAndFlush();
                Environment.Exit(-1);
                break;
        }
    }

    private static void OnUnhandledExceptionStallDebug(object sender, UnhandledExceptionEventArgs args)
    {
        while (!Debugger.IsAttached)
            Thread.Sleep(100);
    }

    private static void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs args)
    {
        if (!args.Observed)
            Log.Error(args.Exception, "Unobserved exception in Task.");
    }
}
