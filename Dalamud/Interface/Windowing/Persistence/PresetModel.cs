using System.Collections.Generic;

using Newtonsoft.Json;

namespace Dalamud.Interface.Windowing.Persistence;

/// <summary>
/// Class representing a Window System preset.
/// </summary>
internal class PresetModel
{
    /// <summary>
    /// Gets or sets the ID of this preset.
    /// </summary>
    [JsonProperty("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of this preset.
    /// </summary>
    [JsonProperty("n")]
    public string Name { get; set; } = "New Preset";

    /// <summary>
    /// Gets or sets a dictionary containing the windows in the preset, mapping their ID to the preset.
    /// </summary>
    [JsonProperty("w")]
    public Dictionary<uint, PresetWindow> Windows { get; set; } = new();

    /// <summary>
    /// Class representing a window in a preset.
    /// </summary>
    internal class PresetWindow
    {
        /// <summary>
        /// Gets or sets a value indicating whether the window is pinned.
        /// </summary>
        [JsonProperty("p")]
        public bool IsPinned { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the window is clickthrough.
        /// </summary>
        [JsonProperty("ct")]
        public bool IsClickThrough { get; set; }

        /// <summary>
        /// Gets or sets the window's opacity override.
        /// </summary>
        [JsonProperty("a")]
        public float? Alpha { get; set; }

        /// <summary>
        /// Gets a value indicating whether this preset is in the default state.
        /// </summary>
        [JsonIgnore]
        public bool IsDefault =>
            !this.IsPinned &&
            !this.IsClickThrough &&
            !this.Alpha.HasValue;
    }
}
