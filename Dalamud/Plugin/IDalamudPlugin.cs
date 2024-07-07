using System.Threading.Tasks;

namespace Dalamud.Plugin;

/// <summary>
/// This interface represents a basic Dalamud plugin.
/// </summary>
[Obsolete("Use IAsyncDalamudPlugin instead and make sure that your plugin can load and unload asynchronously. This interface will be removed in a future version. Please refer to http://ooo for more information.")]
public interface IDalamudPlugin : IDisposable
{
}

/// <summary>
/// This interface represents a basic Dalamud plugin that can be loaded and unloaded asynchronously.
/// </summary>
public interface IAsyncDalamudPlugin : IAsyncDisposable
{
    /// <summary>Performs plugin-defined tasks associated with loading the plugin asynchronously.</summary>
    /// <returns>A task that represents the asynchronous load operation.</returns>
    ValueTask LoadAsync();
}
