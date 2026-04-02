using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using Dalamud.Bindings.ImGui;
using Dalamud.Configuration.Internal;
using Dalamud.Console;
using Dalamud.Game.Command;
using Dalamud.Game.Player;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.ImGuiNotification.Internal;
using Dalamud.Interface.Internal.Windows.PluginInstaller.Enums;
using Dalamud.Interface.Internal.Windows.PluginInstaller.Modals;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin;
using Dalamud.Plugin.Internal;
using Dalamud.Plugin.Internal.Profiles;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Plugin.Internal.Types.Manifest;
using Dalamud.Utility;

namespace Dalamud.Interface.Internal.Windows.PluginInstaller.Parts;

internal class PluginEntry
{
    private readonly PluginInstallerWindow pluginInstaller;
    private readonly PluginCategoryManager categoryManager;
    private readonly FeedbackModal feedbackModal;

    public readonly Vector4 changelogBgColor = new(0.114f, 0.584f, 0.192f, 0.678f);
    public readonly Vector4 changelogTextColor = new(0.812f, 1.000f, 0.816f, 1.000f);

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginEntry"/> class.
    /// </summary>
    /// <param name="pluginInstaller">Reference to main Installer Window.</param>
    /// <param name="categoryManager">Category Manager.</param>
    /// <param name="feedbackModal">Feedback Modal.</param>
    public PluginEntry(PluginInstallerWindow pluginInstaller, PluginCategoryManager categoryManager, FeedbackModal feedbackModal)
    {
        this.pluginInstaller = pluginInstaller;
        this.categoryManager = categoryManager;
        this.feedbackModal = feedbackModal;
    }

