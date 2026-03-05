using Lumina.Text.ReadOnly;

namespace Dalamud.Game.Config;

/// <summary>
/// Represents a string configuration property.
/// </summary>
/// <param name="Default">The default value.</param>
public record StringConfigProperties(ReadOnlySeString Default);

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
