using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

using Dalamud.Configuration.Internal;
using Dalamud.Networking.Http;
using Dalamud.Utility;

using Newtonsoft.Json;

namespace Dalamud.Support;

/// <summary>
/// Service for fetching Dalamud release information.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal class DalamudReleases : IServiceType
{
    /// <summary>
    /// The default track name.
    /// </summary>
    public const string DefaultTrack = "release";
    
    private const string VersionInfoUrl = "https://kamori.goats.dev/Dalamud/Release/VersionInfo?track={0}";
    private const string VersionMetaUrl = "https://kamori.goats.dev/Dalamud/Release/Meta/";
    
    private readonly HappyHttpClient httpClient;
    private readonly DalamudConfiguration config;

    /// <summary>
    /// Initializes a new instance of the <see cref="DalamudReleases"/> class.
    /// </summary>
    /// <param name="httpClient">The shared HTTP client.</param>
    /// <param name="config">The Dalamud configuration.</param>
    [ServiceManager.ServiceConstructor]
    public DalamudReleases(HappyHttpClient httpClient, DalamudConfiguration config)
    {
        this.httpClient = httpClient;
        this.config = config;
    }
    
    /// <summary>
    /// Get the latest version info for the current track.
    /// </summary>
    /// <returns>The version info for the current track.</returns>
    public async Task<DalamudVersionInfo> GetVersionForCurrentTrack()
    {
        // note: we have to duplicate logic here so that we can resolve the correct track if our key is invalid.
        // we can work around this by fetching *all* tracks, but that won't handle certain server edge cases like track
        // aliases (which shouldn't be returned by the server).
        
        return await this.GetVersionForTrack(await this.GetCurrentTrack());
    }

    /// <summary>
    /// Gets the current effective track, validating the current beta key.
    /// </summary>
    /// <returns>Returns the track name.</returns>
    public async Task<string> GetCurrentTrack()
    {
        var configuredTrack = this.config.DalamudBetaKind;
        if (configuredTrack.IsNullOrEmpty() || configuredTrack == DefaultTrack) return DefaultTrack;

        var trackData = await this.GetVersionForTrack(configuredTrack);
        
        // key is only considered if it's actually set.
        if (!trackData.Key.IsNullOrEmpty() && trackData.Key != this.config.DalamudBetaKey) return DefaultTrack;

        // track name from remote is authoritative for aliasing purposes.
        return trackData.Track;
    }

    /// <summary>
    /// Gets information about the specified track.
    /// </summary>
    /// <param name="track">The track to get info for.</param>
    /// <returns>Returns a DalamudVersionInfo.</returns>
    public async Task<DalamudVersionInfo> GetVersionForTrack(string? track = DefaultTrack)
    {
        var url = string.Format(VersionInfoUrl, [track]);
        var response = await this.httpClient.SharedHttpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<DalamudVersionInfo>(content);
    }

    /// <summary>
    /// Get all known tracks from the server.
    /// </summary>
    /// <returns>Returns a list of known tracks.</returns>
    public async Task<Dictionary<string, DalamudVersionInfo>> GetVersionsForAllTracks()
    {
        var response = await this.httpClient.SharedHttpClient.GetAsync(VersionMetaUrl);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<Dictionary<string, DalamudVersionInfo>>(content);
    }
    
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "laziness")]
    public class DalamudVersionInfo
    {
        [JsonProperty("key")]
        public string? Key { get; set; }

        [JsonProperty("track")]
        public string Track { get; set; } = null!;

        [JsonProperty("assemblyVersion")]
        public string AssemblyVersion { get; set; } = null!;

        [JsonProperty("runtimeVersion")]
        public string RuntimeVersion { get; set; } = null!;

        [JsonProperty("runtimeRequired")]
        public bool RuntimeRequired { get; set; }

        [JsonProperty("supportedGameVer")]
        public string SupportedGameVer { get; set; } = null!;

        [JsonProperty("isApplicableForCurrentGameVer")]
        public bool IsApplicableForCurrentGameVer { get; set; }
    }
}
