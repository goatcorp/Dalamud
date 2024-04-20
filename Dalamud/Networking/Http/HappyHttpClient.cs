using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

using Dalamud.Plugin.Internal;
using Dalamud.Utility;

namespace Dalamud.Networking.Http;

/// <summary>
/// A service to help build and manage HttpClients with some semblance of Happy Eyeballs (RFC 8305 - IPv4 fallback)
/// awareness.
/// </summary>
[ServiceManager.BlockingEarlyLoadedService($"{nameof(PluginManager)} currently uses this.")]
// ^ TODO: This seems unnecessary, remove the hard dependency at a later time.
//         Otherwise, if PM eventually marks this class as required, note that in the comment above.
internal class HappyHttpClient : IInternalDisposableService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HappyHttpClient"/> class.
    ///
    /// A service to talk to the Smileton Loporrits to build an HTTP Client aware of Happy Eyeballs.
    /// </summary>
    [ServiceManager.ServiceConstructor]
    private HappyHttpClient()
    {
        this.SharedHappyEyeballsCallback = new HappyEyeballsCallback();

        this.SharedHttpClient = new HttpClient(new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
            ConnectCallback = this.SharedHappyEyeballsCallback.ConnectCallback,
        })
        {
            DefaultRequestHeaders =
            {
                UserAgent =
                {
                    new ProductInfoHeaderValue("Dalamud", Util.AssemblyVersion),
                },
            },
        };
    }

    /// <summary>
    /// Gets a <see cref="HttpClient"/> meant to be shared across all (standard) requests made by the application,
    /// where custom configurations are not required.
    ///
    /// May or may not have been properly tested by the Loporrits.
    /// </summary>
    public HttpClient SharedHttpClient { get; }

    /// <summary>
    /// Gets a <see cref="HappyEyeballsCallback"/> meant to be shared across any custom <see cref="HttpClient"/>s that
    /// need to be made in other parts of the application.
    ///
    /// This should be used when shared callback state is desired across multiple clients, as sharing the SocketsHandler
    /// may lead to GC issues.
    /// </summary>
    public HappyEyeballsCallback SharedHappyEyeballsCallback { get; }

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        this.SharedHttpClient.Dispose();
        this.SharedHappyEyeballsCallback.Dispose();

        GC.SuppressFinalize(this);
    }
}
