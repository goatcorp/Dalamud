using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using Dalamud.Plugin.Internal.Types;
using Newtonsoft.Json;

namespace Dalamud.Plugin.Internal
{
    /// <summary>
    /// This class represents a single plugin repository.
    /// </summary>
    internal partial class PluginRepository
    {
        private const string DalamudPluginsMasterUrl = "https://raw.githubusercontent.com/goatcorp/DalamudPlugins/master/pluginmaster.json";

        private static readonly ModuleLog Log = new("PLUGINR");

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginRepository"/> class.
        /// </summary>
        /// <param name="pluginMasterUrl">The plugin master URL.</param>
        /// <param name="isEnabled">Whether the plugin repo is enabled.</param>
        public PluginRepository(string pluginMasterUrl, bool isEnabled)
        {
            this.PluginMasterUrl = pluginMasterUrl;
            this.IsThirdParty = pluginMasterUrl != DalamudPluginsMasterUrl;
            this.IsEnabled = isEnabled;

            // No need to wait for this
            Task.Run(this.ReloadPluginMasterAsync);
        }

        /// <summary>
        /// Gets a new instance of the <see cref="PluginRepository"/> class for the main repo.
        /// </summary>
        public static PluginRepository MainRepo => new(DalamudPluginsMasterUrl, true);

        /// <summary>
        /// Gets the pluginmaster.json URL.
        /// </summary>
        public string PluginMasterUrl { get; }

        /// <summary>
        /// Gets a value indicating whether this plugin repository is from a third party.
        /// </summary>
        public bool IsThirdParty { get; }

        /// <summary>
        /// Gets a value indicating whether this repo is enabled.
        /// </summary>
        public bool IsEnabled { get; }

        /// <summary>
        /// Gets the plugin master list of available plugins.
        /// </summary>
        public ReadOnlyCollection<RemotePluginManifest> PluginMaster { get; private set; }

        /// <summary>
        /// Gets the initialization state of the plugin repository.
        /// </summary>
        public PluginRepositoryState State { get; private set; }

        /// <summary>
        /// Reload the plugin master asynchronously in a task.
        /// </summary>
        /// <returns>The new state.</returns>
        public Task ReloadPluginMasterAsync()
        {
            this.State = PluginRepositoryState.InProgress;
            this.PluginMaster = new List<RemotePluginManifest>().AsReadOnly();

            return Task.Run(() =>
            {
                using var client = new WebClient();

                Log.Information($"Fetching repo: {this.PluginMasterUrl}");

                var data = client.DownloadString(this.PluginMasterUrl);

                var pluginMaster = JsonConvert.DeserializeObject<List<RemotePluginManifest>>(data);
                pluginMaster.Sort((pm1, pm2) => pm1.Name.CompareTo(pm2.Name));

                // Set the source for each remote manifest. Allows for checking if is 3rd party.
                foreach (var manifest in pluginMaster)
                {
                    manifest.SourceRepo = this;
                }

                this.PluginMaster = pluginMaster.AsReadOnly();
            }).ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    Log.Debug($"Successfully fetched repo: {this.PluginMasterUrl}");
                    this.State = PluginRepositoryState.Success;
                }
                else
                {
                    Log.Error(task.Exception, $"PluginMaster failed: {this.PluginMasterUrl}");
                    this.State = PluginRepositoryState.Fail;
                }
            });
        }
    }
}
