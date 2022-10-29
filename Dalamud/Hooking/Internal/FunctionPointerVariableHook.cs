using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;

using Dalamud.Memory;

namespace Dalamud.Hooking.Internal;

/// <summary>
/// Manages a hook with MinHook.
/// </summary>
/// <typeparam name="T">Delegate type to represents a function prototype. This must be the same prototype as original function do.</typeparam>
internal class FunctionPointerVariableHook<T> : Hook<T> where T : Delegate
{
    private readonly IntPtr pfnOriginal;
    private readonly T originalDelegate;
    private readonly T detourDelegate;

    private bool enabled = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="FunctionPointerVariableHook{T}"/> class.
    /// </summary>
    /// <param name="address">A memory address to install a hook.</param>
    /// <param name="detour">Callback function. Delegate must have a same original function prototype.</param>
    /// <param name="callingAssembly">Calling assembly.</param>
    internal FunctionPointerVariableHook(IntPtr address, T detour, Assembly callingAssembly)
        : base(address)
    {
        lock (HookManager.HookEnableSyncRoot)
        {
            var hasOtherHooks = HookManager.Originals.ContainsKey(this.Address);
            if (!hasOtherHooks)
            {
                MemoryHelper.ReadRaw(this.Address, 0x32, out var original);
                HookManager.Originals[this.Address] = original;
            }

            if (!HookManager.MultiHookTracker.TryGetValue(this.Address, out var indexList))
                indexList = HookManager.MultiHookTracker[this.Address] = new();

            this.pfnOriginal = Marshal.ReadIntPtr(this.Address);
            this.originalDelegate = Marshal.GetDelegateForFunctionPointer<T>(this.pfnOriginal);
            this.detourDelegate = detour;

            // Add afterwards, so the hookIdent starts at 0.
            indexList.Add(this);

            HookManager.TrackedHooks.TryAdd(Guid.NewGuid(), new HookInfo(this, detour, callingAssembly));
        }
    }

    /// <inheritdoc/>
    public override T Original
    {
        get
        {
            this.CheckDisposed();
            return this.originalDelegate;
        }
    }

    /// <inheritdoc/>
    public override bool IsEnabled
    {
        get
        {
            this.CheckDisposed();
            return this.enabled;
        }
    }

    /// <inheritdoc/>
    public override string BackendName => "MinHook";

    /// <inheritdoc/>
    public override void Dispose()
    {
        if (this.IsDisposed)
            return;

        this.Disable();

        var index = HookManager.MultiHookTracker[this.Address].IndexOf(this);
        HookManager.MultiHookTracker[this.Address][index] = null;

        base.Dispose();
    }

    /// <inheritdoc/>
    public override void Enable()
    {
        this.CheckDisposed();

        if (!this.enabled)
        {
            lock (HookManager.HookEnableSyncRoot)
            {
                if (!NativeFunctions.VirtualProtect(this.Address, (UIntPtr)Marshal.SizeOf<IntPtr>(), MemoryProtection.ExecuteReadWrite, out var oldProtect))
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                Marshal.WriteIntPtr(this.Address, Marshal.GetFunctionPointerForDelegate(this.detourDelegate));
                NativeFunctions.VirtualProtect(this.Address, (UIntPtr)Marshal.SizeOf<IntPtr>(), oldProtect, out _);
            }
        }
    }

    /// <inheritdoc/>
    public override void Disable()
    {
        this.CheckDisposed();

        if (this.enabled)
        {
            lock (HookManager.HookEnableSyncRoot)
            {
                if (!NativeFunctions.VirtualProtect(this.Address, (UIntPtr)Marshal.SizeOf<IntPtr>(), MemoryProtection.ExecuteReadWrite, out var oldProtect))
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                Marshal.WriteIntPtr(this.Address, this.pfnOriginal);
                NativeFunctions.VirtualProtect(this.Address, (UIntPtr)Marshal.SizeOf<IntPtr>(), oldProtect, out _);
            }
        }
    }
}
