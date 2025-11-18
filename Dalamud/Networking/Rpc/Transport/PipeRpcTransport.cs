using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Logging.Internal;
using Dalamud.Utility;

namespace Dalamud.Networking.Rpc.Transport;

/// <summary>
/// Simple multi-client JSON-RPC named pipe host using StreamJsonRpc.
/// </summary>
internal class PipeRpcTransport : IRpcTransport
{
    private readonly ModuleLog log = new("RPC/Host");

    private readonly RpcServiceRegistry registry;
    private readonly CancellationTokenSource cts = new();
    private readonly ConcurrentDictionary<Guid, RpcConnection> sessions = new();
    private Task? acceptLoopTask;

    /// <summary>
    /// Initializes a new instance of the <see cref="PipeRpcTransport"/> class.
    /// </summary>
    /// <param name="registry">The RPC service registry to use.</param>
    /// <param name="pipeName">The pipe name to create.</param>
    public PipeRpcTransport(RpcServiceRegistry registry, string? pipeName = null)
    {
        this.registry = registry;
        // Default pipe name based on current process ID for uniqueness per Dalamud instance.
        this.PipeName = pipeName ?? $"DalamudRPC.{Environment.ProcessId}";
    }

    /// <summary>
    /// Gets the name of the named pipe this RPC host is using.
    /// </summary>
    public string PipeName { get; }

    /// <inheritdoc/>
    public IReadOnlyDictionary<Guid, RpcConnection> Connections => this.sessions;

    /// <summary>Starts accepting client connections.</summary>
    public void Start()
    {
        if (this.acceptLoopTask != null) return;
        this.acceptLoopTask = Task.Factory.StartNew(this.AcceptLoopAsync, TaskCreationOptions.LongRunning);
    }

    /// <summary>Invoke an RPC request on a specific client expecting a result.</summary>
    /// <param name="clientId">The client ID to invoke.</param>
    /// <param name="method">The method to invoke.</param>
    /// <param name="arguments">Any arguments to invoke.</param>
    /// <returns>An optional return based on the specified RPC.</returns>
    /// <typeparam name="T">The expected response type.</typeparam>
    public Task<T> InvokeClientAsync<T>(Guid clientId, string method, params object[] arguments)
    {
        if (!this.sessions.TryGetValue(clientId, out var session))
            throw new KeyNotFoundException($"No client {clientId}");

        return session.Rpc.InvokeAsync<T>(method, arguments);
    }

    /// <summary>Send a notification to all connected clients (no response expected).</summary>
    /// <param name="method">The method name to broadcast.</param>
    /// <param name="arguments">The arguments to broadcast.</param>
    /// <returns>Returns a Task when completed.</returns>
    public Task BroadcastNotifyAsync(string method, params object[] arguments)
    {
        var list = this.sessions.Values;
        var tasks = new List<Task>(list.Count);
        foreach (var s in list)
        {
            tasks.Add(s.Rpc.NotifyAsync(method, arguments));
        }

        return Task.WhenAll(tasks);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.cts.Cancel();
        this.acceptLoopTask?.Wait(1000);

        foreach (var kv in this.sessions)
        {
            kv.Value.Dispose();
        }

        this.sessions.Clear();
        this.cts.Dispose();
        this.log.Information("PipeRpcHost disposed ({Pipe})", this.PipeName);
        GC.SuppressFinalize(this);
    }

    private PipeSecurity BuildPipeSecurity()
    {
        var ps = new PipeSecurity();
        ps.AddAccessRule(new PipeAccessRule(WindowsIdentity.GetCurrent().User!, PipeAccessRights.FullControl, AccessControlType.Allow));

        return ps;
    }

    private async Task AcceptLoopAsync()
    {
        this.log.Information("PipeRpcHost starting on pipe {Pipe}", this.PipeName);
        var token = this.cts.Token;
        var security = this.BuildPipeSecurity();

        while (!token.IsCancellationRequested)
        {
            NamedPipeServerStream? server = null;
            try
            {
                server = NamedPipeServerStreamAcl.Create(
                    this.PipeName,
                    PipeDirection.InOut,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Message,
                    PipeOptions.Asynchronous,
                    65536,
                    65536,
                    security);

                await server.WaitForConnectionAsync(token).ConfigureAwait(false);

                var session = new RpcConnection(server, this.registry);
                this.sessions.TryAdd(session.Id, session);

                this.log.Debug("RPC connection created: {Id}", session.Id);

                _ = session.Completion.ContinueWith(t =>
                {
                    this.sessions.TryRemove(session.Id, out _);
                    this.log.Debug("RPC connection removed: {Id}", session.Id);
                }, TaskScheduler.Default);
            }
            catch (OperationCanceledException)
            {
                server?.Dispose();
                break;
            }
            catch (Exception ex)
            {
                server?.Dispose();
                this.log.Error(ex, "Error in pipe accept loop");
                await Task.Delay(500, token).ConfigureAwait(false);
            }
        }
    }
}
