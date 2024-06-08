using Dalamud.Game.Text.SeStringHandling;

namespace Dalamud.Game.Config;

/// <summary>
/// Represents a string configuration property.
/// </summary>
/// <param name="Default">The default value.</param>
public record StringConfigProperties(SeString? Default);

/// <summary>
/// Represents a uint configuration property.
/// </summary>
/// <param name="Default">The default value.</param>
/// <param name="Minimum">The minimum value.</param>
/// <param name="Maximum">The maximum value.</param>
public record UIntConfigProperties(uint Default, uint Minimum, uint Maximum);

/// <summary>
/// Represents a floating point configuration property.
/// </summary>
/// <param name="Default">The default value.</param>
/// <param name="Minimum">The minimum value.</param>
/// <param name="Maximum">The maximum value.</param>
public record FloatConfigProperties(float Default, float Minimum, float Maximum);
