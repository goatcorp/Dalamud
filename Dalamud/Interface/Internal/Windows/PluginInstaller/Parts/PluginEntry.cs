using System.Linq;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Configuration.Internal;
using Dalamud.Console;
using Dalamud.Game.Command;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Internal.Windows.PluginInstaller.Enums;
using Dalamud.Interface.Internal.Windows.PluginInstaller.Modals;
using Dalamud.Interface.Utility;
using Dalamud.Plugin.Internal;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Plugin.Internal.Types.Manifest;
using Dalamud.Utility;

namespace Dalamud.Interface.Internal.Windows.PluginInstaller.Parts;

internal class PluginEntry
{
    private readonly PluginInstallerWindow pluginInstaller;
    private readonly PluginCategoryManager categoryManager;
    private readonly FeedbackModal feedbackModal;

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

        if (this.pluginInstaller.DrawPluginCollapsingHeader(label, plugin, plugin.Manifest, flags, () => this.pluginInstaller.DrawInstalledPluginContextMenu(plugin, testingOptIn), index))
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
                this.pluginInstaller.DrawDevPluginValidationIssues(devPlugin);
                ImGuiHelpers.ScaledDummy(5);
            }

            // Controls
            this.pluginInstaller.DrawPluginControlButton(plugin, availablePluginUpdate);
            this.pluginInstaller.DrawDevPluginButtons(plugin);
            this.pluginInstaller.DrawVisitRepoUrlButton(plugin.Manifest.RepoUrl, false);
            this.pluginInstaller.DrawDeletePluginButton(plugin);

            if (canFeedback)
            {
                ImGui.SameLine();
                this.feedbackModal.DrawSendFeedbackButton(plugin.Manifest, plugin.IsTesting, false);
            }

            if (availablePluginUpdate != default && !plugin.IsDev)
            {
                ImGui.SameLine();
                this.pluginInstaller.DrawUpdateSinglePluginButton(availablePluginUpdate);
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

        ImGui.PushStyleColor(ImGuiCol.ChildBg, this.pluginInstaller.changelogBgColor);
        ImGui.PushStyleColor(ImGuiCol.Text, this.pluginInstaller.changelogTextColor);

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
}
