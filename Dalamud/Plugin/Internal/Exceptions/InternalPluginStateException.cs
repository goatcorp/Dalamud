namespace Dalamud.Plugin.Internal.Exceptions;

/// <summary>
/// An exception to be thrown when policy blocks a plugin from loading.
/// </summary>
internal class InternalPluginStateException : InvalidPluginOperationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InternalPluginStateException"/> class.
    /// </summary>
    /// <param name="message">The message to associate with this exception.</param>
    public InternalPluginStateException(string message)
        : base(message)
    {
    }
}
