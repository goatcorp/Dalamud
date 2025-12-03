using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dalamud.Networking.Rpc.Transport;

/// <summary>
/// Interface for RPC host implementations (named pipes or Unix sockets).
/// </summary>
internal interface IRpcTransport : IDisposable
{
    /// <summary>
    /// Gets a list of active RPC connections.
    /// </summary>
    IReadOnlyDictionary<Guid, RpcConnection> Connections { get; }

    /// <summary>Starts accepting client connections.</summary>
    void Start();

    /// <summary>Invoke an RPC request on a specific client expecting a result.</summary>
    /// <param name="clientId">The client ID to invoke.</param>
    /// <param name="method">The method to invoke.</param>
    /// <param name="arguments">Any arguments to invoke.</param>
    /// <returns>An optional return based on the specified RPC.</returns>
    /// <typeparam name="T">The expected response type.</typeparam>
    Task<T> InvokeClientAsync<T>(Guid clientId, string method, params object[] arguments);

    /// <summary>Send a notification to all connected clients (no response expected).</summary>
    /// <param name="method">The method name to broadcast.</param>
    /// <param name="arguments">The arguments to broadcast.</param>
    /// <returns>Returns a Task when completed.</returns>
    Task BroadcastNotifyAsync(string method, params object[] arguments);
}
