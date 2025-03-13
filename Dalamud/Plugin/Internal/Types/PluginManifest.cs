using System.Collections.Generic;

using Dalamud.Common.Game;
using Dalamud.Plugin.Internal.Types.Manifest;
using Newtonsoft.Json;

namespace Dalamud.Plugin.Internal.Types;

/// <summary>
/// Information about a plugin, packaged in a json file with the DLL.
/// </summary>
internal record PluginManifest : IPluginManifest
{
    /// <inheritdoc/>
    [JsonProperty]
    public string? Author { get; init; }

    /// <inheritdoc/>
    [JsonProperty]
    public string Name { get; set; } = null!;

    /// <inheritdoc/>
    [JsonProperty]
    public string? Punchline { get; init; }

    /// <inheritdoc/>
    [JsonProperty]
    public string? Description { get; init; }

    /// <inheritdoc/>
    [JsonProperty]
    public string? Changelog { get; init; }

    /// <inheritdoc/>
    [JsonProperty]
    public List<string>? Tags { get; init; }

    /// <summary>
    /// Gets a list of category tags defined on the plugin.
    /// </summary>
    [JsonProperty]
    public List<string>? CategoryTags { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether or not the plugin is hidden in the plugin installer.
    /// This value comes from the plugin master and is in addition to the list of hidden names kept by Dalamud.
    /// </summary>
    [JsonProperty]
    public bool IsHide { get; set; }

    /// <inheritdoc/>
    [JsonProperty]
    public string InternalName { get; set; } = null!;

    /// <inheritdoc/>
    [JsonProperty]
    public Version AssemblyVersion { get; set; } = null!;

    /// <inheritdoc/>
    [JsonProperty]
    public Version? TestingAssemblyVersion { get; init; }

    /// <inheritdoc/>
    [JsonProperty]
    public bool IsTestingExclusive { get; init; }

    /// <inheritdoc/>
    [JsonProperty]
    public string? RepoUrl { get; init; }

    /// <summary>
    /// Gets the version of the game this plugin works with.
    /// </summary>
    [JsonProperty]
    [JsonConverter(typeof(GameVersionConverter))]
    public GameVersion? ApplicableVersion { get; init; } = GameVersion.Any;

    /// <inheritdoc/>
    [JsonProperty]
    public int DalamudApiLevel { get; init; } = PluginManager.DalamudApiLevel;

    /// <inheritdoc/>
    [JsonProperty]
    public int? TestingDalamudApiLevel { get; init; }

    /// <inheritdoc/>
    [JsonProperty]
    public long DownloadCount { get; init; }

    /// <inheritdoc/>
    [JsonProperty]
    public long LastUpdate { get; set; }

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

    /// <inheritdoc/>
    [JsonProperty]
    public bool CanUnloadAsync { get; set; }

    /// <inheritdoc/>
    [JsonProperty]
    public bool SupportsProfiles { get; init; } = true;

    /// <inheritdoc/>
    public List<string>? ImageUrls { get; init; }

    /// <inheritdoc/>
    public string? IconUrl { get; init; }

    /// <summary>
    /// Gets a value indicating whether this plugin accepts feedback.
    /// </summary>
    public bool AcceptsFeedback { get; init; } = true;

    /// <inheritdoc/>
    public string? FeedbackMessage { get; init; }

    /// <inheritdoc/>
    [JsonProperty("_Dip17Channel")]
    public string? Dip17Channel { get; init; }
}
