using System.Linq;

using Dalamud.Console;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Networking.Rpc.Model;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Plugin.Services;

#pragma warning disable DAL_RPC

namespace Dalamud.Networking.Rpc.Service.Links;

/// <inheritdoc cref="IPluginLinkHandler" />
[PluginInterface]
[ServiceManager.ScopedService]
[ResolveVia<IPluginLinkHandler>]
public class PluginLinkHandler : IInternalDisposableService, IPluginLinkHandler
{
    private readonly LinkHandlerService linkHandler;
    private readonly LocalPlugin localPlugin;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginLinkHandler"/> class.
    /// </summary>
    /// <param name="localPlugin">The plugin to bind this service to.</param>
    /// <param name="linkHandler">The central link handler.</param>
    internal PluginLinkHandler(LocalPlugin localPlugin, LinkHandlerService linkHandler)
    {
        this.linkHandler = linkHandler;
        this.localPlugin = localPlugin;

        this.linkHandler.Register("plugin", this.HandleUri);
    }

    /// <inheritdoc/>
    public event IPluginLinkHandler.PluginUriReceived? OnUriReceived;

    /// <inheritdoc/>
    public void DisposeService()
    {
        this.OnUriReceived = null;
        this.linkHandler.Unregister("plugin", this.HandleUri);
    }

    private void HandleUri(DalamudUri uri)
    {
        var target = uri.Path.Split("/").ElementAtOrDefault(1);
        var thisPlugin = ConsoleManagerPluginUtil.GetSanitizedNamespaceName(this.localPlugin.InternalName);
        if (target == null || !string.Equals(target, thisPlugin, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        this.OnUriReceived?.Invoke(uri);
    }
}
