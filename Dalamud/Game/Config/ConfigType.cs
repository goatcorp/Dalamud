namespace Dalamud.Game.Config;

/// <summary>
/// Types of options used by the game config.
/// </summary>
public enum ConfigType
{
    /// <summary>
    /// Unused config index.
    /// </summary>
    Unused = 0,
    
    /// <summary>
    /// A label entry with no value.
    /// </summary>
    Category = 1,
    
    /// <summary>
    /// A config entry with an unsigned integer value.
    /// </summary>
    UInt = 2,
    
    /// <summary>
    /// A config entry with a float value.
    /// </summary>
    Float = 3,
    
    /// <summary>
    /// A config entry with a string value.
    /// </summary>
    String = 4,
}