    public void DrawInstalledPlugin(LocalPlugin plugin, int index, RemotePluginManifest? remoteManifest, AvailablePluginUpdate? availablePluginUpdate, bool showInstalled = false)
    {
        var configuration = Service<DalamudConfiguration>.Get();
        var commandManager = Service<CommandManager>.Get();

        var testingOptIn =
            configuration.PluginTestingOptIns?.FirstOrDefault(x => x.InternalName == plugin.Manifest.InternalName);
        var trouble = false;

        // Name
        var label = plugin.Manifest.Name;

        // Dev
        if (plugin.IsDev)
        {
            label += PluginInstallerLocs.PluginTitleMod_DevPlugin;
        }

        // Testing
        if (plugin.IsTesting)
        {
            label += PluginInstallerLocs.PluginTitleMod_TestingVersion;
        }

        var hasTestingAvailable = this.pluginInstaller.pluginListAvailable.Any(x => x.InternalName == plugin.InternalName && x.IsAvailableForTesting);
        if (hasTestingAvailable && configuration.DoPluginTest && testingOptIn == null)
        {
            label += PluginInstallerLocs.PluginTitleMod_TestingAvailable;
        }

        // Freshly installed
        if (showInstalled)
        {
            label += PluginInstallerLocs.PluginTitleMod_Installed;
        }

        // Disabled
        if (!plugin.IsWantedByAnyProfile || !plugin.CheckPolicy())
        {
            label += PluginInstallerLocs.PluginTitleMod_Disabled;
            trouble = true;
        }

        // Load error
        if (plugin.State is PluginState.LoadError or PluginState.DependencyResolutionFailed && plugin.CheckPolicy()
                                                                                            && !plugin.IsOutdated && !plugin.IsBanned && !plugin.IsOrphaned)
        {
            label += PluginInstallerLocs.PluginTitleMod_LoadError;
            trouble = true;
        }

        // Unload error
        if (plugin.State == PluginState.UnloadError)
        {
            label += PluginInstallerLocs.PluginTitleMod_UnloadError;
            trouble = true;
        }

        // Dev plugins can never update
        if (plugin.IsDev)
            availablePluginUpdate = null;

        // Update available
        var isMainRepoCrossUpdate = availablePluginUpdate != null &&
                                    availablePluginUpdate.UpdateManifest.RepoUrl != plugin.Manifest.RepoUrl &&
                                    availablePluginUpdate.UpdateManifest.RepoUrl == PluginRepository.MainRepoUrl;
        if (availablePluginUpdate != null)
        {
            label += PluginInstallerLocs.PluginTitleMod_HasUpdate;
        }

        // Freshly updated
        var thisWasUpdated = false;
        if (this.pluginInstaller.updatedPlugins != null && !plugin.IsDev)
        {
            var update = this.pluginInstaller.updatedPlugins.FirstOrDefault(update => update.InternalName == plugin.Manifest.InternalName);
            if (update != null)
            {
                if (update.Status == PluginUpdateStatus.StatusKind.Success)
                {
                    thisWasUpdated = true;
                    label += PluginInstallerLocs.PluginTitleMod_Updated;
                }
                else
                {
                    label += PluginInstallerLocs.PluginTitleMod_UpdateFailed;
                }
            }
        }

        // Outdated API level
        if (plugin.IsOutdated)
        {
            label += PluginInstallerLocs.PluginTitleMod_OutdatedError;
            trouble = true;
        }

        // Banned
        if (plugin.IsBanned)
        {
            label += PluginInstallerLocs.PluginTitleMod_BannedError;
            trouble = true;
        }

        // Orphaned, if we don't have a cross-repo update
        if (plugin.IsOrphaned && !isMainRepoCrossUpdate)
        {
            label += PluginInstallerLocs.PluginTitleMod_OrphanedError;
            trouble = true;
        }

        // Out of service
        if (plugin.IsDecommissioned && !plugin.IsOrphaned)
        {
            label += PluginInstallerLocs.PluginTitleMod_NoService;
            trouble = true;
        }

        // Scheduled for deletion
        if (plugin.Manifest.ScheduledForDeletion)
        {
            label += PluginInstallerLocs.PluginTitleMod_ScheduledForDeletion;
        }

        ImGui.PushID($"installed{index}{plugin.Manifest.InternalName}");

        var applicableChangelog = plugin.IsTesting ? remoteManifest?.TestingChangelog : remoteManifest?.Changelog;
        var hasChangelog = !applicableChangelog.IsNullOrWhitespace();
        var didDrawApplicableChangelogInsideCollapsible = false;

        Version? availablePluginUpdateVersion = null;
        string? availableChangelog = null;
        var didDrawAvailableChangelogInsideCollapsible = false;

        if (availablePluginUpdate != null)
        {
            availablePluginUpdateVersion =
                availablePluginUpdate.UseTesting ? availablePluginUpdate.UpdateManifest.TestingAssemblyVersion : availablePluginUpdate.UpdateManifest.AssemblyVersion;

            availableChangelog =
                availablePluginUpdate.UseTesting ? availablePluginUpdate.UpdateManifest.TestingChangelog : availablePluginUpdate.UpdateManifest.Changelog;
        }

        var flags = PluginHeaderFlags.None;
        if (plugin.IsThirdParty)
            flags |= PluginHeaderFlags.IsThirdParty;
        if (trouble)
            flags |= PluginHeaderFlags.HasTrouble;
        if (availablePluginUpdate != null)
            flags |= PluginHeaderFlags.UpdateAvailable;
        if (isMainRepoCrossUpdate)
            flags |= PluginHeaderFlags.MainRepoCrossUpdate;
        if (plugin.IsOrphaned)
            flags |= PluginHeaderFlags.IsOrphan;
        if (plugin.IsTesting)
            flags |= PluginHeaderFlags.IsTesting;

        if (this.pluginInstaller.DrawPluginCollapsingHeader(label, plugin, plugin.Manifest, flags, () => this.DrawInstalledPluginContextMenu(plugin, testingOptIn), index))
        {
            if (!this.pluginInstaller.WasPluginSeen(plugin.Manifest.InternalName))
                configuration.SeenPluginInternalName.Add(plugin.Manifest.InternalName);

            var manifest = plugin.Manifest;

            ImGui.Indent();

            // Name
            ImGui.Text(manifest.Name);

            // Download count
            var downloadText = plugin.IsDev
                                   ? PluginInstallerLocs.PluginBody_AuthorWithoutDownloadCount(manifest.Author)
                                   : manifest.DownloadCount > 0
                                       ? PluginInstallerLocs.PluginBody_AuthorWithDownloadCount(manifest.Author, manifest.DownloadCount)
                                       : PluginInstallerLocs.PluginBody_AuthorWithDownloadCountUnavailable(manifest.Author);

            ImGui.SameLine();
            ImGui.TextColored(ImGuiColors.DalamudGrey3, downloadText);

            var acceptsFeedback =
                this.pluginInstaller.pluginListAvailable.Any(x => x.InternalName == plugin.InternalName && x.AcceptsFeedback);

            var isThirdParty = plugin.IsThirdParty;
            var canFeedback = !isThirdParty &&
                              !plugin.IsDev &&
                              !plugin.IsOrphaned &&
                              (plugin.Manifest.DalamudApiLevel == PluginManager.DalamudApiLevel ||
                               (plugin.Manifest.TestingDalamudApiLevel == PluginManager.DalamudApiLevel && hasTestingAvailable)) &&
                              acceptsFeedback &&
                              availablePluginUpdate == default;

            // Installed from
            if (plugin.IsDev)
            {
                var fileText = PluginInstallerLocs.PluginBody_DevPluginPath(plugin.DllFile.FullName);
                ImGui.TextColored(ImGuiColors.DalamudGrey3, fileText);
            }
            else if (isThirdParty)
            {
                var repoText = PluginInstallerLocs.PluginBody_Plugin3rdPartyRepo(manifest.InstalledFromUrl);
                ImGui.TextColored(ImGuiColors.DalamudGrey3, repoText);
            }

            // Description
            if (!string.IsNullOrWhiteSpace(manifest.Description))
            {
                ImGui.TextWrapped(manifest.Description);
            }

            // Working Plugin ID
            if (this.pluginInstaller.hasDevPlugins)
            {
                ImGuiHelpers.ScaledDummy(3);
                ImGui.TextColored(ImGuiColors.DalamudGrey, $"WorkingPluginId: {plugin.EffectiveWorkingPluginId}");
                ImGui.TextColored(ImGuiColors.DalamudGrey, $"Command prefix: {ConsoleManagerPluginUtil.GetSanitizedNamespaceName(plugin.InternalName)}");
                ImGuiHelpers.ScaledDummy(3);
            }

            // Available commands (if loaded)
            if (plugin.IsLoaded)
            {
                var commands = commandManager.Commands
                                             .Where(cInfo =>
                                                        cInfo.Value is { ShowInHelp: true } &&
                                                        commandManager.GetHandlerAssemblyName(cInfo.Key, cInfo.Value) == plugin.Manifest.InternalName);

                if (commands.Any())
                {
                    ImGui.Dummy(ImGuiHelpers.ScaledVector2(10f, 10f));
                    foreach (var command in commands
                                            .OrderBy(cInfo => cInfo.Value.DisplayOrder)
                                            .ThenBy(cInfo => cInfo.Key))
                    {
                        ImGui.TextWrapped($"{command.Key} → {command.Value.HelpMessage}");
                    }

                    ImGuiHelpers.ScaledDummy(3);
                }
            }

            if (plugin is LocalDevPlugin devPlugin)
            {
                this.DrawDevPluginValidationIssues(devPlugin);
                ImGuiHelpers.ScaledDummy(5);
            }

            // Controls
            this.DrawPluginControlButton(plugin, availablePluginUpdate);
            this.DrawDevPluginButtons(plugin);
            VisitRepoUrlButton.Draw(plugin.Manifest.RepoUrl, false);
            this.DrawDeletePluginButton(plugin);

            if (canFeedback)
            {
                ImGui.SameLine();
                this.feedbackModal.DrawSendFeedbackButton(plugin.Manifest, plugin.IsTesting, false);
            }

            if (availablePluginUpdate != default && !plugin.IsDev)
            {
                ImGui.SameLine();
                this.DrawUpdateSinglePluginButton(availablePluginUpdate);
            }

            ImGui.SameLine();
            ImGui.TextColored(ImGuiColors.DalamudGrey3, $" v{plugin.EffectiveVersion}");

            ImGuiHelpers.ScaledDummy(5);

            if (this.pluginInstaller.DrawPluginImages(plugin, manifest, isThirdParty, index))
                ImGuiHelpers.ScaledDummy(5);

            ImGui.Unindent();

            if (hasChangelog)
            {
                if (ImGui.TreeNode(PluginInstallerLocs.PluginBody_CurrentChangeLog(plugin.EffectiveVersion)))
                {
                    didDrawApplicableChangelogInsideCollapsible = true;
                    this.DrawInstalledPluginChangelog(applicableChangelog);
                    ImGui.TreePop();
                }
            }

            if (!availableChangelog.IsNullOrWhitespace() && ImGui.TreeNode(PluginInstallerLocs.PluginBody_UpdateChangeLog(availablePluginUpdateVersion)))
            {
                this.DrawInstalledPluginChangelog(availableChangelog);
                ImGui.TreePop();
                didDrawAvailableChangelogInsideCollapsible = true;
            }
        }

        if (thisWasUpdated &&
            hasChangelog &&
            !didDrawApplicableChangelogInsideCollapsible)
        {
            this.DrawInstalledPluginChangelog(applicableChangelog);
        }

        if (this.categoryManager.CurrentCategoryKind == PluginCategoryManager.CategoryKind.UpdateablePlugins &&
            !availableChangelog.IsNullOrWhitespace() &&
            !didDrawAvailableChangelogInsideCollapsible)
        {
            this.DrawInstalledPluginChangelog(availableChangelog);
        }

        ImGui.PopID();
    }

