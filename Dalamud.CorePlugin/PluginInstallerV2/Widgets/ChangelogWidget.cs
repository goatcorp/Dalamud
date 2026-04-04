using Dalamud.CorePlugin.PluginInstallerV2.Controllers;
using Dalamud.CorePlugin.PluginInstallerV2.Interfaces;

namespace Dalamud.CorePlugin.PluginInstallerV2.Widgets;

internal class ChangelogWidget : IPluginInstallerWidget
{
    public required PluginInstallerWindow2 ParentWindow { get; init; }
    public void Draw()
    {
    }

    /// <inheritdoc/>
    public void OnSearchUpdated(SearchController searchInfo)
    {
    }
}
