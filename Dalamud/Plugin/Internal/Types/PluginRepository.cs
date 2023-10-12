using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

using Dalamud.Logging.Internal;
using Dalamud.Networking.Http;
using Dalamud.Plugin.Internal.Types.Manifest;
using Dalamud.Utility;
using Newtonsoft.Json;

namespace Dalamud.Plugin.Internal.Types;

/// <summary>
/// This class represents a single plugin repository.
/// </summary>
internal class PluginRepository
{
    /// <summary>
    /// The URL of the official main repository.
    /// </summary>
    public const string MainRepoUrl = "https://kamori.goats.dev/Plugin/PluginMaster";

    private static readonly ModuleLog Log = new("PLUGINR");

    private static readonly HttpClient HttpClient = new(new SocketsHttpHandler
    {
        AutomaticDecompression = DecompressionMethods.All,
        ConnectCallback = Service<HappyHttpClient>.Get().SharedHappyEyeballsCallback.ConnectCallback,
    })
    {
        Timeout = TimeSpan.FromSeconds(20),
        DefaultRequestHeaders =
        {
            Accept =
            {
                new MediaTypeWithQualityHeaderValue("application/json"),
            },
            CacheControl = new CacheControlHeaderValue
            {
                NoCache = true,
            },
            UserAgent =
            {
                new ProductInfoHeaderValue("Dalamud", $"{Util.GetGitHash()}[{Util.GetGitCommitCount()}]"),
            },
        },
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginRepository"/> class.
    /// </summary>
    /// <param name="pluginMasterUrl">The plugin master URL.</param>
    /// <param name="isEnabled">Whether the plugin repo is enabled.</param>
    public PluginRepository(string pluginMasterUrl, bool isEnabled)
    {
        this.PluginMasterUrl = pluginMasterUrl;
        this.IsThirdParty = pluginMasterUrl != MainRepoUrl;
        this.IsEnabled = isEnabled;
    }

    /// <summary>
    /// Gets a new instance of the <see cref="PluginRepository"/> class for the main repo.
    /// </summary>
    public static PluginRepository MainRepo => new(MainRepoUrl, true);

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
    public ReadOnlyCollection<RemotePluginManifest>? PluginMaster { get; private set; }

    /// <summary>
    /// Gets the initialization state of the plugin repository.
    /// </summary>
    public PluginRepositoryState State { get; private set; }

    /// <summary>
    /// Reload the plugin master asynchronously in a task.
    /// </summary>
    /// <returns>The new state.</returns>
    public async Task ReloadPluginMasterAsync()
    {
        this.State = PluginRepositoryState.InProgress;
        this.PluginMaster = new List<RemotePluginManifest>().AsReadOnly();

        try
        {
            Log.Information($"Fetching repo: {this.PluginMasterUrl}");

            using var response = await HttpClient.GetAsync(this.PluginMasterUrl);
            response.EnsureSuccessStatusCode();

            var data = await response.Content.ReadAsStringAsync();
            var pluginMaster = JsonConvert.DeserializeObject<List<RemotePluginManifest>>(data);

            if (pluginMaster == null)
            {
                throw new Exception("Deserialized PluginMaster was null.");
            }

            pluginMaster.Sort((pm1, pm2) => string.Compare(pm1.Name, pm2.Name, StringComparison.Ordinal));

            // Set the source for each remote manifest. Allows for checking if is 3rd party.
            foreach (var manifest in pluginMaster)
            {
                manifest.SourceRepo = this;
            }

            var pm = Service<PluginManager>.Get();
            var official = pm.Repos.First();
            Debug.Assert(!official.IsThirdParty, "First repository should be official repository");

            if (official.State == PluginRepositoryState.Success && this.IsThirdParty)
            {
                pluginMaster = pluginMaster.Where(thisRepoEntry =>
                {
                    if (official.PluginMaster!.Any(officialRepoEntry =>
                                                       string.Equals(thisRepoEntry.InternalName, officialRepoEntry.InternalName, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        Log.Warning(
                            "The repository {RepoName} tried to replace the plugin {PluginName}, which is already installed through the official repo - this is no longer allowed for security reasons. " +
                            "Please reach out if you have an use case for this.",
                            this.PluginMasterUrl,
                            thisRepoEntry.InternalName);
                        return false;
                    }

                    return true;
                }).ToList();
            }
            else if (this.IsThirdParty)
            {
                Log.Warning("Official repository not loaded - couldn't check for overrides!");
                this.State = PluginRepositoryState.Fail;
                return;
            }

            this.PluginMaster = pluginMaster.Where(this.IsValidManifest).ToList().AsReadOnly();
            
            // API9 HACK: Force IsHide to false, we should remove that
            if (!this.IsThirdParty)
            {
                foreach (var manifest in this.PluginMaster)
                {
                    manifest.IsHide = false;
                }
            }

            Log.Information($"Successfully fetched repo: {this.PluginMasterUrl}");
            this.State = PluginRepositoryState.Success;
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"PluginMaster failed: {this.PluginMasterUrl}");
            this.State = PluginRepositoryState.Fail;
        }
    }

    private bool IsValidManifest(RemotePluginManifest manifest)
    {
        if (manifest.InternalName.IsNullOrWhitespace())
        {
            Log.Error("Repository at {RepoLink} has a plugin with an invalid InternalName.", this.PluginMasterUrl);
            return false;
        }

        if (manifest.Name.IsNullOrWhitespace())
        {
            Log.Error("Plugin {PluginName} in {RepoLink} has an invalid Name.", manifest.InternalName, this.PluginMasterUrl);
            return false;
        }
        
        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        if (manifest.AssemblyVersion == null)
        {
            Log.Error("Plugin {PluginName} in {RepoLink} has an invalid AssemblyVersion.", manifest.InternalName, this.PluginMasterUrl);
            return false;
        }

        return true;
    }
}
