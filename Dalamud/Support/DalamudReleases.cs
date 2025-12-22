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
    private const string VersionInfoUrl = "https://kamori.goats.dev/Dalamud/Release/VersionInfo?track={0}";

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
    public async Task<DalamudVersionInfo?> GetVersionForCurrentTrack()
    {
        var currentTrack = Versioning.GetActiveTrack();
        if (currentTrack.IsNullOrEmpty())
            return null;

        var url = string.Format(VersionInfoUrl, [currentTrack]);
        var response = await this.httpClient.SharedHttpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<DalamudVersionInfo>(content);
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
