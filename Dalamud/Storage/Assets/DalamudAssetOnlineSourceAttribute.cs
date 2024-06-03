using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Dalamud.Storage.Assets;

/// <summary>
/// Marks that an asset can be download from online.
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
internal class DalamudAssetOnlineSourceAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DalamudAssetOnlineSourceAttribute"/> class.
    /// </summary>
    /// <param name="url">The URL.</param>
    public DalamudAssetOnlineSourceAttribute(string url)
    {
        this.Url = url;
    }

    /// <summary>
    /// Gets the source URL of the file.
    /// </summary>
    public string Url { get; }

    /// <summary>
    /// Downloads to the given stream.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="stream">The stream.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The task.</returns>
    public async Task DownloadAsync(HttpClient client, Stream stream, CancellationToken cancellationToken)
    {
        using var resp = await client.GetAsync(this.Url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        resp.EnsureSuccessStatusCode();
        if (resp.StatusCode != HttpStatusCode.OK)
            throw new NotSupportedException($"Only 200 OK is supported; got {resp.StatusCode}");

        await using var readStream = await resp.Content.ReadAsStreamAsync(cancellationToken);
        await readStream.CopyToAsync(stream, cancellationToken);
        if (resp.Content.Headers.ContentLength is { } length && stream.Length != length)
            throw new IOException($"Expected {length} bytes; got {stream.Length} bytes.");
    }
}
