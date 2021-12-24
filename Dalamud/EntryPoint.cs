using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Configuration.Internal;
using Dalamud.Game;
using Dalamud.Logging.Internal;
using Dalamud.Support;
using Dalamud.Utility;
using Newtonsoft.Json;
using PInvoke;
using Serilog;
using Serilog.Core;
using Serilog.Events;

using static Dalamud.NativeFunctions;

namespace Dalamud
{
    /// <summary>
    /// The main entrypoint for the Dalamud system.
    /// </summary>
    public sealed class EntryPoint
    {
        /// <summary>
        /// A delegate used during initialization of the CLR from Dalamud.Boot.
        /// </summary>
        /// <param name="infoPtr">Pointer to a serialized <see cref="DalamudStartInfo"/> data.</param>
        public delegate void InitDelegate(IntPtr infoPtr);

        /// <summary>
        /// A delegate used from VEH handler on exception which CoreCLR will fast fail by default.
        /// </summary>
        /// <param name="dumpPath">Path to minidump file created in UTF-16.</param>
        /// <param name="logPath">Path to log file to create in UTF-16.</param>
        /// <param name="log">Log text in UTF-16.</param>
        public delegate void VehDelegate(IntPtr dumpPath, IntPtr logPath, IntPtr log);

        /// <summary>
        /// Initialize Dalamud.
        /// </summary>
        /// <param name="infoPtr">Pointer to a serialized <see cref="DalamudStartInfo"/> data.</param>
        public static void Initialize(IntPtr infoPtr)
        {
            var infoStr = Marshal.PtrToStringUTF8(infoPtr);
            var info = JsonConvert.DeserializeObject<DalamudStartInfo>(infoStr);

            new Thread(() => RunThread(info)).Start();
        }

        /// <summary>
        /// Show error message along with stack trace and exit.
        /// </summary>
        /// <param name="dumpPath">Path to minidump file created in UTF-16.</param>
        /// <param name="logPath">Path to log file to create in UTF-16.</param>
        /// <param name="log">Log text in UTF-16.</param>
        public static void VehCallback(IntPtr dumpPath, IntPtr logPath, IntPtr log)
        {
            string stackTrace;
            try
            {
                stackTrace = Environment.StackTrace;
            }
            catch (Exception e)
            {
                stackTrace = "Fail: " + e.ToString();
            }

            var msg = "An error within the game has occurred.\n\n"
                + "This may be caused by a faulty plugin, a broken TexTools modification, any other third-party tool or simply a bug in the game.\n"
                + "Please try \"Start Over\" or \"Download Index Backup\" in TexTools, an integrity check in the XIVLauncher settings, and disabling plugins you don't need.\n\n"
                + "The log file is located at:\n"
                + "{1}\n\n"
                + "Press OK to exit the application.\n\nStack trace:\n{2}";

            try
            {
                File.WriteAllText(
                    Marshal.PtrToStringUni(logPath),
                    "Stack trace:\n" + stackTrace + "\n\n" + Marshal.PtrToStringUni(log));
            }
            catch (Exception e)
            {
                msg += "\n\nAdditionally, failed to write file: " + e.ToString();
            }

            // Show in another thread to prevent messagebox from pumping messages of current thread.
            var msgThread = new Thread(() =>
            {
                Utility.Util.Fatal(
                    msg.Format(
                        Marshal.PtrToStringUni(dumpPath),
                        Marshal.PtrToStringUni(logPath),
                        stackTrace),
                    "Dalamud Error",
                    false);
            });
            msgThread.Start();
            msgThread.Join();
        }

