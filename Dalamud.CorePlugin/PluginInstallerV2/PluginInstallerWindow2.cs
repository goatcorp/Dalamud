using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using Dalamud.Bindings.ImGui;
using Dalamud.Configuration.Internal;
using Dalamud.CorePlugin.PluginInstallerV2.Controllers;
using Dalamud.CorePlugin.PluginInstallerV2.Enums;
using Dalamud.CorePlugin.PluginInstallerV2.Enums.EnumExtensions;
using Dalamud.CorePlugin.PluginInstallerV2.Widgets;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Internal;

namespace Dalamud.CorePlugin.PluginInstallerV2;

/// <summary>
/// Class responsible for drawing the main plugin window.
/// </summary>
internal class PluginInstallerWindow2 : Window, IDisposable
{
    private static readonly ModuleLog Log = new("PluginInstaller2");

    private readonly DevPluginsWidget devPluginsWidget;
    private readonly InstalledPluginsWidget installedPluginsWidget;
    private readonly AvailablePluginsWidget availablePluginsWidget;
    private readonly ChangelogWidget changelogWidget;
    private readonly CollectionsWidget collectionsWidget;

    private SelectedTab selectedTab = SelectedTab.Default;
    private Task? updatePluginsTask;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginInstallerWindow2"/> class.
    /// </summary>
    public PluginInstallerWindow2()
        : base("CorePlugin")
    {
        this.IsOpen = true;

        this.Size = new Vector2(730.0f, 570.0f);
        this.SizeCondition = ImGuiCond.FirstUseEver;

        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = this.Size.Value,
        };

        this.SearchController = new SearchController();
        this.PluginListManager = new PluginListManager();
        this.FontManager = new FontManager();

        this.devPluginsWidget = new DevPluginsWidget { ParentWindow = this };
        this.installedPluginsWidget = new InstalledPluginsWidget { ParentWindow = this };
        this.availablePluginsWidget = new AvailablePluginsWidget { ParentWindow = this };
        this.changelogWidget = new ChangelogWidget { ParentWindow = this };
        this.collectionsWidget = new CollectionsWidget { ParentWindow = this };

        // Delegate search update events to all listeners.
        this.SearchController.OnSearchUpdated += this.devPluginsWidget.OnSearchUpdated;
        this.SearchController.OnSearchUpdated += this.installedPluginsWidget.OnSearchUpdated;
        this.SearchController.OnSearchUpdated += this.availablePluginsWidget.OnSearchUpdated;
        this.SearchController.OnSearchUpdated += this.changelogWidget.OnSearchUpdated;
        this.SearchController.OnSearchUpdated += this.collectionsWidget.OnSearchUpdated;