    private void DrawInstalledPluginChangelog(string changelog)
    {
        ImGuiHelpers.ScaledDummy(5);

        ImGui.PushStyleColor(ImGuiCol.ChildBg, this.changelogBgColor);
        ImGui.PushStyleColor(ImGuiCol.Text, this.changelogTextColor);

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(7, 5));

        if (ImGui.BeginChild("##changelog"u8, new Vector2(-1, 100), true, ImGuiWindowFlags.NoNavFocus | ImGuiWindowFlags.NoNavInputs | ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.Text("Changelog:"u8);
            ImGuiHelpers.ScaledDummy(2);
            ImGui.TextWrapped(changelog!);
        }

        ImGui.EndChild();

        ImGui.PopStyleVar();
        ImGui.PopStyleColor(2);
    }



    private unsafe void DrawInstalledPluginContextMenu(LocalPlugin plugin, PluginTestingOptIn? optIn)
    {
        var pluginManager = Service<PluginManager>.Get();
        var configuration = Service<DalamudConfiguration>.Get();

        if (ImGui.BeginPopupContextItem("InstalledItemContextMenu"u8))
        {
            if (configuration.DoPluginTest)
            {
                var repoManifest = this.pluginInstaller.pluginListAvailable.FirstOrDefault(x => x.InternalName == plugin.Manifest.InternalName);
                if (repoManifest?.IsTestingExclusive == true)
                    ImGui.BeginDisabled();

                if (ImGui.MenuItem(PluginInstallerLocs.PluginContext_TestingOptIn, optIn != null))
                {
                    if (optIn != null)
                    {
                        configuration.PluginTestingOptIns!.Remove(optIn);

                        if (plugin.Manifest.TestingAssemblyVersion > repoManifest?.AssemblyVersion)
                        {
                            this.pluginInstaller.TestingWarningModal.ShowTestingModal();
                        }
                    }
                    else
                    {
                        PluginInstallerWindow.EnsureHaveTestingOptIn(plugin.Manifest);
                    }

                    configuration.QueueSave();
                    _ = pluginManager.ReloadAllReposAsync();
                }

                if (repoManifest?.IsTestingExclusive == true)
                    ImGui.EndDisabled();
            }

            if (ImGui.MenuItem(PluginInstallerLocs.PluginContext_DeletePluginConfigReload))
            {
                this.pluginInstaller.DeletePluginConfigWarningModal.ShowDeletePluginConfigWarningModal(plugin.Manifest.Name, optIn != null).ContinueWith(t =>
                {
                    var shouldDelete = t.Result;

                    if (shouldDelete)
                    {
                        PluginInstallerWindow.Log.Debug($"Deleting config for {plugin.Manifest.InternalName}");

                        this.pluginInstaller.installStatus = OperationStatus.InProgress;

                        Task.Run(() => pluginManager.DeleteConfigurationAsync(plugin))
                            .ContinueWith(task =>
                            {
                                this.pluginInstaller.installStatus = OperationStatus.Idle;

                                this.pluginInstaller.DisplayErrorContinuation(task, PluginInstallerLocs.ErrorModal_DeleteConfigFail(plugin.Manifest.InternalName));
                            });
                    }
                });
            }

            ImGui.EndPopup();
        }
    }

