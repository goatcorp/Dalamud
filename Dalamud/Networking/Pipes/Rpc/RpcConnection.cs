using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

using Serilog;
using StreamJsonRpc;

namespace Dalamud.Networking.Pipes.Rpc;

/// <summary>
/// A single RPC client session connected via named pipe.
/// </summary>
internal class RpcConnection : IDisposable
{
    private readonly NamedPipeServerStream pipe;
    private readonly RpcServiceRegistry registry;
    private readonly CancellationTokenSource cts = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="RpcConnection"/> class.
    /// </summary>
    /// <param name="pipe">The named pipe that this connection will handle.</param>
    /// <param name="registry">A registry of RPC services.</param>
    public RpcConnection(NamedPipeServerStream pipe, RpcServiceRegistry registry)
    {
        this.Id = Guid.CreateVersion7();
        this.pipe = pipe;
        this.registry = registry;

        var formatter = new JsonMessageFormatter();
        var handler = new HeaderDelimitedMessageHandler(pipe, pipe, formatter);

        this.Rpc = new JsonRpc(handler);
        this.Rpc.AllowModificationWhileListening = true;
        this.Rpc.Disconnected += this.OnDisconnected;
        this.registry.Attach(this.Rpc);

        this.Rpc.StartListening();
    }

    /// <summary>
    /// Gets the GUID for this connection.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the JsonRpc instance for this connection.
    /// </summary>
    public JsonRpc Rpc { get; }

    /// <summary>
    /// Gets a task that's called on RPC completion.
    /// </summary>
    public Task Completion => this.Rpc.Completion;

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!this.cts.IsCancellationRequested)
        {
            this.cts.Cancel();
        }

        try
        {
            this.Rpc.Dispose();
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Error disposing JsonRpc for client {Id}", this.Id);
        }

        try
        {
            this.pipe.Dispose();
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Error disposing pipe for client {Id}", this.Id);
        }

        this.cts.Dispose();
        GC.SuppressFinalize(this);
    }

    private void OnDisconnected(object? sender, JsonRpcDisconnectedEventArgs e)
    {
        Log.Debug("RPC client {Id} disconnected: {Reason}", this.Id, e.Description);
        this.registry.Detach(this.Rpc);
        this.Dispose();
    }
}
