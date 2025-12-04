using System.Collections.Generic;
using System.Collections.Specialized;
using System.Web;

namespace Dalamud.Networking.Rpc.Model;

/// <summary>
/// A Dalamud Uri, in the format:
/// <code>dalamud://{NAMESPACE}/{ARBITRARY}</code>
/// </summary>
public record DalamudUri
{
    private readonly Uri rawUri;

    private DalamudUri(Uri uri)
    {
        if (uri.Scheme != "dalamud")
        {
            throw new ArgumentOutOfRangeException(nameof(uri), "URI must be of scheme dalamud.");
        }

        this.rawUri = uri;
    }

    /// <summary>
    /// Gets the namespace that this URI should be routed to. Generally a high level component like "PluginInstaller".
    /// </summary>
    public string Namespace => this.rawUri.Authority;

    /// <summary>
    /// Gets the raw (untargeted) path and query params for this URI.
    /// </summary>
    public string Data =>
        this.rawUri.GetComponents(UriComponents.PathAndQuery | UriComponents.Fragment, UriFormat.UriEscaped);

    /// <summary>
    /// Gets the raw (untargeted) path for this URI.
    /// </summary>
    public string Path => this.rawUri.AbsolutePath;

    /// <summary>
    /// Gets a list of segments based on the provided Data element.
    /// </summary>
    public string[] Segments => this.GetDataSegments();

    /// <summary>
    /// Gets the raw query parameters for this URI, if any.
    /// </summary>
    public string Query => this.rawUri.Query;

    /// <summary>
    /// Gets the query params (as a parsed NameValueCollection) in this URI.
    /// </summary>
    public NameValueCollection QueryParams => HttpUtility.ParseQueryString(this.Query);

    /// <summary>
    /// Gets the fragment (if one is specified) in this URI.
    /// </summary>
    public string Fragment => this.rawUri.Fragment;

    /// <inheritdoc/>
    public override string ToString() => this.rawUri.ToString();

    /// <summary>
    /// Build a DalamudURI from a given URI.
    /// </summary>
    /// <param name="uri">The URI to convert to a Dalamud URI.</param>
    /// <returns>Returns a DalamudUri.</returns>
    public static DalamudUri FromUri(Uri uri)
    {
        return new DalamudUri(uri);
    }

    /// <summary>
    /// Build a DalamudURI from a URI in string format.
    /// </summary>
    /// <param name="uri">The URI to convert to a Dalamud URI.</param>
    /// <returns>Returns a DalamudUri.</returns>
    public static DalamudUri FromUri(string uri) => FromUri(new Uri(uri));

    private string[] GetDataSegments()
    {
        // reimplementation of the System.URI#Segments, under MIT license.
        var path = this.Path;

        var segments = new List<string>();
        var current = 0;
        while (current < path.Length)
        {
            var next = path.IndexOf('/', current);
            if (next == -1)
            {
                next = path.Length - 1;
            }

            segments.Add(path.Substring(current, (next - current) + 1));
            current = next + 1;
        }

        return segments.ToArray();
    }
}
