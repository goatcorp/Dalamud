using Dalamud.CorePlugin.PluginInstallerV2.Controllers;
using Dalamud.CorePlugin.PluginInstallerV2.Interfaces;

namespace Dalamud.CorePlugin.PluginInstallerV2.Widgets;

/// <summary>
/// Widget responsible for drawing all changelogs, including ours.
/// </summary>
internal class ChangelogWidget : IPluginInstallerWidget
{
    /// <inheritdoc/>
    public required PluginInstallerWindow2 ParentWindow { get; init; }

    /// <inheritdoc/>
    public void Draw()
    {
    }

    /// <inheritdoc/>
    public void OnSearchUpdated(SearchController searchInfo)
    {
    }
}
