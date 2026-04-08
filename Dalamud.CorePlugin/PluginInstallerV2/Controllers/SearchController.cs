using System;
using System.Collections.Generic;
using System.Linq;

using Dalamud.CorePlugin.PluginInstallerV2.Enums;
using Dalamud.CorePlugin.PluginInstallerV2.Enums.EnumExtensions;

namespace Dalamud.CorePlugin.PluginInstallerV2.Controllers;

/// <summary>
/// Class responsible for managing the search and sorting information for the Plugin Installer.
/// </summary>
internal class SearchController
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SearchController"/> class.
    /// </summary>
    public SearchController()
    {
        // Generate this each time the window is opened in case of language change.
        this.OptionsDictionary = Enum.GetValues<SortOptions>().ToDictionary(key => key, value => value.GetLocString());
    }

    /// <summary>
    /// Event that is fired when search or sort order is changed.
    /// </summary>
    public event Action<SearchController>? OnSearchUpdated;

    /// <summary>
    /// Gets dictionary of SortOptions => Localized String mapping.
    /// This is regenerated each time the installer is opened.
    /// </summary>
    public Dictionary<SortOptions, string> OptionsDictionary { get; private set; }

    /// <summary>
    /// Gets the current search string.
    /// </summary>
    public string SearchString { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the current sorting mode value.
    /// </summary>
    public SortOptions SelectedSortOption { get; private set; } = SortOptions.Alphabetically;

    /// <summary>
    /// Updates the stored search string, and triggers a search update for listeners.
    /// </summary>
    /// <param name="newString">New search string.</param>
    public void UpdateSearch(string? newString = null)
    {
        this.SearchString = newString ?? string.Empty;
        this.OnSearchUpdated?.Invoke(this);
    }

    /// <summary>
    /// Updates the stores sort option, and triggers a update for listeenrs.
    /// </summary>
    /// <param name="optionEnum">New sort order.</param>
    public void UpdateSortOption(SortOptions optionEnum)
    {
        this.SelectedSortOption = optionEnum;
        this.OnSearchUpdated?.Invoke(this);
    }

    /// <summary>
    /// Clears the search string.
    /// </summary>
    public void ClearSearch()
    {
        this.SearchString = string.Empty;
        this.OnSearchUpdated?.Invoke(this);
    }
}
