using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using Windows.Win32;
using Dalamud.Broker.Game;
using Dalamud.Broker.Ipc;
using Dalamud.Broker.Win32;
using GrpcDotNetNamedPipes;
using Microsoft.Win32.SafeHandles;
using Newtonsoft.Json;
using Serilog;

namespace Dalamud.Broker.Commands;

internal static partial class LaunchCommand
{
    [DllImport("Dalamud.Boot.dll")]
    private static extern int RewriteRemoteEntryPointW(
        SafeHandle hProcess, [MarshalAs(UnmanagedType.LPWStr)] string gamePath,
        [MarshalAs(UnmanagedType.LPWStr)] string loadInfoJson);

    public static async Task Run(LaunchCommandOptions options)
    {
        ProcessHandle? gameProcess = default;

        try
        {
            using var container = AppContainerHelper.CreateContainer();

            // Create the game process.
            gameProcess = CreateGameProcess(options, container);

            // Create dependencies
            var services = new ServiceProvider(options, container, gameProcess);
            
            // Bind services
            var ipcServer = services.GetService<IpcServer>();
            var ipcBinder = services.GetService<IpcServiceBinder>();
            ipcBinder.BindServices(ipcServer.ServiceBinder);
            ipcServer.Start();

            // Start the ipc server 
            Log.Information("Dalamud.Broker is now running at {Path} for the container {ContainerSid}",
                            ipcServer.Path,
                            container.ToIdentityReference());
        }
        catch (Exception ex)
        {
            // If anything went wrong during the initial launch process, we need to bring down the game
            // along with it to not make a stale process. (i.e. handle broker.State == Exited &&
            // game.State == Suspended case)
            // 
            // However, if this is a debug build then F that 💩 since leaving the game alive is much much easier
            // to track down what actually went wrong.
            Log.Fatal(ex, "Something went wrong while launching the game");

#if DEBUG
            // Let the debugger handle this
            throw;
#else
            if (gameProcess != null)
            {
                PInvoke.TerminateProcess(gameProcess.Process, 100);
            }
#endif
        }
    }

    private static ProcessHandle CreateGameProcess(
        LaunchCommandOptions options, AppContainer container)
    {
        var processLaunchContext = new ProcessLaunchContext
        {
            ApplicationPath = options.Game,
        };
        var handle = ProcessLauncher.Start(processLaunchContext);

        // Load dalamud
        if (!options.NoDalamud)
        {
            var startInfo = CreateStartInfo(options);
            
            var errc = RewriteRemoteEntryPointW(handle.Process, options.Game, startInfo);
            if (errc != 0)
            {
                throw new Exception($"Failed to load Dalamud (Error Code {errc:X04}h)");
            }
        }

        return handle;
    }

    private static string CreateStartInfo(LaunchCommandOptions options)
    {
        var info = new DalamudStartInfo
        {
            WorkingDirectory = Directory.GetCurrentDirectory(),
            ConfigurationPath = options.DalamudConfigurationPath,
            PluginDirectory = options.DalamudPluginDirectory,
            DefaultPluginDirectory = options.DalamudDevPluginDirectory,
            AssetDirectory = options.DalamudAssetDirectory,
            BootShowConsole = false,
            CrashHandlerShow = true,
            BootLogPath = @"D:\Projects\FFXIV\minoost\Dalamud.Experiment.Ldm\bin\Debug\dalamud_injector.log",
            BootDotnetOpenProcessHookMode = 0,
            BootWaitMessageBox = 1 | 2 | 4,
            BootVehEnabled = true,
            NoLoadPlugins = options.NoPlugin,
            NoLoadThirdPartyPlugins = options.NoThirdPartyPlugin,
            Language = (ClientLanguage)options.DalamudClientLanguage,
        };

        return JsonConvert.ToString(info);
    }
}
