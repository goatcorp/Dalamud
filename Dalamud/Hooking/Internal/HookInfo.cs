using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Dalamud.Hooking.Internal
{
    /// <summary>
    /// Class containing information about registered hooks.
    /// </summary>
    internal class HookInfo
    {
        private ulong? inProcessMemory = 0;

        /// <summary>
        /// Gets the RVA of the hook.
        /// </summary>
        internal ulong? InProcessMemory
        {
            get
            {
                if (this.Hook.IsDisposed) return 0;
                if (this.inProcessMemory == null) return null;
                if (this.inProcessMemory.Value > 0) return this.inProcessMemory.Value;

                var p = Process.GetCurrentProcess().MainModule;
                var begin = (ulong)p.BaseAddress.ToInt64();
                var end = begin + (ulong)p.ModuleMemorySize;
                var hookAddr = (ulong)this.Hook.Address.ToInt64();

                if (hookAddr >= begin && hookAddr <= end)
                {
                    this.inProcessMemory = hookAddr - begin;
                    return this.inProcessMemory.Value;
                }
                else
                {
                    this.inProcessMemory = null;
                    return null;
                }
            }
        }

        /// <summary>
        /// Gets or sets the tracked hook.
        /// </summary>
        internal IDalamudHook Hook { get; set; }

        /// <summary>
        /// Gets or sets the tracked delegate.
        /// </summary>
        internal Delegate Delegate { get; set; }

        /// <summary>
        /// Gets or sets the hooked assembly.
        /// </summary>
        internal Assembly Assembly { get; set; }
    }
}
