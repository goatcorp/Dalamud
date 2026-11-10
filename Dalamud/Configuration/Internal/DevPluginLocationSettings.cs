// Needs to be here for backward compatibility (config is serialized with types...)
// ReSharper disable once CheckNamespace
namespace Dalamud.Configuration;

/// <summary>
/// Additional locations to load dev plugins from.
/// </summary>
internal sealed class DevPluginLocationSettings
{
    /// <summary>
    /// Gets or sets the dev pluign path.
    /// </summary>
    public string Path { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the third party repo is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets an optional nickname for this dev plugin, shown next to the plugin name
    /// in the plugin list to help distinguish plugins that share the same internal name.
    /// </summary>
    public string? Nickname { get; set; }

    /// <summary>
    /// Clone this object.
    /// </summary>
    /// <returns>A shallow copy of this object.</returns>
    public DevPluginLocationSettings Clone() => this.MemberwiseClone() as DevPluginLocationSettings;
}
