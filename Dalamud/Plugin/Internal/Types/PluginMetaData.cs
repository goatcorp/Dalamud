using Newtonsoft.Json;

namespace Dalamud.Plugin.Internal.Types;

/// <summary>
/// Plugin meta data supplements and overrides to take precedence over plugin manifest.
/// </summary>
internal struct PluginMetaData
{
    /// <summary>
    /// Gets plugin name.
    /// </summary>
    [JsonProperty]
    public string Name { get; private set; }

    /// <summary>
    /// Gets or sets notice/warning to display for plugin (can be used where there is an issue but not ban worthy).
    /// </summary>
    [JsonProperty]
    public string Notice { get; internal set; }

    /// <summary>
    /// Gets or sets developer support state.
    /// </summary>
    [JsonProperty]
    public DevSupportState DevSupportState { get; internal set; }

    /// <summary>
    /// Gets or sets reason / comment for the current developer state.
    /// </summary>
    [JsonProperty]
    public string DevSupportStateReason { get; internal set; }
}
