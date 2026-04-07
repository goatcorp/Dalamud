using Dalamud.CorePlugin.PluginInstallerV2.Controllers;
using Dalamud.CorePlugin.PluginInstallerV2.Interfaces;

namespace Dalamud.CorePlugin.PluginInstallerV2.Widgets;

/// <summary>
/// Class responsible for drawing the Dev Tools Widget.
/// </summary>
internal class DevPluginsWidget : IPluginInstallerWidget
{
    /// <inheritdoc/>
    public required PluginInstallerWindow2 ParentWindow { get; init; }

    /// <inheritdoc/>
    public void Draw()
    {
    }
}
