using System;

namespace Dalamud.Plugin.Internal.Types;

/// <summary>
/// Plugin update status.
/// </summary>
internal class PluginUpdateStatus
{
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
    /// Gets or sets a value indicating whether the plugin was updated.
    /// </summary>
    public bool WasUpdated { get; set; }
}
