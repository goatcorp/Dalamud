namespace Dalamud.CorePlugin.PluginInstallerV2.Enums;

/// <summary>
/// Enum indicating error severity for displaying in the Plugin Installer.
/// </summary>
internal enum ErrorSeverity
{
    /// <summary>
    /// Warning entry, should render with yellow/orangish text.
    /// </summary>
    Warning,

    /// <summary>
    /// Error entry, should render with red/darker text.
    /// </summary>
    Error,
}
