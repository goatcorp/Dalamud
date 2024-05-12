using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Dalamud.Plugin.Internal.Types.Manifest;

/// <summary>
/// Information about a plugin, packaged in a json file with the DLL. This variant includes additional information such as
/// if the plugin is disabled and if it was installed from a testing URL. This is designed for use with manifests on disk.
/// </summary>
[UsedImplicitly]
internal record RemotePluginManifest : PluginManifest
{
    /// <summary>
    /// Gets or sets the plugin repository this manifest came from. Used in reporting which third party repo a manifest
    /// may have come from in the plugins available view. This functionality should not be included in the plugin master.
    /// </summary>
    [JsonIgnore]
    public PluginRepository SourceRepo { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets the changelog to be shown when obtaining the testing version of the plugin.
    /// </summary>
    public string? TestingChangelog { get; set; }

    /// <summary>
    /// Gets a value indicating whether this plugin is eligible for testing.
    /// </summary>
    public bool IsAvailableForTesting => this.TestingAssemblyVersion != null && this.TestingAssemblyVersion > this.AssemblyVersion;
}
