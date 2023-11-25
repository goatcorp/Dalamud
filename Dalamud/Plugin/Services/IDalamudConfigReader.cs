using Dalamud.Configuration.Internal;
using Dalamud.Configuration.Internal.Types;

namespace Dalamud.Plugin.Services;

/// <summary>
/// A service for giving plugins access to select Dalamud settings.
/// </summary>
public interface IDalamudConfigReader
{
    /// <summary>
    /// Gets a value indicating whether the user has permitted plugins to collect analytics on them. Plugins should
    /// respect this setting *as well as* offer individual opt outs if it makes sense.
    /// </summary>
    public PluginAnalyticsConsent PluginAnalyticsConsent { get; }
    
    /// <summary>
    /// Gets an opaque string intended for use as an analytics ID for plugins. This value is derived from a persistent
    /// ID stored in Dalamud's configuration, and is unique per plugin requesting it. 
    /// </summary>
    /// <remarks>
    /// If the user has requested to disable analytics, this property will still return a valid ID that will be shared
    /// across <em>all</em> users of the plugin with analytics disabled.
    /// </remarks>
    public string PluginAnalyticsId { get; }

    
    /// <inheritdoc cref="DalamudConfiguration.IsMbCollect"/>
    public bool IsMbCollect { get; }
    
    /// <inheritdoc cref="DalamudConfiguration.DisableRmtFiltering"/>
    public bool DisableRmtFiltering { get; }
    
    /// <inheritdoc cref="DalamudConfiguration.IsAntiAntiDebugEnabled"/>
    public bool IsAntiAntiDebugEnabled { get; }
    
    /// <inheritdoc cref="DalamudConfiguration.EnablePluginUISoundEffects"/>
    public bool EnablePluginUISoundEffects { get; }
    
    /// <inheritdoc cref="DalamudConfiguration.AutoUpdatePlugins"/>
    public bool AutoUpdatePlugins { get; }
    
    /// <inheritdoc cref="DalamudConfiguration.DoButtonsSystemMenu"/>
    public bool DoButtonsSystemMenu { get; }
}
