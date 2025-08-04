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

    /// <summary>
    /// Native ws2_32 functions.
    /// </summary>
    private static class NativeFunctions
    {
        /// <summary>
        /// See https://docs.microsoft.com/en-us/windows/win32/api/winsock/nf-winsock-setsockopt.
        /// The setsockopt function sets a socket option.
        /// </summary>
        /// <param name="socket">
        /// A descriptor that identifies a socket.
        /// </param>
        /// <param name="level">
        /// The level at which the option is defined (for example, SOL_SOCKET).
        /// </param>
        /// <param name="optName">
        /// The socket option for which the value is to be set (for example, SO_BROADCAST). The optname parameter must be a
        /// socket option defined within the specified level, or behavior is undefined.
        /// </param>
        /// <param name="optVal">
        /// A pointer to the buffer in which the value for the requested option is specified.
        /// </param>
        /// <param name="optLen">
        /// The size, in bytes, of the buffer pointed to by the optval parameter.
        /// </param>
        /// <returns>
        /// If no error occurs, setsockopt returns zero. Otherwise, a value of SOCKET_ERROR is returned, and a specific error
        /// code can be retrieved by calling WSAGetLastError.
        /// </returns>
        [DllImport("ws2_32.dll", CallingConvention = CallingConvention.Winapi, EntryPoint = "setsockopt")]
        public static extern int SetSockOpt(IntPtr socket, SocketOptionLevel level, SocketOptionName optName, ref IntPtr optVal, int optLen);
    }
}