        Log.Verbose("Plugin Installer v2 - Constructed and setup.");
    }

    /// <summary>
    /// Gets search Controller for Plugin Installer Window.
    /// </summary>
    public SearchController SearchController { get; init; }

    /// <summary>
    /// Gets plugin manager responsible for managing plugin lists.
    /// </summary>
    public PluginListManager PluginListManager { get; init; }

    /// <summary>
    /// Gets Font manager responsible for holding a larger font for displaying plugin names.
    /// </summary>
    public FontManager FontManager { get; init; }

    // todo: DEBUG FUNCTIONALITY, REMOVE FOR RELEASE
    private bool ShowChildBorders { get; set; } = false;

    /// <inheritdoc/>
    public void Dispose()
    {
        this.FontManager.Dispose();

        // Probably not necessary to unregister these, but it's good practice.
        this.SearchController.OnSearchUpdated -= this.devPluginsWidget.OnSearchUpdated;
        this.SearchController.OnSearchUpdated -= this.installedPluginsWidget.OnSearchUpdated;
        this.SearchController.OnSearchUpdated -= this.availablePluginsWidget.OnSearchUpdated;
        this.SearchController.OnSearchUpdated -= this.changelogWidget.OnSearchUpdated;
        this.SearchController.OnSearchUpdated -= this.collectionsWidget.OnSearchUpdated;
    }

    /// <inheritdoc/>
    public override void OnOpen()
    {
    }

    /// <inheritdoc/>
    public override void Draw()
    {
        using var id = ImRaii.PushId("PluginInstallerWindow2");

        ImGuiHelpers.ScaledDummy(2.0f);

        this.DrawSearchHeader();
        this.DrawTabBar();

        ImGuiHelpers.ScaledDummy(2.0f);

        ImGui.Separator();

        ImGuiHelpers.ScaledDummy(2.0f);

        this.DrawContents();

        ImGui.Separator();

        ImGuiHelpers.ScaledDummy(2.0f);

        this.DrawFooter();
    }

    private void DrawSearchHeader()
    {
        var headerHeight = 35.0f * ImGuiHelpers.GlobalScale;

        ImGui.SetCursorPosX((ImGui.GetContentRegionMax().X / 6.0f) + 5.0f);
        using var child = ImRaii.Child("SearchHeader"u8, new Vector2(ImGui.GetContentRegionMax().X * (2.0f / 3.0f), headerHeight), this.ShowChildBorders);
        if (!child.Success)
        {
            return;
        }

        var currentSearchString = this.SearchController.SearchString;

        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X * (3.0f / 5.0f));
        if (ImGui.InputTextWithHint("##SearchInput", PluginInstallerLocs.Header_SearchPlaceholder, ref currentSearchString))
        {
            this.SearchController.UpdateSearch(currentSearchString);
        }

        ImGui.SameLine();

        if (ImGuiComponents.IconButton(FontAwesomeIcon.Times))
        {
            this.SearchController.ClearSearch();
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(PluginInstallerLocs.Header_ClearSearchTooltip);
        }

        ImGui.SameLine();

        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        this.DrawSortOptionDropdown();
    }

    private void DrawTabBar()
    {
        var tabBarHeight = 30.0f * ImGuiHelpers.GlobalScale;

        // Might consider reworking this to have fixed button sizes, but somehow center them instead of stretching them.
        ImGui.SetCursorPosX(ImGui.GetCursorPos().X + (5.0f * ImGuiHelpers.GlobalScale));
        using var child = ImRaii.Child("TabBar"u8, new Vector2(ImGui.GetContentRegionAvail().X - (10.0f * ImGuiHelpers.GlobalScale), tabBarHeight), this.ShowChildBorders);
        if (!child.Success)
        {
            return;
        }

        var activeTabs = this.GetActiveTabs();

        using var table = ImRaii.Table($"TabBarTable{activeTabs.Count}", activeTabs.Count);
        if (!table.Success)
        {
            return;
        }

        foreach (var (index, _) in activeTabs.Index())
        {
            ImGui.TableSetupColumn($"##ButtonColumn{index}", ImGuiTableColumnFlags.WidthStretch, 1.0f);
        }

        foreach (var tab in activeTabs)
        {
            using var color = ImRaii.PushColor(ImGuiCol.Button, ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonActive], this.selectedTab == tab);

            ImGui.TableNextColumn();
            if (ImGui.Button(tab.GetLocString(), new Vector2(ImGui.GetContentRegionAvail().X, 25.0f * ImGuiHelpers.GlobalScale)))
            {
                this.selectedTab = tab;
            }
        }
    }

    private List<SelectedTab> GetActiveTabs()
    {
        var dalamudConfiguration = Service<DalamudConfiguration>.Get();
        List<SelectedTab> activeTabs = [];

        if (this.PluginListManager.HasDevPlugins)
        {
            activeTabs.Add(SelectedTab.DevPlugins);
        }

        if (dalamudConfiguration.ProfilesEnabled)
        {
            activeTabs.Add(SelectedTab.Collections);
        }

        activeTabs.AddRange([SelectedTab.InstalledPlugins, SelectedTab.AvailablePlugins, SelectedTab.Changelog]);

        return activeTabs;
    }

    private void DrawContents()
    {
        var footerHeight = 42.0f * ImGuiHelpers.GlobalScale;
        using var contentsChild = ImRaii.Child("Contents"u8, new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().Y - footerHeight), this.ShowChildBorders);
        if (!contentsChild.Success)
        {
            return;
        }

        switch (this.selectedTab)
        {
            case SelectedTab.DevPlugins:
                this.devPluginsWidget.Draw();
                break;

            case SelectedTab.AvailablePlugins:
                this.availablePluginsWidget.Draw();
                break;

            case SelectedTab.InstalledPlugins:
                this.installedPluginsWidget.Draw();
                break;

            case SelectedTab.Changelog:
                this.changelogWidget.Draw();
                break;

            case SelectedTab.Collections:
                this.collectionsWidget.Draw();
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void DrawFooter()
    {
        using var footerChild = ImRaii.Child("Footer", ImGui.GetContentRegionAvail(), this.ShowChildBorders);
        if (!footerChild.Success)
        {
            return;
        }

        var buttonWidth = 150.0f * ImGuiHelpers.GlobalScale;

        // todo: this might permanently disable in some cases probably? I'm not good with async stuff. -midorikami
        using (ImRaii.Disabled(this.updatePluginsTask is not null and not { Status: TaskStatus.RanToCompletion }))
        {
            if (ImGui.Button(PluginInstallerLocs.FooterButton_UpdatePlugins, new Vector2(buttonWidth, -1)))
            {
                this.updatePluginsTask = this.PluginListManager.UpdatePlugins();
            }
        }

        ImGui.SameLine();

        if (ImGui.Button(PluginInstallerLocs.FooterButton_Settings, new Vector2(buttonWidth, -1)))
        {
            Service<DalamudInterface>.Get().ToggleSettingsWindow();
        }

        if (this.PluginListManager.HasDevPlugins)
        {
            ImGui.SameLine();

            if (ImGui.Button(PluginInstallerLocs.FooterButton_ScanDevPlugins, new Vector2(buttonWidth, -1)))
            {
                _ = Service<PluginManager>.Get().ScanDevPluginsAsync();
            }
        }

        ImGui.SameLine(ImGui.GetContentRegionMax().X - buttonWidth - ImGui.GetStyle().FramePadding.X);
        if (ImGui.Button(PluginInstallerLocs.FooterButton_Close, new Vector2(buttonWidth, -1)))
        {
            this.IsOpen = false;
        }
    }

    private void DrawSortOptionDropdown()
    {
        using var sortDropdown = ImRaii.Combo("##SortOrderDropdown", this.SearchController.SelectedSortOption.GetLocString(), ImGuiComboFlags.HeightLarge);

        if (!sortDropdown.Success)
        {
            return;
        }

        foreach (var (optionEnum, locString) in this.SearchController.OptionsDictionary)
        {
            if (ImGui.Selectable(locString, this.SearchController.SelectedSortOption == optionEnum))
            {
                this.SearchController.UpdateSortOption(optionEnum);
            }
        }
    }
}
