namespace Dalamud.Plugin;

/// <summary>
/// This enum reflects reasons for loading a plugin.
/// </summary>
[Flags]
public enum PluginLoadReason
{
    /// <summary>
    /// We don't know why this plugin was loaded.
    /// </summary>
    Unknown = 1 << 0,

    /// <summary>
    /// This plugin was loaded because it was installed with the plugin installer.
    /// </summary>
    Installer = 1 << 1,

    /// <summary>
    /// This plugin was loaded because it was just updated.
    /// </summary>
    Update = 1 << 2,

    /// <summary>
    /// This plugin was loaded because it was told to reload.
    /// </summary>
    Reload = 1 << 3,

    /// <summary>
    /// This plugin was loaded because the game was started or Dalamud was reinjected.
    /// </summary>
    Boot = 1 << 4,
}
