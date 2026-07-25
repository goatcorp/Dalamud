using System.Reflection;
using System.Runtime.InteropServices;

namespace Dalamud.Hooking.Internal;

/// <summary>
/// Manages a hook with safetyhook, via the wrapper exported by Dalamud.Boot.
/// </summary>
/// <typeparam name="T">Delegate type to represents a function prototype. This must be the same prototype as original function do.</typeparam>
internal class SafetyHookHook<T> : Hook<T> where T : Delegate
{
    private readonly T detour;

    // SH does not write the hook until the first enable, so we have to store the unhooker
    private readonly Unhooker unhooker;

    private nint handle;

    /// <summary>
    /// Initializes a new instance of the <see cref="SafetyHookHook{T}"/> class.
    /// </summary>
    /// <param name="address">A memory address to install a hook.</param>
    /// <param name="detour">Callback function. Delegate must have a same original function prototype.</param>
    /// <param name="callingAssembly">Calling assembly.</param>
    internal SafetyHookHook(IntPtr address, T detour, Assembly callingAssembly)
        : base(address)
    {
        using var scope = HookManager.HookEnableSyncRoot.EnterScope();

        this.unhooker = HookManager.RegisterUnhooker(address);

        this.detour = detour;
        this.handle = SafetyHookNative.Create(address, Marshal.GetFunctionPointerForDelegate(detour), out var error);
        if (this.handle == 0)
            throw new InvalidOperationException($"Could not create safetyhook hook at 0x{address:X}: {error.Describe()}");

        // The trampoline lives for as long as the handle does, so it's safe to bind the delegate once here
        this.Original = Marshal.GetDelegateForFunctionPointer<T>(SafetyHookNative.GetTrampoline(this.handle));

        HookManager.TrackedHooks.TryAdd(this.HookId, new HookInfo(this, detour, callingAssembly));
    }

    /// <inheritdoc/>
    public override T Original
    {
        get
        {
            this.CheckDisposed();
            return field;
        }
    }

    /// <inheritdoc/>
    public override bool IsEnabled => !this.IsDisposed && SafetyHookNative.IsEnabled(this.handle) != 0;

    /// <inheritdoc/>
    public override string BackendName => "SafetyHook";

    /// <inheritdoc/>
    public override void Dispose()
    {
        if (this.IsDisposed)
            return;

        using var scope = HookManager.HookEnableSyncRoot.EnterScope();

        HookManager.TrackedHooks.TryRemove(this.HookId, out _);

        // Restores the target function and frees the trampoline, so nothing may touch the handle afterwards
        SafetyHookNative.Destroy(this.handle);
        this.handle = 0;

        // The detour must outlive the hook that jumps to it, and nothing else references it past this point
        GC.KeepAlive(this.detour);

        base.Dispose();
    }

    /// <inheritdoc/>
    public override void Enable()
    {
        using var scope = HookManager.HookEnableSyncRoot.EnterScope();

        this.CheckDisposed();

        if (SafetyHookNative.Enable(this.handle, out var error) == 0)
            throw new InvalidOperationException($"Could not enable safetyhook hook at 0x{this.Address:X}: {error.Describe()}");

        // SH only patches the target function now, so this is the first point at which the unhooker can tell
        // how many bytes it would have to restored
        this.unhooker.TrimAfterHook();
    }

    /// <inheritdoc/>
    public override void Disable()
    {
        using var scope = HookManager.HookEnableSyncRoot.EnterScope();

        if (this.IsDisposed)
            return;

        if (SafetyHookNative.Disable(this.handle, out var error) == 0)
            throw new InvalidOperationException($"Could not disable safetyhook hook at 0x{this.Address:X}: {error.Describe()}");
    }
}
