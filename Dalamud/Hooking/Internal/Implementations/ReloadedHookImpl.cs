using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

using Dalamud.Memory;

namespace Dalamud.Hooking.Internal.Implementations
{
    /// <summary>
    /// Manages a hook which can be used to intercept a call to native function.
    /// This class is basically a thin wrapper around the LocalHook type to provide helper functions.
    /// </summary>
    /// <typeparam name="T">Delegate type to represents a function prototype. This must be the same prototype as original function do.</typeparam>
    internal sealed class ReloadedHookImpl<T> : IDalamudHookImpl<T> where T : Delegate
    {
        private readonly IntPtr address;
        private readonly Reloaded.Hooks.Definitions.IHook<T> hookImpl;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReloadedHookImpl{T}"/> class.
        /// Hook is not activated until Enable() method is called.
        /// </summary>
        /// <param name="address">A memory address to install a hook.</param>
        /// <param name="detour">Callback function. Delegate must have a same original function prototype.</param>
        public ReloadedHookImpl(IntPtr address, T detour)
            : this(address, detour, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReloadedHookImpl{T}"/> class.
        /// Hook is not activated until Enable() method is called.
        /// </summary>
        /// <param name="address">A memory address to install a hook.</param>
        /// <param name="detour">Callback function. Delegate must have a same original function prototype.</param>
        /// <param name="followJmp">Follow any JMPs to the actual method that needs hooking.</param>
        public ReloadedHookImpl(IntPtr address, T detour, bool followJmp)
        {
            if (followJmp)
            {
                // This is horrible hackery to follow various types of JMP.
                // It likely needs to stop when entering a reloaded hook trampoline.
                // I would much rather use Iced to check against a Instruction type.
                while (true)
                {
                    var b1 = Marshal.ReadByte(address);
                    if (b1 == 0xE9)
                    {
                        var jumpOffset = Marshal.ReadInt32(address + 1);
                        address += jumpOffset + 5;
                        continue;
                    }

                    var b2 = Marshal.ReadByte(address, 1);
                    if (b1 == 0xFF && b2 == 0x25)
                    {
                        address = Marshal.ReadIntPtr(address + 6);
                        continue;
                    }

                    break;
                }
            }

            var otherHook = HookManager.Originals.FirstOrDefault(o => o.Address == address);
            if (otherHook == default)
            {
                MemoryHelper.ReadRaw(address, 50, out var original);
                HookManager.Originals.Add((address, original));
            }

            this.address = address;
            this.hookImpl = Reloaded.Hooks.ReloadedHooks.Instance.CreateHook<T>(detour, address.ToInt64());

            HookManager.TrackedHooks.Add(new HookInfo(this, detour, Assembly.GetCallingAssembly()));
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
                return this.hookImpl.OriginalFunction;
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
                return this.hookImpl.IsHookEnabled;
            }
        }

        /// <summary>
        /// Gets a value indicating whether or not the hook has been disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Creates a hook. Hooking address is inferred by calling to GetProcAddress() function.
        /// The hook is not activated until Enable() method is called.
        /// </summary>
        /// <param name="moduleName">A name of the module currently loaded in the memory. (e.g. ws2_32.dll).</param>
        /// <param name="exportName">A name of the exported function name (e.g. send).</param>
        /// <param name="detour">Callback function. Delegate must have a same original function prototype.</param>
        /// <returns>The hook with the supplied parameters.</returns>
        public static IDalamudHookImpl<T> FromSymbol(string moduleName, string exportName, T detour)
        {
            var moduleHandle = NativeFunctions.GetModuleHandleW(moduleName);
            if (moduleHandle == IntPtr.Zero)
                throw new Exception($"Could not get a handle to module {moduleName}");

            var procAddress = NativeFunctions.GetProcAddress(moduleHandle, exportName);
            if (procAddress == IntPtr.Zero)
                throw new Exception($"Could not get the address of {moduleName}::{exportName}");

            return new ReloadedHookImpl<T>(procAddress, detour, true);
        }

        /// <summary>
        /// Remove a hook from the current process.
        /// </summary>
        public void Dispose()
        {
            if (this.IsDisposed)
                return;

            this.IsDisposed = true;

            if (this.hookImpl.IsHookEnabled)
                this.hookImpl.Disable();
        }

        /// <summary>
        /// Starts intercepting a call to the function.
        /// </summary>
        public void Enable()
        {
            this.CheckDisposed();

            if (!this.hookImpl.IsHookActivated)
                this.hookImpl.Activate();

            if (!this.hookImpl.IsHookEnabled)
                this.hookImpl.Enable();
        }

        /// <summary>
        /// Stops intercepting a call to the function.
        /// </summary>
        public void Disable()
        {
            this.CheckDisposed();

            if (!this.hookImpl.IsHookActivated)
                return;

            if (this.hookImpl.IsHookEnabled)
                this.hookImpl.Disable();
        }

        /// <summary>
        /// Check if this object has been disposed already.
        /// </summary>
        private void CheckDisposed()
        {
            if (this.IsDisposed)
            {
                throw new ObjectDisposedException("Hook is already disposed.");
            }
        }
    }
}
