using System.Threading.Tasks;
using System.Text;

using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Plugin.Services;

namespace Dalamud.Storage;

[PluginInterface]
[ServiceManager.ScopedService]
#pragma warning disable SA1015
[ResolveVia<IReliableFileStorage>]
#pragma warning restore SA1015
public class ReliableFileStoragePluginScoped : IReliableFileStorage, IServiceType
{
    // TODO: Make sure pending writes are finalized on plugin unload?

    private readonly LocalPlugin plugin;

    [ServiceManager.ServiceDependency]
    private readonly ReliableFileStorage storage = Service<ReliableFileStorage>.Get();

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
        return this.storage.Exists(path, this.plugin.EffectiveWorkingPluginId);
    }

    /// <inheritdoc/>
    public Task WriteAllTextAsync(string path, string? contents)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        var bytes = Encoding.UTF8.GetBytes(contents ?? string.Empty);
        if (bytes.LongLength > this.MaxFileSizeBytes)
            throw new ArgumentException($"The provided data exceeds the maximum allowed size of {this.MaxFileSizeBytes} bytes.", nameof(contents));

        return this.storage.WriteAllBytesAsync(path, bytes, this.plugin.EffectiveWorkingPluginId);
    }

    /// <inheritdoc/>
    public Task WriteAllTextAsync(string path, string? contents, Encoding encoding)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentNullException.ThrowIfNull(encoding);

        var bytes = encoding.GetBytes(contents ?? string.Empty);
        if (bytes.LongLength > this.MaxFileSizeBytes)
            throw new ArgumentException($"The provided data exceeds the maximum allowed size of {this.MaxFileSizeBytes} bytes.", nameof(contents));

        return this.storage.WriteAllBytesAsync(path, bytes, this.plugin.EffectiveWorkingPluginId);
    }

    /// <inheritdoc/>
    public Task WriteAllBytesAsync(string path, byte[] bytes)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentNullException.ThrowIfNull(bytes);

        if (bytes.LongLength > this.MaxFileSizeBytes)
            throw new ArgumentException($"The provided data exceeds the maximum allowed size of {this.MaxFileSizeBytes} bytes.", nameof(bytes));

        return this.storage.WriteAllBytesAsync(path, bytes, this.plugin.EffectiveWorkingPluginId);
    }

    /// <inheritdoc/>
    public Task<string> ReadAllTextAsync(string path, bool forceBackup = false)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        return this.storage.ReadAllTextAsync(path, forceBackup, this.plugin.EffectiveWorkingPluginId);
    }

    /// <inheritdoc/>
    public Task<string> ReadAllTextAsync(string path, Encoding encoding, bool forceBackup = false)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentNullException.ThrowIfNull(encoding);

        return this.storage.ReadAllTextAsync(path, encoding, forceBackup, this.plugin.EffectiveWorkingPluginId);
    }

    /// <inheritdoc/>
    public Task ReadAllTextAsync(string path, Action<string> reader)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentNullException.ThrowIfNull(reader);

        return this.storage.ReadAllTextAsync(path, reader, this.plugin.EffectiveWorkingPluginId);
    }

    /// <inheritdoc/>
    public Task ReadAllTextAsync(string path, Encoding encoding, Action<string> reader)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentNullException.ThrowIfNull(encoding);
        ArgumentNullException.ThrowIfNull(reader);

        return this.storage.ReadAllTextAsync(path, encoding, reader, this.plugin.EffectiveWorkingPluginId);
    }

    /// <inheritdoc/>
    public Task<byte[]> ReadAllBytesAsync(string path, bool forceBackup = false)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        return this.storage.ReadAllBytesAsync(path, forceBackup, this.plugin.EffectiveWorkingPluginId);
    }
}
