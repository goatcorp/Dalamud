namespace Dalamud.Game.Config;

/// <summary>
/// Represents a change in the configuration.
/// </summary>
/// <param name="Option">The option that was changed.</param>
/// <param name="Name">The name of the option that was changed.</param>
public abstract record ConfigChangeEvent(Enum Option, string Name);

/// <summary>
/// Represents a generic change in the configuration.
/// </summary>
/// <param name="ConfigOption">The option that was changed.</param>
/// <param name="Name">The name of the option that was changed.</param>
/// <typeparam name="T">The type of the option.</typeparam>
public record ConfigChangeEvent<T>(T ConfigOption, string Name) : ConfigChangeEvent(ConfigOption, Name) where T : Enum;
