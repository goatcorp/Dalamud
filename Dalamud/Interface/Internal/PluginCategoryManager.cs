using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using CheapLoc;
using Dalamud.Plugin.Internal;
using Dalamud.Plugin.Internal.Types;

namespace Dalamud.Interface.Internal;

/// <summary>
/// Manage category filters for PluginInstallerWindow.
/// </summary>
internal class PluginCategoryManager
{
    /// <summary>
    /// First categoryId for tag based categories.
    /// </summary>
    public const int FirstTagBasedCategoryId = 100;

    private readonly CategoryInfo[] categoryList =
    {
        new(0, "special.all", () => Locs.Category_All),
        new(1, "special.isTesting", () => Locs.Category_IsTesting, CategoryInfo.AppearCondition.DoPluginTest),
        new(2, "special.availableForTesting", () => Locs.Category_AvailableForTesting, CategoryInfo.AppearCondition.DoPluginTest),
        new(10, "special.devInstalled", () => Locs.Category_DevInstalled),
        new(11, "special.devIconTester", () => Locs.Category_IconTester),
        new(12, "special.dalamud", () => Locs.Category_Dalamud),
        new(13, "special.plugins", () => Locs.Category_Plugins),
        new(14, "special.profiles", () => Locs.Category_PluginProfiles),
        new(FirstTagBasedCategoryId + 0, "other", () => Locs.Category_Other),
        new(FirstTagBasedCategoryId + 1, "jobs", () => Locs.Category_Jobs),
        new(FirstTagBasedCategoryId + 2, "ui", () => Locs.Category_UI),
        new(FirstTagBasedCategoryId + 3, "minigames", () => Locs.Category_MiniGames),
        new(FirstTagBasedCategoryId + 4, "inventory", () => Locs.Category_Inventory),
        new(FirstTagBasedCategoryId + 5, "sound", () => Locs.Category_Sound),
        new(FirstTagBasedCategoryId + 6, "social", () => Locs.Category_Social),
        new(FirstTagBasedCategoryId + 7, "utility", () => Locs.Category_Utility),

        // order doesn't matter, all tag driven categories should have Id >= FirstTagBasedCategoryId
    };

    private GroupInfo[] groupList =
    {
        new(GroupKind.DevTools, () => Locs.Group_DevTools, 10, 11),
        new(GroupKind.Installed, () => Locs.Group_Installed, 0, 1, 14),
        new(GroupKind.Available, () => Locs.Group_Available, 0),
        new(GroupKind.Changelog, () => Locs.Group_Changelog, 0, 12, 13),

        // order important, used for drawing, keep in sync with defaults for currentGroupIdx
    };

    private int currentGroupIdx = 2;
    private int currentCategoryIdx = 0;
    private bool isContentDirty;

    private Dictionary<PluginManifest, int[]> mapPluginCategories = new();
    private List<int> highlightedCategoryIds = new();

    /// <summary>
    /// Type of category group.
    /// </summary>
    public enum GroupKind
    {
        /// <summary>
        /// UI group: dev mode only.
        /// </summary>
        DevTools,

        /// <summary>
        /// UI group: installed plugins.
        /// </summary>
        Installed,

        /// <summary>
        /// UI group: plugins that can be installed.
        /// </summary>
        Available,

        /// <summary>
        /// UI group: changelog of plugins.
        /// </summary>
        Changelog,
    }

    /// <summary>
    /// Gets the list of all known categories.
    /// </summary>
    public CategoryInfo[] CategoryList => this.categoryList;

    /// <summary>
    /// Gets the list of all known UI groups.
    /// </summary>
    public GroupInfo[] GroupList => this.groupList;

