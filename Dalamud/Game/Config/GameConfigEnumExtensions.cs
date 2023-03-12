using Dalamud.Utility;

namespace Dalamud.Game.Config;

/// <summary>
/// Helper functions for accessing GameConfigOptions.
/// </summary>
internal static class GameConfigEnumExtensions
{
    /// <summary>
    /// Gets the name of a SystemConfigOption from it's attribute.
    /// </summary>
    /// <param name="systemConfigOption">The SystemConfigOption.</param>
    /// <returns>Name of the option.</returns>
    public static string GetName(this SystemConfigOption systemConfigOption)
    {
        return systemConfigOption.GetAttribute<GameConfigOptionAttribute>()?.Name ?? $"{systemConfigOption}";
    }

    /// <summary>
    /// Gets the name of a UiConfigOption from it's attribute.
    /// </summary>
    /// <param name="uiConfigOption">The UiConfigOption.</param>
    /// <returns>Name of the option.</returns>
    public static string GetName(this UiConfigOption uiConfigOption)
    {
        return uiConfigOption.GetAttribute<GameConfigOptionAttribute>()?.Name ?? $"{uiConfigOption}";
    }

    /// <summary>
    /// Gets the name of a UiControlOption from it's attribute.
    /// </summary>
    /// <param name="uiControlOption">The UiControlOption.</param>
    /// <returns>Name of the option.</returns>
    public static string GetName(this UiControlOption uiControlOption)
    {
        return uiControlOption.GetAttribute<GameConfigOptionAttribute>()?.Name ?? $"{uiControlOption}";
    }
}
