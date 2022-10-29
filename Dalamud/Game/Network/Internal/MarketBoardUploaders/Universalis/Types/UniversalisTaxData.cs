using Newtonsoft.Json;

namespace Dalamud.Game.Network.Internal.MarketBoardUploaders.Universalis.Types;

/// <summary>
/// A Universalis API structure.
/// </summary>
internal class UniversalisTaxData
{
    /// <summary>
    /// Gets or sets Limsa Lominsa's current tax rate.
    /// </summary>
    [JsonProperty("limsaLominsa")]
    public uint LimsaLominsa { get; set; }

    /// <summary>
    /// Gets or sets Gridania's current tax rate.
    /// </summary>
    [JsonProperty("gridania")]
    public uint Gridania { get; set; }

    /// <summary>
    /// Gets or sets Ul'dah's current tax rate.
    /// </summary>
    [JsonProperty("uldah")]
    public uint Uldah { get; set; }

    /// <summary>
    /// Gets or sets Ishgard's current tax rate.
    /// </summary>
    [JsonProperty("ishgard")]
    public uint Ishgard { get; set; }

    /// <summary>
    /// Gets or sets Kugane's current tax rate.
    /// </summary>
    [JsonProperty("kugane")]
    public uint Kugane { get; set; }

    /// <summary>
    /// Gets or sets The Crystarium's current tax rate.
    /// </summary>
    [JsonProperty("crystarium")]
    public uint Crystarium { get; set; }

    /// <summary>
    /// Gets or sets Old Sharlayan's current tax rate.
    /// </summary>
    [JsonProperty("sharlayan")]
    public uint Sharlayan { get; set; }
}
