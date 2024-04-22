using System.Collections.Generic;

namespace Dalamud.Plugin.Internal.Types.Manifest;

/// <summary>
/// Public interface for the base plugin manifest.
/// </summary>
public interface IPluginManifest
{
    /// <summary>
    /// Gets the internal name of the plugin, which should match the assembly name of the plugin.
    /// </summary>
    public string InternalName { get; }

    /// <summary>
    /// Gets the public name of the plugin.
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// Gets a punchline of the plugins functions.
    /// </summary>
    public string? Punchline { get; }

    /// <summary>
    /// Gets the author/s of the plugin.
    /// </summary>
    public string Author { get; }
    
    /// <summary>
    /// Gets a value indicating whether the plugin can be unloaded asynchronously.
    /// </summary>
    public bool CanUnloadAsync { get; }

    /// <summary>
    /// Gets the assembly version of the plugin.
    /// </summary>
    public Version AssemblyVersion { get; }

    /// <summary>
    /// Gets the assembly version of the plugin's testing variant.
    /// </summary>
    public Version? TestingAssemblyVersion { get; }
    
    /// <summary>
    /// Gets the DIP17 channel name.
    /// </summary>
    public string? Dip17Channel { get; }
    
    /// <summary>
    /// Gets the last time this plugin was updated.
    /// </summary>
    public long LastUpdate { get; }
    
    /// <summary>
    /// Gets a changelog, null if none exists.
    /// </summary>
    public string? Changelog { get; }

    /// <summary>
    /// Gets a list of tags that apply to this plugin.
    /// </summary>
    public List<string>? Tags { get; }

    /// <summary>
    /// Gets the API level of this plugin.
    /// For the current API level, please see <see cref="PluginManager.DalamudApiLevel"/> for the currently used API level.
    /// </summary>
    public int DalamudApiLevel { get; }

    /// <summary>
    /// Gets the API level of the plugin's testing variant.
    /// For the current API level, please see <see cref="PluginManager.DalamudApiLevel"/> for the currently used API level.
    /// </summary>
    public int? TestingDalamudApiLevel { get; }

    /// <summary>
    /// Gets the number of downloads this plugin has.
    /// </summary>
    public long DownloadCount { get; }

    /// <summary>
    /// Gets a value indicating whether the plugin supports profiles.
    /// </summary>
    public bool SupportsProfiles { get; }

    /// <summary>
    /// Gets an URL to the website or source code of the plugin.
    /// </summary>
    public string? RepoUrl { get; }
    
    /// <summary>
    /// Gets a description of the plugins functions.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets a message that is shown to users when sending feedback.
    /// </summary>
    public string? FeedbackMessage { get; }

    /// <summary>
    /// Gets a value indicating whether the plugin is only available for testing.
    /// </summary>
    public bool IsTestingExclusive { get; }

    /// <summary>
    /// Gets a list of screenshot image URLs to show in the plugin installer.
    /// </summary>
    public List<string>? ImageUrls { get; }

    /// <summary>
    /// Gets an URL for the plugin's icon.
    /// </summary>
    public string? IconUrl { get; }
}
