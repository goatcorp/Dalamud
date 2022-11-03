namespace Dalamud.Plugin.Ipc.Exceptions;

/// <summary>
/// This exception is thrown when an IPC method is invoked and the number of types does not match what was previously registered.
/// </summary>
public class IpcLengthMismatchError : IpcError
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IpcLengthMismatchError"/> class.
    /// </summary>
    /// <param name="name">Name of the IPC method.</param>
    /// <param name="requestedLength">The amount of types requested when checking out the IPC.</param>
    /// <param name="actualLength">The amount of types registered by the IPC.</param>
    public IpcLengthMismatchError(string name, int requestedLength, int actualLength)
        : base($"IPC method {name} has a different number of types than was requested. {requestedLength} != {actualLength}")
    {
    }
}
