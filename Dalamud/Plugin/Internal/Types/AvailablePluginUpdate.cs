using Dalamud.Plugin.Internal.Types.Manifest;

namespace Dalamud.Plugin.Internal.Types;

/// <summary>
/// Information about an available plugin update.
/// </summary>
internal record AvailablePluginUpdate
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AvailablePluginUpdate"/> class.
    /// </summary>
    /// <param name="installedPlugin">The installed plugin to update.</param>
    /// <param name="updateManifest">The manifest to use for the update.</param>
    /// <param name="useTesting">If the testing version should be used for the update.</param>
    public AvailablePluginUpdate(LocalPlugin installedPlugin, RemotePluginManifest updateManifest, bool useTesting)
    {
        this.InstalledPlugin = installedPlugin;
        this.UpdateManifest = updateManifest;
        this.UseTesting = useTesting;
    }

    /// <summary>
    /// Gets the currently installed plugin.
    /// </summary>
    public LocalPlugin InstalledPlugin { get; init; }

    /// <summary>
    /// Gets the available update manifest.
    /// </summary>
    public RemotePluginManifest UpdateManifest { get; init; }

    /// <summary>
    /// Gets a value indicating whether the update should use the testing URL.
    /// </summary>
    public bool UseTesting { get; init; }
}
