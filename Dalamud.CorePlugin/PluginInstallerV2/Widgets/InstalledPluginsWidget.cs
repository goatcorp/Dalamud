using Dalamud.CorePlugin.PluginInstallerV2.Interfaces;

namespace Dalamud.CorePlugin.PluginInstallerV2.Widgets;

internal class InstalledPluginsWidget : IPluginInstallerWidget
{
    public required PluginInstallerWindow2 ParentWindow { get; init; }
    public void Draw()
    {
    }
}
