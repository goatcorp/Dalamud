namespace Dalamud.Plugin.Ipc.Exceptions;

/// <summary>
/// This exception is thrown when a null value is provided for a data cache or it does not implement the expected type.
/// </summary>
public class DataCacheCreationError : IpcError
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataCacheCreationError"/> class.
    /// </summary>
    /// <param name="tag">Tag of the data cache.</param>
    /// <param name="creator">The assembly name of the caller.</param>
    /// <param name="expectedType">The type expected.</param>
    /// <param name="ex">The thrown exception.</param>
    public DataCacheCreationError(string tag, string creator, Type expectedType, Exception ex)
        : base($"The creation of the {expectedType} data cache {tag} initialized by {creator} was unsuccessful.", ex)
    {
    }
}
