using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Configuration.Internal;
using Dalamud.Logging.Internal;
using Dalamud.Support;
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
        /// Initialize all Dalamud subsystems and start running on the main thread.
        /// </summary>
        /// <param name="info">The <see cref="DalamudStartInfo"/> containing information needed to initialize Dalamud.</param>
        private static void RunThread(DalamudStartInfo info)
        {
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
                Log.Information(new string('-', 80));
                Log.Information("Initializing a session..");

                var vehManager = new VehManager(0, OnVectoredException);

                // This is due to GitHub not supporting TLS 1.0, so we enable all TLS versions globally
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls;

                var dalamud = new Dalamud(info, levelSwitch, finishSignal, configuration);
                Log.Information("Starting a session..");

                // Run session
                dalamud.LoadTier1();
                dalamud.WaitForUnload();

                dalamud.Dispose();
                vehManager.Dispose();
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

        private static unsafe int OnVectoredException(ref VehManager.ExceptionPointers ex)
        {
            if (!Enum.IsDefined(typeof(VehManager.ExceptionCode), ex.ExceptionRecord->ExceptionCode))
                return VehManager.ExceptionContinueSearch;

            var code = (VehManager.ExceptionCode)ex.ExceptionRecord->ExceptionCode;
            var address = ex.ExceptionRecord->ExceptionAddress;
            var info = $"{code}(0x{ex.ExceptionRecord->ExceptionCode:X})";

            var module = Process.GetCurrentProcess().Modules.Cast<ProcessModule>().FirstOrDefault(m => address >= m.BaseAddress && address < m.BaseAddress + m.ModuleMemorySize);
            if (module != null)
            {
                var rva = address - module.BaseAddress;

                info += $"\nat {module.ModuleName}+{rva:X}";
            }
            else
            {
                info += $"\nat {ex.ExceptionRecord->ExceptionAddress:X}";
            }

            const MessageBoxType flags = NativeFunctions.MessageBoxType.AbortRetryIgnore | NativeFunctions.MessageBoxType.IconError | NativeFunctions.MessageBoxType.SystemModal;
            var result = MessageBoxW(
                Process.GetCurrentProcess().MainWindowHandle,
                $"An error within the game occurred and Dalamud handled it. This may indicate a malfunctioning plugin.\nThe game must close.\n\n{info}\n\nMore information has been recorded separately, please contact us in our Discord or on GitHub.\n\n" +
                "Click \"Abort\" to save further information.\n" +
                "Click \"Retry\" to disable all plugins.\n" +
                "Click \"Ignore\" to do nothing and quit.",
                "Dalamud",
                flags);

            switch (result)
            {
                case (int)User32.MessageBoxResult.IDRETRY:
                {
                    Log.Information("User chose to disable plugins on next launch...");
                    var config = Service<DalamudConfiguration>.Get();
                    config.PluginSafeMode = true;
                    config.Save();
                    break;
                }

                case (int)User32.MessageBoxResult.IDABORT:
                {
                    // TODO: We can also do this for managed exceptions, but do we want to? It requires doing dumps with full memory.
                    Log.Information("User chose to save minidump...");

                    #if DEBUG
                    var path = Path.Combine(Path.GetDirectoryName(typeof(EntryPoint).Assembly.Location), $"dalamud_appcrashd_{DateTimeOffset.Now.ToUnixTimeSeconds()}.dmp");
                    #else
                    var path = Path.Combine(Path.GetDirectoryName(typeof(EntryPoint).Assembly.Location), "..", "..", "..", $"dalamud_appcrash_{DateTimeOffset.Now.ToUnixTimeSeconds()}.dmp");
                    #endif

                    try
                    {
                        var file = new FileStream(path, FileMode.Create);

                        fixed (void* pEx = &ex)
                        {
                            var mdmpInfo = default(MinidumpExceptionInformation);
                            mdmpInfo.ClientPointers = 1;

                            mdmpInfo.ExceptionPointers = new IntPtr(pEx);
                            mdmpInfo.ThreadId = GetCurrentThreadId();

                            MiniDumpWriteDump(GetCurrentProcess(), GetCurrentProcessId(), file.SafeFileHandle.DangerousGetHandle(), (int)MiniDumpType.MiniDumpWithDataSegs, ref mdmpInfo, IntPtr.Zero, IntPtr.Zero);
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, "Failed to save minidump");
                    }

                    break;
                }
            }

            Environment.Exit(-1);

            return VehManager.ExceptionContinueSearch;
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
