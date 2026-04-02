using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using Dalamud.Bindings.ImGui;
using Dalamud.Configuration.Internal;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Internal.Windows.PluginInstaller.Enums;
using Dalamud.Interface.Internal.Windows.PluginInstaller.Modals;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Internal;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Plugin.Internal.Types.Manifest;
using Dalamud.Utility;

namespace Dalamud.Interface.Internal.Windows.PluginInstaller.Parts;

/// <summary>
/// Class responsible for drawing plugin category section.
/// </summary>
internal class PluginInstallerPluginCategories
{
    private readonly PluginInstallerWindow pluginInstaller;
    private readonly PluginCategoryManager categoryManager;
    private readonly Proxies proxies;

    private readonly ChangelogCategory changelogCategory;
    private readonly PluginEntry pluginEntry;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginInstallerPluginCategories"/> class.
    /// </summary>
    /// <param name="pluginInstaller">Reference to main Installer Window.</param>
    /// <param name="categoryManager">Category Manager.</param>
    /// <param name="proxies">Plugin available proxies.</param>
    /// <param name="feedbackModal">Feedback Modal.</param>
    public PluginInstallerPluginCategories(PluginInstallerWindow pluginInstaller, PluginCategoryManager categoryManager, Proxies proxies, FeedbackModal feedbackModal)
    {
        this.pluginInstaller = pluginInstaller;
        this.categoryManager = categoryManager;
        this.proxies = proxies;

        this.changelogCategory = new ChangelogCategory(pluginInstaller, categoryManager);
        this.pluginEntry = new PluginEntry(pluginInstaller, categoryManager, feedbackModal);
    }

    /// <summary>
    /// Draws category selectors.
    /// </summary>
    public void Draw()
    {
        const float useContentHeight = -40f; // button height + spacing
        const float useMenuWidth = 180f;     // works fine as static value, table can be resized by user

        var useContentWidth = ImGui.GetContentRegionAvail().X;

        this.DrawCategoryChild(useContentWidth, useContentHeight, useMenuWidth);
    }

    private static void DrawLinesCentered(string text)
    {
        var lines = text.Split('\n');
        foreach (var line in lines)
        {
            ImGuiHelpers.CenteredText(line);
        }
    }

    private static void DrawWarningIcon()
    {
        ImGuiHelpers.ScaledDummy(10);

        using var color = ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudOrange);
        using var font = ImRaii.PushFont(InterfaceManager.IconFontFixedWidth); // Note: Changed to FixedWidth! -MidoriKami

