using System;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Configuration.Internal;
using Dalamud.Interface.Internal;
using Dalamud.Logging.Internal;
using Newtonsoft.Json;
using NLog;
using NLog.Config;
using NLog.LayoutRenderers;
using NLog.Targets;
using NLog.Targets.Wrappers;

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
            var infoStr = Marshal.PtrToStringAnsi(infoPtr);
            var info = JsonConvert.DeserializeObject<DalamudStartInfo>(infoStr);

            new Thread(() => RunThread(info)).Start();
        }

        /// <summary>
        /// Initialize all Dalamud subsystems and start running on the main thread.
        /// </summary>
        /// <param name="info">The <see cref="DalamudStartInfo"/> containing information needed to initialize Dalamud.</param>
        private static void RunThread(DalamudStartInfo info)
        {
            // Load configuration first to get some early persistent state, like log level
            var configuration = DalamudConfiguration.Load(info.ConfigurationPath);

            // Setup logger
            InitLogging(info.WorkingDirectory, configuration);

            // Log any unhandled exception.
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            var finishSignal = new ManualResetEvent(false);

            try
            {
                Log.Information(new string('-', 80));
                Log.Information("Initializing a session..");

                // This is due to GitHub not supporting TLS 1.0, so we enable all TLS versions globally
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls;

                var dalamud = new Dalamud(info, finishSignal, configuration);
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
                LogManager.Flush();
                LogManager.Shutdown();

                finishSignal.Set();
            }
        }

        private static void InitLogging(string baseDirectory, DalamudConfiguration configuration)
        {
#if DEBUG
            var logPath = Path.Combine(baseDirectory, "dalamud.log");
#else
            var logPath = Path.Combine(baseDirectory, "..", "..", "..", "dalamud.log");
#endif

#if DEBUG
            var logLevel = LogLevel.Trace;
#else
            var logLevel = configuration.LogLevel;
#endif

            LayoutRenderer.Register<DalamudLevelLayoutRenderer>("dalamud-level");
            LayoutRenderer.Register<DalamudDateTimeLayoutRenderer>("dalamud-datetime");

            var target = new FileTarget("dalamud")
            {
                FileName = logPath,
                ArchiveFileName = "dalamud.{###}.log",
                ArchiveNumbering = ArchiveNumberingMode.Rolling,
                ArchiveAboveSize = 5 * 1024 * 1024,  // 5mb for Discord
                Layout = "${dalamud-datetime} [${dalamud-level}] ${message:exceptionSeparator=\r\n:withException=true}",
            };

            var asyncTarget = new AsyncTargetWrapper(target);

            var rule = new LoggingRule("Dalamud");
            rule.EnableLoggingForLevels(logLevel, LogLevel.Fatal);
            rule.Targets.Add(asyncTarget);
            rule.LoggerNamePattern = "*";

            var eventRule = new LoggingRule("Dalamud.Event");
            eventRule.EnableLoggingForLevels(LogLevel.Trace, LogLevel.Fatal);
            eventRule.Targets.Add(NLogEventTarget.Instance);
            eventRule.LoggerNamePattern = "*";

            var config = new LoggingConfiguration();
            config.LoggingRules.Add(rule);
            config.LoggingRules.Add(eventRule);

            LogManager.Configuration = config;
            LogManager.AutoShutdown = true;
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            switch (args.ExceptionObject)
            {
                case Exception ex:
                    Log.Fatal(ex, "Unhandled exception on AppDomain");
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
