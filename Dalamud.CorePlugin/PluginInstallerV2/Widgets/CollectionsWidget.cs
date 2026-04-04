using Dalamud.CorePlugin.PluginInstallerV2.Controllers;
using Dalamud.CorePlugin.PluginInstallerV2.Interfaces;

namespace Dalamud.CorePlugin.PluginInstallerV2.Widgets;

/// <summary>
/// Class repsonsible for drawing plugins collection widget.
/// </summary>
internal class CollectionsWidget : IPluginInstallerWidget
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
