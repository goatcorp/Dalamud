namespace Dalamud.Plugin.Internal.Types;

/// <summary>
/// Current supported level of the plugin.
/// </summary>
internal enum DevSupportState
{
    /// <summary>
    /// New features and support for issues (default).
    /// </summary>
    Active,

    /// <summary>
    /// Stable and only fix issues or update for patches.
    /// </summary>
    MaintenanceOnly,

    /// <summary>
    /// Waiting for a new dev to take over (adopt-a-plugin).
    /// </summary>
    Adoptable,

    /// <summary>
    /// Replaced by another plugin or vanilla feature.
    /// </summary>
    Obsolete,

    /// <summary>
    /// Removed due to plugin rules or other reasons by the dev.
    /// </summary>
    Discontinued,
}

/// <summary>
/// Extension methods for DevSupportState.
/// </summary>
internal static class DevSupportStateExtensions
{
    /// <summary>
    /// Checks if plugin is supported.
    /// </summary>
    /// <param name="value">The enum value to be checked.</param>
    /// <returns>indicator if plugin is supported.</returns>
    internal static bool IsSupported(this DevSupportState value)
    {
        return value is DevSupportState.Active or DevSupportState.MaintenanceOnly;
    }
}
