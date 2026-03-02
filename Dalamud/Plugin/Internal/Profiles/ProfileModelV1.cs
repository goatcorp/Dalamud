using System.Collections.Generic;

using Newtonsoft.Json;

namespace Dalamud.Plugin.Internal.Profiles;

/// <summary>
/// Version 1 of the profile model.
/// </summary>
public class ProfileModelV1 : ProfileModel
{
    /// <summary>
    /// Enum representing the startup policy of a profile.
    /// </summary>
    public enum ProfileStartupPolicy
    {
        /// <summary>
        /// Remember the last state of the profile.
        /// </summary>
        RememberState,

        /// <summary>
        /// Always enable the profile.
        /// </summary>
        AlwaysEnable,

        /// <summary>
        /// Always disable the profile.
        /// </summary>
        AlwaysDisable,
    }

    /// <summary>
    /// Gets the prefix of this version.
    /// </summary>
    public static string SerializedPrefix => "DP1";

    /// <summary>
    /// Gets or sets the policy to use when Dalamud is loading.
    /// </summary>
    [JsonProperty("p")]
    public ProfileStartupPolicy? StartupPolicy { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this profile should be enabled for specific characters.
    /// </summary>
    [JsonProperty("e4c")]
    public bool EnableForCharacters { get; set; }

    /// <summary>
    /// Gets or sets the list of characters in this profile. Only used for the EnableForCharacters startup policy.
    /// </summary>
    [JsonProperty("pc")]
    public List<ProfileModelV1Character> EnabledCharacters { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether this profile is currently enabled.
    /// </summary>
    [JsonProperty("e")]
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating this profile's color.
    /// </summary>
    [JsonProperty("c")]
    public uint Color { get; set; }

    /// <summary>
    /// Gets or sets the list of plugins in this profile.
    /// </summary>
    public List<ProfileModelV1Plugin> Plugins { get; set; } = [];

    /// <summary>
    /// Class representing a single plugin in a profile.
    /// </summary>
    public class ProfileModelV1Plugin
    {
        /// <summary>
        /// Gets or sets the internal name of the plugin.
        /// </summary>
        public string? InternalName { get; set; }

        /// <summary>
        /// Gets or sets an ID uniquely identifying this specific instance of a plugin.
        /// </summary>
        public Guid WorkingPluginId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this entry is enabled.
        /// </summary>
        public bool IsEnabled { get; set; }
    }

    /// <summary>
    /// Class representing a single character in a profile. Only used for the EnableForCharacters startup policy.
    /// </summary>
    /// <param name="displayName">Display name shown to users.</param>
    /// <param name="contentId">Content ID used to match.</param>
    /// <param name="serverName">Server name of the character.</param>
    public class ProfileModelV1Character(string displayName, ulong contentId, string serverName)
    {
        /// <summary>
        /// Gets or sets the display name of the character. Only used to help users identify which character is which.
        /// </summary>
        [JsonProperty("d")]
        public string DisplayName { get; set; } = displayName;

        /// <summary>
        /// Gets or sets the Content ID of the character. This is used to identify which characters a profile should be enabled for when using the EnableForCharacters startup policy.
        /// </summary>
        [JsonProperty("c")]
        public ulong ContentId { get; set; } = contentId;

        /// <summary>
        /// Gets or sets the server/home/world name of the character.
        /// </summary>
        [JsonProperty("s")]
        public string ServerName { get; set; } = serverName;
    }
}
