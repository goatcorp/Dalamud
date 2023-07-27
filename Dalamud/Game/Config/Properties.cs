using Dalamud.Game.Text.SeStringHandling;

namespace Dalamud.Game.Config;

public record StringConfigProperties(SeString? Default);
public record UIntConfigProperties(uint Default, uint Minimum, uint Maximum);
public record FloatConfigProperties(float Default, float Minimum, float Maximum);
