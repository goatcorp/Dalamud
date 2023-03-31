using System.Threading;

using Dalamud.IoC;
using Dalamud.IoC.Internal;

namespace Dalamud.Game;

/// <summary>
/// Class offering cancellation tokens for common gameplay events.
/// </summary>
[PluginInterface]
[InterfaceVersion("1.0")]
[ServiceManager.BlockingEarlyLoadedService]
public class GameLifecycle : IServiceType
{
    private readonly CancellationTokenSource dalamudUnloadCts = new();
    private readonly CancellationTokenSource gameShutdownCts = new();

    private CancellationTokenSource logoutCts = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="GameLifecycle"/> class.
    /// </summary>
    [ServiceManager.ServiceConstructor]
    internal GameLifecycle()
    {
    }

    /// <summary>
    /// Gets a token that is cancelled when Dalamud is unloading.
    /// </summary>
    public CancellationToken DalamudUnloadingToken => this.dalamudUnloadCts.Token;

    /// <summary>
    /// Gets a token that is cancelled when the game is shutting down.
    /// </summary>
    public CancellationToken GameShuttingDownToken => this.gameShutdownCts.Token;

    /// <summary>
    /// Gets a token that is cancelled when a character is logging out.
    /// </summary>
    public CancellationToken LogoutToken => this.logoutCts.Token;

    /// <summary>
    /// Mark an unload.
    /// </summary>
    internal void SetUnloading() => this.dalamudUnloadCts.Cancel();

    /// <summary>
    /// Mark a shutdown.
    /// </summary>
    internal void SetShuttingDown() => this.gameShutdownCts.Cancel();

    /// <summary>
    /// Mark a logout.
    /// </summary>
    internal void SetLogout() => this.logoutCts.Cancel();

    /// <summary>
    /// Unmark a logout.
    /// </summary>
    internal void ResetLogout() => this.logoutCts = new();
}
