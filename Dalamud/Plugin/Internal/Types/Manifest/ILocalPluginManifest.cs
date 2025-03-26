namespace Dalamud.Plugin.Internal.Types.Manifest;

/// <summary>
/// Public interface for the local plugin manifest.
/// </summary>
public interface ILocalPluginManifest : IPluginManifest
{
    /// <summary>
    /// Gets the 3rd party repo URL that this plugin was installed from. Used to display where the plugin was
    /// sourced from on the installed plugin view. This should not be included in the plugin master. This value is null
    /// when installed from the main repo.
    /// </summary>
    public string InstalledFromUrl { get; }

    /// <summary>
    /// Gets a value indicating whether the plugin should be deleted during the next cleanup.
    /// </summary>
    public bool ScheduledForDeletion { get; }
    
    /// <summary>
    /// Gets an ID uniquely identifying this specific installation of a plugin.
    /// </summary>
    public Guid WorkingPluginId { get; }
}
