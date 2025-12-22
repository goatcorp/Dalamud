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

    /// <summary>
    /// Gets the git commit hash value from the assembly or null if it cannot be found. Will be null for Debug builds,
    /// and will be suffixed with `-dirty` if in release with pending changes.
    /// </summary>
    string? GitHash { get; }

    /// <summary>
    /// Gets the git hash value from the assembly or null if it cannot be found.
    /// </summary>
    string? GitHashClientStructs { get; }

    /// <summary>
    /// Gets the SCM Version from the assembly, or null if it cannot be found. The value returned will generally be
    /// the <c>git describe</c> output for this build, which will be a raw version if this is a stable build or an
    /// appropriately-annotated version if this is *not* stable. Local builds will return a `Local Build` text string.
    /// </summary>
    string? ScmVersion { get; }
}
