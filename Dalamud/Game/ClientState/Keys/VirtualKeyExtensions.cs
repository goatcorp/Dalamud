using Dalamud.Utility;

namespace Dalamud.Game.ClientState.Keys;

/// <summary>
/// Extension methods for <see cref="VirtualKey"/>.
/// </summary>
public static class VirtualKeyExtensions
{
    // The array is accessed in a way that this limit doesn't appear to exist
    // but there is other state data past this point, and keys beyond here aren't
    // generally valid for most things anyway
    internal const int MaxValidCode = 0xF0;

    /// <summary>
    /// Get the fancy name associated with this key.
    /// </summary>
    /// <param name="key">The they key to act on.</param>
    /// <returns>The key's fancy name.</returns>
    public static string GetFancyName(this VirtualKey key)
    {
        return key.GetAttribute<VirtualKeyAttribute>().FancyName;
    }

    internal static bool IsValidVirtualKey(ushort vkCode)
    {
        return vkCode <= MaxValidCode;
    }
}
