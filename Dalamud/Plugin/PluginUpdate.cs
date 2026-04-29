namespace Dalamud.Plugin;

/// <summary>
/// The result of checking for an update for a plugin, including the latest version if available, and the changelog if available.
/// </summary>
/// <param name="Version">The version available.</param>
/// <param name="IsTesting">Whether this is a testing version.</param>
/// <param name="Changelog">The changelog of this version.</param>
public record PluginUpdate(Version Version, bool IsTesting, string? Changelog);
