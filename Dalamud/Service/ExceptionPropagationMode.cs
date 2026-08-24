namespace Dalamud;

/// <summary>
/// Specifies how to handle the cases of failed services when calling <see cref="Service{T}.GetNullable"/>.
/// </summary>
internal enum ExceptionPropagationMode
{
    /// <summary>
    /// Propagate all exceptions.
    /// </summary>
    PropagateAll,

    /// <summary>
    /// Propagate all exceptions, except for <see cref="Service{T}.UnloadedException"/>.
    /// </summary>
    PropagateNonUnloaded,

    /// <summary>
    /// Treat all exceptions as null.
    /// </summary>
    None,
}
