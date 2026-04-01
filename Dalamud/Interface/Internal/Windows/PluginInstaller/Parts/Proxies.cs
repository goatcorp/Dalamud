using System.Collections.Generic;
using System.Linq;

using Dalamud.Bindings.ImGui;
using Dalamud.Configuration.Internal;
using Dalamud.Interface.Colors;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Plugin.Internal.Types.Manifest;

namespace Dalamud.Interface.Internal.Windows.PluginInstaller.Parts;

internal class Proxies
{
    private readonly PluginInstallerWindow pluginInstaller;
    private readonly PluginCategoryManager categoryManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="Proxies"/> class.
    /// </summary>
    /// <param name="pluginInstaller">Reference to main Installer Window.</param>
    /// <param name="categoryManager">Category Manager.</param>
    public Proxies(PluginInstallerWindow pluginInstaller, PluginCategoryManager categoryManager)
    {
        this.pluginInstaller = pluginInstaller;
        this.categoryManager = categoryManager;
    }

    /// <summary>
    /// Plugin Available Proxy
    /// </summary>
    /// <param name="RemoteManifest">Remote Manifest.</param>
    /// <param name="LocalPlugin">Local Plugin.</param>
    public record PluginInstallerAvailablePluginProxy(RemotePluginManifest? RemoteManifest, LocalPlugin? LocalPlugin);

    public IEnumerable<PluginInstallerAvailablePluginProxy> GatherProxies()
    {
        var proxies = new List<PluginInstallerAvailablePluginProxy>();

        var availableManifests = this.pluginInstaller.pluginListAvailable;
        var installedPlugins = this.pluginInstaller.pluginListInstalled.ToList(); // Copy intended

        if (availableManifests.Count == 0)
        {
            ImGui.TextColored(ImGuiColors.DalamudGrey, PluginInstallerLocs.TabBody_SearchNoCompatible);
            return proxies;
        }

        var filteredAvailableManifests = availableManifests
                                         .Where(rm => !this.pluginInstaller.IsManifestFiltered(rm))
                                         .ToList();

        if (filteredAvailableManifests.Count == 0)
        {
            return proxies;
        }

        // Go through all AVAILABLE manifests, associate them with a NON-DEV local plugin, if one is available, and remove it from the pile
        foreach (var availableManifest in this.categoryManager.GetCurrentCategoryContent(filteredAvailableManifests).Cast<RemotePluginManifest>())
        {
            var plugin = this.pluginInstaller.pluginListInstalled
                             .FirstOrDefault(plugin => plugin.Manifest.InternalName == availableManifest.InternalName &&
                                                       plugin.Manifest.RepoUrl == availableManifest.RepoUrl &&
                                                       !plugin.IsDev);

            // We "consumed" this plugin from the pile and remove it.
            if (plugin != null)
            {
                installedPlugins.Remove(plugin);
                proxies.Add(new PluginInstallerAvailablePluginProxy(availableManifest, plugin));

                continue;
            }

            proxies.Add(new PluginInstallerAvailablePluginProxy(availableManifest, null));
        }

        // Now, add all applicable local plugins that haven't been "used up", in most cases either dev or orphaned plugins.
        foreach (var installedPlugin in installedPlugins)
        {
            if (this.pluginInstaller.IsManifestFiltered(installedPlugin.Manifest))
                continue;

            // TODO: We should also check categories here, for good measure

            proxies.Add(new PluginInstallerAvailablePluginProxy(null, installedPlugin));
        }

        var configuration = Service<DalamudConfiguration>.Get();

        bool IsProxyHidden(PluginInstallerAvailablePluginProxy proxy)
        {
            var isHidden =
                configuration.HiddenPluginInternalName.Contains(proxy.RemoteManifest?.InternalName);
            if (this.categoryManager.CurrentCategoryKind == PluginCategoryManager.CategoryKind.Hidden)
                return isHidden;
            return !isHidden;
        }

        // Filter out plugins that are not hidden
        proxies = proxies.Where(IsProxyHidden).ToList();

        return proxies;
    }
}
