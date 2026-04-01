using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Internal.Windows.PluginInstaller.Changelog;
using Dalamud.Utility;

namespace Dalamud.Interface.Internal.Windows.PluginInstaller.Parts;

/// <summary>
/// Class responsible for drawing the changelog category.
/// </summary>
internal class ChangelogCategory
{
    private readonly PluginInstallerWindow pluginInstaller;
    private readonly PluginCategoryManager categoryManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChangelogCategory"/> class.
    /// </summary>
    /// <param name="pluginInstaller">Reference to main Installer Window.</param>
    /// <param name="categoryManager">Category Manager.</param>
    public ChangelogCategory(PluginInstallerWindow pluginInstaller, PluginCategoryManager categoryManager)
    {
        this.pluginInstaller = pluginInstaller;
        this.categoryManager = categoryManager;
    }

    /// <summary>
    /// Draw Changelog Category.
    /// </summary>
    public void Draw()
    {
        switch (this.categoryManager.CurrentCategoryKind)
        {
            case PluginCategoryManager.CategoryKind.All:
                this.DrawChangelogList(true, true);
                break;

            case PluginCategoryManager.CategoryKind.DalamudChangelogs:
                this.DrawChangelogList(true, false);
                break;

            case PluginCategoryManager.CategoryKind.PluginChangelogs:
                this.DrawChangelogList(false, true);
                break;

            default:
                ImGui.Text("You found a quiet category. Please don't wake it up."u8);
                break;
        }
    }

    private void DrawChangelogList(bool displayDalamud, bool displayPlugins)
    {
        if (this.pluginInstaller.pluginListInstalled.Count is 0)
        {
            ImGui.TextColored(ImGuiColors.DalamudGrey, PluginInstallerLocs.TabBody_SearchNoInstalled);
            return;
        }

        if (this.pluginInstaller.dalamudChangelogRefreshTask?.IsFaulted == true ||
            this.pluginInstaller.dalamudChangelogRefreshTask?.IsCanceled == true)
        {
            ImGui.TextColored(ImGuiColors.DalamudGrey, PluginInstallerLocs.TabBody_ChangelogError);
            return;
        }

        if (this.pluginInstaller.dalamudChangelogManager?.Changelogs == null)
        {
            ImGui.TextColored(ImGuiColors.DalamudGrey, PluginInstallerLocs.TabBody_LoadingPlugins);

            if (this.pluginInstaller.dalamudChangelogManager != null &&
                this.pluginInstaller.dalamudChangelogRefreshTask == null)
            {
                this.pluginInstaller.dalamudChangelogRefreshTaskCts = new CancellationTokenSource();
                this.pluginInstaller.dalamudChangelogRefreshTask =
                    Task.Run(this.pluginInstaller.dalamudChangelogManager.ReloadChangelogAsync, this.pluginInstaller.dalamudChangelogRefreshTaskCts.Token)
                        .ContinueWith(t =>
                        {
                            if (!t.IsCompletedSuccessfully)
                            {
                                PluginInstallerWindow.Log.Error(t.Exception, "Failed to load changelogs.");
                            }
                        });
            }

            return;
        }

        IEnumerable<IChangelogEntry> changelogs = null;
        if (displayDalamud && displayPlugins && this.pluginInstaller.dalamudChangelogManager.Changelogs != null)
        {
            changelogs = this.pluginInstaller.dalamudChangelogManager.Changelogs;
        }
        else if (displayDalamud && this.pluginInstaller.dalamudChangelogManager.Changelogs != null)
        {
            changelogs = this.pluginInstaller.dalamudChangelogManager.Changelogs.OfType<DalamudChangelogEntry>();
        }
        else if (displayPlugins)
        {
            changelogs = this.pluginInstaller.dalamudChangelogManager.Changelogs.OfType<PluginChangelogEntry>();
        }

        var sortedChangelogs = changelogs?.Where(x => this.pluginInstaller.searchText.IsNullOrWhitespace() || new FuzzyMatcher(this.pluginInstaller.searchText.ToLowerInvariant(), MatchMode.FuzzyParts).Matches(x.Title.ToLowerInvariant()) > 0)
                                         .OrderByDescending(x => x.Date).ToList();

        if (sortedChangelogs == null || sortedChangelogs.Count == 0)
        {
            ImGui.TextColored(
                ImGuiColors.DalamudGrey2,
                this.pluginInstaller.pluginListInstalled.Any(plugin => !plugin.Manifest.Changelog.IsNullOrEmpty())
                    ? PluginInstallerLocs.TabBody_SearchNoMatching
                    : PluginInstallerLocs.TabBody_ChangelogNone);

            return;
        }

        foreach (var logEntry in sortedChangelogs)
        {
            this.pluginInstaller.DrawChangelog(logEntry);
        }
    }
}
