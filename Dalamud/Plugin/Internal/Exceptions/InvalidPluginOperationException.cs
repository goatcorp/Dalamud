namespace Dalamud.Plugin.Internal.Exceptions;

/// <summary>
/// This represents an invalid plugin operation.
/// </summary>
internal class InvalidPluginOperationException : PluginException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidPluginOperationException"/> class.
    /// </summary>
    /// <param name="message">The message describing the invalid operation.</param>
    public InvalidPluginOperationException(string message)
    {
        this.Message = message;
    }

    /// <summary>
    /// Gets the message describing the invalid operation.
    /// </summary>
    public override string Message { get; }
}
