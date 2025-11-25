using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Logging.Internal;
using Dalamud.Utility;

using TerraFX.Interop.Windows;

namespace Dalamud.Networking.Rpc.Transport;

/// <summary>
/// Simple multi-client JSON-RPC Unix socket host using StreamJsonRpc.
/// </summary>
internal class UnixRpcTransport : IRpcTransport
{
    private readonly ModuleLog log = new("RPC/Transport/UnixSocket");

    private readonly RpcServiceRegistry registry;
    private readonly CancellationTokenSource cts = new();
    private readonly ConcurrentDictionary<Guid, RpcConnection> sessions = new();
    private readonly string? cleanupSocketDirectory;

    private Task? acceptLoopTask;
    private Socket? listenSocket;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnixRpcTransport"/> class.
    /// </summary>
    /// <param name="registry">The RPC service registry to use.</param>
    /// <param name="socketDirectory">The Unix socket directory to use. If null, defaults to Dalamud home directory.</param>
    /// <param name="socketName">The name of the socket to create.</param>
    public UnixRpcTransport(RpcServiceRegistry registry, string? socketDirectory = null, string? socketName = null)
    {
        this.registry = registry;
        socketName ??= $"DalamudRPC.{Environment.ProcessId}.sock";

        if (!socketDirectory.IsNullOrEmpty())
        {
            this.SocketPath = Path.Combine(socketDirectory, socketName);
        }
        else
        {
            socketDirectory = Service<Dalamud>.Get().StartInfo.TempDirectory;

            if (socketDirectory == null)
            {
                this.SocketPath = Path.Combine(Path.GetTempPath(), socketName);
                this.log.Warning("Temp dir was not set in StartInfo; using system temp for unix socket.");
            }
            else
            {
                this.SocketPath = Path.Combine(socketDirectory, socketName);
                this.cleanupSocketDirectory = socketDirectory;
            }
        }
    }

    /// <summary>
    /// Gets the path of the Unix socket this RPC host is using.
    /// </summary>
    public string SocketPath { get; }

    /// <inheritdoc/>
    public IReadOnlyDictionary<Guid, RpcConnection> Connections => this.sessions;

    /// <summary>Starts accepting client connections.</summary>
    public void Start()
    {
        if (this.acceptLoopTask != null) return;

        // Make the directory for the socket if it doesn't exist
        var socketDir = Path.GetDirectoryName(this.SocketPath);
        if (!string.IsNullOrEmpty(socketDir) && !Directory.Exists(socketDir))
        {
            this.log.Error("Directory for unix socket does not exist: {Path}", socketDir);
            return;
        }

        // Delete existing socket for this PID, if it exists.
        if (File.Exists(this.SocketPath))
        {
            try
            {
                File.Delete(this.SocketPath);
            }
            catch (Exception ex)
            {
                this.log.Warning(ex, "Failed to delete existing socket file: {Path}", this.SocketPath);
            }
        }

        this.acceptLoopTask = Task.Factory.StartNew(this.AcceptLoopAsync, TaskCreationOptions.LongRunning);

        // note: needs to be run _after_ we're alive so that we don't delete our own socket.
        // TODO: This should *probably* be handed by the launcher instead.
        if (this.cleanupSocketDirectory != null)
        {
            Task.Run(async () => await UnixSocketUtil.CleanStaleSockets(this.cleanupSocketDirectory));
        }
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

        this.listenSocket?.Dispose();

        if (File.Exists(this.SocketPath))
        {
            try
            {
                File.Delete(this.SocketPath);
            }
            catch (Exception ex)
            {
                this.log.Warning(ex, "Failed to delete socket file on dispose: {Path}", this.SocketPath);
            }
        }

        this.cts.Dispose();
        this.log.Information("UnixRpcHost disposed ({Socket})", this.SocketPath);
        GC.SuppressFinalize(this);
    }

    private async Task AcceptLoopAsync()
    {
        var token = this.cts.Token;

        try
        {
            var endpoint = new UnixDomainSocketEndPoint(this.SocketPath);
            this.listenSocket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            this.listenSocket.Bind(endpoint);
            this.listenSocket.Listen(128);

            while (!token.IsCancellationRequested)
            {
                Socket? clientSocket = null;
                try
                {
                    clientSocket = await this.listenSocket.AcceptAsync(token).ConfigureAwait(false);

                    var stream = new NetworkStream(clientSocket, ownsSocket: true);
                    var session = new RpcConnection(stream, this.registry);
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
                    clientSocket?.Dispose();
                    break;
                }
                catch (Exception ex)
                {
                    clientSocket?.Dispose();
                    this.log.Error(ex, "Error in socket accept loop");
                    await Task.Delay(500, token).ConfigureAwait(false);
                }
            }
        }
        catch (Exception ex)
        {
            this.log.Error(ex, "Fatal error in Unix socket accept loop");
        }
    }
}
