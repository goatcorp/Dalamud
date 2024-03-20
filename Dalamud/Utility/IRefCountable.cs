using System.Diagnostics;
using System.Threading;

namespace Dalamud.Utility;

/// <summary>
/// Interface for reference counting.
/// </summary>
internal interface IRefCountable : IDisposable
{
    /// <summary>
    /// Result for <see cref="IRefCountable.AlterRefCount"/>.
    /// </summary>
    public enum RefCountResult
    {
        /// <summary>
        /// The object still has remaining references. No futher action should be done.
        /// </summary>
        StillAlive = 1,

        /// <summary>
        /// The last reference to the object has been released. The object should be fully released.
        /// </summary>
        FinalRelease = 2,

        /// <summary>
        /// The object already has been disposed. <see cref="ObjectDisposedException"/> may be thrown.
        /// </summary>
        AlreadyDisposed = 3,
    }

    /// <summary>
    /// Adds a reference to this reference counted object.
    /// </summary>
    /// <returns>The new number of references.</returns>
    int AddRef();

    /// <summary>
    /// Releases a reference from this reference counted object.<br />
    /// When all references are released, the object will be fully disposed.
    /// </summary>
    /// <returns>The new number of references.</returns>
    int Release();

    /// <summary>
    /// Alias for <see cref="Release()"/>.
    /// </summary>
    void IDisposable.Dispose() => this.Release();

    /// <summary>
    /// Alters <paramref name="refCount"/> by <paramref name="delta"/>.
    /// </summary>
    /// <param name="delta">The delta to the reference count.</param>
    /// <param name="refCount">The reference to the reference count.</param>
    /// <param name="newRefCount">The new reference count.</param>
    /// <returns>The followup action that should be done.</returns>
    public static RefCountResult AlterRefCount(int delta, ref int refCount, out int newRefCount)
    {
        Debug.Assert(delta is 1 or -1, "delta must be 1 or -1");

        while (true)
        {
            var refCountCopy = refCount;
            if (refCountCopy <= 0)
            {
                newRefCount = refCountCopy;
                return RefCountResult.AlreadyDisposed;
            }

            newRefCount = refCountCopy + delta;
            if (refCountCopy != Interlocked.CompareExchange(ref refCount, newRefCount, refCountCopy))
                continue;

            return newRefCount == 0 ? RefCountResult.FinalRelease : RefCountResult.StillAlive;
        }
    }
}
