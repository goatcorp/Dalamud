using Dalamud.CorePlugin.PluginInstallerV2.Controllers;
using Dalamud.CorePlugin.PluginInstallerV2.Interfaces;

namespace Dalamud.CorePlugin.PluginInstallerV2.Widgets;

/// <summary>
/// Class responsible for drawing the InstalledPlugins Lists.
/// </summary>
internal class InstalledPluginsWidget : IPluginInstallerWidget
{
    /// <inheritdoc/>
    public required PluginInstallerWindow2 ParentWindow { get; init; }

    /// <inheritdoc/>
    public void Draw()
    {
    }
}
