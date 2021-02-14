using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Interface;
using EasyHook;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Dalamud {
    public sealed class EntryPoint : IEntryPoint {
        public EntryPoint(RemoteHooking.IContext ctx, DalamudStartInfo info) {
            // Required by EasyHook
        }

        public void Run(RemoteHooking.IContext ctx, DalamudStartInfo info) {
            // Setup logger
            var (logger, levelSwitch) = NewLogger(info.WorkingDirectory);
            Log.Logger = logger;

            var finishSignal = new ManualResetEvent(false);

            try {
                Log.Information(new string('-', 80));
                Log.Information("Initializing a session..");

                // This is due to GitHub not supporting TLS 1.0, so we enable all TLS versions globally
                System.Net.ServicePointManager.SecurityProtocol =
                    SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls | SecurityProtocolType.Ssl3;

                // Log any unhandled exception.
                AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
                TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

                var dalamud = new Dalamud(info, levelSwitch, finishSignal);
                Log.Information("Starting a session..");
                    
                // Run session
                dalamud.Start();
                dalamud.WaitForUnload();

                dalamud.Dispose();
            } catch (Exception ex) {
                Log.Fatal(ex, "Unhandled exception on main thread.");
            } finally {
                AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
                
                Log.Information("Session has ended.");
                Log.CloseAndFlush();

                finishSignal.Set();
            }
        }

        private (Logger logger, LoggingLevelSwitch levelSwitch) NewLogger(string baseDirectory) {
#if DEBUG
            var logPath = Path.Combine(baseDirectory, "dalamud.log");
#else
            var logPath = Path.Combine(baseDirectory, "..", "..", "..", "dalamud.log");
#endif

            var levelSwitch = new LoggingLevelSwitch();

#if DEBUG
            levelSwitch.MinimumLevel = LogEventLevel.Verbose;
#else
            levelSwitch.MinimumLevel = LogEventLevel.Information;
#endif

            var newLogger =  new LoggerConfiguration()
                   .WriteTo.Async(a => a.File(logPath, outputTemplate: "[{Timestamp:HH:mm:ss.fff}][{Level:u3}] {Message:lj}{NewLine}{Exception}"))
                   .WriteTo.EventSink()
                   .MinimumLevel.ControlledBy(levelSwitch)
                   .CreateLogger();

            return (newLogger, levelSwitch);
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs arg) {
            switch (arg.ExceptionObject) {
                case Exception ex:
                    Log.Fatal(ex, "Unhandled exception on AppDomain");
                    break;
                default:
                    Log.Fatal("Unhandled SEH object on AppDomain: {Object}", arg.ExceptionObject);
                    break;
            }
        }

        private void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            if (!e.Observed)
                Log.Error(e.Exception, "Unobserved exception in Task.");
        }
    }
}
