using System.Threading;
using System.Threading.Tasks;

namespace Dalamud.Plugin;

/// <summary>
/// This interface represents a basic Dalamud plugin that loads and unloads asynchronously.
/// All plugins have to implement either <see cref="IDalamudPlugin"/> or <see cref="IAsyncDalamudPlugin"/>.
/// </summary>
public interface IAsyncDalamudPlugin : IAsyncDisposable
{
    /// <summary>
    /// Performs plugin-defined tasks associated with loading the plugin asynchronously.
    /// The plugin will not be considered loaded until this method completes, and will not
    /// be unloaded until <see cref="IAsyncDisposable.DisposeAsync"/> completes.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the load operation. If cancellation is requested,
    /// the plugin should throw <see cref="OperationCanceledException"/> to signal that it
    /// did not fully load. Dalamud will not consider the plugin loaded in this case.
    /// <see cref="IAsyncDisposable.DisposeAsync"/> will still be called.
    /// </param>
    /// <returns>A task that represents the asynchronous load operation.</returns>
    Task LoadAsync(CancellationToken cancellationToken);
}
