using System.Net.Sockets;
using System.Runtime.InteropServices;

using Dalamud.Hooking;

namespace Dalamud.Game.Network.Internal;

/// <summary>
/// This class enables TCP optimizations in the game socket for better performance.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal sealed class WinSockHandlers : IInternalDisposableService
{
    private Hook<SocketDelegate> ws2SocketHook;

    [ServiceManager.ServiceConstructor]
    private WinSockHandlers()
    {
        this.ws2SocketHook = Hook<SocketDelegate>.FromImport(null, "ws2_32.dll", "socket", 23, this.OnSocket);
        this.ws2SocketHook?.Enable();
    }

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate IntPtr SocketDelegate(int af, int type, int protocol);

    /// <summary>
    /// Disposes of managed and unmanaged resources.
    /// </summary>
    void IInternalDisposableService.DisposeService()
    {
        this.ws2SocketHook?.Dispose();
    }

    private IntPtr OnSocket(int af, int type, int protocol)
    {
        var socket = this.ws2SocketHook.Original(af, type, protocol);

        // IPPROTO_TCP
        if (type == 1)
        {
            // INVALID_SOCKET
            if (socket != new IntPtr(-1))
            {
                // In case you're not aware of it: (albeit you should)
                // https://linux.die.net/man/7/tcp
                // https://assets.extrahop.com/whitepapers/TCP-Optimization-Guide-by-ExtraHop.pdf
                var value = new IntPtr(1);
                _ = NativeFunctions.SetSockOpt(socket, SocketOptionLevel.Tcp, SocketOptionName.NoDelay, ref value, 4);

                // Enable tcp_quickack option. This option is undocumented in MSDN but it is supported in Windows 7 and onwards.
                value = new IntPtr(1);
                _ = NativeFunctions.SetSockOpt(socket, SocketOptionLevel.Tcp, SocketOptionName.AddMembership, ref value, 4);
            }
        }

        return socket;
    }
}
