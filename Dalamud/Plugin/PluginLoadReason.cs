namespace Dalamud.Plugin;

/// <summary>
/// This enum reflects reasons for loading a plugin.
/// </summary>
public enum PluginLoadReason
{
    /// <summary>
    /// We don't know why this plugin was loaded.
    /// </summary>
    Unknown,

    /// <summary>
    /// This plugin was loaded because it was installed with the plugin installer.
    /// </summary>
    Installer,

    /// <summary>
    /// This plugin was loaded because it was just updated.
    /// </summary>
    Update,

    /// <summary>
    /// This plugin was loaded because it was told to reload.
    /// </summary>
    Reload,

    /// <summary>
    /// This plugin was loaded because the game was started or Dalamud was reinjected.
    /// </summary>
    Boot,
}

// TODO(api9): This should be a mask, so that we can combine Installer | ProfileLoaded
