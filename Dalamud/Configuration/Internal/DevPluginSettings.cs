namespace Dalamud.Configuration.Internal;

/// <summary>
/// Settings for DevPlugins.
/// </summary>
internal sealed class DevPluginSettings
{
    /// <summary>
    /// Gets or sets a value indicating whether this plugin should automatically start when Dalamud boots up.
    /// </summary>
    public bool StartOnBoot { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether this plugin should automatically reload on file change.
    /// </summary>
    public bool AutomaticReloading { get; set; } = false;
}
