namespace Dalamud.Memory.Exceptions;

/// <summary>
/// An exception thrown when VirtualAlloc fails.
/// </summary>
public class MemoryAllocationException : MemoryException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryAllocationException"/> class.
    /// </summary>
    public MemoryAllocationException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryAllocationException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public MemoryAllocationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryAllocationException"/> class.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public MemoryAllocationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
