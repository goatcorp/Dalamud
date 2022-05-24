namespace Dalamud.Injector;

/// <summary>
/// The Dalamud loading method.
/// </summary>
internal enum DalamudLoadMode
{
    /// <summary>
    /// Rewrite the entrypoint.
    /// </summary>
    Entrypoint,

    /// <summary>
    /// Traditional process injection.
    /// </summary>
    Inject,
}
