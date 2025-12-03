using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

using Dalamud.Logging.Internal;
using Dalamud.Networking.Rpc.Transport;

namespace Dalamud.Networking.Rpc;

/// <summary>
/// The Dalamud service repsonsible for hosting the RPC.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal class RpcHostService : IServiceType, IInternalDisposableService
{
    private readonly ModuleLog log = new("RPC");
    private readonly RpcServiceRegistry registry = new();
    private readonly List<IRpcTransport> transports = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="RpcHostService"/> class.
    /// </summary>
    [ServiceManager.ServiceConstructor]
    public RpcHostService()
    {
        this.StartUnixTransport();

        if (this.transports.Count == 0)
        {
            this.log.Warning("No RPC hosts could be started on this platform");
        }
    }

    /// <summary>
    /// Gets all active RPC transports.
    /// </summary>
    public IReadOnlyList<IRpcTransport> Transports => this.transports;

    /// <summary>
    /// Add a new service Object to the RPC host.
    /// </summary>
    /// <param name="service">The object to add.</param>
    public void AddService(object service) => this.registry.AddService(service);

    /// <summary>
    /// Add a new standalone method to the RPC host.
    /// </summary>
    /// <param name="name">The method name to add.</param>
    /// <param name="handler">The handler to add.</param>
    public void AddMethod(string name, Delegate handler) => this.registry.AddMethod(name, handler);

    /// <inheritdoc/>
    public void DisposeService()
    {
        foreach (var host in this.transports)
        {
            host.Dispose();
        }

        this.transports.Clear();
    }

    /// <inheritdoc cref="IRpcTransport.InvokeClientAsync"/>
    public async Task<T> InvokeClientAsync<T>(Guid clientId, string method, params object[] arguments)
    {
        var clients = this.transports.SelectMany(t => t.Connections).ToImmutableDictionary();

        if (!clients.TryGetValue(clientId, out var session))
            throw new KeyNotFoundException($"No client {clientId}");

        return await session.Rpc.InvokeAsync<T>(method, arguments).ConfigureAwait(false);
    }

    /// <inheritdoc cref="IRpcTransport.BroadcastNotifyAsync"/>
    public async Task BroadcastNotifyAsync(string method, params object[] arguments)
    {
        await foreach (var transport in this.transports.ToAsyncEnumerable().ConfigureAwait(false))
        {
            await transport.BroadcastNotifyAsync(method, arguments).ConfigureAwait(false);
        }
    }

    private void StartUnixTransport()
    {
        var transport = new UnixRpcTransport(this.registry);
        this.transports.Add(transport);
        transport.Start();
        this.log.Information("RpcHostService listening to UNIX socket: {Socket}", transport.SocketPath);
    }
}