    /// <summary>
    /// Gets or sets current group.
    /// </summary>
    public int CurrentGroupIdx
    {
        get => this.currentGroupIdx;
        set
        {
            if (this.currentGroupIdx != value)
            {
                this.currentGroupIdx = value;
                this.currentCategoryIdx = 0;
                this.isContentDirty = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets current category, index in current Group.Categories array.
    /// </summary>
    public int CurrentCategoryIdx
    {
        get => this.currentCategoryIdx;
        set
        {
            if (this.currentCategoryIdx != value)
            {
                this.currentCategoryIdx = value;
                this.isContentDirty = true;
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether current group + category selection changed recently.
    /// Changes in Available group should be followed with <see cref="GetCurrentCategoryContent"/>, everything else can use <see cref="ResetContentDirty"/>.
    /// </summary>
    public bool IsContentDirty => this.isContentDirty;

    /// <summary>
    /// Gets a value indicating whether <see cref="CurrentCategoryIdx"/> and <see cref="CurrentGroupIdx"/> are valid.
    /// </summary>
    public bool IsSelectionValid =>
        (this.currentGroupIdx >= 0) &&
        (this.currentGroupIdx < this.groupList.Length) &&
        (this.currentCategoryIdx >= 0) &&
        (this.currentCategoryIdx < this.groupList[this.currentGroupIdx].Categories.Count);

    /// <summary>
    /// Rebuild available categories based on currently available plugins.
    /// </summary>
    /// <param name="availablePlugins">list of all available plugin manifests to install.</param>
    public void BuildCategories(IEnumerable<PluginManifest> availablePlugins)
    {
        // rebuild map plugin name -> categoryIds
        this.mapPluginCategories.Clear();

        var groupAvail = Array.Find(this.groupList, x => x.GroupKind == GroupKind.Available);
        var prevCategoryIds = new List<int>();
        prevCategoryIds.AddRange(groupAvail.Categories);

        var categoryList = new List<int>();
        var allCategoryIndices = new List<int>();

        foreach (var manifest in availablePlugins)
        {
            categoryList.Clear();

            var pluginCategoryTags = this.GetCategoryTagsForManifest(manifest);
            if (pluginCategoryTags != null)
            {
                foreach (var tag in pluginCategoryTags)
                {
                    // only tags from whitelist can be accepted
                    var matchIdx = Array.FindIndex(this.CategoryList, x => x.Tag.Equals(tag, StringComparison.InvariantCultureIgnoreCase));
                    if (matchIdx >= 0)
                    {
                        var categoryId = this.CategoryList[matchIdx].CategoryId;
                        if (categoryId >= FirstTagBasedCategoryId)
                        {
                            categoryList.Add(categoryId);

                            if (!allCategoryIndices.Contains(matchIdx))
                            {
                                allCategoryIndices.Add(matchIdx);
                            }
                        }
                    }
                }
            }

            if (PluginManager.HasTestingVersion(manifest) || manifest.IsTestingExclusive)
                categoryList.Add(2);

            // always add, even if empty
            this.mapPluginCategories.Add(manifest, categoryList.ToArray());
        }

        // sort all categories by their loc name
        allCategoryIndices.Sort((idxX, idxY) => this.CategoryList[idxX].Name.CompareTo(this.CategoryList[idxY].Name));
        allCategoryIndices.Insert(0, 2); // "Available for testing"

        // rebuild all categories in group, leaving first entry = All intact and always on top
        if (groupAvail.Categories.Count > 1)
        {
            groupAvail.Categories.RemoveRange(1, groupAvail.Categories.Count - 1);
        }

        foreach (var categoryIdx in allCategoryIndices)
        {
            groupAvail.Categories.Add(this.CategoryList[categoryIdx].CategoryId);
        }

        // compare with prev state and mark as dirty if needed
        var noCategoryChanges = Enumerable.SequenceEqual(prevCategoryIds, groupAvail.Categories);
        if (!noCategoryChanges)
        {
            this.isContentDirty = true;
        }
    }

    /// <summary>
    /// Filters list of available plugins based on currently selected category.
    /// Resets <see cref="IsContentDirty"/>.
    /// </summary>
    /// <param name="plugins">List of available plugins to install.</param>
    /// <returns>Filtered list of plugins.</returns>
    public List<PluginManifest> GetCurrentCategoryContent(IEnumerable<PluginManifest> plugins)
    {
        var result = new List<PluginManifest>();

        if (this.IsSelectionValid)
        {
            var groupInfo = this.groupList[this.currentGroupIdx];

            var includeAll = (this.currentCategoryIdx == 0) || (groupInfo.GroupKind != GroupKind.Available);
            if (includeAll)
            {
                result.AddRange(plugins);
            }
            else
            {
                var selectedCategoryInfo = Array.Find(this.categoryList, x => x.CategoryId == groupInfo.Categories[this.currentCategoryIdx]);

                foreach (var plugin in plugins)
                {
                    if (this.mapPluginCategories.TryGetValue(plugin, out var pluginCategoryIds))
                    {
                        var matchIdx = Array.IndexOf(pluginCategoryIds, selectedCategoryInfo.CategoryId);
                        if (matchIdx >= 0)
                        {
                            result.Add(plugin);
                        }
                    }
                }
            }
        }

        this.ResetContentDirty();
        return result;
    }

    /// <summary>
    /// Clears <see cref="IsContentDirty"/> flag, indicating that all cached values about currently selected group + category have been updated.
    /// </summary>
    public void ResetContentDirty()
    {
        this.isContentDirty = false;
    }

    /// <summary>
    /// Sets category highlight based on list of plugins. Used for searching.
    /// </summary>
    /// <param name="plugins">List of plugins whose categories should be highlighted.</param>
    public void SetCategoryHighlightsForPlugins(IEnumerable<PluginManifest> plugins)
    {
        this.highlightedCategoryIds.Clear();

        if (plugins != null)
        {
            foreach (var entry in plugins)
            {
                if (this.mapPluginCategories.TryGetValue(entry, out var pluginCategories))
                {
                    foreach (var categoryId in pluginCategories)
                    {
                        if (!this.highlightedCategoryIds.Contains(categoryId))
                        {
                            this.highlightedCategoryIds.Add(categoryId);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Checks if category should be highlighted.
    /// </summary>
    /// <param name="categoryId">CategoryId to check.</param>
    /// <returns>true if highlight is needed.</returns>
    public bool IsCategoryHighlighted(int categoryId) => this.highlightedCategoryIds.Contains(categoryId);

    private IEnumerable<string> GetCategoryTagsForManifest(PluginManifest pluginManifest)
    {
        if (pluginManifest.CategoryTags != null)
        {
            return pluginManifest.CategoryTags;
        }

        return null;
    }

    /// <summary>
    /// Plugin installer category info.
    /// </summary>
    public struct CategoryInfo
    {
        /// <summary>
        /// Unique Id number of category, tag match based should be greater of equal <see cref="FirstTagBasedCategoryId"/>.
        /// </summary>
        public int CategoryId;

        /// <summary>
        /// Tag from plugin manifest to match.
        /// </summary>
        public string Tag;

        private Func<string> nameFunc;

        /// <summary>
        /// Initializes a new instance of the <see cref="CategoryInfo"/> struct.
        /// </summary>
        /// <param name="categoryId">Unique id of category.</param>
        /// <param name="tag">Tag to match.</param>
        /// <param name="nameFunc">Function returning localized name of category.</param>
        /// <param name="condition">Condition to be checked when deciding whether this category should be shown.</param>
        public CategoryInfo(int categoryId, string tag, Func<string> nameFunc, AppearCondition condition = AppearCondition.None)
        {
            this.CategoryId = categoryId;
            this.Tag = tag;
            this.nameFunc = nameFunc;
            this.Condition = condition;
        }

        /// <summary>
        /// Conditions for categories.
        /// </summary>
        public enum AppearCondition
        {
            /// <summary>
            /// Check no conditions.
            /// </summary>
            None,

            /// <summary>
            /// Check if plugin testing is enabled.
            /// </summary>
            DoPluginTest,
        }

        /// <summary>
        /// Gets or sets the condition to be checked when rendering.
        /// </summary>
        public AppearCondition Condition { get; set; }

        /// <summary>
        /// Gets the name of category.
        /// </summary>
        public string Name => this.nameFunc();
    }

    /// <summary>
    /// Plugin installer UI group, a container for categories.
    /// </summary>
    public struct GroupInfo
    {
        /// <summary>
        /// Type of group.
        /// </summary>
        public GroupKind GroupKind;

        /// <summary>
        /// List of categories in container.
        /// </summary>
        public List<int> Categories;

        private Func<string> nameFunc;

        /// <summary>
        /// Initializes a new instance of the <see cref="GroupInfo"/> struct.
        /// </summary>
        /// <param name="groupKind">Type of group.</param>
        /// <param name="nameFunc">Function returning localized name of category.</param>
        /// <param name="categories">List of category Ids to hardcode.</param>
        public GroupInfo(GroupKind groupKind, Func<string> nameFunc, params int[] categories)
        {
            this.GroupKind = groupKind;
            this.nameFunc = nameFunc;

            this.Categories = new();
            this.Categories.AddRange(categories);
        }

        /// <summary>
        /// Gets the name of UI group.
        /// </summary>
        public string Name => this.nameFunc();
    }

    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "locs")]
    internal static class Locs
    {
        #region UI groups

        public static string Group_DevTools => Loc.Localize("InstallerDevTools", "Dev Tools");

        public static string Group_Installed => Loc.Localize("InstallerInstalledPlugins", "Installed Plugins");

        public static string Group_Available => Loc.Localize("InstallerAllPlugins", "All Plugins");

        public static string Group_Changelog => Loc.Localize("InstallerChangelog", "Changelog");

        #endregion

        #region Categories

        public static string Category_All => Loc.Localize("InstallerCategoryAll", "All");

        public static string Category_IsTesting => Loc.Localize("InstallerCategoryIsTesting", "Currently Testing");

        public static string Category_AvailableForTesting => Loc.Localize("InstallerCategoryAvailableForTesting", "Testing Available");

        public static string Category_DevInstalled => Loc.Localize("InstallerInstalledDevPlugins", "Installed Dev Plugins");

        public static string Category_IconTester => "Image/Icon Tester";

        public static string Category_PluginProfiles => Loc.Localize("InstallerCategoryPluginProfiles", "Plugin Collections");

        public static string Category_Other => Loc.Localize("InstallerCategoryOther", "Other");

        public static string Category_Jobs => Loc.Localize("InstallerCategoryJobs", "Jobs");

        public static string Category_UI => Loc.Localize("InstallerCategoryUI", "UI");

        public static string Category_MiniGames => Loc.Localize("InstallerCategoryMiniGames", "Mini games");

        public static string Category_Inventory => Loc.Localize("InstallerCategoryInventory", "Inventory");

        public static string Category_Sound => Loc.Localize("InstallerCategorySound", "Sound");

        public static string Category_Social => Loc.Localize("InstallerCategorySocial", "Social");

        public static string Category_Utility => Loc.Localize("InstallerCategoryUtility", "Utility");

        public static string Category_Plugins => Loc.Localize("InstallerCategoryPlugins", "Plugins");

        public static string Category_Dalamud => Loc.Localize("InstallerCategoryDalamud", "Dalamud");

        #endregion
    }
}
