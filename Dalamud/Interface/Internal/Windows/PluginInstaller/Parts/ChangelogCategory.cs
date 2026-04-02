using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Internal.Windows.PluginInstaller.Changelog;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
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
            this.DrawChangelog(logEntry);
        }
    }

    private void DrawChangelog(IChangelogEntry log)
    {
        ImGui.Separator();

        var startCursor = ImGui.GetCursorPos();

        var iconSize = ImGuiHelpers.ScaledVector2(64, 64);
        var cursorBeforeImage = ImGui.GetCursorPos();
        var rectOffset = ImGui.GetWindowContentRegionMin() + ImGui.GetWindowPos();
        if (ImGui.IsRectVisible(rectOffset + cursorBeforeImage, rectOffset + cursorBeforeImage + iconSize))
        {
            IDalamudTextureWrap icon;
            if (log is PluginChangelogEntry pluginLog)
            {
                icon = this.pluginInstaller.imageCache.DefaultIcon;
                var hasIcon = this.pluginInstaller.imageCache.TryGetIcon(pluginLog.Plugin, pluginLog.Plugin.Manifest, pluginLog.Plugin.IsThirdParty, out var cachedIconTex, out _);
                if (hasIcon && cachedIconTex != null)
                {
                    icon = cachedIconTex;
                }
            }
            else
            {
                icon = this.pluginInstaller.imageCache.CorePluginIcon;
            }

            ImGui.Image(icon.Handle, iconSize);
        }
        else
        {
            ImGui.Dummy(iconSize);
        }

        ImGui.SameLine();

        ImGuiHelpers.ScaledDummy(5);

        ImGui.SameLine();
        var cursor = ImGui.GetCursorPos();
        ImGui.Text(log.Title);

        ImGui.SameLine();
        ImGui.TextColored(ImGuiColors.DalamudGrey3, $" v{log.Version}");
        if (log.Author != null)
        {
            ImGui.SameLine();
            ImGui.TextColored(ImGuiColors.DalamudGrey3, PluginInstallerLocs.PluginBody_AuthorWithoutDownloadCount(log.Author));
        }

        if (log.Date != DateTime.MinValue)
        {
            var whenText = log.Date.LocRelativePastLong();
            var whenSize = ImGui.CalcTextSize(whenText);
            ImGui.SameLine(ImGui.GetWindowWidth() - whenSize.X - (25 * ImGuiHelpers.GlobalScale));
            ImGui.TextColored(ImGuiColors.DalamudGrey3, whenText);
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Published on " + log.Date.LocAbsolute());
        }

        cursor.Y += ImGui.GetTextLineHeightWithSpacing();
        ImGui.SetCursorPos(cursor);

        ImGui.TextWrapped(log.Text);

        var endCursor = ImGui.GetCursorPos();

        var sectionSize = Math.Max(
            66 * ImGuiHelpers.GlobalScale, // min size due to icons
            endCursor.Y - startCursor.Y);

        startCursor.Y += sectionSize;
        ImGui.SetCursorPos(startCursor);
    }
}
