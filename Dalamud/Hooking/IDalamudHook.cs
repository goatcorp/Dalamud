using System;

namespace Dalamud.Hooking {
    internal interface IDalamudHook {
        public IntPtr Address { get; }
        public bool IsEnabled { get; }
        public bool IsDisposed { get; }
    }
}
