using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Plugin.Services;

namespace Dalamud.Storage;

#pragma warning disable Dalamud001

/// <summary>
/// Plugin-scoped VFS wrapper.
/// </summary>
[PluginInterface]
[ServiceManager.ScopedService]
#pragma warning disable SA1015
[ResolveVia<IReliableFileStorage>]
#pragma warning restore SA1015
public class ReliableFileStoragePluginScoped : IReliableFileStorage, IInternalDisposableService
{
    private readonly Lock pendingLock = new();
    private readonly HashSet<Task> pendingWrites = [];

    private readonly LocalPlugin plugin;

    [ServiceManager.ServiceDependency]
    private readonly ReliableFileStorage storage = Service<ReliableFileStorage>.Get();

    // When true, the scope is disposing and new write requests are rejected.
    private volatile bool isDisposing = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReliableFileStoragePluginScoped"/> class.
    /// </summary>
    /// <param name="plugin">The owner plugin.</param>
    [ServiceManager.ServiceConstructor]
    internal ReliableFileStoragePluginScoped(LocalPlugin plugin)
    {
        this.plugin = plugin;
    }

    /// <inheritdoc/>
    public long MaxFileSizeBytes => 64 * 1024 * 1024;

    /// <inheritdoc/>
    public bool Exists(string path)
    {
        if (this.isDisposing)
            throw new ObjectDisposedException(nameof(ReliableFileStoragePluginScoped));

        return this.storage.Exists(path, this.plugin.EffectiveWorkingPluginId);
    }

    /// <inheritdoc/>
    public Task WriteAllTextAsync(string path, string? contents)
    {
        // Route through WriteAllBytesAsync so all write tracking and size checks are centralized.
        ArgumentException.ThrowIfNullOrEmpty(path);

        var bytes = Encoding.UTF8.GetBytes(contents ?? string.Empty);
        return this.WriteAllBytesAsync(path, bytes);
    }

    /// <inheritdoc/>
    public Task WriteAllTextAsync(string path, string? contents, Encoding encoding)
    {
        // Route through WriteAllBytesAsync so all write tracking and size checks are centralized.
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentNullException.ThrowIfNull(encoding);

        var bytes = encoding.GetBytes(contents ?? string.Empty);
        return this.WriteAllBytesAsync(path, bytes);
    }

    /// <inheritdoc/>
    public Task WriteAllBytesAsync(string path, byte[] bytes)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentNullException.ThrowIfNull(bytes);

        if (bytes.LongLength > this.MaxFileSizeBytes)
            throw new ArgumentException($"The provided data exceeds the maximum allowed size of {this.MaxFileSizeBytes} bytes.", nameof(bytes));

        // Start the underlying write task
        var task = Task.Run(() => this.storage.WriteAllBytesAsync(path, bytes, this.plugin.EffectiveWorkingPluginId));

        // Track the task so we can wait for it on dispose
        lock (this.pendingLock)
        {
            if (this.isDisposing)
                throw new ObjectDisposedException(nameof(ReliableFileStoragePluginScoped));

            this.pendingWrites.Add(task);
        }

        // Remove when done, if the task is already done this runs synchronously here and removes immediately
        _ = task.ContinueWith(t =>
        {
            lock (this.pendingLock)
            {
                this.pendingWrites.Remove(t);
            }
        }, TaskContinuationOptions.ExecuteSynchronously);

        return task;
    }

    /// <inheritdoc/>
    public Task<string> ReadAllTextAsync(string path, bool forceBackup = false)
    {
        if (this.isDisposing)
            throw new ObjectDisposedException(nameof(ReliableFileStoragePluginScoped));

        ArgumentException.ThrowIfNullOrEmpty(path);

        return this.storage.ReadAllTextAsync(path, forceBackup, this.plugin.EffectiveWorkingPluginId);
    }

    /// <inheritdoc/>
    public Task<string> ReadAllTextAsync(string path, Encoding encoding, bool forceBackup = false)
    {
        if (this.isDisposing)
            throw new ObjectDisposedException(nameof(ReliableFileStoragePluginScoped));

        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentNullException.ThrowIfNull(encoding);

        return this.storage.ReadAllTextAsync(path, encoding, forceBackup, this.plugin.EffectiveWorkingPluginId);
    }

    /// <inheritdoc/>
    public Task ReadAllTextAsync(string path, Action<string> reader)
    {
        if (this.isDisposing)
            throw new ObjectDisposedException(nameof(ReliableFileStoragePluginScoped));

        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentNullException.ThrowIfNull(reader);

        return this.storage.ReadAllTextAsync(path, reader, this.plugin.EffectiveWorkingPluginId);
    }

    /// <inheritdoc/>
    public Task ReadAllTextAsync(string path, Encoding encoding, Action<string> reader)
    {
        if (this.isDisposing)
            throw new ObjectDisposedException(nameof(ReliableFileStoragePluginScoped));

        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentNullException.ThrowIfNull(encoding);
        ArgumentNullException.ThrowIfNull(reader);

        return this.storage.ReadAllTextAsync(path, encoding, reader, this.plugin.EffectiveWorkingPluginId);
    }

    /// <inheritdoc/>
    public Task<byte[]> ReadAllBytesAsync(string path, bool forceBackup = false)
    {
        if (this.isDisposing)
            throw new ObjectDisposedException(nameof(ReliableFileStoragePluginScoped));

        ArgumentException.ThrowIfNullOrEmpty(path);

        return this.storage.ReadAllBytesAsync(path, forceBackup, this.plugin.EffectiveWorkingPluginId);
    }

    /// <inheritdoc/>
    public void DisposeService()
    {
        Task[] tasksSnapshot;
        lock (this.pendingLock)
        {
            // Mark disposing to reject new writes.
            this.isDisposing = true;

            if (this.pendingWrites.Count == 0)
                return;

            tasksSnapshot = this.pendingWrites.ToArray();
        }

        try
        {
            // Wait for all pending writes to complete. If some complete while we're waiting they will be in tasksSnapshot
            // and are observed here; newly started writes are rejected due to isDisposing.
            Task.WaitAll(tasksSnapshot);
        }
        catch (AggregateException)
        {
            // Swallow exceptions here: the underlying write failures will have been surfaced earlier to callers.
            // We don't want dispose to throw and crash unload sequences.
        }
    }
}