        public void DrawPluginControlButton(LocalPlugin plugin, AvailablePluginUpdate? availableUpdate)
    {
        var notifications = Service<NotificationManager>.Get();
        var pluginManager = Service<PluginManager>.Get();
        var profileManager = Service<ProfileManager>.Get();
        var config = Service<DalamudConfiguration>.Get();

        var applicableForProfiles = plugin.Manifest.SupportsProfiles /*&& !plugin.IsDev*/;
        var profilesThatWantThisPlugin = profileManager.Profiles
                                                       .Where(x => x.WantsPlugin(plugin.EffectiveWorkingPluginId) != null)
                                                       .ToArray();
        var isInSingleProfile = profilesThatWantThisPlugin.Length == 1;
        var isDefaultPlugin = profileManager.IsInDefaultProfile(plugin.EffectiveWorkingPluginId);

        // Disable everything if the updater is running or another plugin is operating
        var disabled = this.pluginInstaller.updateStatus == OperationStatus.InProgress || this.pluginInstaller.installStatus == OperationStatus.InProgress;

        // Disable everything if the plugin is outdated
        disabled = disabled || (plugin.IsOutdated && !pluginManager.LoadAllApiLevels && !plugin.IsDev) || plugin.IsBanned;

        // Disable everything if the plugin is orphaned
        // Control will immediately be disabled once the plugin is disabled
        disabled = disabled || (plugin.IsOrphaned && !plugin.IsLoaded);

        // Disable everything if the plugin failed to load
        // Now handled by the first case below
        // disabled = disabled || plugin.State == PluginState.LoadError || plugin.State == PluginState.DependencyResolutionFailed;

        // Disable everything if we're loading plugins
        disabled = disabled || plugin.State == PluginState.Loading || plugin.State == PluginState.Unloading;

        // Disable everything if we're applying profiles
        disabled = disabled || profileManager.IsBusy;

        var toggleId = plugin.Manifest.InternalName;
        var isLoadedAndUnloadable = plugin.State == PluginState.Loaded ||
                                    plugin.State == PluginState.DependencyResolutionFailed;

        // StyleModelV1.DalamudStandard.Push();

        var profileChooserPopupName = $"###pluginProfileChooser{plugin.Manifest.InternalName}";
        if (ImGui.BeginPopup(profileChooserPopupName))
        {
            var didAny = false;

            foreach (var profile in profileManager.Profiles.Where(x => !x.IsDefaultProfile))
            {
                var inProfile = profile.WantsPlugin(plugin.EffectiveWorkingPluginId) != null;
                if (ImGui.Checkbox($"###profilePick{profile.Guid}{plugin.Manifest.InternalName}", ref inProfile))
                {
                    if (inProfile)
                    {
                        Task.Run(() => profile.AddOrUpdateAsync(plugin.EffectiveWorkingPluginId, plugin.Manifest.InternalName, true))
                            .ContinueWith(this.pluginInstaller.DisplayErrorContinuation, PluginInstallerLocs.Profiles_CouldNotAdd);
                    }
                    else
                    {
                        Task.Run(() => profile.RemoveAsync(plugin.EffectiveWorkingPluginId))
                            .ContinueWith(this.pluginInstaller.DisplayErrorContinuation, PluginInstallerLocs.Profiles_CouldNotRemove);
                    }
                }

                ImGui.SameLine();

                ImGui.Text(profile.Name);

                didAny = true;
            }

            if (!didAny)
                ImGui.TextColored(ImGuiColors.DalamudGrey, PluginInstallerLocs.Profiles_None);

            ImGui.Separator();

            if (ImGuiComponents.IconButton(FontAwesomeIcon.Times))
            {
                // TODO: Work this out
                Task.Run(() => profileManager.DefaultProfile.AddOrUpdateAsync(plugin.EffectiveWorkingPluginId, plugin.Manifest.InternalName, plugin.IsLoaded, false))
                    .GetAwaiter().GetResult();
                foreach (var profile in profileManager.Profiles.Where(x => !x.IsDefaultProfile && x.Plugins.Any(y => y.InternalName == plugin.Manifest.InternalName)))
                {
                    Task.Run(() => profile.RemoveAsync(plugin.EffectiveWorkingPluginId, false))
                        .GetAwaiter().GetResult();
                }

                Task.Run(() => profileManager.ApplyAllWantStatesAsync("Remove from profile"))
                    .ContinueWith(this.pluginInstaller.DisplayErrorContinuation, PluginInstallerLocs.ErrorModal_ProfileApplyFail);
            }

            ImGui.SameLine();
            ImGui.Text(PluginInstallerLocs.Profiles_RemoveFromAll);

            ImGui.EndPopup();
        }

        var inMultipleProfiles = !isDefaultPlugin && !isInSingleProfile;
        var inSingleNonDefaultProfileWhichIsDisabled =
            isInSingleProfile && !profilesThatWantThisPlugin.First().IsEnabled;
        var inSingleNonDefaultProfileWhichDoesNotWantActive =
            isInSingleProfile && !profilesThatWantThisPlugin.First().CheckWantsActiveFromGameState(Service<PlayerState>.Get().ContentId);

        if (plugin.State is PluginState.UnloadError or PluginState.LoadError or PluginState.DependencyResolutionFailed && !plugin.IsDev && !plugin.IsOutdated)
        {
            ImGuiComponents.DisabledToggleButton(toggleId, false);

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(PluginInstallerLocs.PluginButtonToolTip_LoadUnloadFailed);
        }
        else if (this.pluginInstaller.enableDisableStatus == OperationStatus.InProgress && this.pluginInstaller.enableDisableWorkingPluginId == plugin.EffectiveWorkingPluginId)
        {
            ImGuiComponents.DisabledToggleButton(toggleId, this.pluginInstaller.loadingIndicatorKind == LoadingIndicatorKind.EnablingSingle);
        }
        else if (disabled || inMultipleProfiles || inSingleNonDefaultProfileWhichIsDisabled || inSingleNonDefaultProfileWhichDoesNotWantActive || pluginManager.SafeMode)
        {
            ImGuiComponents.DisabledToggleButton(toggleId, isLoadedAndUnloadable);

            if (pluginManager.SafeMode && ImGui.IsItemHovered())
                ImGui.SetTooltip(PluginInstallerLocs.PluginButtonToolTip_SafeMode);
            else if (inMultipleProfiles && ImGui.IsItemHovered())
                ImGui.SetTooltip(PluginInstallerLocs.PluginButtonToolTip_NeedsToBeInSingleProfile);
            else if (inSingleNonDefaultProfileWhichIsDisabled && ImGui.IsItemHovered())
                ImGui.SetTooltip(PluginInstallerLocs.PluginButtonToolTip_SingleProfileDisabled(profilesThatWantThisPlugin.First().Name));
            else if (inSingleNonDefaultProfileWhichDoesNotWantActive && ImGui.IsItemHovered())
                ImGui.SetTooltip(PluginInstallerLocs.PluginButtonToolTip_SingleProfileDoesNotWantActive(profilesThatWantThisPlugin.First().Name));
        }
        else
        {
            if (ImGuiComponents.ToggleButton(toggleId, ref isLoadedAndUnloadable))
            {
                var applicableProfile = profilesThatWantThisPlugin.First();
                PluginInstallerWindow.Log.Verbose(
                    "Switching {InternalName} in {Profile} to {State}",
                    plugin.InternalName,
                    applicableProfile,
                    isLoadedAndUnloadable);

                try
                {
                    // Reload the devPlugin manifest if it's a dev plugin
                    // The plugin might rely on changed values in the manifest
                    if (plugin is LocalDevPlugin devPlugin)
                    {
                        devPlugin.ReloadManifest();
                    }
                }
                catch (Exception ex)
                {
                    PluginInstallerWindow.Log.Error(ex, "Could not reload DevPlugin manifest");
                }

                // NOTE: We don't use the profile manager to actually handle loading/unloading here,
                // because that might cause us to show an error if a plugin we don't actually care about
                // fails to load/unload. Instead, we just do it ourselves and then update the profile.
                // There is probably a smarter way to handle this, but it's probably more code.
                if (!isLoadedAndUnloadable)
                {
                    this.pluginInstaller.enableDisableStatus = OperationStatus.InProgress;
                    this.pluginInstaller.loadingIndicatorKind = LoadingIndicatorKind.DisablingSingle;
                    this.pluginInstaller.enableDisableWorkingPluginId = plugin.EffectiveWorkingPluginId;

                    Task.Run(async () =>
                    {
                        await plugin.UnloadAsync();
                        await applicableProfile.AddOrUpdateAsync(
                            plugin.EffectiveWorkingPluginId,
                            plugin.Manifest.InternalName,
                            false,
                            false);

                        notifications.AddNotification(PluginInstallerLocs.Notifications_PluginDisabled(plugin.Manifest.Name), PluginInstallerLocs.Notifications_PluginDisabledTitle, NotificationType.Success);
                    }).ContinueWith(t =>
                    {
                        this.pluginInstaller.enableDisableStatus = OperationStatus.Complete;
                        this.pluginInstaller.DisplayErrorContinuation(t, PluginInstallerLocs.ErrorModal_UnloadFail(plugin.Name));
                    });
                }
                else
                {
                    async Task Enabler()
                    {
                        this.pluginInstaller.enableDisableStatus = OperationStatus.InProgress;
                        this.pluginInstaller.loadingIndicatorKind = LoadingIndicatorKind.EnablingSingle;
                        this.pluginInstaller.enableDisableWorkingPluginId = plugin.EffectiveWorkingPluginId;

                        await applicableProfile.AddOrUpdateAsync(plugin.EffectiveWorkingPluginId, plugin.Manifest.InternalName, true, false);
                        await plugin.LoadAsync(PluginLoadReason.Installer);

                        notifications.AddNotification(PluginInstallerLocs.Notifications_PluginEnabled(plugin.Manifest.Name), PluginInstallerLocs.Notifications_PluginEnabledTitle, NotificationType.Success);
                    }

                    var continuation = (Task t) =>
                    {
                        this.pluginInstaller.enableDisableStatus = OperationStatus.Complete;
                        this.pluginInstaller.DisplayErrorContinuation(t, PluginInstallerLocs.ErrorModal_LoadFail(plugin.Name));
                    };

                    if (availableUpdate != default && !availableUpdate.InstalledPlugin.IsDev)
                    {
                        this.pluginInstaller.UpdateModal.ShowUpdateModal(plugin).ContinueWith(async t =>
                        {
                            var shouldUpdate = t.Result;

                            if (shouldUpdate)
                            {
                                // We need to update the profile right here, because PM will not enable the plugin otherwise
                                await applicableProfile.AddOrUpdateAsync(plugin.EffectiveWorkingPluginId, plugin.Manifest.InternalName, true, false);
                                await this.UpdateSinglePlugin(availableUpdate);
                            }
                            else
                            {
                                _ = Task.Run(Enabler).ContinueWith(continuation);
                            }
                        });
                    }
                    else
                    {
                        _ = Task.Run(Enabler).ContinueWith(continuation);
                    }
                }
            }
        }

        // StyleModelV1.DalamudStandard.Pop();

        ImGui.SameLine();
        ImGuiHelpers.ScaledDummy(15, 0);

        if (plugin.State == PluginState.Loaded)
        {
            // Only if the plugin isn't broken.
            this.DrawOpenPluginSettingsButton(plugin);

            ImGui.SameLine();
            ImGuiHelpers.ScaledDummy(5, 0);
        }

        if (applicableForProfiles && config.ProfilesEnabled)
        {
            ImGui.SameLine();
            if (ImGuiComponents.IconButton(FontAwesomeIcon.Toolbox))
            {
                ImGui.OpenPopup(profileChooserPopupName);
            }

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(PluginInstallerLocs.PluginButtonToolTip_PickProfiles);
        }
        else if (!applicableForProfiles && config.ProfilesEnabled)
        {
            ImGui.SameLine();

            ImGui.BeginDisabled();
            ImGuiComponents.IconButton(FontAwesomeIcon.Toolbox);
            ImGui.EndDisabled();

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(PluginInstallerLocs.PluginButtonToolTip_ProfilesNotSupported);
        }
    }

