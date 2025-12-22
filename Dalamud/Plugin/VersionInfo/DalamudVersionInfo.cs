namespace Dalamud.Plugin.VersionInfo;

/// <inheritdoc />
internal class DalamudVersionInfo(Version version, string? track, string? gitHash, string? gitHashClientStructs, string? scmVersion) : IDalamudVersionInfo
{
    /// <inheritdoc/>
    public Version Version { get; } = version;

    /// <inheritdoc/>
    public string? BetaTrack { get; } = track;

    /// <inheritdoc/>
    public string? GitHash { get; } = gitHash;

    /// <inheritdoc/>
    public string? GitHashClientStructs { get; } = gitHashClientStructs;

    /// <inheritdoc/>
    public string? ScmVersion { get; } = scmVersion;
}
