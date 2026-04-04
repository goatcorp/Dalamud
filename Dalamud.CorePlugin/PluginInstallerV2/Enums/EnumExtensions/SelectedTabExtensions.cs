using Dalamud.Interface.Internal;

namespace Dalamud.CorePlugin.PluginInstallerV2.Enums.EnumExtensions;

/// <summary>
/// Extension class for getting localized strings from SelectedTab enum.
/// </summary>
internal static class SelectedTabExtensions
{
    /// <summary>
    /// Gets the translated string for this enum.
    /// </summary>
    /// <param name="selectedTab">Enum value to translate.</param>
    /// <returns>Translated string.</returns>
    public static string GetLocString(this SelectedTab selectedTab) => selectedTab switch
    {
        SelectedTab.DevPlugins => PluginCategoryManager.Locs.Group_DevTools,
        SelectedTab.InstalledPlugins => PluginCategoryManager.Locs.Group_Installed,
        SelectedTab.AvailablePlugins => PluginCategoryManager.Locs.Group_Available,
        SelectedTab.Changelog => PluginCategoryManager.Locs.Group_Changelog,
        _ => $"No Translation Available for {selectedTab}",
    };
}
