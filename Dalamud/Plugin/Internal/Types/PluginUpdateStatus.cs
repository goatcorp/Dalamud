using CheapLoc;

namespace Dalamud.Plugin.Internal.Types;

/// <summary>
/// Plugin update status.
/// </summary>
internal class PluginUpdateStatus
{
    /// <summary>
    /// Enum containing possible statuses of a plugin update.
    /// </summary>
    public enum StatusKind
    {
        /// <summary>
        /// The update is pending.
        /// </summary>
        Pending,
        
        /// <summary>
        /// The update failed to download.
        /// </summary>
        FailedDownload,
        
        /// <summary>
        /// The outdated plugin did not unload correctly.
        /// </summary>
        FailedUnload,
        
        /// <summary>
        /// The updated plugin did not load correctly.
        /// </summary>
        FailedLoad,
        
        /// <summary>
        /// The update succeeded.
        /// </summary>
        Success,
    }
    
    /// <summary>
    /// Gets the plugin internal name.
    /// </summary>
    public string InternalName { get; init; } = null!;

    /// <summary>
    /// Gets the plugin name.
    /// </summary>
    public string Name { get; init; } = null!;

    /// <summary>
    /// Gets the plugin version.
    /// </summary>
    public Version Version { get; init; } = null!;

    /// <summary>
    /// Gets or sets a value indicating the status of the update.
    /// </summary>
    public StatusKind Status { get; set; } = StatusKind.Pending;

    /// <summary>
    /// Gets a value indicating whether the plugin has a changelog if it was updated.
    /// </summary>
    public bool HasChangelog { get; init; }

    /// <summary>
    /// Get a localized version of the update status.
    /// </summary>
    /// <param name="status">Status to localize.</param>
    /// <returns>Localized text.</returns>
    public static string LocalizeUpdateStatusKind(StatusKind status) => status switch
    {
        StatusKind.Pending => Loc.Localize("InstallerUpdateStatusPending", "Pending"),
        StatusKind.FailedDownload => Loc.Localize("InstallerUpdateStatusFailedDownload", "Download failed"),
        StatusKind.FailedUnload => Loc.Localize("InstallerUpdateStatusFailedUnload", "Unload failed"),
        StatusKind.FailedLoad => Loc.Localize("InstallerUpdateStatusFailedLoad", "Load failed"),
        StatusKind.Success => Loc.Localize("InstallerUpdateStatusSuccess", "Success"),
        _ => "???",
    };
}