        ImGuiHelpers.CenteredText(FontAwesomeIcon.ExclamationTriangle.ToIconString());
    }

    private void DrawCategoryChild(float useContentWidth, float useContentHeight, float useMenuWidth)
    {
        using var installerMainChild = ImRaii.Child("InstallerCategories"u8, new Vector2(useContentWidth, useContentHeight * ImGuiHelpers.GlobalScale));
        if (!installerMainChild)
        {
            return;
        }

        using var style = ImRaii.PushStyle(ImGuiStyleVar.CellPadding, ImGuiHelpers.ScaledVector2(5, 0));

        try
        {
            this.DrawCategorySelectorsChild(useMenuWidth);

            ImGui.SameLine();

            this.DrawScrollingChild();
        }
        catch (Exception ex)
        {
            PluginInstallerWindow.Log.Error(ex, "Could not draw plugin categories");
        }
    }

    private void DrawScrollingChild()
    {
        using var scrollingChild = ImRaii.Child(
            "ScrollingPlugins"u8,
            new Vector2(-1, -1),
            false,
            ImGuiWindowFlags.NoBackground);

        if (!scrollingChild)
        {
            return;
        }

        try
        {
            this.DrawPluginCategoryContent();
        }
        catch (Exception ex)
        {
            PluginInstallerWindow.Log.Error(ex, "Could not draw category content");
        }
    }

    private void DrawCategorySelectorsChild(float useMenuWidth)
    {
        using var categoriesChild = ImRaii.Child("InstallerCategoriesSelector"u8, new Vector2(useMenuWidth * ImGuiHelpers.GlobalScale, -1), false);
        if (!categoriesChild) return;

        this.DrawPluginCategorySelectors();
    }

    private void DrawPluginCategorySelectors()
    {
        foreach (var groupInfo in this.categoryManager.GroupList)
        {
            var canShowGroup = (groupInfo.GroupKind != PluginCategoryManager.GroupKind.DevTools) || this.pluginInstaller.hasDevPlugins;
            if (!canShowGroup)
            {
                continue;
            }

            var isCurrent = groupInfo.GroupKind == this.categoryManager.CurrentGroupKind;
            ImGui.SetNextItemOpen(isCurrent);
            this.DrawHeaderContents(groupInfo, isCurrent);
            ImGuiHelpers.ScaledDummy(5);
        }
    }

    private unsafe void DrawHeaderContents(PluginCategoryManager.GroupInfo groupInfo, bool isCurrent)
    {
        if (!ImGui.CollapsingHeader(groupInfo.Name, isCurrent ? ImGuiTreeNodeFlags.OpenOnDoubleClick : ImGuiTreeNodeFlags.None))
        {
            return;
        }

        if (!isCurrent)
        {
            this.categoryManager.CurrentGroupKind = groupInfo.GroupKind;

            // Reset search text when switching groups
            this.pluginInstaller.searchText = string.Empty;
        }

        using var indent = ImRaii.PushIndent();
        var categoryItemSize = new Vector2(ImGui.GetContentRegionAvail().X - (5 * ImGuiHelpers.GlobalScale), ImGui.GetTextLineHeight());

        foreach (var categoryKind in groupInfo.Categories)
        {
            var categoryInfo = this.categoryManager.CategoryList.First(x => x.CategoryKind == categoryKind);

            switch (categoryInfo.Condition)
            {
                case PluginCategoryManager.CategoryInfo.AppearCondition.None:
                    // Do nothing
                    break;

                case PluginCategoryManager.CategoryInfo.AppearCondition.DoPluginTest:
                    if (!Service<DalamudConfiguration>.Get().DoPluginTest)
                        continue;
                    break;

                case PluginCategoryManager.CategoryInfo.AppearCondition.AnyHiddenPlugins:
                    if (!this.pluginInstaller.hasHiddenPlugins)
                        continue;
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            var navHighlightColor = ImGui.GetStyleColorVec4(ImGuiCol.NavHighlight);
            var navColor = navHighlightColor is null ? Vector4.One : *navHighlightColor;
            var hasSearchHighlight = this.categoryManager.IsCategoryHighlighted(categoryInfo.CategoryKind);
            using (ImRaii.PushColor(ImGuiCol.Text, navColor, hasSearchHighlight))
            {
                if (ImGui.Selectable(categoryInfo.Name, this.categoryManager.CurrentCategoryKind == categoryKind, ImGuiSelectableFlags.None, categoryItemSize))
                {
                    this.categoryManager.CurrentCategoryKind = categoryKind;
                }
            }
        }
    }

    private void DrawPluginCategoryContent()
    {
        var ready = this.DrawPluginListLoading();
        if (!this.categoryManager.IsSelectionValid || !ready)
        {
            return;
        }

        var pm = Service<PluginManager>.Get();
        if (pm.SafeMode)
        {
            DrawWarningIcon();
            DrawLinesCentered(PluginInstallerLocs.SafeModeDisclaimer);

            ImGuiHelpers.ScaledDummy(10);
        }

        this.DrawNewDalamudVersionAvailableNotice();

        this.DrawCurrentPluginList();
    }

    private void DrawCurrentPluginList()
    {
        // Scaling this might be incorrect? Unsure. -MidoriKami
        using var itemSpacing = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, ImGuiHelpers.ScaledVector2(1, 3));

        var groupInfo = this.categoryManager.CurrentGroup;
        if (this.categoryManager.IsContentDirty)
        {
            // reset opened list of collapsibles when switching between categories
            this.pluginInstaller.openPluginCollapsibles.Clear();

            // do NOT reset dirty flag when Available group is selected, it will be handled by DrawAvailablePluginList()
            if (groupInfo.GroupKind != PluginCategoryManager.GroupKind.Available)
            {
                this.categoryManager.ResetContentDirty();
            }
        }

        switch (groupInfo.GroupKind)
        {
            case PluginCategoryManager.GroupKind.DevTools:
                // this one is never sorted and remains in hardcoded order from group ctor
                switch (this.categoryManager.CurrentCategoryKind)
                {
                    case PluginCategoryManager.CategoryKind.DevInstalled:
                        this.DrawInstalledPluginList(InstalledPluginListFilter.Dev);
                        break;

                    case PluginCategoryManager.CategoryKind.IconTester:
                        this.pluginInstaller.ImageTester.Draw();
                        break;

                    default:
                        ImGui.Text("You found a mysterious category. Please keep it to yourself."u8);
                        break;
                }

                break;

            case PluginCategoryManager.GroupKind.Installed:
                switch (this.categoryManager.CurrentCategoryKind)
                {
                    case PluginCategoryManager.CategoryKind.All:
                        this.DrawInstalledPluginList(InstalledPluginListFilter.None);
                        break;

                    case PluginCategoryManager.CategoryKind.IsTesting:
                        this.DrawInstalledPluginList(InstalledPluginListFilter.Testing);
                        break;

                    case PluginCategoryManager.CategoryKind.UpdateablePlugins:
                        this.DrawInstalledPluginList(InstalledPluginListFilter.Updateable);
                        break;

                    case PluginCategoryManager.CategoryKind.EnabledPlugins:
                        this.DrawInstalledPluginList(InstalledPluginListFilter.Enabled);
                        break;

                    case PluginCategoryManager.CategoryKind.DisabledPlugins:
                        this.DrawInstalledPluginList(InstalledPluginListFilter.Disabled);
                        break;

                    case PluginCategoryManager.CategoryKind.IncompatiblePlugins:
                        this.DrawInstalledPluginList(InstalledPluginListFilter.Incompatible);
                        break;

                    case PluginCategoryManager.CategoryKind.PluginProfiles:
                        this.pluginInstaller.profileManagerWidget.Draw();
                        break;

                    default:
                        ImGui.Text("You found a secret category. Please feel a sense of pride and accomplishment."u8);
                        break;
                }

                break;

            case PluginCategoryManager.GroupKind.Changelog:
                this.changelogCategory.Draw();
                break;

            default:
                this.DrawAvailablePluginList();
                break;
        }
    }

    private void DrawNewDalamudVersionAvailableNotice()
    {
        if (this.pluginInstaller.staleDalamudNewVersion is null)
        {
            return;
        }

        DrawWarningIcon();
        DrawLinesCentered(
            "A new version of Dalamud is available.\n" +
            "Please restart the game to ensure compatibility with updated plugins.\n" +
            $"old: {Versioning.GetScmVersion()} new: {this.pluginInstaller.staleDalamudNewVersion}");

        ImGuiHelpers.ScaledDummy(10);
    }

    private void DrawAvailablePluginList()
    {
        var i = 0;
        foreach (var proxy in this.proxies.GatherProxies())
        {
            IPluginManifest applicableManifest = proxy.LocalPlugin != null ? proxy.LocalPlugin.Manifest : proxy.RemoteManifest;

            if (applicableManifest == null)
                throw new Exception("Could not determine manifest for available plugin");

            ImGui.PushID($"{applicableManifest.InternalName}{applicableManifest.AssemblyVersion}");

            if (proxy.LocalPlugin != null)
            {
                var update = this.pluginInstaller.pluginListUpdatable.FirstOrDefault(up => up.InstalledPlugin == proxy.LocalPlugin);
                this.pluginEntry.DrawInstalledPlugin(proxy.LocalPlugin, i++, proxy.RemoteManifest, update);
            }
            else if (proxy.RemoteManifest != null)
            {
                this.DrawAvailablePlugin(proxy.RemoteManifest, i++);
            }

            ImGui.PopID();
        }

        // Reset the category to "All" if we're on the "Hidden" category and there are no hidden plugins (we removed the last one)
        if (i == 0 && this.categoryManager.CurrentCategoryKind == PluginCategoryManager.CategoryKind.Hidden)
        {
            this.categoryManager.CurrentCategoryKind = PluginCategoryManager.CategoryKind.All;
        }

        using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudGrey))
        {
            var hasSearch = !this.pluginInstaller.searchText.IsNullOrEmpty();

            if (i == 0 && !hasSearch)
            {
                DrawMutedBodyText(PluginInstallerLocs.TabBody_NoPluginsAvailable, 60, 20);
            }
            else if (i == 0 && hasSearch)
            {
                DrawMutedBodyText(PluginInstallerLocs.TabBody_SearchNoMatching, 60, 20);
            }
            else if (hasSearch)
            {
                DrawMutedBodyText(PluginInstallerLocs.TabBody_NoMoreResultsFor(this.pluginInstaller.searchText), 20, 20);
            }
        }
    }

    private static void DrawMutedBodyText(string text, float paddingBefore, float paddingAfter)
    {
        ImGuiHelpers.ScaledDummy(paddingBefore);

        using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudGrey))
        {
            foreach (var line in text.Split('\n'))
            {
                ImGuiHelpers.CenteredText(line);
            }
        }

        ImGuiHelpers.ScaledDummy(paddingAfter);
    }

    public void DrawInstalledPluginList(InstalledPluginListFilter filter)
    {
        var pluginList = this.pluginInstaller.pluginListInstalled;
        var manager = Service<PluginManager>.Get();

        if (pluginList.Count == 0)
        {
            DrawMutedBodyText(PluginInstallerLocs.TabBody_SearchNoInstalled, 60, 20);
            return;
        }

        var filteredList = pluginList
                           .Where(plugin => !this.pluginInstaller.IsManifestFiltered(plugin.Manifest))
                           .ToList();

        if (filteredList.Count == 0)
        {
            DrawMutedBodyText(PluginInstallerLocs.TabBody_SearchNoMatching, 60, 20);
            return;
        }

        var drewAny = false;
        var i = 0;
        foreach (var plugin in filteredList)
        {
            if (filter == InstalledPluginListFilter.Testing && !manager.HasTestingOptIn(plugin.Manifest))
                continue;

            if (filter == InstalledPluginListFilter.Enabled && (!plugin.IsWantedByAnyProfile || plugin.IsOutdated || plugin.IsBanned || plugin.IsOrphaned || plugin.IsDecommissioned))
                continue;

            if (filter == InstalledPluginListFilter.Disabled && (plugin.IsWantedByAnyProfile || plugin.IsOutdated || plugin.IsBanned || plugin.IsOrphaned || plugin.IsDecommissioned))
                continue;

            if (filter == InstalledPluginListFilter.Incompatible && !(plugin.IsOutdated || plugin.IsBanned || plugin.IsOrphaned || plugin.IsDecommissioned))
                continue;

            // Find applicable update and manifest, if we have them
            AvailablePluginUpdate? update = null;
            RemotePluginManifest? remoteManifest = null;

            if (filter != InstalledPluginListFilter.Dev)
            {
                update = this.pluginInstaller.pluginListUpdatable.FirstOrDefault(up => up.InstalledPlugin == plugin);
                if (filter == InstalledPluginListFilter.Updateable && update == null)
                    continue;

                // Find the applicable remote manifest
                remoteManifest = this.pluginInstaller.pluginListAvailable
                                     .FirstOrDefault(rm => rm.InternalName == plugin.Manifest.InternalName &&
                                                           rm.RepoUrl == plugin.Manifest.RepoUrl);
            }
            else if (!plugin.IsDev)
            {
                continue;
            }

            this.pluginInstaller.PluginEntry.DrawInstalledPlugin(plugin, i++, remoteManifest, update);
            drewAny = true;
        }

        if (!drewAny)
        {
            var text = filter switch
            {
                InstalledPluginListFilter.None => PluginInstallerLocs.TabBody_NoPluginsInstalled,
                InstalledPluginListFilter.Testing => PluginInstallerLocs.TabBody_NoPluginsTesting,
                InstalledPluginListFilter.Updateable => PluginInstallerLocs.TabBody_NoPluginsUpdateable,
                InstalledPluginListFilter.Dev => PluginInstallerLocs.TabBody_NoPluginsDev,
                InstalledPluginListFilter.Enabled => PluginInstallerLocs.TabBody_NoPluginsEnabled,
                InstalledPluginListFilter.Disabled => PluginInstallerLocs.TabBody_NoPluginsDisabled,
                InstalledPluginListFilter.Incompatible => PluginInstallerLocs.TabBody_NoPluginsIncompatible,
                _ => throw new ArgumentException(null, nameof(filter)),
            };

            ImGuiHelpers.ScaledDummy(60);

            using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudGrey))
            {
                foreach (var line in text.Split('\n'))
                {
                    ImGuiHelpers.CenteredText(line);
                }
            }
        }
        else if (!this.pluginInstaller.searchText.IsNullOrEmpty())
        {
            DrawMutedBodyText(PluginInstallerLocs.TabBody_NoMoreResultsFor(this.pluginInstaller.searchText), 20, 20);
            ImGuiHelpers.ScaledDummy(20);
        }
    }

    private bool DrawPluginListLoading()
    {
        var pluginManager = Service<PluginManager>.Get();

        var ready = pluginManager.PluginsReady && pluginManager.ReposReady;

        if (!ready)
        {
            ImGui.TextColored(ImGuiColors.DalamudGrey, PluginInstallerLocs.TabBody_LoadingPlugins);
        }

        var failedRepos = pluginManager.Repos
                                       .Where(repo => repo.State == PluginRepositoryState.Fail)
                                       .ToArray();

        if (failedRepos.Length > 0)
        {
            var failText = PluginInstallerLocs.TabBody_DownloadFailed;
            var aggFailText = failedRepos
                              .Select(repo => $"{failText} ({repo.PluginMasterUrl})")
                              .Aggregate((s1, s2) => $"{s1}\n{s2}");

            ImGui.TextColored(ImGuiColors.DalamudRed, aggFailText);
        }

        return ready;
    }

    private void DrawAvailablePlugin(RemotePluginManifest manifest, int index)
    {
        var configuration = Service<DalamudConfiguration>.Get();
        var pluginManager = Service<PluginManager>.Get();

        var canUseTesting = pluginManager.CanUseTesting(manifest);
        var useTesting = pluginManager.UseTesting(manifest);
        var wasSeen = this.pluginInstaller.WasPluginSeen(manifest.InternalName);

        var effectiveApiLevel = useTesting ? manifest.TestingDalamudApiLevel.Value : manifest.DalamudApiLevel;
        var isOutdated = effectiveApiLevel < PluginManager.DalamudApiLevel;

        var isIncompatible = manifest.MinimumDalamudVersion != null &&
                             manifest.MinimumDalamudVersion > Versioning.GetAssemblyVersionParsed();

        var enableInstallButton = this.pluginInstaller.updateStatus != OperationStatus.InProgress &&
                                  this.pluginInstaller.installStatus != OperationStatus.InProgress &&
                                  !isOutdated &&
                                  !isIncompatible;

        // Check for valid versions
        if ((useTesting && manifest.TestingAssemblyVersion == null) || manifest.AssemblyVersion == null)
        {
            // Without a valid version, quit
            return;
        }

        // Name
        var label = manifest.Name;

        // Testing
        if (useTesting)
        {
            label += PluginInstallerLocs.PluginTitleMod_TestingVersion;
        }
        else if (manifest.IsTestingExclusive)
        {
            label += PluginInstallerLocs.PluginTitleMod_TestingExclusive;
        }
        else if (canUseTesting)
        {
            label += PluginInstallerLocs.PluginTitleMod_TestingAvailable;
        }

        if (isIncompatible)
        {
            label += PluginInstallerLocs.PluginTitleMod_Incompatible;
        }

        var isThirdParty = manifest.SourceRepo.IsThirdParty;

        ImGui.PushID($"available{index}{manifest.InternalName}");

        var flags = PluginHeaderFlags.None;
        if (isThirdParty)
            flags |= PluginHeaderFlags.IsThirdParty;
        if (!wasSeen)
            flags |= PluginHeaderFlags.IsNew;
        if (isOutdated)
            flags |= PluginHeaderFlags.IsInstallableOutdated;
        if (useTesting || manifest.IsTestingExclusive)
            flags |= PluginHeaderFlags.IsTesting;
        if (isIncompatible)
            flags |= PluginHeaderFlags.IsIncompatible;

        if (this.pluginInstaller.DrawPluginCollapsingHeader(label, null, manifest, flags, () => this.DrawAvailablePluginContextMenu(manifest), index))
        {
            if (!wasSeen)
                configuration.SeenPluginInternalName.Add(manifest.InternalName);

            ImGuiHelpers.ScaledDummy(5);

            ImGui.Indent();

            // Installable from
            if (manifest.SourceRepo.IsThirdParty)
            {
                var repoText = PluginInstallerLocs.PluginBody_Plugin3rdPartyRepo(manifest.SourceRepo.PluginMasterUrl);
                ImGui.TextColored(ImGuiColors.DalamudGrey3, repoText);

                ImGuiHelpers.ScaledDummy(2);
            }

            // Description
            if (!string.IsNullOrWhiteSpace(manifest.Description))
            {
                ImGui.TextWrapped(manifest.Description);
            }

            ImGuiHelpers.ScaledDummy(5);

            var versionString = useTesting
                                    ? $"{manifest.TestingAssemblyVersion}"
                                    : $"{manifest.AssemblyVersion}";

            if (pluginManager.SafeMode)
            {
                ImGuiComponents.DisabledButton(PluginInstallerLocs.PluginButton_SafeMode);
            }
            else if (!enableInstallButton)
            {
                ImGuiComponents.DisabledButton(PluginInstallerLocs.PluginButton_InstallVersion(versionString));
            }
            else
            {
                using var color = ImRaii.PushColor(ImGuiCol.Button, ImGuiColors.DalamudRed.Darken(0.3f).Fade(0.4f));
                var buttonText = PluginInstallerLocs.PluginButton_InstallVersion(versionString);
                if (ImGui.Button($"{buttonText}##{buttonText}{index}"))
                {
                    this.pluginInstaller.StartInstall(manifest, useTesting);
                }
            }

            ImGui.SameLine();
            ImGuiHelpers.ScaledDummy(10);
            ImGui.SameLine();

            if (VisitRepoUrlButton.Draw(manifest.RepoUrl, true))
            {
                ImGui.SameLine();
                ImGuiHelpers.ScaledDummy(3);
            }

            if (!manifest.SourceRepo.IsThirdParty && manifest.AcceptsFeedback && !isOutdated)
            {
                ImGui.SameLine();
                this.pluginInstaller.FeedbackModal.DrawSendFeedbackButton(manifest, false, true);
            }

            ImGuiHelpers.ScaledDummy(5);

            if (this.pluginInstaller.DrawPluginImages(null, manifest, isThirdParty, index))
                ImGuiHelpers.ScaledDummy(5);

            ImGui.Unindent();
        }

        ImGui.PopID();
    }

    private void DrawAvailablePluginContextMenu(RemotePluginManifest manifest)
    {
        var configuration = Service<DalamudConfiguration>.Get();
        var pluginManager = Service<PluginManager>.Get();

        var hasTestingVersionAvailable = configuration.DoPluginTest && manifest.IsAvailableForTesting;

        if (ImGui.BeginPopupContextItem("ItemContextMenu"u8))
        {
            if (hasTestingVersionAvailable)
            {
                if (ImGui.Selectable(PluginInstallerLocs.PluginContext_InstallTestingVersion))
                {
                    PluginInstallerWindow.EnsureHaveTestingOptIn(manifest);
                    this.pluginInstaller.StartInstall(manifest, true);
                }

                ImGui.Separator();
            }

            if (ImGui.Selectable(PluginInstallerLocs.PluginContext_MarkAllSeen))
            {
                configuration.SeenPluginInternalName.AddRange(this.pluginInstaller.pluginListAvailable.Select(x => x.InternalName));
                configuration.QueueSave();
            }

            var isHidden = configuration.HiddenPluginInternalName.Contains(manifest.InternalName);
            switch (isHidden)
            {
                case false when ImGui.Selectable(PluginInstallerLocs.PluginContext_HidePlugin):
                    configuration.HiddenPluginInternalName.Add(manifest.InternalName);
                    configuration.QueueSave();
                    break;

                case true when ImGui.Selectable(PluginInstallerLocs.PluginContext_UnhidePlugin):
                    configuration.HiddenPluginInternalName.Remove(manifest.InternalName);
                    configuration.QueueSave();
                    break;
            }

            if (ImGui.Selectable(PluginInstallerLocs.PluginContext_DeletePluginConfig))
            {
                this.pluginInstaller.DeletePluginConfigWarningModal.ShowDeletePluginConfigWarningModal(manifest.Name).ContinueWith(t =>
                {
                    var shouldDelete = t.Result;

                    if (shouldDelete)
                    {
                        PluginInstallerWindow.Log.Debug($"Deleting config for {manifest.InternalName}");

                        this.pluginInstaller.installStatus = OperationStatus.InProgress;

                        Task.Run(() =>
                            {
                                pluginManager.PluginConfigs.Delete(manifest.InternalName);
                                var dir = pluginManager.PluginConfigs.GetDirectory(manifest.InternalName);

                                if (Directory.Exists(dir))
                                    Directory.Delete(dir, true);
                            })
                            .ContinueWith(task =>
                            {
                                this.pluginInstaller.installStatus = OperationStatus.Idle;

                                this.pluginInstaller.DisplayErrorContinuation(task, PluginInstallerLocs.ErrorModal_DeleteConfigFail(manifest.InternalName));
                            });
                    }
                });
            }

            ImGui.EndPopup();
        }
    }
}
