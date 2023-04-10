using System;
using System.Collections.Generic;

using Dalamud.Game;
using Newtonsoft.Json;

namespace Dalamud.Plugin.Internal.Types;

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
    public string Name { get; set; } = null!;

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
    public string InternalName { get; init; } = null!;

    /// <summary>
    /// Gets the current assembly version of the plugin.
    /// </summary>
    [JsonProperty]
    public Version AssemblyVersion { get; init; } = null!;

    /// <summary>
    /// Gets the current testing assembly version of the plugin.
    /// </summary>
    [JsonProperty]
    public Version? TestingAssemblyVersion { get; init; }

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
    public string DownloadLinkInstall { get; init; } = null!;

    /// <summary>
    /// Gets the download link used to update the plugin.
    /// </summary>
    [JsonProperty]
    public string DownloadLinkUpdate { get; init; } = null!;

    /// <summary>
    /// Gets the download link used to get testing versions of the plugin.
    /// </summary>
    [JsonProperty]
    public string DownloadLinkTesting { get; init; } = null!;

    /// <summary>
    /// Gets the required Dalamud load step for this plugin to load. Takes precedence over LoadPriority.
    /// Valid values are:
    /// 0. During Framework.Tick, when drawing facilities are available.
    /// 1. During Framework.Tick.
    /// 2. No requirement.
    /// </summary>
    [JsonProperty]
    public int LoadRequiredState { get; init; }

    /// <summary>
    /// Gets a value indicating whether Dalamud must load this plugin not at the same time with other plugins and the game.
    /// </summary>
    [JsonProperty]
    public bool LoadSync { get; init; }

    /// <summary>
    /// Gets the load priority for this plugin. Higher values means higher priority. 0 is default priority.
    /// </summary>
    [JsonProperty]
    public int LoadPriority { get; init; }

    /// <summary>
    /// Gets a value indicating whether the plugin can be unloaded asynchronously.
    /// </summary>
    [JsonProperty]
    public bool CanUnloadAsync { get; init; }

    /// <summary>
    /// Gets a value indicating whether the plugin supports profiles.
    /// </summary>
    [JsonProperty]
    public bool SupportsProfiles { get; init; } = true;

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
    /// Gets a value indicating whether this plugin is DIP17.
    /// To be removed.
    /// </summary>
    [JsonProperty("_isDip17Plugin")]
    public bool IsDip17Plugin { get; init; } = false;

    /// <summary>
    /// Gets the DIP17 channel name.
    /// </summary>
    [JsonProperty("_Dip17Channel")]
    public string? Dip17Channel { get; init; }
}
