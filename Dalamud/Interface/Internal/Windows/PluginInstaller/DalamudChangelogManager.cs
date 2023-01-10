using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Dalamud.Interface.Internal.Windows.PluginInstaller;

/// <summary>
/// Class responsible for managing Dalamud changelogs.
/// </summary>
internal class DalamudChangelogManager : IDisposable
{
    private const string ChangelogUrl = "https://kamori.goats.dev/Plugin/CoreChangelog";

    private readonly HttpClient client = new();

    /// <summary>
    /// Gets a list of all available changelogs.
    /// </summary>
    public IReadOnlyList<DalamudChangelog>? Changelogs { get; private set; }

    /// <summary>
    /// Reload the changelog list.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task ReloadChangelogAsync()
    {
        this.Changelogs = await this.client.GetFromJsonAsync<List<DalamudChangelog>>(ChangelogUrl);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.client.Dispose();
    }
}
