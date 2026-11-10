using System.Collections.Generic;

using Dalamud.Utility;

namespace Dalamud.Plugin.Internal.Types.Manifest;

/// <summary>
/// Public interface for the base plugin manifest.
/// </summary>
[Api16ToDo("Make internal, create copy if it really needs to be exposed or just remove")]
public interface IPluginManifest
{
    /// <summary>
    /// Gets the internal name of the plugin, which should match the assembly name of the plugin.
    /// </summary>
    string InternalName { get; }

    /// <summary>
    /// Gets the public name of the plugin.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets a punchline of the plugins functions.
    /// </summary>
    string? Punchline { get; }

    /// <summary>
    /// Gets the author/s of the plugin.
    /// </summary>
    string Author { get; }

    /// <summary>
    /// Gets a value indicating whether the plugin can be unloaded asynchronously.
    /// </summary>
    bool CanUnloadAsync { get; }

    /// <summary>
    /// Gets the assembly version of the plugin.
    /// </summary>
    Version AssemblyVersion { get; }

    /// <summary>
    /// Gets the minimum Dalamud assembly version this plugin requires.
    /// </summary>
    Version? MinimumDalamudVersion { get; }

    /// <summary>
    /// Gets the DIP17 channel name.
    /// </summary>
    string? Dip17Channel { get; }

    /// <summary>
    /// Gets the last time this plugin was updated.
    /// </summary>
    long LastUpdate { get; }

    /// <summary>
    /// Gets a changelog, null if none exists.
    /// </summary>
    string? Changelog { get; }

    /// <summary>
    /// Gets a list of tags that apply to this plugin.
    /// </summary>
    List<string>? Tags { get; }

    /// <summary>
    /// Gets the API level of this plugin.
    /// For the current API level, please see <see cref="PluginManager.DalamudApiLevel"/> for the currently used API level.
    /// </summary>
    int DalamudApiLevel { get; }

    /// <summary>
    /// Gets the number of downloads this plugin has.
    /// </summary>
    long DownloadCount { get; }

    /// <summary>
    /// Gets a value indicating whether the plugin supports profiles.
    /// </summary>
    bool SupportsProfiles { get; }

    /// <summary>
    /// Gets an URL to the website or source code of the plugin.
    /// </summary>
    string? RepoUrl { get; }

    /// <summary>
    /// Gets a description of the plugins functions.
    /// </summary>
    string? Description { get; }

    /// <summary>
    /// Gets a message that is shown to users when sending feedback.
    /// </summary>
    string? FeedbackMessage { get; }

    /// <summary>
    /// Gets a list of screenshot image URLs to show in the plugin installer.
    /// </summary>
    List<string>? ImageUrls { get; }

    /// <summary>
    /// Gets an URL for the plugin's icon.
    /// </summary>
    string? IconUrl { get; }

#pragma warning disable SA1600
#pragma warning disable SA1516
    [Api16ToDo("Remove from public API, testing plugins don't have this information in the local manifest")]
    Version? TestingAssemblyVersion { get; }
    int? TestingDalamudApiLevel { get; }
    bool IsTestingExclusive { get; }
    bool IsAvailableForTesting { get; }
#pragma warning restore SA1516
#pragma warning restore SA1600
}
