using Newtonsoft.Json;

namespace Dalamud.Configuration.Internal;

/// <summary>
/// Represents a per-character style assignment.
/// </summary>
internal class CharacterStyleAssignment
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CharacterStyleAssignment"/> class.
    /// </summary>
    /// <param name="displayName">Display name of the character.</param>
    /// <param name="contentId">Content ID used to match the character on login.</param>
    /// <param name="serverName">Home world name of the character.</param>
    public CharacterStyleAssignment(string displayName, ulong contentId, string serverName)
    {
        this.DisplayName = displayName;
        this.ContentId = contentId;
        this.ServerName = serverName;
    }

    /// <summary>
    /// Gets or sets the display name of the character. Used to identify the character in the UI.
    /// </summary>
    [JsonProperty("d")]
    public string DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the Content ID of the character. Used to match the character on login.
    /// </summary>
    [JsonProperty("c")]
    public ulong ContentId { get; set; }

    /// <summary>
    /// Gets or sets the home world name of the character. Displayed alongside the character name in the UI.
    /// </summary>
    [JsonProperty("s")]
    public string ServerName { get; set; }

    /// <summary>
    /// Gets or sets the name of the style to apply when this character logs in. Null means no choice was made
    /// and we use the last selected one.
    /// </summary>
    [JsonProperty("style")]
    public string? StyleName { get; set; }
}
