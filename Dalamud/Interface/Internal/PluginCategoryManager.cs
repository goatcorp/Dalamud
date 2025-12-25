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
    private const int FirstTagBasedCategoryId = 100;

    private readonly CategoryInfo[] categoryList =
    [
        new(CategoryKind.All, "special.all", () => Locs.Category_All),
        new(CategoryKind.IsTesting, "special.isTesting", () => Locs.Category_IsTesting, CategoryInfo.AppearCondition.DoPluginTest),
        new(CategoryKind.AvailableForTesting, "special.availableForTesting", () => Locs.Category_AvailableForTesting, CategoryInfo.AppearCondition.DoPluginTest),
        new(CategoryKind.Hidden, "special.hidden", () => Locs.Category_Hidden, CategoryInfo.AppearCondition.AnyHiddenPlugins),
        new(CategoryKind.DevInstalled, "special.devInstalled", () => Locs.Category_DevInstalled),
        new(CategoryKind.IconTester, "special.devIconTester", () => Locs.Category_IconTester),
        new(CategoryKind.DalamudChangelogs, "special.dalamud", () => Locs.Category_Dalamud),
        new(CategoryKind.PluginChangelogs, "special.plugins", () => Locs.Category_Plugins),
        new(CategoryKind.PluginProfiles, "special.profiles", () => Locs.Category_PluginProfiles),
        new(CategoryKind.UpdateablePlugins, "special.updateable", () => Locs.Category_UpdateablePlugins),
        new(CategoryKind.EnabledPlugins, "special.enabled", () => Locs.Category_EnabledPlugins),
        new(CategoryKind.DisabledPlugins, "special.disabled", () => Locs.Category_DisabledPlugins),
        new(CategoryKind.IncompatiblePlugins, "special.incompatible", () => Locs.Category_IncompatiblePlugins),

        // Tag-driven categories
        new(CategoryKind.Other, "other", () => Locs.Category_Other),
        new(CategoryKind.Jobs, "jobs", () => Locs.Category_Jobs),
        new(CategoryKind.Ui, "ui", () => Locs.Category_UI),
        new(CategoryKind.MiniGames, "minigames", () => Locs.Category_MiniGames),
        new(CategoryKind.Inventory, "inventory", () => Locs.Category_Inventory),
        new(CategoryKind.Sound, "sound", () => Locs.Category_Sound),
        new(CategoryKind.Social, "social", () => Locs.Category_Social),
        new(CategoryKind.Utility, "utility", () => Locs.Category_Utility)

        // order doesn't matter, all tag driven categories should have Id >= FirstTagBasedCategoryId
    ];

    private GroupInfo[] groupList =
    [
        new(GroupKind.DevTools, () => Locs.Group_DevTools, CategoryKind.DevInstalled, CategoryKind.IconTester),
        new(GroupKind.Installed, () => Locs.Group_Installed, CategoryKind.All, CategoryKind.IsTesting, CategoryKind.UpdateablePlugins, CategoryKind.PluginProfiles, CategoryKind.EnabledPlugins, CategoryKind.DisabledPlugins, CategoryKind.IncompatiblePlugins),
        new(GroupKind.Available, () => Locs.Group_Available, CategoryKind.All),
        new(GroupKind.Changelog, () => Locs.Group_Changelog, CategoryKind.All, CategoryKind.DalamudChangelogs, CategoryKind.PluginChangelogs)

        // order important, used for drawing, keep in sync with defaults for currentGroupIdx
    ];

    private int currentGroupIdx = 2;
    private CategoryKind currentCategoryKind = CategoryKind.All;
    private bool isContentDirty;

    private Dictionary<PluginManifest, CategoryKind[]> mapPluginCategories = new();
    private List<CategoryKind> highlightedCategoryKinds = new();

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
    /// Type of category.
    /// </summary>
    public enum CategoryKind
    {
        /// <summary>
        /// All plugins.
        /// </summary>
        All = 0,
        
        /// <summary>
        /// Plugins currently being tested.
        /// </summary>
        IsTesting = 1,
        
        /// <summary>
        /// Plugins available for testing.
        /// </summary>
        AvailableForTesting = 2,
        
        /// <summary>
        /// Plugins that were hidden.
        /// </summary>
        Hidden = 3,
        
        /// <summary>
        /// Installed dev plugins.
        /// </summary>
        DevInstalled = 10,
        
        /// <summary>
        /// Icon tester.
        /// </summary>
        IconTester = 11,
        
        /// <summary>
        /// Changelogs for Dalamud.
        /// </summary>
        DalamudChangelogs = 12,
        
        /// <summary>
        /// Changelogs for plugins.
        /// </summary>
        PluginChangelogs = 13,
        
        /// <summary>
        /// Change plugin profiles.
        /// </summary>
        PluginProfiles = 14,
        
        /// <summary>
        /// Updateable plugins.
        /// </summary>
        UpdateablePlugins = 15,
        
        /// <summary>
        /// Enabled plugins.
        /// </summary>
        EnabledPlugins = 16,

        /// <summary>
        /// Disabled plugins.
        /// </summary>
        DisabledPlugins = 17,

        /// <summary>
        /// Incompatible plugins.
        /// </summary>
        IncompatiblePlugins = 18,

        /// <summary>
        /// Plugins tagged as "other".
        /// </summary>
        Other = FirstTagBasedCategoryId + 0,
        
        /// <summary>
        /// Plugins tagged as "jobs".
        /// </summary>
        Jobs = FirstTagBasedCategoryId + 1,
        
        /// <summary>
        /// Plugins tagged as "ui".
        /// </summary>
        Ui = FirstTagBasedCategoryId + 2,
        
        /// <summary>
        /// Plugins tagged as "minigames".
        /// </summary>
        MiniGames = FirstTagBasedCategoryId + 3,
        
        /// <summary>
        /// Plugins tagged as "inventory".
        /// </summary>
        Inventory = FirstTagBasedCategoryId + 4,
        
        /// <summary>
        /// Plugins tagged as "sound".
        /// </summary>
        Sound = FirstTagBasedCategoryId + 5,
        
        /// <summary>
        /// Plugins tagged as "social".
        /// </summary>
        Social = FirstTagBasedCategoryId + 6,
        
        /// <summary>
        /// Plugins tagged as "utility".
        /// </summary>
        Utility = FirstTagBasedCategoryId + 7,
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
    /// Gets or sets the current group kind.
    /// </summary>
    public GroupKind CurrentGroupKind
    {
        get => this.groupList[this.currentGroupIdx].GroupKind;
        set
        {
            var newIdx = Array.FindIndex(this.groupList, x => x.GroupKind == value);
            if (newIdx >= 0)
            {
                this.currentGroupIdx = newIdx;
                this.currentCategoryKind = this.CurrentGroup.Categories.First();
                this.isContentDirty = true;
            }
        }
    }
    
    /// <summary>
    /// Gets information about currently selected group.
    /// </summary>
    public GroupInfo CurrentGroup => this.groupList[this.currentGroupIdx];
    
    /// <summary>
    /// Gets or sets the current category kind.
    /// </summary>
    public CategoryKind CurrentCategoryKind
    {
        get => this.currentCategoryKind;
        set
        {
            if (this.currentCategoryKind != value)
            {
                this.currentCategoryKind = value;
                this.isContentDirty = true;
            }
        }
    }
    
    /// <summary>
    /// Gets information about currently selected category.
    /// </summary>
    public CategoryInfo CurrentCategory => this.categoryList.First(x => x.CategoryKind == this.currentCategoryKind);

    /// <summary>
    /// Gets a value indicating whether current group + category selection changed recently.
    /// Changes in Available group should be followed with <see cref="GetCurrentCategoryContent"/>, everything else can use <see cref="ResetContentDirty"/>.
    /// </summary>
    public bool IsContentDirty => this.isContentDirty;

    /// <summary>
    /// Gets a value indicating whether <see cref="CurrentCategoryKind"/> and <see cref="CurrentGroupKind"/> are valid.
    /// </summary>
    public bool IsSelectionValid =>
        (this.currentGroupIdx >= 0) &&
        (this.currentGroupIdx < this.groupList.Length) &&
        this.groupList[this.currentGroupIdx].Categories.Contains(this.currentCategoryKind);

    /// <summary>
    /// Rebuild available categories based on currently available plugins.
    /// </summary>
    /// <param name="availablePlugins">list of all available plugin manifests to install.</param>
    public void BuildCategories(IEnumerable<PluginManifest> availablePlugins)
    {
        // rebuild map plugin name -> categoryIds
        this.mapPluginCategories.Clear();

        var groupAvail = Array.Find(this.groupList, x => x.GroupKind == GroupKind.Available);
        var prevCategoryIds = new List<CategoryKind>();
        prevCategoryIds.AddRange(groupAvail.Categories);

        var categoryList = new List<CategoryKind>();
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
                        var categoryKind = this.CategoryList[matchIdx].CategoryKind;
                        if ((int)categoryKind >= FirstTagBasedCategoryId)
                        {
                            categoryList.Add(categoryKind);

                            if (!allCategoryIndices.Contains(matchIdx))
                            {
                                allCategoryIndices.Add(matchIdx);
                            }
                        }
                    }
                }
            }

            if (manifest.IsTestingExclusive || manifest.IsAvailableForTesting)
                categoryList.Add(CategoryKind.AvailableForTesting);

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
            groupAvail.Categories.Add(this.CategoryList[categoryIdx].CategoryKind);
        }
        
        // Hidden at the end
        groupAvail.Categories.Add(CategoryKind.Hidden);

        // compare with prev state and mark as dirty if needed
        var noCategoryChanges = prevCategoryIds.SequenceEqual(groupAvail.Categories);
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

            var includeAll = this.currentCategoryKind == CategoryKind.All ||
                             this.currentCategoryKind == CategoryKind.Hidden ||
                             groupInfo.GroupKind != GroupKind.Available;

            if (includeAll)
            {
                result.AddRange(plugins);
            }
            else
            {
                var selectedCategoryInfo = Array.Find(this.categoryList, x => x.CategoryKind == this.currentCategoryKind);

                foreach (var plugin in plugins)
                {
                    if (this.mapPluginCategories.TryGetValue(plugin, out var pluginCategoryIds))
                    {
                        var matchIdx = Array.IndexOf(pluginCategoryIds, selectedCategoryInfo.CategoryKind);
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
        ArgumentNullException.ThrowIfNull(plugins);
        
        this.highlightedCategoryKinds.Clear();

        foreach (var entry in plugins)
        {
            if (this.mapPluginCategories.TryGetValue(entry, out var pluginCategories))
            {
                foreach (var categoryKind in pluginCategories)
                {
                    if (!this.highlightedCategoryKinds.Contains(categoryKind))
                    {
                        this.highlightedCategoryKinds.Add(categoryKind);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Checks if category should be highlighted.
    /// </summary>
    /// <param name="categoryKind">CategoryKind to check.</param>
    /// <returns>true if highlight is needed.</returns>
    public bool IsCategoryHighlighted(CategoryKind categoryKind) => this.highlightedCategoryKinds.Contains(categoryKind);

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
        public CategoryKind CategoryKind;

        /// <summary>
        /// Tag from plugin manifest to match.
        /// </summary>
        public string Tag;

        private Func<string> nameFunc;

        /// <summary>
        /// Initializes a new instance of the <see cref="CategoryInfo"/> struct.
        /// </summary>
        /// <param name="categoryKind">Kind of the category.</param>
        /// <param name="tag">Tag to match.</param>
        /// <param name="nameFunc">Function returning localized name of category.</param>
        /// <param name="condition">Condition to be checked when deciding whether this category should be shown.</param>
        public CategoryInfo(CategoryKind categoryKind, string tag, Func<string> nameFunc, AppearCondition condition = AppearCondition.None)
        {
            this.CategoryKind = categoryKind;
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
            
            /// <summary>
            /// Check if there are any hidden plugins.
            /// </summary>
            AnyHiddenPlugins,
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
        public List<CategoryKind> Categories;

        private Func<string> nameFunc;

        /// <summary>
        /// Initializes a new instance of the <see cref="GroupInfo"/> struct.
        /// </summary>
        /// <param name="groupKind">Type of group.</param>
        /// <param name="nameFunc">Function returning localized name of category.</param>
        /// <param name="categories">List of category Ids to hardcode.</param>
        public GroupInfo(GroupKind groupKind, Func<string> nameFunc, params CategoryKind[] categories)
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

        public static string Category_Hidden => Loc.Localize("InstallerCategoryHidden", "Hidden");
        
        public static string Category_DevInstalled => Loc.Localize("InstallerInstalledDevPlugins", "Installed Dev Plugins");

        public static string Category_IconTester => "Image/Icon Tester";

        public static string Category_PluginProfiles => Loc.Localize("InstallerCategoryPluginProfiles", "Plugin Collections");

        public static string Category_UpdateablePlugins => Loc.Localize("InstallerCategoryCanBeUpdated", "Can be updated");

        public static string Category_EnabledPlugins => Loc.Localize("InstallerCategoryEnabledPlugins", "Enabled");

        public static string Category_DisabledPlugins => Loc.Localize("InstallerCategoryDisabledPlugins", "Disabled");

        public static string Category_IncompatiblePlugins => Loc.Localize("InstallerCategoryIncompatiblePlugins", "Incompatible");
        
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