    public void DrawUpdateSinglePluginButton(AvailablePluginUpdate update)
    {
        if (ImGuiComponents.IconButton(FontAwesomeIcon.Download))
        {
            Task.Run(() => this.UpdateSinglePlugin(update));
        }

        if (ImGui.IsItemHovered())
        {
            var updateVersion = update.UseTesting
                                    ? update.UpdateManifest.TestingAssemblyVersion
                                    : update.UpdateManifest.AssemblyVersion;
            ImGui.SetTooltip(PluginInstallerLocs.PluginButtonToolTip_UpdateSingle(updateVersion.ToString()));
        }
    }

    private async Task<bool> UpdateSinglePlugin(AvailablePluginUpdate update)
    {
        var pluginManager = Service<PluginManager>.Get();

        this.pluginInstaller.installStatus = OperationStatus.InProgress;
        this.pluginInstaller.loadingIndicatorKind = LoadingIndicatorKind.UpdatingSingle;

        return await Task.Run(async () => await pluginManager.UpdateSinglePluginAsync(update, true, false))
                         .ContinueWith(task =>
                         {
                             // There is no need to set as Complete for an individual plugin installation
                             this.pluginInstaller.installStatus = OperationStatus.Idle;

                             if (task.IsCompletedSuccessfully &&
                                 task.Result.Status != PluginUpdateStatus.StatusKind.Success)
                             {
                                 this.pluginInstaller.ErrorModal.ShowErrorModal(
                                     PluginInstallerLocs.ErrorModal_SingleUpdateFail(update.UpdateManifest.Name, PluginUpdateStatus.LocalizeUpdateStatusKind(task.Result.Status)));
                                 return false;
                             }

                             return this.pluginInstaller.DisplayErrorContinuation(task, PluginInstallerLocs.ErrorModal_SingleUpdateFail(update.UpdateManifest.Name, "Exception"));
                         });
    }

