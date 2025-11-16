using System.Threading.Tasks;

using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Networking.Pipes.Rpc;
using Dalamud.Utility;

namespace Dalamud.Networking.Pipes.Internal;

/// <summary>
/// A minimal service to respond with information about this client.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal sealed class ClientHelloService : IInternalDisposableService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClientHelloService"/> class.
    /// </summary>
    /// <param name="rpcHostService">Injected host service.</param>
    [ServiceManager.ServiceConstructor]
    public ClientHelloService(RpcHostService rpcHostService)
    {
        rpcHostService.AddMethod("hello", this.HandleHello);
    }

    /// <summary>
    /// Handle a hello request.
    /// </summary>
    /// <param name="request">.</param>
    /// <returns>Respond with information.</returns>
    public async Task<ClientHelloResponse> HandleHello(ClientHelloRequest request)
    {
        var framework = await Service<Framework>.GetAsync();
        var dalamud = await Service<Dalamud>.GetAsync();
        var clientState = await Service<ClientState>.GetAsync();

        var response = await framework.RunOnFrameworkThread(() => new ClientHelloResponse
        {
            ApiVersion = "1.0",
            DalamudVersion = Util.GetScmVersion(),
            GameVersion = dalamud.StartInfo.GameVersion?.ToString() ?? "Unknown",
            PlayerName = clientState.IsLoggedIn ? clientState.LocalPlayer?.Name.ToString() ?? "Unknown" : null,
        });

        return response;
    }

    /// <inheritdoc/>
    public void DisposeService()
    {
    }
}

/// <summary>
/// A request from a client to say hello.
/// </summary>
internal record ClientHelloRequest
{
    /// <summary>
    /// Gets the API version this client is expecting.
    /// </summary>
    public string ApiVersion { get; init; } = string.Empty;

    /// <summary>
    /// Gets the user agent of the client.
    /// </summary>
    public string UserAgent { get; init; } = string.Empty;
}

/// <summary>
/// A response from Dalamud to a hello request.
/// </summary>
internal record ClientHelloResponse
{
    /// <summary>
    /// Gets the API version this server has offered.
    /// </summary>
    public string? ApiVersion { get; init; }

    /// <summary>
    /// Gets the current Dalamud version.
    /// </summary>
    public string? DalamudVersion { get; init; }

    /// <summary>
    /// Gets the current game version.
    /// </summary>
    public string? GameVersion { get; init; }

    /// <summary>
    /// Gets or sets the player name, or null if the player isn't logged in.
    /// </summary>
    public string? PlayerName { get; set; }
}
