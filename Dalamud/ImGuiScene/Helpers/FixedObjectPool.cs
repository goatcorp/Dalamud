using System.Threading;

namespace Dalamud.ImGuiScene.Helpers;

/// <summary>
/// Pool of <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The object type contained within. If it is an <see cref="IDisposable"/>, objects will be disposed along with this pool.</typeparam>
internal class FixedObjectPool<T> : IDisposable
    where T : class
{
    private readonly Func<int, T> creator;
    private readonly bool noExtraAllocation;
    private readonly T?[] objects;
    private int allocated;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixedObjectPool{T}"/> class.
    /// </summary>
    /// <param name="creator">The creator.</param>
    /// <param name="capacity">The initial capacity. Non-positive number means <see cref="Environment.ProcessorCount"/>.</param>
    /// <param name="noExtraAllocation">If set to true, <see cref="Rent"/> will wait if no pooled object is available.</param>
    public FixedObjectPool(Func<int, T> creator, int capacity = 0, bool noExtraAllocation = false)
    {
        if (capacity <= 0)
            capacity = Environment.ProcessorCount;

        this.creator = creator;
        this.noExtraAllocation = noExtraAllocation;
        this.objects = new T[capacity];
    }

    /// <summary>
    /// Gets the capacity of this <see cref="FixedObjectPool{T}"/>.
    /// </summary>
    public int Capacity => this.objects.Length;

    /// <inheritdoc/>
    public void Dispose()
    {
        if (this.disposed)
            return;

        this.disposed = true;
        foreach (ref var h in this.objects.AsSpan())
        {
            if (Interlocked.Exchange(ref h, default) is { } obj)
                (obj as IDisposable)?.Dispose();
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Tries to rent an object from this event pool, without allocating a new object nor waiting.
    /// </summary>
    /// <param name="returner">A disposable, with the object accessible via a field.</param>
    /// <param name="beforeReturn">The action to be executed before return. Must not throw.</param>
    /// <returns>True if successful.</returns>
    public bool TryRent(out Returner returner, Action? beforeReturn = null)
    {
        ObjectDisposedException.ThrowIf(this.disposed, this);
        
        foreach (ref var h in this.objects.AsSpan())
        {
            var @object = Interlocked.Exchange(ref h, default);
            if (@object != default)
            {
                returner = new(@object, this, beforeReturn);
                return true;
            }
        }

        for (var known = this.allocated; known < this.Capacity; known = this.allocated)
        {
            if (known == Interlocked.CompareExchange(ref this.allocated, known + 1, known))
            {
                returner = new(this.creator(known), this, beforeReturn);
                return true;
            }
        }

        returner = default;
        return false;
    }

    /// <summary>
    /// Rents an object from this event pool, allocating a new object as necessary and if allowed.
    /// </summary>
    /// <param name="beforeReturn">The action to be executed before return. Must not throw.</param>
    /// <returns>A disposable, with the object accessible via a field.</returns>
    public Returner Rent(Action? beforeReturn = null)
    {
        if (this.TryRent(out var r, beforeReturn))
            return r;

        if (!this.noExtraAllocation)
            return new(this.creator(-1), this, beforeReturn);

        while (true)
        {
            lock (this.objects)
                Monitor.Wait(this.objects);
            if (this.TryRent(out r, beforeReturn))
                return r;
        }
    }

    /// <summary>
    /// Manually returns the object.<br />
    /// <strong>Warning</strong>: The beforeReturn parameter provided to <see cref="Rent"/> will be ignored.
    /// </summary>
    /// <param name="object">The returning object.</param>
    public void Return(T @object)
    {
        if (!this.disposed)
        {
            foreach (ref var h in this.objects.AsSpan())
            {
                @object = Interlocked.Exchange(ref h, @object);
                if (@object == default)
                {
                    lock (this.objects)
                        Monitor.Pulse(this.objects);
                    return;
                }
            }
        }

        (@object as IDisposable)?.Dispose();
    }

    /// <summary>
    /// A struct that must be disposed after use.
    /// </summary>
    public struct Returner : IDisposable
    {
        private FixedObjectPool<T>? pool;
        private Action? beforeReturn;

        /// <summary>
        /// Initializes a new instance of the <see cref="Returner"/> struct.
        /// </summary>
        /// <param name="object">The handle to contain.</param>
        /// <param name="pool">The pool to return to.</param>
        /// <param name="beforeReturn">Optional action to be executed before returning.</param>
        public Returner(T @object, FixedObjectPool<T> pool, Action? beforeReturn)
        {
            this.O = @object;
            this.pool = pool;
            this.beforeReturn = beforeReturn;
        }

        /// <summary>
        /// Gets the handle.
        /// </summary>
        public T O { get; }

        public static implicit operator T(Returner p) => p.O;
        
        /// <inheritdoc/>
        public void Dispose()
        {
            try
            {
                this.beforeReturn?.Invoke();
            }
            finally
            {
                this.beforeReturn = null;
                this.pool?.Return(this.O);
                this.pool = null;
            }
        }
    }
}
