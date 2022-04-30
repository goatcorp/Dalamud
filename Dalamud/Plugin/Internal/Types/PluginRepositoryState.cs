namespace Dalamud.Plugin.Internal.Types;

/// <summary>
/// Values representing plugin repository state.
/// </summary>
internal enum PluginRepositoryState
{
    /// <summary>
    /// State is unknown.
    /// </summary>
    Unknown,

    /// <summary>
    /// Currently loading.
    /// </summary>
    InProgress,

    /// <summary>
    /// Load was successful.
    /// </summary>
    Success,

    /// <summary>
    /// Load failed.
    /// </summary>
    Fail,
}
