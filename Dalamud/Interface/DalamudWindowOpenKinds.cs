namespace Dalamud.Interface;

/// <summary>
/// Enum describing pages the plugin installer can be opened to.
/// </summary>
public enum PluginInstallerOpenKind
{
    /// <summary>
    /// Open to the "All Plugins" page.
    /// </summary>
    AllPlugins,

    /// <summary>
    /// Open to the "Installed Plugins" page.
    /// </summary>
    InstalledPlugins,

    /// <summary>
    /// Open to the "Changelogs" page.
    /// </summary>
    Changelogs,
}

/// <summary>
/// Enum describing tabs the settings window can be opened to.
/// </summary>
public enum SettingsOpenKind
{
    /// <summary>
    /// Open to the "General" page.
    /// </summary>
    General,
    
    /// <summary>
    /// Open to the "Look &#038; Feel" page.
    /// </summary>
    LookAndFeel,

    /// <summary>
    /// Open to the "Server Info Bar" page.
    /// </summary>
    ServerInfoBar,

    /// <summary>
    /// Open to the "Experimental" page.
    /// </summary>
    Experimental,

    /// <summary>
    /// Open to the "About" page.
    /// </summary>
    About,
}
