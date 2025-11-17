using System.Collections.Concurrent;
using System.Collections.Generic;

using Dalamud.Logging.Internal;
using Dalamud.Networking.Pipes.Rpc;
using Dalamud.Utility;

namespace Dalamud.Networking.Pipes.Internal;

/// <summary>
/// A service responsible for handling Dalamud URIs and dispatching them accordingly.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal class LinkHandlerService : IInternalDisposableService
{
    private readonly ModuleLog log = new("LinkHandler");

    // key: namespace (e.g. "plugin" or "PluginInstaller") -> list of handlers
    private readonly ConcurrentDictionary<string, List<Action<DalamudUri>>> handlers
        = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="LinkHandlerService"/> class.
    /// </summary>
    /// <param name="rpcHostService">The injected RPC host service.</param>
    [ServiceManager.ServiceConstructor]
    public LinkHandlerService(RpcHostService rpcHostService)
    {
        rpcHostService.AddMethod("handleLink", this.HandleLinkCall);
    }

    /// <inheritdoc/>
    public void DisposeService()
    {
    }

    /// <summary>
    /// Register a handler for a namespace. All URIs with this namespace will be dispatched to the handler.
    /// </summary>
    /// <param name="ns">The namespace to use for this subscription.</param>
    /// <param name="handler">The command handler.</param>
    public void Register(string ns, Action<DalamudUri> handler)
    {
        if (string.IsNullOrWhiteSpace(ns))
            throw new ArgumentNullException(nameof(ns));

        var list = this.handlers.GetOrAdd(ns, _ => []);
        lock (list)
        {
            list.Add(handler);
        }

        this.log.Verbose("Registered handler for {Namespace}", ns);
    }

    /// <summary>
    /// Unregister a handler.
    /// </summary>
    /// <param name="ns">The namespace to use for this subscription.</param>
    /// <param name="handler">The command handler.</param>
    public void Unregister(string ns, Action<DalamudUri> handler)
    {
        if (string.IsNullOrWhiteSpace(ns))
            return;

        if (!this.handlers.TryGetValue(ns, out var list))
            return;

        list.RemoveAll(x => x == handler);

        if (list.Count == 0)
            this.handlers.TryRemove(ns, out _);

        this.log.Verbose("Unregistered handler for {Namespace}", ns);
    }

    /// <summary>
    /// Dispatch a URI to matching handlers.
    /// </summary>
    /// <param name="uri">The URI to parse and dispatch.</param>
    public void Dispatch(DalamudUri uri)
    {
        this.log.Information("Received URI: {Uri}", uri.ToString());

        var ns = uri.Namespace;
        if (!this.handlers.TryGetValue(ns, out var actions))
            return;

        foreach (var h in actions)
        {
            h.InvokeSafely(uri);
        }
    }

    /// <summary>
    /// The RPC-invokable link handler.
    /// </summary>
    /// <param name="uri">A plain-text URI to parse.</param>
    public void HandleLinkCall(string uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
            return;

        var du = DalamudUri.FromUri(uri);
        this.Dispatch(du);
    }
}
