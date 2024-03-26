using Dalamud.Interface.Internal.Windows.Settings.Widgets;

namespace Dalamud.Configuration.Internal.Types;

/// <summary>
/// An enum defining possible states for the analytics settings exposed to plugins.
/// </summary>
public enum PluginAnalyticsConsent
{
    /// <summary>
    /// Indicates that plugin developers should *not* enable any analytics functionality, and should not request to
    /// turn them on.
    /// </summary>
    [SettingsAnnotation("Opted Out", "Request plugins not collect analytics.")]
    OptedOut,
    
    /// <summary>
    /// Indicates that plugin developers should ask the user before collecting any analytics on them.
    /// </summary>
    [SettingsAnnotation("Ask Before Collecting", "Allow analytics, but request plugins ask before collecting any.")]
    Ask,
    
    /// <summary>
    /// Indicates that plugins developers are permitted to enable analytics without prompting the user for permission,
    /// should they so choose.
    /// </summary>
    [SettingsAnnotation("Opted In", "Automatically allow plugins to collect analytics.")]
    OptedIn,
}
