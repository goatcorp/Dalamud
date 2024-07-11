namespace Dalamud.Game.Config;

/// <summary>
/// An exception thrown when a matching config option is not present in the config section.
/// </summary>
public class ConfigOptionNotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigOptionNotFoundException"/> class.
    /// </summary>
    /// <param name="sectionName">Name of the section being accessed.</param>
    /// <param name="configOptionName">Name of the config option that was not found.</param>
    public ConfigOptionNotFoundException(string sectionName, string configOptionName)
        : base($"The option '{configOptionName}' is not available in {sectionName}.")
    {
    }
}
