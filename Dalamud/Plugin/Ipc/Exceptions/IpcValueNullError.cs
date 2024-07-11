namespace Dalamud.Plugin.Ipc.Exceptions;

/// <summary>
/// This exception is thrown when a null value is passed to an IPC requiring a value type.
/// </summary>
public class IpcValueNullError : IpcError
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IpcValueNullError"/> class.
    /// </summary>
    /// <param name="name">Name of the IPC.</param>
    /// <param name="expectedType">The type expected.</param>
    /// <param name="index">Index of the failing argument.</param>
    public IpcValueNullError(string name, Type expectedType, int index)
        : base($"IPC {name} expects a value type({expectedType.FullName}) at index {index}, null given.")
    {
        // ignored
    }
}
