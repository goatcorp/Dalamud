using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using EasyHook;

namespace Dalamud.Hooking {
    /// <summary>
    /// Manages a hook which can be used to intercept a call to native function.
    /// This class is basically a thin wrapper around the LocalHook type to provide helper functions.
    /// </summary>
    /// <typeparam name="T">Delegate type to represents a function prototype. This must be the same prototype as original function do.</typeparam>
    public sealed class Hook<T> : IDisposable where T : Delegate {
        private bool isDisposed;

        private readonly IntPtr address;

        private readonly T original;
        
        private readonly LocalHook hookInfo;

        /// <summary>
        /// A memory address of the target function. 
        /// </summary>
        /// <exception cref="ObjectDisposedException">Hook is already disposed.</exception>
        public IntPtr Address {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get {
                CheckDisposed();
                return this.address;
            }
        }

        /// <summary>
        /// A delegate function that can be used to call the actual function as if function is not hooked yet.
        /// </summary>
        /// <remarks></remarks>
        /// <exception cref="ObjectDisposedException">Hook is already disposed.</exception>
        public T Original {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get {
                CheckDisposed();
                return this.original;
            }
        }

        
        /// <summary>
        /// Creates a hook. Hooking address is inferred by calling to GetProcAddress() function. Hook is not activated until Enable() method is called.
        /// </summary>
        /// <param name="moduleName">A name of the module currently loaded in the memory. (e.g. ws2_32.dll)</param>
        /// <param name="exportName">A name of the exported function name (e.g. send)</param>
        /// <param name="detour">Callback function. Delegate must have a same original function prototype.</param>
        /// <param name="callbackParam">A callback object which can be accessed within the detour.</param>
        /// <returns></returns>
        public static Hook<T> FromSymbol(string moduleName, string exportName, Delegate detour, object callbackParam = null) {
            // Get a function address from the symbol name.
            var address = LocalHook.GetProcAddress(moduleName, exportName);
            
            return new Hook<T>(address, detour, callbackParam);
        }
        
        /// <summary>
        /// Createss a hook. Hook is not activated until Enable() method is called.
        /// </summary>
        /// <param name="address">A memory address to install a hook.</param>
        /// <param name="detour">Callback function. Delegate must have a same original function prototype.</param>
        /// <param name="callbackParam">A callback object which can be accessed within the detour.</param>
        public Hook(IntPtr address, Delegate detour, object callbackParam = null) {
            this.hookInfo = LocalHook.Create(address, detour, callbackParam); // Installs a hook here
            this.address  = address;
            this.original = Marshal.GetDelegateForFunctionPointer<T>(this.hookInfo.HookBypassAddress);
        }

        /// <summary>
        /// Remove a hook from the current process.
        /// </summary>
        public void Dispose() {
            if (this.isDisposed) {
                return;
            }
            
            this.hookInfo.Dispose();
            
            this.isDisposed = true;
        }

        /// <summary>
        /// Starts intercepting a call to the function. 
        /// </summary>
        public void Enable() {
            CheckDisposed();
            
            this.hookInfo.ThreadACL.SetExclusiveACL(null);
        }

        /// <summary>
        /// Stops intercepting a call to the function. 
        /// </summary>
        public void Disable() {
            CheckDisposed();
            
            this.hookInfo.ThreadACL.SetInclusiveACL(null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckDisposed() {
            if (this.isDisposed) {
                throw new ObjectDisposedException("Hook is already disposed.");
            }
        }
    }
}
