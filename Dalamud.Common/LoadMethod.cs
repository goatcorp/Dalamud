namespace Dalamud.Common;

/// <summary>
/// Enum describing the method Dalamud has been loaded.
/// </summary>
public enum LoadMethod
{
    /// <summary>
    /// Load Dalamud by rewriting the games entrypoint.
    /// </summary>
    Entrypoint,

    /// <summary>
    /// Load Dalamud via DLL-injection.
    /// </summary>
    DllInject,
}
