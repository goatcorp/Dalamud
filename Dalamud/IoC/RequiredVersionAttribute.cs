namespace Dalamud.IoC;

/// <summary>
/// This attribute indicates the version of a service module that is required for the plugin to load.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public class RequiredVersionAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequiredVersionAttribute"/> class.
    /// </summary>
    /// <param name="version">The required version.</param>
    public RequiredVersionAttribute(string version)
    {
        this.Version = new(version);
    }

    /// <summary>
    /// Gets the required version.
    /// </summary>
    public Version Version { get; }
}
