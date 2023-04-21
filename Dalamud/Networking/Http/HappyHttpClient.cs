using System;
using System.Net;
using System.Net.Http;
using Dalamud.IoC;

namespace Dalamud.Networking.Http;

/// <summary>
/// A service to help build and manage HttpClients with some semblance of Happy Eyeballs (RFC 8305 - IPv4 fallback)
/// awareness.
/// </summary>
[PluginInterface]
[ServiceManager.BlockingEarlyLoadedService]
public class HappyHttpClient : IDisposable, IServiceType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HappyHttpClient"/> class.
    ///
    /// A service to talk to the Smileton Loporrits to build an HTTP Client aware of Happy Eyeballs.
    /// </summary>
    [ServiceManager.ServiceConstructor]
    private HappyHttpClient()
    {
        this.SharedSocketsHandler = new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
            ConnectCallback = new HappyEyeballsCallback().ConnectCallback,
        };

        this.SharedHttpClient = new HttpClient(this.SharedSocketsHandler, false);
    }

    /// <summary>
    /// Gets a <see cref="HttpClient"/> meant to be shared across all (standard) requests made by the application,
    /// where custom configurations are not required.
    ///
    /// May or may not have been properly tested by the Loporrits.
    /// </summary>
    public HttpClient SharedHttpClient { get; }

    /// <summary>
    /// Gets a <see cref="SocketsHttpHandler"/> meant to be shared across any custom <see cref="HttpClient"/>s that
    /// need to be made in other parts of the application.
    ///
    /// This handler comes pre-loaded with a common <see cref="HappyEyeballsCallback"/> for your convenience.
    /// </summary>
    public SocketsHttpHandler SharedSocketsHandler { get; }

    /// <inheritdoc/>
    void IDisposable.Dispose()
    {
        this.SharedHttpClient.Dispose();
        this.SharedSocketsHandler.Dispose();

        GC.SuppressFinalize(this);
    }
}
