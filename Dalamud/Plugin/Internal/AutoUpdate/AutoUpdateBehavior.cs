namespace Dalamud.Plugin.Internal.AutoUpdate;

/// <summary>
/// Enum describing how plugins should be auto-updated at startup-.
/// </summary>
internal enum AutoUpdateBehavior
{
    /// <summary>
    /// Plugins should not be updated and the user should not be notified.
    /// </summary>
    None,
    
    /// <summary>
    /// The user should merely be notified about updates.
    /// </summary>
    OnlyNotify,
    
    /// <summary>
    /// Only plugins from the main repository should be updated.
    /// </summary>
    UpdateMainRepo,
    
    /// <summary>
    /// All plugins should be updated.
    /// </summary>
    UpdateAll,
}
