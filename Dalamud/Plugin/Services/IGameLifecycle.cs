using System.Threading;

namespace Dalamud.Plugin.Services;

/// <summary>
/// Class offering cancellation tokens for common gameplay events.
/// </summary>
public interface IGameLifecycle : IDalamudService
{
    /// <summary>
    /// Gets a token that is cancelled when Dalamud is unloading.
    /// </summary>
    CancellationToken DalamudUnloadingToken { get; }

    /// <summary>
    /// Gets a token that is cancelled when the game is shutting down.
    /// </summary>
    CancellationToken GameShuttingDownToken { get; }

    /// <summary>
    /// Gets a token that is cancelled when a character is logging out.
    /// </summary>
    CancellationToken LogoutToken { get; }
}
