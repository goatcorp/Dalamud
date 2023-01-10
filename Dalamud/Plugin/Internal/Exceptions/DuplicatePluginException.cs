namespace Dalamud.Plugin.Internal.Exceptions;

/// <summary>
/// This exception that is thrown when a plugin is instructed to load while another plugin with the same
/// assembly name is already present and loaded.
/// </summary>
internal class DuplicatePluginException : PluginException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DuplicatePluginException"/> class.
    /// </summary>
    /// <param name="assemblyName">Name of the conflicting assembly.</param>
    public DuplicatePluginException(string assemblyName)
    {
        this.AssemblyName = assemblyName;
    }

    /// <summary>
    /// Gets the name of the conflicting assembly.
    /// </summary>
    public string AssemblyName { get; init; }

    /// <inheritdoc/>
    public override string Message => $"A plugin with the same assembly name of {this.AssemblyName} is already loaded";
}
