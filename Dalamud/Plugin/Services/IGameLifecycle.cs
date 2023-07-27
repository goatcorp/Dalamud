using System.Threading;

namespace Dalamud.Plugin.Services;

/// <summary>
/// Class offering cancellation tokens for common gameplay events.
/// </summary>
public interface IGameLifecycle
{
    /// <summary>
    /// Gets a token that is cancelled when Dalamud is unloading.
    /// </summary>
    public CancellationToken DalamudUnloadingToken { get; }
    
    /// <summary>
    /// Gets a token that is cancelled when the game is shutting down.
    /// </summary>
    public CancellationToken GameShuttingDownToken { get; }

    /// <summary>
    /// Gets a token that is cancelled when a character is logging out.
    /// </summary>
    public CancellationToken LogoutToken { get; }
}