    public void DrawOpenPluginSettingsButton(LocalPlugin plugin)
    {
        var hasMainUi = plugin.DalamudInterface?.LocalUiBuilder.HasMainUi ?? false;
        var hasConfig = plugin.DalamudInterface?.LocalUiBuilder.HasConfigUi ?? false;
        if (hasMainUi)
        {
            ImGui.SameLine();
            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.ArrowUpRightFromSquare, PluginInstallerLocs.PluginButton_OpenUi))
            {
                try
                {
                    plugin.DalamudInterface.LocalUiBuilder.OpenMain();
                }
                catch (Exception ex)
                {
                    PluginInstallerWindow.Log.Error(ex, $"Error during OpenMain(): {plugin.Name}");
                }
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(PluginInstallerLocs.PluginButtonToolTip_OpenUi);
            }
        }

        if (hasConfig)
        {
            if (hasMainUi)
            {
                ImGui.SameLine();
                ImGuiHelpers.ScaledDummy(5, 0);
            }

            ImGui.SameLine();
            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Cog, PluginInstallerLocs.PluginButton_OpenSettings))
            {
                try
                {
                    plugin.DalamudInterface.LocalUiBuilder.OpenConfig();
                }
                catch (Exception ex)
                {
                    PluginInstallerWindow.Log.Error(ex, $"Error during OpenConfig: {plugin.Name}");
                }
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(PluginInstallerLocs.PluginButtonToolTip_OpenConfiguration);
            }
        }
    }

        public void DrawDevPluginValidationIssues(LocalDevPlugin devPlugin)
    {
        if (!devPlugin.IsLoaded)
        {
            ImGui.TextColoredWrapped(ImGuiColors.DalamudGrey, "You have to load this plugin to see validation issues."u8);
        }
        else
        {
            var problems = PluginValidator.CheckForProblems(devPlugin);
            if (problems.Count == 0)
            {
                ImGui.PushFont(InterfaceManager.IconFont);
                ImGui.Text(FontAwesomeIcon.Check.ToIconString());
                ImGui.PopFont();
                ImGui.SameLine();
                ImGui.TextColoredWrapped(ImGuiColors.HealerGreen, "No validation issues found in this plugin!"u8);
            }
            else
            {
                var numValidProblems = problems.Count(problem => devPlugin.DismissedValidationProblems.All(name => name != problem.GetType().Name));
                var shouldBother = numValidProblems > 0;
                var validationIssuesText = shouldBother ? $"Found {problems.Count} validation issue{(problems.Count > 1 ? "s" : string.Empty)} in this plugin!" : $"{problems.Count} dismissed validation issue{(problems.Count > 1 ? "s" : string.Empty)} in this plugin.";

                using var col = ImRaii.PushColor(ImGuiCol.Text, shouldBother ? ImGuiColors.DalamudOrange : ImGuiColors.DalamudGrey);
                using var tree = ImRaii.TreeNode($"{validationIssuesText}###validationIssueCollapsible");
                if (tree.Success)
                {
                    foreach (var problem in problems)
                    {
                        var thisProblemIsDismissed = devPlugin.DismissedValidationProblems.Contains(problem.GetType().Name);

                        if (!thisProblemIsDismissed)
                        {
                            using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudWhite))
                            {
                                if (ImGuiComponents.IconButton(
                                        $"##dismissValidationIssue{problem.GetType().Name}",
                                        FontAwesomeIcon.TimesCircle))
                                {
                                    devPlugin.DismissedValidationProblems.Add(problem.GetType().Name);
                                    Service<DalamudConfiguration>.Get().QueueSave();
                                }

                                if (ImGui.IsItemHovered())
                                {
                                    ImGui.SetTooltip("Dismiss this issue"u8);
                                }
                            }

                            ImGui.SameLine();
                        }

                        var iconColor = problem.Severity switch
                        {
                            PluginValidator.ValidationSeverity.Fatal => ImGuiColors.DalamudRed,
                            PluginValidator.ValidationSeverity.Warning => ImGuiColors.DalamudOrange,
                            PluginValidator.ValidationSeverity.Information => ImGuiColors.TankBlue,
                            _ => ImGuiColors.DalamudGrey,
                        };

                        using (ImRaii.PushColor(ImGuiCol.Text, iconColor))
                        using (ImRaii.PushFont(InterfaceManager.IconFont))
                        {
                            switch (problem.Severity)
                            {
                                case PluginValidator.ValidationSeverity.Fatal:
                                    ImGui.Text(FontAwesomeIcon.TimesCircle.ToIconString());
                                    break;

                                case PluginValidator.ValidationSeverity.Warning:
                                    ImGui.Text(FontAwesomeIcon.ExclamationTriangle.ToIconString());
                                    break;

                                case PluginValidator.ValidationSeverity.Information:
                                    ImGui.Text(FontAwesomeIcon.InfoCircle.ToIconString());
                                    break;

                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                        }

                        ImGui.SameLine();

                        using (ImRaii.PushColor(ImGuiCol.Text, thisProblemIsDismissed ? ImGuiColors.DalamudGrey : ImGuiColors.DalamudWhite))
                        {
                            ImGui.TextWrapped(problem.GetLocalizedDescription());
                        }
                    }
                }
            }
        }
    }


    public void DrawDevPluginButtons(LocalPlugin localPlugin)
    {
        ImGui.SameLine();

        var configuration = Service<DalamudConfiguration>.Get();

        if (localPlugin is LocalDevPlugin plugin)
        {
            var isInDefaultProfile =
                Service<ProfileManager>.Get().IsInDefaultProfile(localPlugin.EffectiveWorkingPluginId);

            // https://colorswall.com/palette/2868/
            var greenColor = new Vector4(0x5C, 0xB8, 0x5C, 0xFF) / 0xFF;
            var redColor = new Vector4(0xD9, 0x53, 0x4F, 0xFF) / 0xFF;

            // Load on boot
            using (ImRaii.Disabled(!isInDefaultProfile))
            {
                ImGui.PushStyleColor(ImGuiCol.Button, plugin.StartOnBoot ? greenColor : redColor);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, plugin.StartOnBoot ? greenColor : redColor);

                ImGui.SameLine();
                if (ImGuiComponents.IconButton(FontAwesomeIcon.PowerOff))
                {
                    plugin.StartOnBoot ^= true;
                    configuration.QueueSave();
                }

                ImGui.PopStyleColor(2);

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(isInDefaultProfile ? PluginInstallerLocs.PluginButtonToolTip_StartOnBoot : PluginInstallerLocs.PluginButtonToolTip_NeedsToBeInDefault);
                }
            }

            // Automatic reload
            ImGui.PushStyleColor(ImGuiCol.Button, plugin.AutomaticReload ? greenColor : redColor);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, plugin.AutomaticReload ? greenColor : redColor);

            ImGui.SameLine();
            if (ImGuiComponents.IconButton(FontAwesomeIcon.SyncAlt))
            {
                plugin.AutomaticReload ^= true;
                configuration.QueueSave();
            }

            ImGui.PopStyleColor(2);

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(PluginInstallerLocs.PluginButtonToolTip_AutomaticReloading);
            }

            // Error Notifications
            ImGui.PushStyleColor(ImGuiCol.Button, plugin.NotifyForErrors ? greenColor : redColor);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, plugin.NotifyForErrors ? greenColor : redColor);

            ImGui.SameLine();
            if (ImGuiComponents.IconButton(FontAwesomeIcon.Bolt))
            {
                plugin.NotifyForErrors ^= true;
                configuration.QueueSave();
            }

            ImGui.PopStyleColor(2);

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(PluginInstallerLocs.PluginButtonToolTip_NotifyForErrors);
            }
        }
    }

    public void DrawDeletePluginButton(LocalPlugin plugin)
    {
        /*var unloaded = plugin.State == PluginState.Unloaded || plugin.State == PluginState.LoadError;

        // When policy check fails, the plugin is never loaded
        var showButton = unloaded && (plugin.IsDev || plugin.IsOutdated || plugin.IsBanned || plugin.IsOrphaned || !plugin.CheckPolicy());

        if (!showButton)
            return;*/

        var pluginManager = Service<PluginManager>.Get();

        var devNotDeletable = plugin.IsDev && plugin.State != PluginState.Unloaded && plugin.State != PluginState.DependencyResolutionFailed;

        ImGui.SameLine();
        if (plugin.State == PluginState.Loaded || devNotDeletable)
        {
            ImGui.PushFont(InterfaceManager.IconFont);
            ImGuiComponents.DisabledButton(FontAwesomeIcon.TrashAlt.ToIconString());
            ImGui.PopFont();

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(
                    plugin.State == PluginState.Loaded
                        ? PluginInstallerLocs.PluginButtonToolTip_DeletePluginLoaded
                        : PluginInstallerLocs.PluginButtonToolTip_DeletePluginRestricted);
            }
        }
        else
        {
            if (ImGuiComponents.IconButton(FontAwesomeIcon.TrashAlt))
            {
                try
                {
                    if (plugin.IsDev)
                    {
                        plugin.DllFile.Delete();
                    }
                    else
                    {
                        plugin.ScheduleDeletion(!plugin.Manifest.ScheduledForDeletion);
                    }

                    if (plugin.State is PluginState.Unloaded or PluginState.DependencyResolutionFailed)
                    {
                        pluginManager.RemovePlugin(plugin);
                    }
                }
                catch (Exception ex)
                {
                    PluginInstallerWindow.Log.Error(ex, $"Plugin installer threw an error during removal of {plugin.Name}");

                    this.pluginInstaller.ErrorModal.ShowErrorModal(PluginInstallerLocs.ErrorModal_DeleteFail(plugin.Name));
                }
            }

            if (ImGui.IsItemHovered())
            {
                string tooltipMessage;
                if (plugin.Manifest.ScheduledForDeletion)
                {
                    tooltipMessage = PluginInstallerLocs.PluginButtonToolTip_DeletePluginScheduledCancel;
                }
                else if (plugin.State is PluginState.Unloaded or PluginState.DependencyResolutionFailed)
                {
                    tooltipMessage = PluginInstallerLocs.PluginButtonToolTip_DeletePlugin;
                }
                else
                {
                    tooltipMessage = PluginInstallerLocs.PluginButtonToolTip_DeletePluginScheduled;
                }

                ImGui.SetTooltip(tooltipMessage);
            }
        }
    }

}
