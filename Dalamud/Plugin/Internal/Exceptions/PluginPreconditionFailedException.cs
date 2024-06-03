namespace Dalamud.Plugin.Internal.Exceptions;

/// <summary>
/// An exception to be thrown when policy blocks a plugin from loading.
/// </summary>
internal class PluginPreconditionFailedException : InvalidPluginOperationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginPreconditionFailedException"/> class.
    /// </summary>
    /// <param name="message">The message to associate with this exception.</param>
    public PluginPreconditionFailedException(string message)
        : base(message)
    {
    }
}
