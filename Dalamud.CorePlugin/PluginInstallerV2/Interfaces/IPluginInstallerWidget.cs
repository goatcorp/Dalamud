using Dalamud.CorePlugin.PluginInstallerV2.Controllers;

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

    /// <summary>
    /// Function that is called when the search is updated.
    /// </summary>
    /// <param name="searchInfo">Information regarding the current search and sort settings.</param>
    void OnSearchUpdated(SearchController searchInfo);
}
