namespace Dalamud.Memory.Exceptions;

/// <summary>
/// An exception thrown when VirtualProtect fails.
/// </summary>
public class MemoryPermissionException : MemoryException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryPermissionException"/> class.
    /// </summary>
    public MemoryPermissionException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryPermissionException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public MemoryPermissionException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryPermissionException"/> class.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public MemoryPermissionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
