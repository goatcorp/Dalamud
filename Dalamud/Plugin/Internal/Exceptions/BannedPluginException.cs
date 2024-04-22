namespace Dalamud.Plugin.Internal.Exceptions;

/// <summary>
/// This represents a banned plugin that attempted an operation.
/// </summary>
internal class BannedPluginException : PluginPreconditionFailedException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BannedPluginException"/> class.
    /// </summary>
    /// <param name="message">The message describing the invalid operation.</param>
    public BannedPluginException(string message)
        : base(message)
    {
    }
}
