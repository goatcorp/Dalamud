using System.Reflection;

namespace Dalamud.Hooking.Internal;

/// <summary>
/// Manages a hook with MinHook.
/// </summary>
/// <typeparam name="T">Delegate type to represents a function prototype. This must be the same prototype as original function do.</typeparam>
internal class MinHookHook<T> : Hook<T> where T : Delegate
{
    private readonly MinSharp.Hook<T> minHookImpl;

    /// <summary>
    /// Initializes a new instance of the <see cref="MinHookHook{T}"/> class.
    /// </summary>
    /// <param name="address">A memory address to install a hook.</param>
    /// <param name="detour">Callback function. Delegate must have a same original function prototype.</param>
    /// <param name="callingAssembly">Calling assembly.</param>
    internal MinHookHook(IntPtr address, T detour, Assembly callingAssembly)
        : base(address)
    {
        lock (HookManager.HookEnableSyncRoot)
        {
            var unhooker = HookManager.RegisterUnhooker(this.Address);

            if (!HookManager.MultiHookTracker.TryGetValue(this.Address, out var indexList))
                indexList = HookManager.MultiHookTracker[this.Address] = new();

            var index = (ulong)indexList.Count;

            this.minHookImpl = new MinSharp.Hook<T>(this.Address, detour, index);

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
            return this.minHookImpl.Original;
        }
    }

    /// <inheritdoc/>
    public override bool IsEnabled
    {
        get
        {
            this.CheckDisposed();
            return this.minHookImpl.Enabled;
        }
    }

    /// <inheritdoc/>
    public override string BackendName => "MinHook";

    /// <inheritdoc/>
    public override void Dispose()
    {
        if (this.IsDisposed)
            return;

        lock (HookManager.HookEnableSyncRoot)
        {
            this.minHookImpl.Dispose();

            var index = HookManager.MultiHookTracker[this.Address].IndexOf(this);
            HookManager.MultiHookTracker[this.Address][index] = null;
        }

        base.Dispose();
    }

    /// <inheritdoc/>
    public override void Enable()
    {
        this.CheckDisposed();

        if (!this.minHookImpl.Enabled)
        {
            lock (HookManager.HookEnableSyncRoot)
            {
                this.minHookImpl.Enable();
            }
        }
    }

    /// <inheritdoc/>
    public override void Disable()
    {
        this.CheckDisposed();

        if (this.minHookImpl.Enabled)
        {
            lock (HookManager.HookEnableSyncRoot)
            {
                this.minHookImpl.Disable();
            }
        }
    }
}
