using System.Diagnostics.CodeAnalysis;

using Dalamud.Networking.Pipes;

namespace Dalamud.Plugin.Services;

/// <summary>
/// A service to allow plugins to subscribe to dalamud:// URIs targeting them. Plugins will receive any URI sent to the
/// <c>dalamud://plugin/{PLUGIN_INTERNAL_NAME}/...</c> namespace.
/// </summary>
[Experimental("DAL_RPC", Message = "This service will be finalized around 7.41 and may change before then.")]
public interface IPluginLinkHandler
{
    /// <summary>
    /// A delegate containing the received URI.
    /// </summary>
    /// <param name="uri">The URI opened by the user.</param>
    public delegate void PluginUriReceived(DalamudUri uri);

    /// <summary>
    /// The event fired when a URI targeting this plugin is received.
    /// </summary>
    event PluginUriReceived OnUriReceived;
}
