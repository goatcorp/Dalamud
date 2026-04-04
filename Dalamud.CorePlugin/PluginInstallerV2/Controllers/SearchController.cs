using System;
using System.Collections.Generic;
using System.Linq;

using Dalamud.CorePlugin.PluginInstallerV2.Enums;
using Dalamud.CorePlugin.PluginInstallerV2.Enums.EnumExtensions;

namespace Dalamud.CorePlugin.PluginInstallerV2.Controllers;

/// <summary>
/// Class responsible for
/// </summary>
internal class SearchController
{
    public string SearchString = string.Empty;

    public Dictionary<SortOptions, string> SortOptions;
    public SortOptions SelectedSortOption = Enums.SortOptions.Alphabetically;

    public SearchController()
    {
        // Generate this each time the window is opened in case of language change.
        this.SortOptions = Enum.GetValues<SortOptions>().ToDictionary(key => key, value => value.GetLocString());
    }

    public void UpdateSearch()
    {
    }

    public void ClearSearch()
    {
    }
}
