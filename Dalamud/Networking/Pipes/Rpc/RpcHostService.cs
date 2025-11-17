using Dalamud.Logging.Internal;

namespace Dalamud.Networking.Pipes.Rpc;

/// <summary>
/// The Dalamud service repsonsible for hosting the RPC.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal class RpcHostService : IServiceType, IInternalDisposableService
{
    private readonly ModuleLog log = new("RPC");
    private readonly PipeRpcHost host;

    /// <summary>
    /// Initializes a new instance of the <see cref="RpcHostService"/> class.
    /// </summary>
    [ServiceManager.ServiceConstructor]
    public RpcHostService()
    {
        this.host = new PipeRpcHost();
        this.host.Start();

        this.log.Information("RpcHostService started on pipe {Pipe}", this.host.PipeName);
    }

    /// <summary>
    /// Gets the RPC host to drill down.
    /// </summary>
    public PipeRpcHost Host => this.host;

    /// <summary>
    /// Add a new service Object to the RPC host.
    /// </summary>
    /// <param name="service">The object to add.</param>
    public void AddService(object service) => this.host.AddService(service);

    /// <summary>
    /// Add a new standalone method to the RPC host.
    /// </summary>
    /// <param name="name">The method name to add.</param>
    /// <param name="handler">The handler to add.</param>
    public void AddMethod(string name, Delegate handler) => this.host.AddMethod(name, handler);

    /// <inheritdoc/>
    public void DisposeService()
    {
        this.host.Dispose();
    }
}
