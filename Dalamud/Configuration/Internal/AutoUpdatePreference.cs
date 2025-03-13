namespace Dalamud.Configuration.Internal;

/// <summary>
/// Class representing a plugin that has opted in to auto-updating.
/// </summary>
internal class AutoUpdatePreference
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AutoUpdatePreference"/> class.
    /// </summary>
    /// <param name="pluginId">The unique ID representing the plugin.</param>
    public AutoUpdatePreference(Guid pluginId)
    {
        this.WorkingPluginId = pluginId;
    }
    
    /// <summary>
    /// The kind of opt-in.
    /// </summary>
    public enum OptKind
    {
        /// <summary>
        /// Never auto-update this plugin.
        /// </summary>
        NeverUpdate,
        
        /// <summary>
        /// Always auto-update this plugin, regardless of the user's settings.
        /// </summary>
        AlwaysUpdate,
    }
    
    /// <summary>
    /// Gets or sets the unique ID representing the plugin.
    /// </summary>
    public Guid WorkingPluginId { get; set; }

    /// <summary>
    /// Gets or sets the type of opt-in.
    /// </summary>
    public OptKind Kind { get; set; } = OptKind.AlwaysUpdate;
}
