using System.Runtime.CompilerServices;
using System.Threading;

using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;

namespace Dalamud.Game.Addon;

/// <summary>Argument pool for Addon Lifecycle services.</summary>
[ServiceManager.EarlyLoadedService]
internal sealed class AddonLifecyclePooledArgs : IServiceType
{
    private readonly AddonSetupArgs?[] addonSetupArgPool = new AddonSetupArgs?[64];
    private readonly AddonFinalizeArgs?[] addonFinalizeArgPool = new AddonFinalizeArgs?[64];
    private readonly AddonDrawArgs?[] addonDrawArgPool = new AddonDrawArgs?[64];
    private readonly AddonUpdateArgs?[] addonUpdateArgPool = new AddonUpdateArgs?[64];
    private readonly AddonRefreshArgs?[] addonRefreshArgPool = new AddonRefreshArgs?[64];
    private readonly AddonRequestedUpdateArgs?[] addonRequestedUpdateArgPool = new AddonRequestedUpdateArgs?[64];
    private readonly AddonReceiveEventArgs?[] addonReceiveEventArgPool = new AddonReceiveEventArgs?[64];

    [ServiceManager.ServiceConstructor]
    private AddonLifecyclePooledArgs()
    {
    }

    /// <summary>Rents an instance of an argument.</summary>
    /// <param name="arg">The rented instance.</param>
    /// <returns>The returner.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledEntry<AddonSetupArgs> Rent(out AddonSetupArgs arg) => new(out arg, this.addonSetupArgPool);

    /// <summary>Rents an instance of an argument.</summary>
    /// <param name="arg">The rented instance.</param>
    /// <returns>The returner.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledEntry<AddonFinalizeArgs> Rent(out AddonFinalizeArgs arg) => new(out arg, this.addonFinalizeArgPool);

    /// <summary>Rents an instance of an argument.</summary>
    /// <param name="arg">The rented instance.</param>
    /// <returns>The returner.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledEntry<AddonDrawArgs> Rent(out AddonDrawArgs arg) => new(out arg, this.addonDrawArgPool);

    /// <summary>Rents an instance of an argument.</summary>
    /// <param name="arg">The rented instance.</param>
    /// <returns>The returner.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledEntry<AddonUpdateArgs> Rent(out AddonUpdateArgs arg) => new(out arg, this.addonUpdateArgPool);

    /// <summary>Rents an instance of an argument.</summary>
    /// <param name="arg">The rented instance.</param>
    /// <returns>The returner.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledEntry<AddonRefreshArgs> Rent(out AddonRefreshArgs arg) => new(out arg, this.addonRefreshArgPool);

    /// <summary>Rents an instance of an argument.</summary>
    /// <param name="arg">The rented instance.</param>
    /// <returns>The returner.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledEntry<AddonRequestedUpdateArgs> Rent(out AddonRequestedUpdateArgs arg) =>
        new(out arg, this.addonRequestedUpdateArgPool);

    /// <summary>Rents an instance of an argument.</summary>
    /// <param name="arg">The rented instance.</param>
    /// <returns>The returner.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledEntry<AddonReceiveEventArgs> Rent(out AddonReceiveEventArgs arg) =>
        new(out arg, this.addonReceiveEventArgPool);

    /// <summary>Returns the object to the pool on dispose.</summary>
    /// <typeparam name="T">The type.</typeparam>
    public readonly ref struct PooledEntry<T>
        where T : AddonArgs, new()
    {
        private readonly Span<T> pool;
        private readonly T obj;

        /// <summary>Initializes a new instance of the <see cref="PooledEntry{T}"/> struct.</summary>
        /// <param name="arg">An instance of the argument.</param>
        /// <param name="pool">The pool to rent from and return to.</param>
        public PooledEntry(out T arg, Span<T> pool)
        {
            this.pool = pool;
            foreach (ref var item in pool)
            {
                if (Interlocked.Exchange(ref item, null) is { } v)
                {
                    this.obj = arg = v;
                    return;
                }
            }

            this.obj = arg = new();
        }

        /// <summary>Returns the item to the pool.</summary>
        public void Dispose()
        {
            var tmp = this.obj;
            foreach (ref var item in this.pool)
            {
                if (Interlocked.Exchange(ref item, tmp) is not { } tmp2)
                    return;
                tmp = tmp2;
            }
        }
    }
}
