using Dalamud.Utility;

namespace Dalamud.Game.ClientState.Keys;

/// <summary>
/// Extension methods for <see cref="VirtualKey"/>.
/// </summary>
public static class VirtualKeyExtensions
{
    /// <summary>
    /// Get the fancy name associated with this key.
    /// </summary>
    /// <param name="key">The they key to act on.</param>
    /// <returns>The key's fancy name.</returns>
    public static string GetFancyName(this VirtualKey key)
    {
        return key.GetAttribute<VirtualKeyAttribute>().FancyName;
    }
}
