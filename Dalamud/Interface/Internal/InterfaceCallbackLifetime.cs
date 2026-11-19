using System.Threading;

namespace Dalamud.Interface.Internal;

/// <summary>Tracks interface callbacks until their native continuations have returned.</summary>
internal sealed class InterfaceCallbackLifetime
{
    // Monitor.Wait releases this lock so callbacks can exit while shutdown waits.
    private readonly object syncRoot = new();
    private int activeCallbacks;
    private bool stopping;
    private bool drained;

    /// <summary>Enters a callback before or during shutdown.</summary>
    /// <returns>A scope indicating whether hook resources are retained and backend access is permitted.</returns>
    public Scope Enter()
    {
        lock (this.syncRoot)
        {
            if (this.drained)
                return default;

            this.activeCallbacks++;
            return new(this, !this.stopping);
        }
    }

    /// <summary>Rejects backend work for subsequent callbacks without waiting for active callbacks.</summary>
    public void Stop()
    {
        lock (this.syncRoot)
            this.stopping = true;
    }

    /// <summary>Waits for tracked callbacks to return, then makes later entries untracked.</summary>
    /// <remarks>
    /// Call after <see cref="Stop"/> and after disabling hooks and detaching event handlers. Do not call from a
    /// tracked callback, on the framework thread, or while holding graphics or native-framework exclusion locks:
    /// an active callback or its native continuation may need those threads and locks to finish.
    /// Later callbacks must use a retained continuation that does not depend on disposed hook resources.
    /// </remarks>
    public void WaitForDrain()
    {
        lock (this.syncRoot)
        {
            while (this.activeCallbacks != 0)
                Monitor.Wait(this.syncRoot);

            this.drained = true;
        }
    }

    private void Exit()
    {
        lock (this.syncRoot)
        {
            if (--this.activeCallbacks == 0)
                Monitor.PulseAll(this.syncRoot);
        }
    }

    /// <summary>Keeps hook resources alive through a callback's native continuation.</summary>
    /// <remarks>Dispose exactly once on leaving the callback; do not copy and dispose additional copies.</remarks>
    internal readonly ref struct Scope
    {
        private readonly InterfaceCallbackLifetime? owner;

        /// <summary>Initializes a new instance of the <see cref="Scope"/> struct.</summary>
        /// <param name="owner">The lifetime tracking the callback.</param>
        /// <param name="canUseBackend">Whether this callback may access interface resources.</param>
        internal Scope(InterfaceCallbackLifetime owner, bool canUseBackend)
        {
            this.owner = owner;
            this.CanUseBackend = canUseBackend;
        }

        /// <summary>Gets a value indicating whether hook resources are retained for this callback.</summary>
        public bool IsTracked => this.owner is not null;

        /// <summary>Gets a value indicating whether this callback may access interface resources.</summary>
        public bool CanUseBackend { get; }

        /// <summary>Releases this callback after its native continuation has returned.</summary>
        public void Dispose() => this.owner?.Exit();
    }
}
