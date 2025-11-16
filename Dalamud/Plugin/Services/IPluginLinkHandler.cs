using Dalamud.Networking.Pipes;

namespace Dalamud.Plugin.Services;

/// <summary>
/// A service to allow plugins to subscribe to dalamud:// URIs targeting them. Plugins will receive any URI sent to the
/// <code>dalamud://plugin/{PLUGIN_INTERNAL_NAME}/...</code> namespace.
/// </summary>
public interface IPluginLinkHandler
{
    /// <summary>
    /// A delegate containing the received URI.
    /// </summary>
    delegate void PluginUriReceived(DalamudUri uri);

    /// <summary>
    /// The event fired when a URI targeting this plugin is received.
    /// </summary>
    event PluginUriReceived OnUriReceived;
}
