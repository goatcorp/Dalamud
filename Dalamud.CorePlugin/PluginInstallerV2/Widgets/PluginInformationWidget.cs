using Dalamud.CorePlugin.PluginInstallerV2.Controllers;
using Dalamud.CorePlugin.PluginInstallerV2.Interfaces;

namespace Dalamud.CorePlugin.PluginInstallerV2.Widgets;

/// <summary>
/// Class responsible for displaying compressive information about a plugin.
/// This is the big page with the plugins images, description, changelog, controls, etc.
/// </summary>
internal class PluginInformationWidget : IPluginInstallerWidget
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
        // Do nothing, search should be ignored while this widget is active.
    }
}
