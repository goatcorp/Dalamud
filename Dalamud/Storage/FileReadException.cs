namespace Dalamud.Storage;

/// <summary>
/// Thrown if all read operations fail.
/// </summary>
public class FileReadException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileReadException"/> class.
    /// </summary>
    /// <param name="inner">Inner error that caused this exception.</param>
    internal FileReadException(Exception inner)
        : base("Failed to read file", inner)
    {
    }
}
