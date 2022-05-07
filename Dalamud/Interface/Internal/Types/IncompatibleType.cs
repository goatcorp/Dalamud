namespace Dalamud.Interface.Internal.Types;

/// <summary>
/// Enum describing incompatible type for the user interface.
/// </summary>
public enum IncompatibleType
{
    /// <summary>
    /// Show all incompatible types.
    /// </summary>
    All,

    /// <summary>
    /// Show outdated plugins by api level or game version.
    /// </summary>
    Outdated,

    /// <summary>
    /// Show plugins available for adoption by a new developer.
    /// </summary>
    Adoptable,

    /// <summary>
    /// Show plugins that are no longer needed due to game feature or replacement plugin.
    /// </summary>
    Obsolete,

    /// <summary>
    /// Show plugins removed due to main repo rules or developer's discretion.
    /// </summary>
    Discontinued,
}
