namespace Dalamud.Plugin;

/// <summary>
/// This interface represents a basic Dalamud plugin.
/// All plugins have to implement either <see cref="IDalamudPlugin"/> or <see cref="IAsyncDalamudPlugin"/>.
/// </summary>
public interface IDalamudPlugin : IDisposable
{
}
