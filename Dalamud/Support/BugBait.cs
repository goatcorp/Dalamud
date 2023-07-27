using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Dalamud.Networking.Http;
using Dalamud.Plugin.Internal.Types.Manifest;
using Dalamud.Utility;
using Newtonsoft.Json;

namespace Dalamud.Support;

/// <summary>
/// Class responsible for sending feedback.
/// </summary>
internal static class BugBait
{
    private const string BugBaitUrl = "https://kiko.goats.dev/feedback";

    /// <summary>
    /// Send feedback to Discord.
    /// </summary>
    /// <param name="plugin">The plugin to send feedback about.</param>
    /// <param name="isTesting">Whether or not the plugin is a testing plugin.</param>
    /// <param name="content">The content of the feedback.</param>
    /// <param name="reporter">The reporter name.</param>
    /// <param name="includeException">Whether or not the most recent exception to occur should be included in the report.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public static async Task SendFeedback(IPluginManifest plugin, bool isTesting, string content, string reporter, bool includeException)
    {
        if (content.IsNullOrWhitespace())
            return;

        var model = new FeedbackModel
        {
            Content = content,
            Reporter = reporter,
            Name = plugin.InternalName,
            Version = isTesting ? plugin.TestingAssemblyVersion?.ToString() : plugin.AssemblyVersion.ToString(),
            DalamudHash = Util.GetGitHash(),
        };

        if (includeException)
        {
            model.Exception = Troubleshooting.LastException == null ? "Was included, but none happened" : Troubleshooting.LastException?.ToString();
        }
        
        var httpClient = Service<HappyHttpClient>.Get().SharedHttpClient;

        var postContent = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync(BugBaitUrl, postContent);

        response.EnsureSuccessStatusCode();
    }

    private class FeedbackModel
    {
        [JsonProperty("content")]
        public string? Content { get; set; }

        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("dhash")]
        public string? DalamudHash { get; set; }

        [JsonProperty("version")]
        public string? Version { get; set; }

        [JsonProperty("reporter")]
        public string? Reporter { get; set; }

        [JsonProperty("exception")]
        public string? Exception { get; set; }
    }
}
