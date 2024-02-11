namespace Dalamud.Plugin.Internal.Profiles;

/// <summary>
/// Class representing a single plugin in a profile.
/// </summary>
internal class ProfilePluginEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProfilePluginEntry"/> class.
    /// </summary>
    /// <param name="internalName">The internal name of the plugin.</param>
    /// <param name="workingPluginId">The ID of the plugin.</param>
    /// <param name="state">A value indicating whether or not this entry is enabled.</param>
    public ProfilePluginEntry(string internalName, Guid workingPluginId, bool state)
    {
        this.InternalName = internalName;
        this.WorkingPluginId = workingPluginId;
        this.IsEnabled = state;
    }

    /// <summary>
    /// Gets the internal name of the plugin.
    /// </summary>
    public string InternalName { get; }
    
    /// <summary>
    /// Gets or sets an ID uniquely identifying this specific instance of a plugin.
    /// </summary>
    public Guid WorkingPluginId { get; set; }

    /// <summary>
    /// Gets a value indicating whether or not this entry is enabled.
    /// </summary>
    public bool IsEnabled { get; }
}
