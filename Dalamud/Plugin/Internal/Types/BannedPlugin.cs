using Newtonsoft.Json;

namespace Dalamud.Plugin.Internal.Types;

/// <summary>
/// Banned plugin version that is blocked from installation.
/// </summary>
internal struct BannedPlugin
{
    /// <summary>
    /// Gets plugin name.
    /// </summary>
    [JsonProperty]
    public string Name { get; private set; }

    /// <summary>
    /// Gets plugin assembly version.
    /// </summary>
    [JsonProperty]
    public Version AssemblyVersion { get; private set; }

    /// <summary>
    /// Gets reason for the ban.
    /// </summary>
    [JsonProperty]
    public string Reason { get; private set; }
}
