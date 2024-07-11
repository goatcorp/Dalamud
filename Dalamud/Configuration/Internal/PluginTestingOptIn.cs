namespace Dalamud.Configuration.Internal;

/// <summary>
/// Represents a plugin that has opted in to testing.
/// </summary>
internal record PluginTestingOptIn
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginTestingOptIn"/> class.
    /// </summary>
    /// <param name="internalName">The internal name of the plugin.</param>
    public PluginTestingOptIn(string internalName)
    {
        this.InternalName = internalName;
        this.Branch = "testing-live"; // TODO: make these do something, needs work in plogon
    }

    /// <summary>
    /// Gets the internal name of the plugin to test.
    /// </summary>
    public string InternalName { get; private set; }

    /// <summary>
    /// Gets the testing branch to use.
    /// </summary>
    public string Branch { get; private set; }
}
