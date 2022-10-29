namespace Dalamud.Configuration;

/// <summary>
/// Configuration to store settings for a dalamud plugin.
/// </summary>
public interface IPluginConfiguration
{
    /// <summary>
    /// Gets or sets configuration version.
    /// </summary>
    int Version { get; set; }
}
