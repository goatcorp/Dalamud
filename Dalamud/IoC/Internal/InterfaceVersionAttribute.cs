namespace Dalamud.IoC.Internal;

/// <summary>
/// This attribute represents the current version of a module that is loaded in the Service Locator.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
internal class InterfaceVersionAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InterfaceVersionAttribute"/> class.
    /// </summary>
    /// <param name="version">The current version.</param>
    public InterfaceVersionAttribute(string version)
    {
        this.Version = new(version);
    }

    /// <summary>
    /// Gets the service version.
    /// </summary>
    public Version Version { get; }
}
