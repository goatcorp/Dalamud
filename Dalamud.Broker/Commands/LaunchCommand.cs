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
    private static extern int RewriteRemoteEntryPointW(SafeHandle hProcess, [MarshalAs(UnmanagedType.LPWStr)] string gamePath, [MarshalAs(UnmanagedType.LPWStr)] string loadInfoJson);

    public static async Task Run(LaunchCommandOptions options)
    {
        var pipePath = CreateIpcPipePath();

        using var serviceProvider = new ServiceProvider();
        using var appContainer = AppContainerHelper.CreateContainer();

        using var server = StartIpcServer(pipePath, serviceProvider, appContainer);
        var (gameProcess, gameThread) = StartGame(serviceProvider, options);

        var waiter = new ProcessWaiter(gameProcess);
        await waiter.WaitAsync();
    }

    private static string CreateIpcPipePath()
    {
        // NOTE:
        // Current strategy is to create a pipe with unique name then pass the path to Dalamud via StartupObject.
        //
        // Because the broker and ffxiv have different namespace root for kernel objects (CreatePrivateNamespaceA?)
        // we simply create a pipe on `\Global` (which is just an alias for `\Sessions\0`, according to Windows Internals book)
        // and set appropriate ACEs to let the sandboxed game can connect to the broker to simplify this "resolving path" part.
        //
        // Note that connecting to the broker requires both AppContainer SID and current user SID to have
        // ReadWrite access to it.
        // (This is realized by passing a PipeSecurity object at IpcServer creation time)

        // From API docs:
        // The method creates a Version 4 Universally Unique Identifier (UUID) as described in RFC 4122, Sec. 4.4.
        //                    ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
        //                          6 bits for metadata + 122 random bits!
        var uuid = Guid.NewGuid();

        return $@"Global\Dalamud.Broker.{uuid}";
    }

    private static NamedPipeServer StartIpcServer(string pipePath, ServiceProvider service, AppContainer container)
    {
        var currentUserSid = new NTAccount(Environment.UserName);
        var containerSid = container.GetIdentityReference();

        // Set up a security descriptor for the pipe.
        // Since the pipe is bidirectional both the user and container must have rw access to it.
        var pipeSecurity = new PipeSecurity();
        pipeSecurity.AddAccessRule(new PipeAccessRule(currentUserSid, PipeAccessRights.ReadWrite,
                                                      AccessControlType.Allow));
        pipeSecurity.AddAccessRule(
            new PipeAccessRule(containerSid, PipeAccessRights.ReadWrite, AccessControlType.Allow));

        // Create an ipc server
        var serverOptions = new NamedPipeServerOptions
        {
            PipeSecurity = pipeSecurity,
        };
        var server = new NamedPipeServer(pipePath, serverOptions);

        // Bind services
        var services = new IpcServiceProvider();
        services.BindServices(server.ServiceBinder);

        // Start the server
        server.Start();
        Log.Information("ipc server is now running (User={User}, ContainerSid={ContainerSid}, Path={Path})",
                        currentUserSid,
                        containerSid,
                        pipePath);

        return server;
    }

    private static (SafeProcessHandle, SafeProcessHandle) StartGame(ServiceProvider service, LaunchCommandOptions options)
    {
        // TODO:
        var processLaunchContext = new ProcessLaunchContext
        {
            ApplicationPath = options.Game,
        };
        var (process, thread) = ProcessLauncher.Start(processLaunchContext);

        return (process, thread);
    }
    
    private static string CreateStartInfo(LaunchCommandOptions options)
    {
        var info = new DalamudStartInfo
        {
            WorkingDirectory = Directory.GetCurrentDirectory(),
            ConfigurationPath = @"C:\Users\Workbench\AppData\Roaming\XIVLauncher\dalamudConfig.json",
            PluginDirectory = @"C:\Users\Workbench\AppData\Roaming\XIVLauncher\installedPlugins",
            DefaultPluginDirectory = @"C:\Users\Workbench\AppData\Roaming\XIVLauncher\devPlugins",
            AssetDirectory = @"C:\Users\Workbench\AppData\Roaming\XIVLauncher\dalamudAssets\dev",
            BootShowConsole = true,
            CrashHandlerShow = true,
            BootLogPath = @"D:\Projects\FFXIV\minoost\Dalamud.Experiment.Ldm\bin\Debug\dalamud_injector.log",
            BootDotnetOpenProcessHookMode = 0,
            BootWaitMessageBox = 1 | 2 | 4,
            BootVehEnabled = true,
            NoLoadPlugins = options.NoPlugin,
            NoLoadThirdPartyPlugins = options.NoThirdPartyPlugin,
            Language = ClientLanguage.English,
        };

        return JsonConvert.ToString(info);
    }
}
