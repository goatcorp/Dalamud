namespace Dalamud.Plugin.Ipc.Exceptions;

/// <summary>
/// This exception is thrown when a null value is provided for a data cache or it does not implement the expected type.
/// </summary>
public class DataCacheValueNullError : IpcError
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataCacheValueNullError"/> class.
    /// </summary>
    /// <param name="tag">Tag of the data cache.</param>
    /// <param name="expectedType">The type expected.</param>
    public DataCacheValueNullError(string tag, Type expectedType)
        : base($"The data cache {tag} expects a type of {expectedType} but does not implement it.")
    {
    }
}
