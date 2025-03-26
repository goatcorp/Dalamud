namespace Dalamud.Game.Config;

/// <summary>
/// Represents a change in the configuration.
/// </summary>
/// <param name="Option">The option tha twas changed.</param>
public abstract record ConfigChangeEvent(Enum Option);

/// <summary>
/// Represents a generic change in the configuration.
/// </summary>
/// <param name="ConfigOption">The option that was changed.</param>
/// <typeparam name="T">The type of the option.</typeparam>
public record ConfigChangeEvent<T>(T ConfigOption) : ConfigChangeEvent(ConfigOption) where T : Enum;
