using System;

namespace Dalamud.Hooking.Internal
{
    /// <summary>
    /// Interface describing a generic hook.
    /// </summary>
    internal interface IDalamudHook
    {
        /// <summary>
        /// Gets the address to hook.
        /// </summary>
        public IntPtr Address { get; }

        /// <summary>
        /// Gets a value indicating whether or not the hook is enabled.
        /// </summary>
        public bool IsEnabled { get; }

        /// <summary>
        /// Gets a value indicating whether or not the hook is disposed.
        /// </summary>
        public bool IsDisposed { get; }
    }
}
