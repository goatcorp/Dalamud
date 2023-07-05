using System;

namespace Dalamud.Game.Config;

public abstract record ConfigChangeEvent(Enum GenericOption);

public record ConfigChangeEvent<T>(T ConfigOption) : ConfigChangeEvent(ConfigOption) where T : Enum;
