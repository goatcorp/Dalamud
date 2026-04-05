using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Dalamud.Configuration.Internal;
using Dalamud.Plugin.Internal;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Plugin.Internal.Types.Manifest;

namespace Dalamud.CorePlugin.PluginInstallerV2.Controllers;

/// <summary>
/// Class responsible for managing the plugin lists for the Plugin Installer.
/// </summary>
internal class PluginListManager
{
    private readonly Lock listLock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginListManager"/> class.
    /// </summary>
    public PluginListManager()
    {
        var pluginManager = Service<PluginManager>.Get();
        var configuration = Service<DalamudConfiguration>.Get();

        lock (this.listLock)
        {
            this.PluginListAvailable = pluginManager.AvailablePlugins.ToList();
            this.PluginListInstalled = pluginManager.InstalledPlugins.ToList();
            this.HiddenPlugins = configuration.HiddenPluginInternalName.ToList();
        }
    }

    /// <summary>
    /// Gets list of available plugins.
    /// </summary>
    public List<RemotePluginManifest> PluginListAvailable { get; private set; }

    /// <summary>
    /// Gets list of Installed Plugins.
    /// </summary>
    public List<LocalPlugin> PluginListInstalled { get; private set; }

    /// <summary>
    /// Gets list of Updatable Plugins.
    /// </summary>
    public List<AvailablePluginUpdate> PluginListUpdatable { get; private set; } = [];

    /// <summary>
    /// Gets a value indicating whether there are any Dev Plugins Installed.
    /// </summary>
    public bool HasDevPlugins
        => this.PluginListInstalled.Any(plugin => plugin.IsDev);

    /// <summary>
    /// Gets a value indicating whether there are any hidden plugins.
    /// </summary>
    public bool HasHiddenPlugins
        => this.HiddenPlugins.Count != 0;

    private List<string> HiddenPlugins { get; set; }

    /// <summary>
    /// Updates the plugins lists to be in the specified order.
    /// </summary>
    /// <param name="searchController">Reference to Search Controller with Information on how to sort/filter the results.</param>
    public void UpdateSortOrder(SearchController searchController)
    {
    }
}
