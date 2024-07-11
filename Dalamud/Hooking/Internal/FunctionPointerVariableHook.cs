using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;

using Dalamud.Memory;
using JetBrains.Annotations;

namespace Dalamud.Hooking.Internal;

/// <summary>
/// Manages a hook with MinHook.
/// </summary>
/// <typeparam name="T">Delegate type to represents a function prototype. This must be the same prototype as original function do.</typeparam>
internal class FunctionPointerVariableHook<T> : Hook<T>
    where T : Delegate
{
    private readonly nint pfnDetour;

    // Keep it referenced so that pfnDetour doesn't become invalidated.
    [UsedImplicitly]
    private readonly T detourDelegate;

    private readonly nint pfnThunk;
    private readonly nint ppfnThunkJumpTarget;

    private readonly nint pfnOriginal;
    private readonly T originalDelegate;

    private bool enabled;

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
            var unhooker = HookManager.RegisterUnhooker(this.Address, 8, 8);

            if (!HookManager.MultiHookTracker.TryGetValue(this.Address, out var indexList))
            {
                indexList = HookManager.MultiHookTracker[this.Address] = new List<IDalamudHook>();
            }

            this.detourDelegate = detour;
            this.pfnDetour = Marshal.GetFunctionPointerForDelegate(detour);

            unsafe
            {
                // Note: WINE seemingly tries to clean up all heap allocations on process exit.
                // We want our allocation to be kept there forever, until no running thread remains.
                // Therefore we're using VirtualAlloc instead of HeapCreate/HeapAlloc.
                var pfnThunkBytes = (byte*)NativeFunctions.VirtualAlloc(
                    0,
                    12,
                    NativeFunctions.AllocationType.Reserve | NativeFunctions.AllocationType.Commit,
                    MemoryProtection.ExecuteReadWrite);
                if (pfnThunkBytes == null)
                {
                    throw new OutOfMemoryException("Failed to allocate memory for import hooks.");
                }

                // movabs rax, imm
                pfnThunkBytes[0] = 0x48;
                pfnThunkBytes[1] = 0xB8;

                // jmp rax
                pfnThunkBytes[10] = 0xFF;
                pfnThunkBytes[11] = 0xE0;

                this.pfnThunk = (nint)pfnThunkBytes;
            }

            this.ppfnThunkJumpTarget = this.pfnThunk + 2;

            if (!NativeFunctions.VirtualProtect(
                    this.Address,
                    (UIntPtr)Marshal.SizeOf<IntPtr>(),
                    MemoryProtection.ExecuteReadWrite,
                    out var oldProtect))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            this.pfnOriginal = Marshal.ReadIntPtr(this.Address);
            this.originalDelegate = Marshal.GetDelegateForFunctionPointer<T>(this.pfnOriginal);
            Marshal.WriteIntPtr(this.ppfnThunkJumpTarget, this.pfnOriginal);
            Marshal.WriteIntPtr(this.Address, this.pfnThunk);

            // This really should not fail, but then even if it does, whatever.
            NativeFunctions.VirtualProtect(this.Address, (UIntPtr)Marshal.SizeOf<IntPtr>(), oldProtect, out _);

            // Add afterwards, so the hookIdent starts at 0.
            indexList.Add(this);

            unhooker.TrimAfterHook();

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
        {
            return;
        }

        this.Disable();

        var index = HookManager.MultiHookTracker[this.Address].IndexOf(this);
        HookManager.MultiHookTracker[this.Address][index] = null;

        base.Dispose();
    }

    /// <inheritdoc/>
    public override void Enable()
    {
        this.CheckDisposed();

        if (this.enabled)
        {
            return;
        }

        lock (HookManager.HookEnableSyncRoot)
        {
            Marshal.WriteIntPtr(this.ppfnThunkJumpTarget, this.pfnDetour);
            this.enabled = true;
        }
    }

    /// <inheritdoc/>
    public override void Disable()
    {
        this.CheckDisposed();

        if (!this.enabled)
        {
            return;
        }

        lock (HookManager.HookEnableSyncRoot)
        {
            Marshal.WriteIntPtr(this.ppfnThunkJumpTarget, this.pfnOriginal);
            this.enabled = false;
        }
    }
}
