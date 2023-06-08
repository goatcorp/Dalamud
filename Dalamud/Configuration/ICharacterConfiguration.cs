namespace Dalamud.Configuration;

/// <summary>
/// Configuration to store settings for a dalamud plugin.
/// This configuration is per-character.
/// </summary>
public interface ICharacterConfiguration : IPluginConfiguration
{
    /// <summary>
    /// Gets or sets the Content ID of the character this config belongs to.
    /// </summary>
    public ulong ContentId { get; set; }
}
