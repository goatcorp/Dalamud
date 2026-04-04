namespace Dalamud.CorePlugin.PluginInstallerV2.Enums;

/// <summary>
/// Enum representing which tab is actively selected in Plugin Installer Window.
/// </summary>
internal enum SelectedTab
{
    /// <summary>
    /// The default tab to open to, we may want to change this later.
    /// </summary>
    Default = AvailablePlugins,

    /// <summary>
    /// Show Dev Plugins Widget.
    /// </summary>
    DevPlugins = 1,

    /// <summary>
    /// Show Installed Plugins Widget.
    /// </summary>
    InstalledPlugins = 2,

    /// <summary>
    /// Show Available Plugins Widget.
    /// </summary>
    AvailablePlugins = 3,

    /// <summary>
    /// Show Changelog Widget.
    /// </summary>
    Changelog = 4,
}
