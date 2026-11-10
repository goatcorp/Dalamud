namespace Dalamud.Plugin;

/// <summary>
/// Causes for a change to the plugin list.
/// </summary>
public enum PluginListInvalidationKind
{
    /// <summary>
    /// A plugin was loaded.
    /// </summary>
    Loaded,

    /// <summary>
    /// A plugin was unloaded.
    /// </summary>
    Unloaded,

    /// <summary>
    /// An installer-initiated update reloaded plugins.
    /// </summary>
    Update,

    /// <summary>
    /// An auto-update reloaded plugins.
    /// </summary>
    AutoUpdate,
}
