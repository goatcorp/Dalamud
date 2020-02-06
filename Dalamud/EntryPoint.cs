using System;
using System.IO;
using System.Net;
using Dalamud.Interface;
using EasyHook;
using Serilog;
using Serilog.Core;

namespace Dalamud {
    public sealed class EntryPoint : IEntryPoint {
        public EntryPoint(RemoteHooking.IContext ctx, DalamudStartInfo info) {
            // Required by EasyHook
        }

        public void Run(RemoteHooking.IContext ctx, DalamudStartInfo info) {
            // Setup logger
            Log.Logger = NewLogger(info.WorkingDirectory);

            try {
                Log.Information("Initializing a session..");

                // This is due to GitHub not supporting TLS 1.0, so we enable all TLS versions globally
                System.Net.ServicePointManager.SecurityProtocol =
                    SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls;

                // Log any unhandled exception.
                AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

                using (var dalamud = new Dalamud(info)) {
                    Log.Information("Starting a session..");
                    
                    // Run session
                    dalamud.Start();
                    dalamud.WaitForUnload();
                }
            } catch (Exception ex) {
                Log.Fatal(ex, "Unhandled exception on main thread.");
            } finally {
                AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
                
                Log.Information("Session has ended.");
                Log.CloseAndFlush();
            }
        }
        
        private Logger NewLogger(string baseDirectory) {
            var logPath = Path.Combine(baseDirectory, "dalamud.txt");
            
            return new LoggerConfiguration()
                   .WriteTo.Async(a => a.File(logPath))
                   .WriteTo.EventSink()
#if DEBUG
                   .MinimumLevel.Verbose()
#else
                .MinimumLevel.Information()
#endif
                   .CreateLogger();
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
    }
}
