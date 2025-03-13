namespace Dalamud.Plugin.Ipc.Exceptions;

/// <summary>
/// This exception is thrown when an IPC errors are encountered.
/// </summary>
public abstract class IpcError : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IpcError"/> class.
    /// </summary>
    public IpcError()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IpcError"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public IpcError(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IpcError"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="ex">The exception that is the cause of the current exception.</param>
    public IpcError(string message, Exception ex)
        : base(message, ex)
    {
    }
}
