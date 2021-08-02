using System;

using Dalamud.Hooking.Internal;
using Dalamud.Hooking.Internal.Implementations;

namespace Dalamud.Hooking
{
    /// <summary>
    /// Manages a hook which can be used to intercept a call to native function.
    /// This class is basically a thin wrapper around the LocalHook type to provide helper functions.
    /// </summary>
    /// <typeparam name="T">Delegate type to represents a function prototype. This must be the same prototype as original function do.</typeparam>
    public sealed class Hook<T> : IDisposable where T : Delegate
    {
        private readonly IDalamudHookImpl<T> hookImpl;

        /// <summary>
        /// Initializes a new instance of the <see cref="Hook{T}"/> class.
        /// Hook is not activated until Enable() method is called.
        /// </summary>
        /// <param name="address">A memory address to install a hook.</param>
        /// <param name="detour">Callback function. Delegate must have a same original function prototype.</param>
        public Hook(IntPtr address, T detour)
            : this(address, detour, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Hook{T}"/> class.
        /// Hook is not activated until Enable() method is called.
        /// </summary>
        /// <param name="address">A memory address to install a hook.</param>
        /// <param name="detour">Callback function. Delegate must have a same original function prototype.</param>
        /// <param name="followJmp">Follow any JMPs to the actual method that needs hooking.</param>
        /// <remarks>
        /// The followJmp parameter is only used when ReloadedHooks are used, which currently is only for Linux users.
        /// Generally, this is only necessary when hooking Win32 functions.
        /// </remarks>
        public Hook(IntPtr address, T detour, bool followJmp)
        {
            this.hookImpl = HookManager.DirtyLinuxUser
                ? new ReloadedHookImpl<T>(address, detour, followJmp)
                : new CoreHookImpl<T>(address, detour);
        }

        /// <summary>
        /// Gets a memory address of the target function.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Hook is already disposed.</exception>
        public IntPtr Address => this.hookImpl.Address;

        /// <summary>
        /// Gets a delegate function that can be used to call the actual function as if function is not hooked yet.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Hook is already disposed.</exception>
        public T Original => this.hookImpl.Original;

        /// <summary>
        /// Gets a value indicating whether or not the hook is enabled.
        /// </summary>
        public bool IsEnabled => this.hookImpl.IsEnabled;

        /// <summary>
        /// Gets a value indicating whether or not the hook has been disposed.
        /// </summary>
        public bool IsDisposed => this.hookImpl.IsDisposed;


        /// <summary>
        /// Creates a hook. Hooking address is inferred by calling to GetProcAddress() function.
        /// The hook is not activated until Enable() method is called.
        /// </summary>
        /// <param name="moduleName">A name of the module currently loaded in the memory. (e.g. ws2_32.dll).</param>
        /// <param name="exportName">A name of the exported function name (e.g. send).</param>
        /// <param name="detour">Callback function. Delegate must have a same original function prototype.</param>
        /// <returns>The hook with the supplied parameters.</returns>
        public static Hook<T> FromSymbol(string moduleName, string exportName, T detour)
        {
            if (HookManager.DirtyLinuxUser)
            {
                var moduleHandle = NativeFunctions.GetModuleHandleW(moduleName);
                if (moduleHandle == IntPtr.Zero)
                    throw new Exception($"Could not get a handle to module {moduleName}");

                var procAddress = NativeFunctions.GetProcAddress(moduleHandle, exportName);
                if (procAddress == IntPtr.Zero)
                    throw new Exception($"Could not get the address of {moduleName}::{exportName}");

                return new Hook<T>(procAddress, detour, true);
            }
            else
            {
                var address = CoreHook.LocalHook.GetProcAddress(moduleName, exportName);
                return new Hook<T>(address, detour);
            }
        }

        /// <summary>
        /// Remove a hook from the current process.
        /// </summary>
        public void Dispose() => this.hookImpl.Dispose();

        /// <summary>
        /// Starts intercepting a call to the function.
        /// </summary>
        public void Enable() => this.hookImpl.Enable();

        /// <summary>
        /// Stops intercepting a call to the function.
        /// </summary>
        public void Disable() => this.hookImpl.Disable();

    }
}
