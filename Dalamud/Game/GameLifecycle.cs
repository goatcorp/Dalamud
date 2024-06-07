using System.Threading;

using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Plugin.Services;

namespace Dalamud.Game;

/// <summary>
/// Class offering cancellation tokens for common gameplay events.
/// </summary>
[PluginInterface]
[InterfaceVersion("1.0")]
[ServiceManager.EarlyLoadedService]
#pragma warning disable SA1015
[ResolveVia<IGameLifecycle>]
#pragma warning restore SA1015
internal class GameLifecycle : IServiceType, IGameLifecycle
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

    /// <inheritdoc/>
    public CancellationToken DalamudUnloadingToken => this.dalamudUnloadCts.Token;

    /// <inheritdoc/>
    public CancellationToken GameShuttingDownToken => this.gameShutdownCts.Token;

    /// <inheritdoc/>
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
