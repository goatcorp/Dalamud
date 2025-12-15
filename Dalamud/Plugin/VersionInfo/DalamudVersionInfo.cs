namespace Dalamud.Plugin.VersionInfo;

/// <inheritdoc />
internal class DalamudVersionInfo(Version version, string? track) : IDalamudVersionInfo
{
    /// <inheritdoc/>
    public Version Version { get; } = version;

    /// <inheritdoc/>
    public string? BetaTrack { get; } = track;
}
