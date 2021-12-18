using System;
using System.Collections.Generic;

using Dalamud.Game;
using Newtonsoft.Json;

namespace Dalamud.Plugin.Internal.Types
{
    /// <summary>
    /// Information about a plugin, packaged in a json file with the DLL.
    /// </summary>
    internal record PluginManifest
    {
        /// <summary>
        /// Gets the author/s of the plugin.
        /// </summary>
        [JsonProperty]
        public string? Author { get; init; }

        /// <summary>
        /// Gets or sets the public name of the plugin.
        /// </summary>
        [JsonProperty]
        public string Name { get; set; }

        /// <summary>
        /// Gets a punchline of the plugins functions.
        /// </summary>
        [JsonProperty]
        public string? Punchline { get; init; }

        /// <summary>
        /// Gets a description of the plugins functions.
        /// </summary>
        [JsonProperty]
        public string? Description { get; init; }

        /// <summary>
        /// Gets a changelog.
        /// </summary>
        [JsonProperty]
        public string? Changelog { get; init; }

        /// <summary>
        /// Gets a list of tags defined on the plugin.
        /// </summary>
        [JsonProperty]
        public List<string>? Tags { get; init; }

        /// <summary>
        /// Gets a list of category tags defined on the plugin.
        /// </summary>
        [JsonProperty]
        public List<string>? CategoryTags { get; init; }

        /// <summary>
        /// Gets a value indicating whether or not the plugin is hidden in the plugin installer.
        /// This value comes from the plugin master and is in addition to the list of hidden names kept by Dalamud.
        /// </summary>
        [JsonProperty]
        public bool IsHide { get; init; }

        /// <summary>
        /// Gets the internal name of the plugin, which should match the assembly name of the plugin.
        /// </summary>
        [JsonProperty]
        public string InternalName { get; init; }

        /// <summary>
        /// Gets the current assembly version of the plugin.
        /// </summary>
        [JsonProperty]
        public Version AssemblyVersion { get; init; }

        /// <summary>
        /// Gets the current testing assembly version of the plugin.
        /// </summary>
        [JsonProperty]
        public Version? TestingAssemblyVersion { get; init; }

        /// <summary>
        /// Gets a value indicating whether the <see cref="AssemblyVersion"/> is not null.
        /// </summary>
        [JsonIgnore]
        public bool HasAssemblyVersion => this.AssemblyVersion != null;

        /// <summary>
        /// Gets a value indicating whether the <see cref="TestingAssemblyVersion"/> is not null.
        /// </summary>
        [JsonIgnore]
        public bool HasTestingAssemblyVersion => this.TestingAssemblyVersion != null;

        /// <summary>
        /// Gets a value indicating whether the plugin is only available for testing.
        /// </summary>
        [JsonProperty]
        public bool IsTestingExclusive { get; init; }

        /// <summary>
        /// Gets an URL to the website or source code of the plugin.
        /// </summary>
        [JsonProperty]
        public string? RepoUrl { get; init; }

        /// <summary>
        /// Gets the version of the game this plugin works with.
        /// </summary>
        [JsonProperty]
        [JsonConverter(typeof(GameVersionConverter))]
        public GameVersion? ApplicableVersion { get; init; } = GameVersion.Any;

        /// <summary>
        /// Gets the API level of this plugin. For the current API level, please see <see cref="PluginManager.DalamudApiLevel"/>
        /// for the currently used API level.
        /// </summary>
        [JsonProperty]
        public int DalamudApiLevel { get; init; } = PluginManager.DalamudApiLevel;

        /// <summary>
        /// Gets the number of downloads this plugin has.
        /// </summary>
        [JsonProperty]
        public long DownloadCount { get; init; }

        /// <summary>
        /// Gets the last time this plugin was updated.
        /// </summary>
        [JsonProperty]
        public long LastUpdate { get; init; }

        /// <summary>
        /// Gets the download link used to install the plugin.
        /// </summary>
        [JsonProperty]
        public string DownloadLinkInstall { get; init; }

        /// <summary>
        /// Gets the download link used to update the plugin.
        /// </summary>
        [JsonProperty]
        public string DownloadLinkUpdate { get; init; }

        /// <summary>
        /// Gets the download link used to get testing versions of the plugin.
        /// </summary>
        [JsonProperty]
        public string DownloadLinkTesting { get; init; }

        /// <summary>
        /// Gets the load priority for this plugin. Higher values means higher priority. 0 is default priority.
        /// </summary>
        [JsonProperty]
        public int LoadPriority { get; init; }

        /// <summary>
        /// Gets a list of screenshot image URLs to show in the plugin installer.
        /// </summary>
        public List<string>? ImageUrls { get; init; }

        /// <summary>
        /// Gets an URL for the plugin's icon.
        /// </summary>
        public string? IconUrl { get; init; }

        /// <summary>
        /// Gets a value indicating whether this plugin accepts feedback.
        /// </summary>
        public bool AcceptsFeedback { get; init; } = true;

        /// <summary>
        /// Gets a message that is shown to users when sending feedback.
        /// </summary>
        public string? FeedbackMessage { get; init; }

        /// <summary>
        /// Gets a value indicating the webhook URL feedback is sent to.
        /// </summary>
        public string? FeedbackWebhook { get; init; }
    }
}
