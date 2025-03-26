using System.Threading;

namespace Dalamud.Utility;

/// <summary>
/// Scope for plugin list locks.
/// </summary>
public class ScopedSyncRoot : IDisposable
{
    private readonly object lockObj;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScopedSyncRoot"/> class.
    /// </summary>
    /// <param name="lockObj">The object to lock.</param>
    public ScopedSyncRoot(object lockObj)
    {
        this.lockObj = lockObj;
        Monitor.Enter(lockObj);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Monitor.Exit(this.lockObj);
    }
}
