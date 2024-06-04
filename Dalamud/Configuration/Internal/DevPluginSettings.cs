using System.Collections.Generic;

namespace Dalamud.Configuration.Internal;

/// <summary>
/// Settings for DevPlugins.
/// </summary>
internal sealed class DevPluginSettings
{
    /// <summary>
    /// Gets or sets a value indicating whether this plugin should automatically start when Dalamud boots up.
    /// </summary>
    public bool StartOnBoot { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether this plugin should automatically reload on file change.
    /// </summary>
    public bool AutomaticReloading { get; set; } = false;
    
    /// <summary>
    /// Gets or sets an ID uniquely identifying this specific instance of a devPlugin.
    /// </summary>
    public Guid WorkingPluginId { get; set; } = Guid.Empty;
    
    /// <summary>
    /// Gets or sets a list of validation problems that have been dismissed by the user.
    /// </summary>
    public List<string> DismissedValidationProblems { get; set; } = new();
}
