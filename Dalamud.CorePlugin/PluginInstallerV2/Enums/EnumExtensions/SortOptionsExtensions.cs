namespace Dalamud.CorePlugin.PluginInstallerV2.Enums.EnumExtensions;

/// <summary>
/// Extension class for getting localized strings from SortOptions enum.
/// </summary>
internal static class SortOptionsExtensions
{
    /// <summary>
    /// Gets the translated string for this enum.
    /// </summary>
    /// <param name="sortOption">Enum value to translate.</param>
    /// <returns>Translated string.</returns>
    public static string GetLocString(this SortOptions sortOption) => sortOption switch
    {
        SortOptions.Alphabetically => PluginInstallerLocs.SortBy_Alphabetical,
        SortOptions.DownloadCount => PluginInstallerLocs.SortBy_DownloadCounts,
        SortOptions.LastUpdate => PluginInstallerLocs.SortBy_LastUpdate,
        SortOptions.New => PluginInstallerLocs.SortBy_NewOrNot,
        SortOptions.NotInstalled => PluginInstallerLocs.SortBy_NotInstalled,
        SortOptions.Enabled => PluginInstallerLocs.SortBy_EnabledDisabled,
        SortOptions.InCollection => PluginInstallerLocs.SortBy_ProfileOrNot,
        _ => $"No Translation Available for {sortOption}",
    };
}
