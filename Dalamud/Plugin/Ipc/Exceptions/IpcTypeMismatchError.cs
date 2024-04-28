namespace Dalamud.Plugin.Ipc.Exceptions;

/// <summary>
/// This exception is thrown when an IPC method is checked out, but the type does not match what was previously registered.
/// </summary>
public class IpcTypeMismatchError : IpcError
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IpcTypeMismatchError"/> class.
    /// </summary>
    /// <param name="name">Name of the IPC method.</param>
    /// <param name="requestedType">The before type.</param>
    /// <param name="actualType">The after type.</param>
    /// <param name="ex">The exception that is the cause of the current exception.</param>
    public IpcTypeMismatchError(string name, Type requestedType, Type actualType, Exception ex)
        : base($"IPC method {name} blew up when converting from {requestedType.Name} to {actualType}", ex)
    {
    }
}
