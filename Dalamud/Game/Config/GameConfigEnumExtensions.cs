using System.Collections.Concurrent;

using Dalamud.Utility;

namespace Dalamud.Game.Config;

/// <summary>
/// Helper functions for accessing GameConfigOptions.
/// </summary>
internal static class GameConfigEnumExtensions
{
    private static readonly ConcurrentDictionary<SystemConfigOption, string> SystemNameCache = new();
    private static readonly ConcurrentDictionary<UiConfigOption, string> UIConfigNameCache = new();
    private static readonly ConcurrentDictionary<UiControlOption, string> UIControlNameCache = new();

    /// <summary>
    /// Gets the name of a SystemConfigOption from it's attribute.
    /// </summary>
    /// <param name="systemConfigOption">The SystemConfigOption.</param>
    /// <returns>Name of the option.</returns>
    public static string GetName(this SystemConfigOption systemConfigOption)
    {
        if (SystemNameCache.TryGetValue(systemConfigOption, out var name)) return name;
        name = systemConfigOption.GetAttribute<GameConfigOptionAttribute>()?.Name ?? $"{systemConfigOption}";
        SystemNameCache.TryAdd(systemConfigOption, name);
        return name;
    }

    /// <summary>
    /// Gets the name of a UiConfigOption from it's attribute.
    /// </summary>
    /// <param name="uiConfigOption">The UiConfigOption.</param>
    /// <returns>Name of the option.</returns>
    public static string GetName(this UiConfigOption uiConfigOption)
    {
        if (UIConfigNameCache.TryGetValue(uiConfigOption, out var name)) return name;
        name = uiConfigOption.GetAttribute<GameConfigOptionAttribute>()?.Name ?? $"{uiConfigOption}";
        UIConfigNameCache.TryAdd(uiConfigOption, name);
        return name;
    }

    /// <summary>
    /// Gets the name of a UiControlOption from it's attribute.
    /// </summary>
    /// <param name="uiControlOption">The UiControlOption.</param>
    /// <returns>Name of the option.</returns>
    public static string GetName(this UiControlOption uiControlOption)
    {
        if (UIControlNameCache.TryGetValue(uiControlOption, out var name)) return name;
        name = uiControlOption.GetAttribute<GameConfigOptionAttribute>()?.Name ?? $"{uiControlOption}";
        UIControlNameCache.TryAdd(uiControlOption, name);
        return name;
    }
}
