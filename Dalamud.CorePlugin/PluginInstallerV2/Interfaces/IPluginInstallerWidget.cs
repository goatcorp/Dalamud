namespace Dalamud.CorePlugin.PluginInstallerV2.Interfaces;

/// <summary>
/// Interface for defining Plugin Installer Widgets.
/// </summary>
internal interface IPluginInstallerWidget
{
    /// <summary>
    /// Gets a reference to parent window.
    /// </summary>
    PluginInstallerWindow2 ParentWindow { get; init; }

    /// <summary>
    /// Draw this widget.
    /// </summary>
    void Draw();
}
