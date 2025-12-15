namespace Dalamud.Plugin.VersionInfo;

/// <summary>
/// Interface exposing various information related to Dalamud versioning.
/// </summary>
public interface IDalamudVersionInfo
{
    /// <summary>
    /// Gets the Dalamud version.
    /// </summary>
    Version Version { get; }

    /// <summary>
    /// Gets the currently used beta track.
    /// Please don't tell users to switch branches. They have it bad enough, fix your things instead.
    /// Null if this build wasn't launched from XIVLauncher.
    /// </summary>
    string? BetaTrack { get; }
}
