using System;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using Windows.Win32.Foundation;
using Dalamud.Broker.Helper;
using Dalamud.Broker.Ipc;
using Dalamud.Broker.Services;
using GrpcDotNetNamedPipes;
using Serilog;

namespace Dalamud.Broker.Commands;

internal static class LaunchCommand
{
    public static void Run(LaunchCommandOptions options)
    {
        using var appContainer = AppContainerHelper.CreateContainer();
        
        var server = StartIpcServer(appContainer.Psid, CreateIpcPipePath());
        
        
        
        // TODO:
        // 1. start ffxiv
        // 2. create a ipc service
        // 3. resume the game 
    }

    private static void StartGame(LaunchCommandOptions options)
    {
        // TODO:
        
    }

    private static unsafe NamedPipeServer StartIpcServer(PSID containerPsid, string pipeName)
    {
        var currentUserSid = new NTAccount(Environment.UserName);
        var containerSid = new SecurityIdentifier((IntPtr)containerPsid.Value);
        
        var pipeSecurity = new PipeSecurity();
        pipeSecurity.AddAccessRule(new PipeAccessRule(currentUserSid, PipeAccessRights.ReadWrite, AccessControlType.Allow));
        pipeSecurity.AddAccessRule(new PipeAccessRule(containerSid, PipeAccessRights.ReadWrite, AccessControlType.Allow));

        var serverOptions = new NamedPipeServerOptions
        {
            PipeSecurity = pipeSecurity,
        };
        var server = new NamedPipeServer(pipeName, serverOptions);
        
        var services = new IpcService();
        services.BindServices(server.ServiceBinder);
        
        server.Start();

        Log.Information("Started an ipc server for {User} (ContainerSid={ContainerSid}, Path={Path})", currentUserSid,
                        containerSid, pipeName);
        
        return server;
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
}
