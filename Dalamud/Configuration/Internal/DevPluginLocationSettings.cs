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
    /// Clone this object.
    /// </summary>
    /// <returns>A shallow copy of this object.</returns>
    public DevPluginLocationSettings Clone() => this.MemberwiseClone() as DevPluginLocationSettings;
}
