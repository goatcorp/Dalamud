using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Hooking;
using EasyHook;
using Serilog;

namespace Dalamud.Game.Internal
{
    class AntiDebug : IDisposable
    {
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate bool IsDebuggerPresentDelegate();

        private Hook<IsDebuggerPresentDelegate> debuggerPresentHook;

        public AntiDebug() {
            this.debuggerPresentHook = new Hook<IsDebuggerPresentDelegate>(LocalHook.GetProcAddress("Kernel32", "IsDebuggerPresent"),
                new IsDebuggerPresentDelegate(IsDebuggerPresentDetour));

            Log.Verbose("IsDebuggerPresent address {IsDebuggerPresent}", this.debuggerPresentHook.Address);
        }

        public void Enable() {
            //this.debuggerPresentHook.Enable();
        }

        public void Dispose() {
            this.debuggerPresentHook.Disable();
        }

        private bool IsDebuggerPresentDetour() {
            return false;
        }
    }
}
