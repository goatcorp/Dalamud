using System;

namespace Dalamud.Hooking.Internal.Implementations
{
    /// <summary>
    /// Manages a hook which can be used to intercept a call to native function.
    /// This class is basically a thin wrapper around the LocalHook type to provide helper functions.
    /// </summary>
    /// <typeparam name="T">Delegate type to represents a function prototype. This must be the same prototype as original function do.</typeparam>
    internal interface IDalamudHookImpl<T> : IDisposable, IDalamudHook where T : Delegate
    {
        /// <summary>
        /// Gets a delegate function that can be used to call the actual function as if function is not hooked yet.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Hook is already disposed.</exception>
        public T Original { get; }

        /// <summary>
        /// Starts intercepting a call to the function.
        /// </summary>
        public void Enable();

        /// <summary>
        /// Stops intercepting a call to the function.
        /// </summary>
        public void Disable();
    }
}
