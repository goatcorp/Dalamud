using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using CoreHook;
using Dalamud.Hooking.Internal;

namespace Dalamud.Hooking
{
    /// <summary>
    /// Manages a hook which can be used to intercept a call to native function.
    /// This class is basically a thin wrapper around the LocalHook type to provide helper functions.
    /// </summary>
    /// <typeparam name="T">Delegate type to represents a function prototype. This must be the same prototype as original function do.</typeparam>
    public sealed class Hook<T> : IDisposable, IDalamudHook where T : Delegate
    {
        private readonly IntPtr address;

        private readonly T original;

        private readonly LocalHook hookInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="Hook{T}"/> class.
        /// Hook is not activated until Enable() method is called.
        /// </summary>
        /// <param name="address">A memory address to install a hook.</param>
        /// <param name="detour">Callback function. Delegate must have a same original function prototype.</param>
        public Hook(IntPtr address, T detour)
        {
            this.hookInfo = LocalHook.Create(address, detour, null); // Installs a hook here
            this.address = address;
            this.original = Marshal.GetDelegateForFunctionPointer<T>(this.hookInfo.OriginalAddress);
            HookManager.TrackedHooks.Add(new HookInfo() { Delegate = detour, Hook = this, Assembly = Assembly.GetCallingAssembly() });
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Hook{T}"/> class.
        /// Hook is not activated until Enable() method is called.
        /// </summary>
        /// <param name="address">A memory address to install a hook.</param>
        /// <param name="detour">Callback function. Delegate must have a same original function prototype.</param>
        /// <param name="callbackParam">A callback object which can be accessed within the detour.</param>
        [Obsolete("There is no need to specify new YourDelegateType or callbackParam", true)]
        public Hook(IntPtr address, Delegate detour, object callbackParam = null)
            : this(address, detour as T)
        {
        }

        /// <summary>
        /// Gets a memory address of the target function.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Hook is already disposed.</exception>
        public IntPtr Address
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                this.CheckDisposed();
                return this.address;
            }
        }

        /// <summary>
        /// Gets a delegate function that can be used to call the actual function as if function is not hooked yet.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Hook is already disposed.</exception>
        public T Original
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                this.CheckDisposed();
                return this.original;
            }
        }

        /// <summary>
        /// Gets a value indicating whether or not the hook is enabled.
        /// </summary>
        public bool IsEnabled
        {
            get
            {
                this.CheckDisposed();
                return this.hookInfo.ThreadACL.IsExclusive;
            }
        }

        /// <summary>
        /// Gets a value indicating whether or not the hook has been disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Creates a hook. Hooking address is inferred by calling to GetProcAddress() function.
        /// Hook is not activated until Enable() method is called.
        /// </summary>
        /// <param name="moduleName">A name of the module currently loaded in the memory. (e.g. ws2_32.dll).</param>
        /// <param name="exportName">A name of the exported function name (e.g. send).</param>
        /// <param name="detour">Callback function. Delegate must have a same original function prototype.</param>
        /// <returns>The hook with the supplied parameters.</returns>
        public static Hook<T> FromSymbol(string moduleName, string exportName, T detour)
        {
            // Get a function address from the symbol name.
            var address = LocalHook.GetProcAddress(moduleName, exportName);

            return new Hook<T>(address, detour);
        }

        /// <summary>
        /// Creates a hook. Hooking address is inferred by calling to GetProcAddress() function.
        /// Hook is not activated until Enable() method is called.
        /// </summary>
        /// <param name="moduleName">A name of the module currently loaded in the memory. (e.g. ws2_32.dll).</param>
        /// <param name="exportName">A name of the exported function name (e.g. send).</param>
        /// <param name="detour">Callback function. Delegate must have a same original function prototype.</param>
        /// <param name="callbackParam">A callback object which can be accessed within the detour.</param>
        /// <returns>The hook with the supplied parameters.</returns>
        [Obsolete("There is no need to specify new YourDelegateType or callbackParam", true)]
        public static Hook<T> FromSymbol(string moduleName, string exportName, Delegate detour, object callbackParam = null) => FromSymbol(moduleName, exportName, detour as T);

        /// <summary>
        /// Remove a hook from the current process.
        /// </summary>
        public void Dispose()
        {
            if (this.IsDisposed)
            {
                return;
            }

            this.IsDisposed = true;
            this.hookInfo.Dispose();
        }

        /// <summary>
        /// Starts intercepting a call to the function.
        /// </summary>
        public void Enable()
        {
            this.CheckDisposed();

            this.hookInfo.ThreadACL.SetExclusiveACL(null);
        }

        /// <summary>
        /// Stops intercepting a call to the function.
        /// </summary>
        public void Disable()
        {
            this.CheckDisposed();

            this.hookInfo.ThreadACL.SetInclusiveACL(null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckDisposed()
        {
            if (this.IsDisposed)
            {
                throw new ObjectDisposedException("Hook is already disposed.");
            }
        }
    }
}