        /// <summary>
        /// Initialize all Dalamud subsystems and start running on the main thread.
        /// </summary>
        /// <param name="info">The <see cref="DalamudStartInfo"/> containing information needed to initialize Dalamud.</param>
        private static void RunThread(DalamudStartInfo info)
        {
            if (EnvironmentConfiguration.DalamudWaitForDebugger)
            {
                while (!Debugger.IsAttached)
                {
                    Thread.Sleep(100);
                }
            }

            // Setup logger
            var levelSwitch = InitLogging(info.WorkingDirectory);

            // Load configuration first to get some early persistent state, like log level
            var configuration = DalamudConfiguration.Load(info.ConfigurationPath);

            // Set the appropriate logging level from the configuration
#if !DEBUG
            levelSwitch.MinimumLevel = configuration.LogLevel;
#endif

            // Log any unhandled exception.
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            var finishSignal = new ManualResetEvent(false);

            try
            {
                if (info.DelayInitializeMs > 0)
                {
                    Log.Information(string.Format("Waiting for {0}ms before starting a session.", info.DelayInitializeMs));
                    Thread.Sleep(info.DelayInitializeMs);
                }

                Log.Information(new string('-', 80));
                Log.Information("Initializing a session..");

                // This is due to GitHub not supporting TLS 1.0, so we enable all TLS versions globally
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls;

                if (!Util.IsLinux())
                    InitSymbolHandler(info);

                var dalamud = new Dalamud(info, levelSwitch, finishSignal, configuration);
                Log.Information("Starting a session..");

                // Run session
                dalamud.LoadTier1();
                dalamud.WaitForUnload();

                dalamud.Dispose();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Unhandled exception on main thread.");
            }
            finally
            {
                TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
                AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;

                Log.Information("Session has ended.");
                Log.CloseAndFlush();

                finishSignal.Set();
            }
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

        private static LoggingLevelSwitch InitLogging(string baseDirectory)
        {
#if DEBUG
            var logPath = Path.Combine(baseDirectory, "dalamud.log");
            var oldPath = Path.Combine(baseDirectory, "dalamud.log.old");
#else
            var logPath = Path.Combine(baseDirectory, "..", "..", "..", "dalamud.log");
            var oldPath = Path.Combine(baseDirectory, "..", "..", "..", "dalamud.log.old");
#endif

            CullLogFile(logPath, oldPath, 1 * 1024 * 1024);
            CullLogFile(oldPath, null, 10 * 1024 * 1024);

            var levelSwitch = new LoggingLevelSwitch(LogEventLevel.Verbose);
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Async(a => a.File(logPath))
                .WriteTo.Sink(SerilogEventSink.Instance)
                .MinimumLevel.ControlledBy(levelSwitch)
                .CreateLogger();

            return levelSwitch;
        }

        private static void CullLogFile(string logPath, string? oldPath, int cullingFileSize)
        {
            try
            {
                var bufferSize = 4096;

                var logFile = new FileInfo(logPath);

                if (!logFile.Exists)
                    logFile.Create();

                if (logFile.Length <= cullingFileSize)
                    return;

                var amountToCull = logFile.Length - cullingFileSize;

                if (amountToCull < bufferSize)
                    return;

                if (oldPath != null)
                {
                    var oldFile = new FileInfo(oldPath);

                    if (!oldFile.Exists)
                        oldFile.Create().Close();

                    using var reader = new BinaryReader(logFile.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                    using var writer = new BinaryWriter(oldFile.Open(FileMode.Append, FileAccess.Write, FileShare.ReadWrite));

                    var read = -1;
                    var total = 0;
                    var buffer = new byte[bufferSize];
                    while (read != 0 && total < amountToCull)
                    {
                        read = reader.Read(buffer, 0, buffer.Length);
                        writer.Write(buffer, 0, read);
                        total += read;
                    }
                }

                {
                    using var reader = new BinaryReader(logFile.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                    using var writer = new BinaryWriter(logFile.Open(FileMode.Open, FileAccess.Write, FileShare.ReadWrite));

                    reader.BaseStream.Seek(amountToCull, SeekOrigin.Begin);

                    var read = -1;
                    var total = 0;
                    var buffer = new byte[bufferSize];
                    while (read != 0)
                    {
                        read = reader.Read(buffer, 0, buffer.Length);
                        writer.Write(buffer, 0, read);
                        total += read;
                    }

                    writer.BaseStream.SetLength(total);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Log cull failed");

                /*
                var caption = "XIVLauncher Error";
                var message = $"Log cull threw an exception: {ex.Message}\n{ex.StackTrace ?? string.Empty}";
                _ = MessageBoxW(IntPtr.Zero, message, caption, MessageBoxType.IconError | MessageBoxType.Ok);
                */
            }
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
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

                    const MessageBoxType flags = NativeFunctions.MessageBoxType.YesNo | NativeFunctions.MessageBoxType.IconError | NativeFunctions.MessageBoxType.SystemModal;
                    var result = MessageBoxW(
                        Process.GetCurrentProcess().MainWindowHandle,
                        $"An internal error in a Dalamud plugin occurred.\nThe game must close.\n\nType: {ex.GetType().Name}\n{info}\n\nMore information has been recorded separately, please contact us in our Discord or on GitHub.\n\nDo you want to disable all plugins the next time you start the game?",
                        "Dalamud",
                        flags);

                    if (result == (int)User32.MessageBoxResult.IDYES)
                    {
                        Log.Information("User chose to disable plugins on next launch...");
                        var config = Service<DalamudConfiguration>.Get();
                        config.PluginSafeMode = true;
                        config.Save();
                    }

                    Environment.Exit(-1);

                    break;
                default:
                    Log.Fatal("Unhandled SEH object on AppDomain: {Object}", args.ExceptionObject);
                    break;
            }
        }

        private static void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs args)
        {
            if (!args.Observed)
                Log.Error(args.Exception, "Unobserved exception in Task.");
        }
    }
}
