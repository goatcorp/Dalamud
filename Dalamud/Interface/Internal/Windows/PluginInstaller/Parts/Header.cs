using System.Linq;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Components;
using Dalamud.Interface.Internal.Windows.PluginInstaller.Enums;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

namespace Dalamud.Interface.Internal.Windows.PluginInstaller.Parts;

/// <summary>
/// Class responsible for drawing the Header.
/// </summary>
internal class PluginInstallerHeader
{
    private readonly PluginInstallerWindow pluginInstaller;
    private readonly PluginCategoryManager categoryManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginInstallerHeader"/> class.
    /// </summary>
    /// <param name="pluginInstaller">Reference to main Installer Window.</param>
    /// <param name="categoryManager">Category Manager.</param>
    public PluginInstallerHeader(PluginInstallerWindow pluginInstaller, PluginCategoryManager categoryManager)
    {
        this.pluginInstaller = pluginInstaller;
        this.categoryManager = categoryManager;
    }

    /// <summary>
    /// Updates Categories when Plugins Change.
    /// </summary>
    public void UpdateCategoriesOnPluginsChange()
    {
        this.categoryManager.BuildCategories(this.pluginInstaller.pluginListAvailable);
        this.UpdateCategoriesOnSearchChange(null);
    }

    /// <summary>
    /// Updates Categories when Search Changes.
    /// </summary>
    /// <param name="previousSearchText">Previous search text.</param>
    public void UpdateCategoriesOnSearchChange(string? previousSearchText)
    {
        if (string.IsNullOrEmpty(this.pluginInstaller.searchText))
        {
            this.categoryManager.SetCategoryHighlightsForPlugins([]);

            // Reset here for good measure, as we're returning from a search
            this.pluginInstaller.openPluginCollapsibles.Clear();
        }
        else
        {
            var pluginsMatchingSearch = this.pluginInstaller.pluginListAvailable.Where(rm => !this.pluginInstaller.IsManifestFiltered(rm)).ToArray();

            // Check if the search results are different, and clear the open collapsible's if they are
            if (previousSearchText != null)
            {
                var previousSearchResults = this.pluginInstaller.pluginListAvailable.Where(rm => !this.pluginInstaller.IsManifestFiltered(rm)).ToArray();
                if (!previousSearchResults.SequenceEqual(pluginsMatchingSearch))
                    this.pluginInstaller.openPluginCollapsibles.Clear();
            }

            this.categoryManager.SetCategoryHighlightsForPlugins(pluginsMatchingSearch);
        }
    }

    /// <summary>
    /// Draw the Header.
    /// </summary>
    public void Draw()
    {
        var style = ImGui.GetStyle();
        var windowSize = ImGui.GetWindowContentRegionMax();

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - (5 * ImGuiHelpers.GlobalScale));

        var searchInputWidth = 180 * ImGuiHelpers.GlobalScale;
        var searchClearButtonWidth = 25 * ImGuiHelpers.GlobalScale;

        var sortByText = PluginInstallerLocs.SortBy_Label;
        var sortByTextWidth = ImGui.CalcTextSize(sortByText).X;
        var sortSelectables = new (string Localization, PluginSortKind SortKind)[]
        {
            (PluginInstallerLocs.SortBy_SearchScore, PluginSortKind.SearchScore),
            (PluginInstallerLocs.SortBy_Alphabetical, PluginSortKind.Alphabetical),
            (PluginInstallerLocs.SortBy_DownloadCounts, PluginSortKind.DownloadCount),
            (PluginInstallerLocs.SortBy_LastUpdate, PluginSortKind.LastUpdate),
            (PluginInstallerLocs.SortBy_NewOrNot, PluginSortKind.NewOrNot),
            (PluginInstallerLocs.SortBy_NotInstalled, PluginSortKind.NotInstalled),
            (PluginInstallerLocs.SortBy_EnabledDisabled, PluginSortKind.EnabledDisabled),
            (PluginInstallerLocs.SortBy_ProfileOrNot, PluginSortKind.ProfileOrNot),
        };
        var longestSelectableWidth = sortSelectables.Select(t => ImGui.CalcTextSize(t.Localization).X).Max();
        var selectableWidth = longestSelectableWidth + (style.FramePadding.X * 2);  // This does not include the label
        var sortSelectWidth = selectableWidth + sortByTextWidth + style.ItemInnerSpacing.X;  // Item spacing between the selectable and the label

