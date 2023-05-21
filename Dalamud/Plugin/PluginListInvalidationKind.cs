namespace Dalamud.Plugin;

/// <summary>
/// Causes for a change to the plugin list.
/// </summary>
public enum PluginListInvalidationKind
{
    /// <summary>
    /// An installer-initiated update reloaded plugins.
    /// </summary>
    Update,

    /// <summary>
    /// An auto-update reloaded plugins.
    /// </summary>
    AutoUpdate,
}
