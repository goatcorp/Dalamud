using Dalamud.Game.Text.SeStringHandling;

namespace Dalamud.Utility;

/// <summary>
/// Extension methods for SeStrings.
/// </summary>
public static class SeStringExtensions
{
    /// <summary>
    /// Convert a Lumina SeString into a Dalamud SeString.
    /// This conversion re-parses the string.
    /// </summary>
    /// <param name="originalString">The original Lumina SeString.</param>
    /// <returns>The re-parsed Dalamud SeString.</returns>
    public static SeString ToDalamudString(this Lumina.Text.SeString originalString) => SeString.Parse(originalString.RawData);

    /// <summary>
    /// Validate if character name is valid.
    /// Both forename and surname must be between 2 and 15 characters and not total more than 20 characters combined.
    /// Only letters, hyphens, and apostrophes can be used.
    /// The first character of either name must be a letter.
    /// Hyphens cannot be used in succession or placed immediately before or after apostrophes.
    /// </summary>
    /// <param name="value">character name to validate.</param>
    /// <returns>indicator if character is name is valid.</returns>
    public static bool IsValidCharacterName(this SeString value)
    {
        return value.ToString().IsValidCharacterName();
    }
}
