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
}
