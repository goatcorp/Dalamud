using System.Diagnostics;
using System.Threading.Tasks;

using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Utility;

using Lumina.Excel.Sheets;

namespace Dalamud.Networking.Rpc.Service;

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
        var dalamud = await Service<Dalamud>.GetAsync();

        return new ClientHelloResponse
        {
            ApiVersion = "1.0",
            DalamudVersion = Versioning.GetScmVersion(),
            GameVersion = dalamud.StartInfo.GameVersion?.ToString() ?? "Unknown",
            ProcessId = Environment.ProcessId,
            ProcessStartTime = new DateTimeOffset(Process.GetCurrentProcess().StartTime).ToUnixTimeSeconds(),
            ClientState = await this.GetClientIdentifier(),
        };
    }

    /// <inheritdoc/>
    public void DisposeService()
    {
    }

    private async Task<string> GetClientIdentifier()
    {
        var framework = await Service<Framework>.GetAsync();
        var clientState = await Service<ClientState>.GetAsync();
        var dataManager = await Service<DataManager>.GetAsync();

        var clientIdentifier = $"FFXIV Process ${Environment.ProcessId}";

        await framework.RunOnFrameworkThread(() =>
        {
            if (clientState.IsLoggedIn)
            {
                var player = clientState.LocalPlayer;
                if (player != null)
                {
                    var world = dataManager.GetExcelSheet<World>().GetRow(player.HomeWorld.RowId);
                    clientIdentifier = $"Logged in as {player.Name.TextValue} @ {world.Name.ExtractText()}";
                }
            }
            else
            {
                clientIdentifier = "On login screen";
            }
        });

        return clientIdentifier;
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
    /// Gets the process ID of this client.
    /// </summary>
    public int? ProcessId { get; init; }

    /// <summary>
    /// Gets the time this process started.
    /// </summary>
    public long? ProcessStartTime { get; init; }

    /// <summary>
    /// Gets a state for this client for user display.
    /// </summary>
    public string? ClientState { get; init; }
}
