using System.Runtime.InteropServices;
using System.Security.Principal;
using Dalamud.Broker.Ipc;
using Dalamud.Broker.Win32;
using Jab;
using Microsoft.Win32.SafeHandles;

namespace Dalamud.Broker.Commands;

internal static partial class LaunchCommand
{
    [ServiceProvider]
    [Singleton(typeof(IpcServer), Instance = nameof(IpcServer))]
    [Singleton(typeof(ProcessHandle), Instance = nameof(ProcessHandle))]
    [Singleton<IpcServiceBinder>]
    [Import<IIpcServices>]
    private partial class ServiceProvider
    {
        public IpcServer IpcServer { get; }

        public ProcessHandle ProcessHandle { get; }

        public ServiceProvider(LaunchCommandOptions options, AppContainer container, ProcessHandle handle)
        {
            this.IpcServer = new IpcServer(CreateIpcPath(), container.ToIdentityReference());
            TryAddDisposable(this.IpcServer);

            this.ProcessHandle = handle;
            TryAddDisposable(this.ProcessHandle);
        }

        private static string CreateIpcPath()
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
}
