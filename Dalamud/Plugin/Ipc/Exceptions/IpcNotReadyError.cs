namespace Dalamud.Plugin.Ipc.Exceptions;

/// <summary>
/// This exception is thrown when an IPC method is invoked, but no actions or funcs have been registered yet.
/// </summary>
public class IpcNotReadyError : IpcError
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IpcNotReadyError"/> class.
    /// </summary>
    /// <param name="name">Name of the IPC method.</param>
    public IpcNotReadyError(string name)
        : base($"IPC method {name} was not registered yet")
    {
    }
}
