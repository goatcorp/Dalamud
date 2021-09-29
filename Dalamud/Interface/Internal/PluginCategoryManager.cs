using System;
using System.Collections.Generic;

using CheapLoc;
using Dalamud.Plugin.Internal.Types;

namespace Dalamud.Interface.Internal
{
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
            new(10, "special.devInstalled", () => Locs.Category_DevInstalled),
            new(11, "special.devIconTester", () => Locs.Category_IconTester),
            new(FirstTagBasedCategoryId + 0, "other", () => Locs.Category_Other),
            new(FirstTagBasedCategoryId + 1, "jobs", () => Locs.Category_Jobs),
            new(FirstTagBasedCategoryId + 2, "ui", () => Locs.Category_UI),
            new(FirstTagBasedCategoryId + 3, "minigame", () => Locs.Category_MiniGames),
            new(FirstTagBasedCategoryId + 4, "inventory", () => Locs.Category_Inventory),
            new(FirstTagBasedCategoryId + 5, "sound", () => Locs.Category_Sound),
            new(FirstTagBasedCategoryId + 6, "social", () => Locs.Category_Social),

            // order doesn't matter, all tag driven categories should have Id >= FirstTagBasedCategoryId
        };

        private GroupInfo[] groupList =
        {
            new(GroupKind.DevTools, () => Locs.Group_DevTools, 10, 11),
            new(GroupKind.Installed, () => Locs.Group_Installed, 0),
            new(GroupKind.Available, () => Locs.Group_Available, 0),

            // order important, used for drawing, keep in sync with defaults for currentGroupIdx
        };

        private int currentGroupIdx = 2;
        private int currentCategoryIdx = 0;
        private bool isContentDirty;

        private Dictionary<PluginManifest, int[]> mapPluginCategories = new();
        private List<int> highlightedCategoryIds = new();

        /// <summary>
        /// Forces plugin category tags, overrides settings from manifest.
        ///   key: PluginManifest.InternalName (lowercase, no spaces),
        ///   value: list of category tags, <see cref="categoryList"/>.
        /// </summary>
        private Dictionary<string, string[]> mapPluginCategoryTagOverrides = new();

        /// <summary>
        /// Fallback plugin category tags, used only when manifest doesn't contain any.
        ///   key: PluginManifest.InternalName (lowercase, no spaces),
        ///   value: list of category tags, <see cref="categoryList"/>.
        /// </summary>
        private Dictionary<string, string[]> mapPluginCategoryTagFallbacks = new()
        {
            // temporary for testing, should be removed when manifests are updated
            ["accuratecountdown"] = new string[] { "UI" },
            ["adventurerinneed"] = new string[] { "UI" },
            ["aethersense"] = new string[] { "Other" },
            ["autovisor"] = new string[] { "UI" },
            ["betterpartyfinder"] = new string[] { "UI" },
            ["browserhost.plugin"] = new string[] { "UI" },
            ["burnttoast"] = new string[] { "UI" },
            ["chatalerts"] = new string[] { "social" },
            ["chatbubbles"] = new string[] { "social" },
            ["chatcoordinates"] = new string[] { "social" },
            ["chatextender"] = new string[] { "social" },
            ["chattranslator"] = new string[] { "social" },
            ["compass"] = new string[] { "UI" },
            ["dalamud.charactersync"] = new string[] { "other" },
            ["dalamud.discordbridge"] = new string[] { "other" },
            ["dalamud.loadingimage"] = new string[] { "other" },
            ["dalamud.richpresence"] = new string[] { "other" },
            ["dalamudvox"] = new string[] { "Other" },
            ["damageinfoplugin"] = new string[] { "UI" },
            ["deepdungeondex"] = new string[] { "UI" },
            ["easyeyes"] = new string[] { "UI" },
            ["engagetimer"] = new string[] { "jobs" },
            ["expandedsearchinfo"] = new string[] { "UI" },
            ["fantasyplayer.dalamud"] = new string[] { "UI" },
            ["fauxhollowssolver"] = new string[] { "minigames" },
            ["fcnamecolor"] = new string[] { "UI", "social" },
            ["fpsplugin"] = new string[] { "UI" },
            ["gatherbuddy"] = new string[] { "jobs" },
            ["gentletouch"] = new string[] { "UI" },
            ["globetrotter"] = new string[] { "UI" },
            ["goodmemory"] = new string[] { "UI" },
            ["harphero"] = new string[] { "jobs" },
            ["housemate"] = new string[] { "UI" },
            ["itemsearchplugin"] = new string[] { "inventory" },
            ["jobbars"] = new string[] { "jobs" },
            ["jobicons"] = new string[] { "jobs" },
            ["kapture"] = new string[] { "UI" },
            ["kingdomheartsplugin"] = new string[] { "UI" },
            ["macrochain"] = new string[] { "jobs" },
            ["maplinker"] = new string[] { "UI" },
            ["marketboardplugin"] = new string[] { "UI" },
            ["minicactpotsolver"] = new string[] { "minigames" },
            ["moaction"] = new string[] { "jobs" },
            ["namingway"] = new string[] { "jobs", "UI" },
            ["neatnoter"] = new string[] { "UI" },
            ["nosoliciting"] = new string[] { "UI", "social" },
            ["oopsalllalafells"] = new string[] { "other" },
            ["orchestrion"] = new string[] { "sound" },
            ["owofy"] = new string[] { "social" },
            ["peepingtom"] = new string[] { "UI" },
            ["pennypincher"] = new string[] { "UI", "inventory" },
            ["pingplugin"] = new string[] { "UI" },
            ["pixelperfect"] = new string[] { "UI" },
            ["playertrack"] = new string[] { "UI", "social" },
            ["prefpro"] = new string[] { "UI" },
            ["pricecheck"] = new string[] { "UI", "inventory" },
            ["qolbar"] = new string[] { "UI" },
            ["quest map"] = new string[] { "UI" },
            ["remindme"] = new string[] { "UI" },
            ["rezpls"] = new string[] { "UI" },
            ["sillychat"] = new string[] { "other" },
            ["simpletweaksplugin"] = new string[] { "UI" },
            ["skillswap"] = new string[] { "jobs" },
            ["slidecast"] = new string[] { "jobs" },
            ["sonarplugin"] = new string[] { "UI" },
            ["soundfilter"] = new string[] { "sound" },
            ["soundsetter"] = new string[] { "sound" },
            ["teleporterplugin"] = new string[] { "UI" },
            ["textboxstyler"] = new string[] { "UI" },
            ["texttotalk"] = new string[] { "UI" },
            ["thegreatseparator"] = new string[] { "UI" },
            ["titleedit"] = new string[] { "UI" },
            ["tourist"] = new string[] { "UI" },
            ["triadbuddy"] = new string[] { "minigames" },
            ["vfxeditor"] = new string[] { "UI" },
            ["visibility"] = new string[] { "UI" },
            ["voidlist"] = new string[] { "UI" },
            ["waymarkpresetplugin"] = new string[] { "UI" },
            ["wintitle"] = new string[] { "UI" },
            ["woldo"] = new string[] { "UI" },
            ["wondroustailssolver"] = new string[] { "minigames" },
            ["xivchat"] = new string[] { "social" },
            ["xivcombo"] = new string[] { "jobs" },
        };

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
        /// Gets or sets current category.
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
        /// Gets a value indicating whether category content needs to be rebuild with BuildCategoryContent() function.
        /// </summary>
        public bool IsContentDirty => this.isContentDirty;

        /// <summary>
        /// Gets a value indicating whether <see cref="CurrentCategoryIdx"/> and <see cref="CurrentGroupIdx"/> are valid.
        /// </summary>
        public bool IsSelectionValid =>
            (this.currentGroupIdx >= 0) &&
            (this.currentGroupIdx < this.groupList.Length) &&
            (this.currentCategoryIdx >= 0) &&
            (this.currentCategoryIdx < this.categoryList.Length);

        /// <summary>
        /// Rebuild available categories based on currently available plugins.
        /// </summary>
        /// <param name="availablePlugins">list of all available plugin manifests to install.</param>
        public void BuildCategories(IEnumerable<PluginManifest> availablePlugins)
        {
            // rebuild map plugin name -> categoryIds
            this.mapPluginCategories.Clear();

            var categoryList = new List<int>();
            var allCategoryIndices = new List<int>();

            foreach (var plugin in availablePlugins)
            {
                categoryList.Clear();

                var pluginCategoryTags = this.GetCategoryTagsForManifest(plugin);
                if (pluginCategoryTags != null)
                {
                    foreach (var tag in pluginCategoryTags)
                    {
                        // only tags from whitelist can be accepted
                        int matchIdx = Array.FindIndex(this.CategoryList, x => x.Tag.Equals(tag, StringComparison.InvariantCultureIgnoreCase));
                        if (matchIdx >= 0)
                        {
                            int categoryId = this.CategoryList[matchIdx].CategoryId;
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

                // always add, even if empty
                this.mapPluginCategories.Add(plugin, categoryList.ToArray());
            }

            // sort all categories by their loc name
            allCategoryIndices.Sort((idxX, idxY) => this.CategoryList[idxX].Name.CompareTo(this.CategoryList[idxY].Name));

            // rebuild all categories in group, leaving first entry = All intact and always on top
            var groupAvail = Array.Find(this.groupList, x => x.GroupKind == GroupKind.Available);
            if (groupAvail.Categories.Count > 1)
            {
                groupAvail.Categories.RemoveRange(1, groupAvail.Categories.Count - 1);
            }

            foreach (var categoryIdx in allCategoryIndices)
            {
                groupAvail.Categories.Add(this.CategoryList[categoryIdx].CategoryId);
            }

            this.isContentDirty = true;
        }

        /// <summary>
        /// Filters list of available plugins based on currently selected category.
        /// Resets <see cref="isContentDirty"/>.
        /// </summary>
        /// <param name="plugins">List of available plugins to install.</param>
        /// <returns>Filtered list of plugins.</returns>
        public List<PluginManifest> GetCurrentCategoryContent(IEnumerable<PluginManifest> plugins)
        {
            var result = new List<PluginManifest>();

            if (this.IsSelectionValid)
            {
                var groupInfo = this.groupList[this.currentGroupIdx];

                bool includeAll = (this.currentCategoryIdx == 0) || (groupInfo.GroupKind != GroupKind.Available);
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
                            int matchIdx = Array.IndexOf(pluginCategoryIds, selectedCategoryInfo.CategoryId);
                            if (matchIdx >= 0)
                            {
                                result.Add(plugin);
                            }
                        }
                    }
                }
            }

            this.isContentDirty = false;
            return result;
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
            var nameKey = pluginManifest.InternalName.ToLowerInvariant().Replace(" ", string.Empty);
            if (this.mapPluginCategoryTagOverrides.TryGetValue(nameKey, out var overrideTags))
            {
                return overrideTags;
            }

            if (pluginManifest.CategoryTags != null)
            {
                return pluginManifest.CategoryTags;
            }

            if (this.mapPluginCategoryTagFallbacks.TryGetValue(nameKey, out var fallbackTags))
            {
                return fallbackTags;
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
            public CategoryInfo(int categoryId, string tag, Func<string> nameFunc)
            {
                this.CategoryId = categoryId;
                this.Tag = tag;
                this.nameFunc = nameFunc;
            }

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

        private static class Locs
        {
            #region UI groups

            public static string Group_DevTools => Loc.Localize("InstallerDevTools", "Dev Tools");

            public static string Group_Installed => Loc.Localize("InstallerInstalledPlugins", "Installed Plugins");

            public static string Group_Available => Loc.Localize("InstallerAvailablePlugins", "Available Plugins");

            #endregion

            #region Categories

            public static string Category_All => Loc.Localize("InstallerCategoryAll", "All");

            public static string Category_DevInstalled => Loc.Localize("InstallerInstalledDevPlugins", "Installed Dev Plugins");

            public static string Category_IconTester => "Image/Icon Tester";

            public static string Category_Other => Loc.Localize("InstallerCategoryOther", "Other");

            public static string Category_Jobs => Loc.Localize("InstallerCategoryJobs", "Jobs");

            public static string Category_UI => Loc.Localize("InstallerCategoryUI", "UI");

            public static string Category_MiniGames => Loc.Localize("InstallerCategoryMiniGames", "Mini games");

            public static string Category_Inventory => Loc.Localize("InstallerCategoryInventory", "Inventory");

            public static string Category_Sound => Loc.Localize("InstallerCategorySound", "Sound");

            public static string Category_Social => Loc.Localize("InstallerCategorySocial", "Social");

            #endregion
        }
    }
}
