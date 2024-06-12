using System.Reflection;

using Reloaded.Hooks;

namespace Dalamud.Hooking.Internal;

/// <summary>
/// Class facilitating hooks via reloaded.
/// </summary>
/// <typeparam name="T">Delegate of the hook.</typeparam>
internal class ReloadedHook<T> : Hook<T> where T : Delegate
{
    private readonly Reloaded.Hooks.Definitions.IHook<T> hookImpl;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReloadedHook{T}"/> class.
    /// </summary>
    /// <param name="address">A memory address to install a hook.</param>
    /// <param name="detour">Callback function. Delegate must have a same original function prototype.</param>
    internal ReloadedHook(IntPtr address, T detour)
        : base(address)
    {
        lock (HookManager.HookSyncRoot)
        {
            var unhooker = HookManager.RegisterUnhooker(address);
            this.hookImpl = ReloadedHooks.Instance.CreateHook<T>(detour, address.ToInt64());
            this.hookImpl.Activate();
            this.hookImpl.Disable();

            unhooker.TrimAfterHook();
        }
    }

    /// <inheritdoc/>
    public override T Original
    {
        get
        {
            this.CheckDisposed();
            return this.hookImpl.OriginalFunction;
        }
    }

    /// <inheritdoc/>
    public override bool IsEnabled
    {
        get
        {
            this.CheckDisposed();
            return this.hookImpl.IsHookEnabled;
        }
    }

    /// <inheritdoc/>
    public override string BackendName => "Reloaded";

    /// <inheritdoc/>
    public override void Dispose()
    {
        if (this.IsDisposed)
            return;

        this.Disable();

        base.Dispose();
    }

    /// <inheritdoc/>
    public override void Enable()
    {
        this.CheckDisposed();

        lock (HookManager.HookSyncRoot)
        {
            if (!this.hookImpl.IsHookEnabled)
                this.hookImpl.Enable();
        }
    }

    /// <inheritdoc/>
    public override void Disable()
    {
        this.CheckDisposed();

        lock (HookManager.HookSyncRoot)
        {
            if (!this.hookImpl.IsHookActivated)
                return;

            if (this.hookImpl.IsHookEnabled)
                this.hookImpl.Disable();
        }
    }
}
