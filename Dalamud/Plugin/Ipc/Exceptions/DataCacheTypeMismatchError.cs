namespace Dalamud.Plugin.Ipc.Exceptions;

/// <summary>
/// This exception is thrown when a data cache is accessed with the wrong type.
/// </summary>
public class DataCacheTypeMismatchError : IpcError
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataCacheTypeMismatchError"/> class.
    /// </summary>
    /// <param name="tag">Tag of the data cache.</param>
    /// <param name="creator">Assembly name of the plugin creating the cache.</param>
    /// <param name="requestedType">The requested type.</param>
    /// <param name="actualType">The stored type.</param>
    public DataCacheTypeMismatchError(string tag, string creator, Type requestedType, Type actualType)
        : base($"Data cache {tag} was requested with type {requestedType}, but {creator} created type {actualType}.")
    {
    }
}
