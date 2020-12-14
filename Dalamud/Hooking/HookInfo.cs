using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Dalamud.Hooking {
    internal class HookInfo {

        internal static List<HookInfo> TrackedHooks = new List<HookInfo>();

        internal IDalamudHook Hook { get; set; }
        internal Delegate Delegate { get; set; }
        internal Assembly Assembly { get; set; }

        private ulong? inProcessMemory = 0;
        internal ulong? InProcessMemory {
            get {
                if (Hook.IsDisposed) return 0;
                if (this.inProcessMemory == null) return null;
                if (this.inProcessMemory.Value > 0) return this.inProcessMemory.Value;
                var p = Process.GetCurrentProcess().MainModule;
                var begin = (ulong) p.BaseAddress.ToInt64();
                var end = begin + (ulong) p.ModuleMemorySize;
                var hookAddr = (ulong) Hook.Address.ToInt64();
                if (hookAddr >= begin && hookAddr <= end) {
                    this.inProcessMemory = hookAddr - begin;
                    return this.inProcessMemory.Value;
                } else {
                    this.inProcessMemory = null;
                    return null;
                }
            }
        }

    }
}
