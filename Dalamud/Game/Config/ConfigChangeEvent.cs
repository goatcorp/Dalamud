namespace Dalamud.Game.Config;

public abstract record ConfigChangeEvent(Enum Option);

public record ConfigChangeEvent<T>(T ConfigOption) : ConfigChangeEvent(ConfigOption) where T : Enum;
