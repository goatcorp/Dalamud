using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Utility;

namespace Dalamud.Networking.Http;

// Inspired by and adapted from https://github.com/jellyfin/jellyfin/pull/8598

/// <summary>
/// A class to provide a <see cref="SocketsHttpHandler.ConnectCallback"/> method (and tracked state) to implement a
/// variant of the Happy Eyeballs algorithm for HTTP connections to dual-stack servers.
///
/// Each instance of this class tracks its own state.
/// </summary>
public class HappyEyeballsCallback : IDisposable
{
    private readonly ConcurrentDictionary<DnsEndPoint, AddressFamily> addressFamilyCache = new();

    private readonly AddressFamily? forcedAddressFamily;
    private readonly int ipv6GracePeriod;

    /// <summary>
    /// Initializes a new instance of the <see cref="HappyEyeballsCallback"/> class.
    /// </summary>
    /// <param name="forcedAddressFamily">Optional override to force a specific AddressFamily.</param>
    /// <param name="ipv6GracePeriod">Grace period for IPv6 connectivity before starting IPv4 attempt.</param>
    public HappyEyeballsCallback(AddressFamily? forcedAddressFamily = null, int ipv6GracePeriod = 100)
    {
        this.forcedAddressFamily = forcedAddressFamily;
        this.ipv6GracePeriod = ipv6GracePeriod;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.addressFamilyCache.Clear();

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// The connection callback to provide to a <see cref="SocketsHttpHandler"/>.
    /// </summary>
    /// <param name="context">The context for an HTTP connection.</param>
    /// <param name="token">The cancellation token to abort this request.</param>
    /// <returns>Returns a Stream for consumption by HttpClient.</returns>
    public async ValueTask<Stream> ConnectCallback(SocketsHttpConnectionContext context, CancellationToken token)
    {
        var addressFamilyOverride = this.GetAddressFamilyOverride(context);

        if (addressFamilyOverride.HasValue)
        {
            return this.AttemptConnection(addressFamilyOverride.Value, context, token).GetAwaiter().GetResult();
        }

        using var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(token);

        NetworkStream stream;

        // Give IPv6 a chance to connect first.
        // However, only give it ipv4WaitMillis to connect before throwing IPv4 into the mix.
        var tryConnectIPv6 = this.AttemptConnection(AddressFamily.InterNetworkV6, context, linkedToken.Token);
        var timedV6Attempt = Task.WhenAny(tryConnectIPv6, Task.Delay(this.ipv6GracePeriod, linkedToken.Token));

        if (await timedV6Attempt == tryConnectIPv6 && tryConnectIPv6.IsCompletedSuccessfully)
        {
            stream = tryConnectIPv6.GetAwaiter().GetResult();
        }
        else
        {
            var race = AsyncUtils.FirstSuccessfulTask(new List<Task<NetworkStream>>
            {
                tryConnectIPv6,
                this.AttemptConnection(AddressFamily.InterNetwork, context, linkedToken.Token),
            });

            // If our connections all fail, this will explode with an exception.
            stream = race.GetAwaiter().GetResult();
        }

        this.addressFamilyCache[context.DnsEndPoint] = stream.Socket.AddressFamily;
        return stream;
    }

    private AddressFamily? GetAddressFamilyOverride(SocketsHttpConnectionContext context)
    {
        if (this.forcedAddressFamily.HasValue)
        {
            return this.forcedAddressFamily.Value;
        }

        // Force IPv4 if IPv6 support isn't detected to avoid the resolution delay.
        if (!Socket.OSSupportsIPv6)
        {
            return AddressFamily.InterNetwork;
        }

        if (this.addressFamilyCache.TryGetValue(context.DnsEndPoint, out var cachedValue))
        {
            // TODO: Find some way to delete this after a while.
            return cachedValue;
        }

        return null;
    }

    private async Task<NetworkStream> AttemptConnection(
        AddressFamily family, SocketsHttpConnectionContext context, CancellationToken token)
    {
        var socket = new Socket(family, SocketType.Stream, ProtocolType.Tcp)
        {
            NoDelay = true,
        };

        try
        {
            await socket.ConnectAsync(context.DnsEndPoint, token).ConfigureAwait(false);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }
}
