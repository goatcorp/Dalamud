using Dalamud.Hooking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dalamud.Game
{
    internal sealed class WinSockHandlers : IDisposable
    {
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate IntPtr SocketDelegate(int af, int type, int protocol);
        private Hook<SocketDelegate> ws2SocketHook;

        [DllImport("ws2_32.dll", CallingConvention = CallingConvention.Winapi)]
        private static extern int setsockopt(IntPtr socket, SocketOptionLevel level, SocketOptionName optName, ref IntPtr optVal, int optLen);

        public WinSockHandlers() {
            this.ws2SocketHook = Hook<SocketDelegate>.FromSymbol("ws2_32.dll", "socket", new SocketDelegate(OnSocket));
            this.ws2SocketHook.Enable();
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
                    setsockopt(socket, SocketOptionLevel.Tcp, SocketOptionName.NoDelay, ref value, 4);

                    // Enable tcp_quickack option. This option is undocumented in MSDN but it is supported in Windows 7 and onwards.
                    value = new IntPtr(1);
                    setsockopt(socket, SocketOptionLevel.Tcp, (SocketOptionName)12, ref value, 4);
                }
            }

            return socket;
        }

        public void Dispose() {
            ws2SocketHook.Dispose();
        }
    }
}
