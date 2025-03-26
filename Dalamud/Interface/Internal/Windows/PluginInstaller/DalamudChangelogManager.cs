using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;

using Dalamud.Networking.Http;
using Dalamud.Plugin.Internal;
using Dalamud.Utility;
using Serilog;

namespace Dalamud.Interface.Internal.Windows.PluginInstaller;

/// <summary>
/// Class responsible for managing Dalamud changelogs.
/// </summary>
internal class DalamudChangelogManager
{
    private const string DalamudChangelogUrl = "https://kamori.goats.dev/Dalamud/Release/Changelog";
    private const string PluginChangelogUrl = "https://kamori.goats.dev/Plugin/History/{0}?track={1}";

    private readonly PluginManager manager;

    /// <summary>
    /// Initializes a new instance of the <see cref="DalamudChangelogManager"/> class.
    /// </summary>
    /// <param name="manager">The responsible PluginManager.</param>
    public DalamudChangelogManager(PluginManager manager)
    {
        this.manager = manager;
    }

    /// <summary>
    /// Gets a list of all available changelogs.
    /// </summary>
    public IReadOnlyList<IChangelogEntry>? Changelogs { get; private set; }

    /// <summary>
    /// Reload the changelog list.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task ReloadChangelogAsync()
    {
        var client = Service<HappyHttpClient>.Get().SharedHttpClient;
        this.Changelogs = null;

        var dalamudChangelogs = await client.GetFromJsonAsync<List<DalamudChangelog>>(DalamudChangelogUrl);
        var changelogs = dalamudChangelogs.Select(x => new DalamudChangelogEntry(x)).Cast<IChangelogEntry>().ToList();

        foreach (var plugin in this.manager.InstalledPlugins)
        {
            if (!plugin.IsThirdParty && !plugin.IsDev)
            {
                try
                {
                    var pluginChangelogs = await client.GetFromJsonAsync<PluginHistory>(string.Format(
                                               PluginChangelogUrl,
                                               plugin.Manifest.InternalName,
                                               plugin.Manifest.Dip17Channel));

                    changelogs.AddRange(pluginChangelogs.Versions
                                                                   .Where(x => x.Dip17Track ==
                                                                               plugin.Manifest.Dip17Channel)
                                                                   .Select(x => new PluginChangelogEntry(plugin, x)));
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to load changelog for {PluginName}", plugin.Manifest.Name);
                }
            }
            else
            {
                if (plugin.Manifest.Changelog.IsNullOrWhitespace())
                    continue;

                changelogs.Add(new PluginChangelogEntry(plugin));
            }
        }

        this.Changelogs = changelogs.OrderByDescending(x => x.Date).ToList();
    }

    /// <summary>
    /// API response for a history of plugin versions.
    /// </summary>
    internal class PluginHistory
    {
        /// <summary>
        /// Gets or sets the version history of the plugin.
        /// </summary>
        public List<PluginVersion> Versions { get; set; } = null!;

        /// <summary>
        /// A single plugin version.
        /// </summary>
        internal class PluginVersion
        {
#pragma warning disable SA1600
            public string Version { get; set; } = null!;

            public string Dip17Track { get; set; } = null!;

            public string? Changelog { get; set; }

            public DateTime PublishedAt { get; set; }

            public int? PrNumber { get; set; }

            public string? PublishedBy { get; set; }
#pragma warning restore SA1600
        }
    }
}
