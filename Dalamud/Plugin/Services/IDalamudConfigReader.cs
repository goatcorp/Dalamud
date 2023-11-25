using Dalamud.Configuration.Internal;
using Dalamud.Configuration.Internal.Types;

namespace Dalamud.Plugin.Services;

/// <summary>
/// A service for giving plugins access to select Dalamud settings.
/// </summary>
public interface IDalamudConfigReader
{
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