        var headerText = PluginInstallerLocs.Header_Hint;
        var headerTextSize = ImGui.CalcTextSize(headerText);
        ImGui.Text(headerText);

        ImGui.SameLine();

        // Shift down a little to align with the middle of the header text
        var downShift = ImGui.GetCursorPosY() + (headerTextSize.Y / 4) - 2;
        ImGui.SetCursorPosY(downShift);

        ImGui.SetCursorPosX(windowSize.X - sortSelectWidth - (style.ItemSpacing.X * 2) - searchInputWidth - searchClearButtonWidth);

        var isProfileManager =
            this.pluginInstaller.categoryManager.CurrentGroupKind == PluginCategoryManager.GroupKind.Installed &&
            this.pluginInstaller.categoryManager.CurrentCategoryKind == PluginCategoryManager.CategoryKind.PluginProfiles;

        this.DrawSearch(isProfileManager, searchInputWidth, downShift, searchClearButtonWidth);

        this.DrawSort(isProfileManager, downShift, selectableWidth, sortByText, sortSelectables);
    }

    private void DrawSearch(bool isProfileManager, float searchInputWidth, float downShift, float searchClearButtonWidth)
    {
        // Disable search if profile editor
        using var disabled = ImRaii.Disabled(isProfileManager);

        var searchTextChanged = false;
        var prevSearchText = this.pluginInstaller.searchText;
        ImGui.SetNextItemWidth(searchInputWidth);
        searchTextChanged |= ImGui.InputTextWithHint(
            "###XlPluginInstaller_Search"u8,
            PluginInstallerLocs.Header_SearchPlaceholder,
            ref this.pluginInstaller.searchText,
            100,
            ImGuiInputTextFlags.AutoSelectAll);

        ImGui.SameLine();
        ImGui.SetCursorPosY(downShift);

        ImGui.SetNextItemWidth(searchClearButtonWidth);
        if (ImGuiComponents.IconButton(FontAwesomeIcon.Times))
        {
            this.pluginInstaller.searchText = string.Empty;
            searchTextChanged = true;
        }

        if (searchTextChanged)
        {
            if (this.pluginInstaller.adaptiveSort)
            {
                if (string.IsNullOrWhiteSpace(this.pluginInstaller.searchText))
                {
                    this.pluginInstaller.sortKind = PluginSortKind.Alphabetical;
                    this.pluginInstaller.filterText = PluginInstallerLocs.SortBy_Alphabetical;
                }
                else
                {
                    this.pluginInstaller.sortKind = PluginSortKind.SearchScore;
                    this.pluginInstaller.filterText = PluginInstallerLocs.SortBy_SearchScore;
                }

                this.pluginInstaller.ResortPlugins();
            }
            else if (this.pluginInstaller.sortKind == PluginSortKind.SearchScore)
            {
                this.pluginInstaller.ResortPlugins();
            }

            this.UpdateCategoriesOnSearchChange(prevSearchText);
        }
    }

    private void DrawSort(bool isProfileManager, float downShift, float selectableWidth, string sortByText, (string Localization, PluginSortKind SortKind)[] sortSelectables)
    {
        // Disable sort if changelogs or profile editor
        using var disabled = ImRaii.Disabled(this.pluginInstaller.categoryManager.CurrentGroupKind is PluginCategoryManager.GroupKind.Changelog || isProfileManager);

        ImGui.SameLine();
        ImGui.SetCursorPosY(downShift);
        ImGui.SetNextItemWidth(selectableWidth);

        using var combo = ImRaii.Combo(sortByText, this.pluginInstaller.filterText, ImGuiComboFlags.NoArrowButton);
        if (!combo)
        {
            return;
        }

        foreach (var selectable in sortSelectables)
        {
            if (selectable.SortKind is PluginSortKind.SearchScore && string.IsNullOrWhiteSpace(this.pluginInstaller.searchText))
                continue;

            if (ImGui.Selectable(selectable.Localization))
            {
                this.pluginInstaller.sortKind = selectable.SortKind;
                this.pluginInstaller.filterText = selectable.Localization;
                this.pluginInstaller.adaptiveSort = false;

                lock (this.pluginInstaller.listLock)
                {
                    this.pluginInstaller.ResortPlugins();

                    // Positions of plugins within the list is likely to change
                    this.pluginInstaller.openPluginCollapsibles.Clear();
                }
            }
        }
    }
}
