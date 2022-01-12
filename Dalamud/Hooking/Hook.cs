using System;
using System.Collections.Generic;
using System.Reflection;

using Dalamud.Configuration.Internal;
using Dalamud.Hooking.Internal;
using Dalamud.Memory;
using Reloaded.Hooks;

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
        private readonly Reloaded.Hooks.Definitions.IHook<T> hookImpl;
        private readonly MinSharp.Hook<T> minHookImpl;
        private readonly bool isMinHook;

        /// <summary>
        /// Initializes a new instance of the <see cref="Hook{T}"/> class.
        /// Hook is not activated until Enable() method is called.
        /// </summary>
        /// <param name="address">A memory address to install a hook.</param>
        /// <param name="detour">Callback function. Delegate must have a same original function prototype.</param>
        public Hook(IntPtr address, T detour)
            : this(address, detour, false, Assembly.GetCallingAssembly())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Hook{T}"/> class.
        /// Hook is not activated until Enable() method is called.
        /// Please do not use MinHook unless you have thoroughly troubleshot why Reloaded does not work.
        /// </summary>
        /// <param name="address">A memory address to install a hook.</param>
        /// <param name="detour">Callback function. Delegate must have a same original function prototype.</param>
        /// <param name="useMinHook">Use the MinHook hooking library instead of Reloaded.</param>
        public Hook(IntPtr address, T detour, bool useMinHook)
            : this(address, detour, useMinHook, Assembly.GetCallingAssembly())
        {
        }

        private Hook(IntPtr address, T detour, bool useMinHook, Assembly callingAssembly)
        {
            address = HookManager.FollowJmp(address);
            this.isMinHook = !EnvironmentConfiguration.DalamudForceReloaded && (EnvironmentConfiguration.DalamudForceMinHook || useMinHook);

            var hasOtherHooks = HookManager.Originals.ContainsKey(address);
            if (!hasOtherHooks)
            {
                MemoryHelper.ReadRaw(address, 0x32, out var original);
                HookManager.Originals[address] = original;
            }

            this.address = address;
            if (this.isMinHook)
            {
                if (!HookManager.MultiHookTracker.TryGetValue(address, out var indexList))
                    indexList = HookManager.MultiHookTracker[address] = new();

                var index = (ulong)indexList.Count;

                this.minHookImpl = new MinSharp.Hook<T>(address, detour, index);

                // Add afterwards, so the hookIdent starts at 0.
                indexList.Add(this);
            }
            else
            {
                this.hookImpl = ReloadedHooks.Instance.CreateHook<T>(detour, address.ToInt64());
            }

            HookManager.TrackedHooks.TryAdd(Guid.NewGuid(), new HookInfo(this, detour, callingAssembly));
        }

        /// <summary>
        /// Gets a memory address of the target function.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Hook is already disposed.</exception>
        public IntPtr Address
        {
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
            get
            {
                this.CheckDisposed();
                if (this.isMinHook)
                {
                    return this.minHookImpl.Original;
                }
                else
                {
                    return this.hookImpl.OriginalFunction;
                }
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
                if (this.isMinHook)
                {
                    return this.minHookImpl.Enabled;
                }
                else
                {
                    return this.hookImpl.IsHookEnabled;
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether or not the hook has been disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <inheritdoc/>
        public string BackendName
        {
            get
            {
                if (this.isMinHook)
                    return "MinHook";

                return "Reloaded";
            }
        }

        /// <summary>
        /// Creates a hook. Hooking address is inferred by calling to GetProcAddress() function.
        /// The hook is not activated until Enable() method is called.
        /// </summary>
        /// <param name="moduleName">A name of the module currently loaded in the memory. (e.g. ws2_32.dll).</param>
        /// <param name="exportName">A name of the exported function name (e.g. send).</param>
        /// <param name="detour">Callback function. Delegate must have a same original function prototype.</param>
        /// <returns>The hook with the supplied parameters.</returns>
        public static Hook<T> FromSymbol(string moduleName, string exportName, T detour)
            => FromSymbol(moduleName, exportName, detour, false);

        /// <summary>
        /// Creates a hook. Hooking address is inferred by calling to GetProcAddress() function.
        /// The hook is not activated until Enable() method is called.
        /// Please do not use MinHook unless you have thoroughly troubleshot why Reloaded does not work.
        /// </summary>
        /// <param name="moduleName">A name of the module currently loaded in the memory. (e.g. ws2_32.dll).</param>
        /// <param name="exportName">A name of the exported function name (e.g. send).</param>
        /// <param name="detour">Callback function. Delegate must have a same original function prototype.</param>
        /// <param name="useMinHook">Use the MinHook hooking library instead of Reloaded.</param>
        /// <returns>The hook with the supplied parameters.</returns>
        public static Hook<T> FromSymbol(string moduleName, string exportName, T detour, bool useMinHook)
        {
            var moduleHandle = NativeFunctions.GetModuleHandleW(moduleName);
            if (moduleHandle == IntPtr.Zero)
                throw new Exception($"Could not get a handle to module {moduleName}");

            var procAddress = NativeFunctions.GetProcAddress(moduleHandle, exportName);
            if (procAddress == IntPtr.Zero)
                throw new Exception($"Could not get the address of {moduleName}::{exportName}");

            return new Hook<T>(procAddress, detour, useMinHook);
        }

        /// <summary>
        /// Remove a hook from the current process.
        /// </summary>
        public void Dispose()
        {
            if (this.IsDisposed)
                return;

            if (this.isMinHook)
            {
                this.minHookImpl.Dispose();

                var index = HookManager.MultiHookTracker[this.address].IndexOf(this);
                HookManager.MultiHookTracker[this.address][index] = null;
            }
            else
            {
                this.Disable();
            }

            this.IsDisposed = true;
        }

        /// <summary>
        /// Starts intercepting a call to the function.
        /// </summary>
        public void Enable()
        {
            this.CheckDisposed();

            if (this.isMinHook)
            {
                if (!this.minHookImpl.Enabled)
                {
                    this.minHookImpl.Enable();
                }
            }
            else
            {
                if (!this.hookImpl.IsHookActivated)
                    this.hookImpl.Activate();

                if (!this.hookImpl.IsHookEnabled)
                    this.hookImpl.Enable();
            }
        }

        /// <summary>
        /// Stops intercepting a call to the function.
        /// </summary>
        public void Disable()
        {
            this.CheckDisposed();

            if (this.isMinHook)
            {
                if (this.minHookImpl.Enabled)
                {
                    this.minHookImpl.Disable();
                }
            }
            else
            {
                if (!this.hookImpl.IsHookActivated)
                    return;

                if (this.hookImpl.IsHookEnabled)
                    this.hookImpl.Disable();
            }
        }

        /// <summary>
        /// Check if this object has been disposed already.
        /// </summary>
        private void CheckDisposed()
        {
            if (this.IsDisposed)
            {
                throw new ObjectDisposedException(message: "Hook is already disposed", null);
            }
        }
    }
}
