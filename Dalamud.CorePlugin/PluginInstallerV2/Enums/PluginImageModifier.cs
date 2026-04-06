namespace Dalamud.CorePlugin.PluginInstallerV2.Enums;

/// <summary>
/// Modifiers for the Plugin Image, these show as additional decorators on top of plugin icons.
/// </summary>
public enum PluginImageModifier
{
    /// <summary>
    /// Plugin is installable, but not installed, and there are no issues.
    /// </summary>
    None,

    /// <summary>
    /// This plugin is incompatible and can't be loaded.
    /// </summary>
    Incompatible,

    /// <summary>
    /// This plugin is outdated, but is installable with an update.
    /// </summary>
    Outdated,

    /// <summary>
    /// This plugin is installed, but disabled.
    /// </summary>
    Disabled,

    /// <summary>
    /// This plugin is installed, and updatable.
    /// </summary>
    Updatable,

    /// <summary>
    /// Plugin is installed, and in normal state.
    /// </summary>
    Installed,
}
