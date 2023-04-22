using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

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
    private readonly Dictionary<DnsEndPoint, AddressFamily> addressFamilyCache = new();

    private readonly AddressFamily? forcedAddressFamily;
    private readonly int ipv4WaitMillis;

    /// <summary>
    /// Initializes a new instance of the <see cref="HappyEyeballsCallback"/> class.
    /// </summary>
    /// <param name="forcedAddressFamily">Optional override to force a specific AddressFamily.</param>
    /// <param name="ipv4WaitMillis">Time to wait before initiating the IPv4 request.</param>
    public HappyEyeballsCallback(AddressFamily? forcedAddressFamily = null, int ipv4WaitMillis = 100)
    {
        this.forcedAddressFamily = forcedAddressFamily;
        this.ipv4WaitMillis = ipv4WaitMillis;
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
        var tryV6 = this.AttemptConnection(AddressFamily.InterNetworkV6, context, linkedToken.Token);
        var tryV4 = this.AttemptConnection(AddressFamily.InterNetwork, context, linkedToken.Token, this.ipv4WaitMillis);

        var victor = await Task.WhenAny(tryV6, tryV4).ConfigureAwait(false);
        if (victor.IsCompletedSuccessfully)
        {
            var victorStream = victor.GetAwaiter().GetResult();

            this.addressFamilyCache[context.DnsEndPoint] = victorStream.Socket.AddressFamily;

            return victorStream;
        }

        // The loser can still fail, but we'll wait for it.
        // If it succeeds, cache the result. If not, just throw the exception up the chain.
        var loser = victor == tryV6 ? tryV4 : tryV6;
        var loserStream = loser.GetAwaiter().GetResult();

        this.addressFamilyCache[context.DnsEndPoint] = loserStream.Socket.AddressFamily;

        return loserStream;
    }

    private AddressFamily? GetAddressFamilyOverride(SocketsHttpConnectionContext context)
    {
        if (this.forcedAddressFamily.HasValue)
        {
            return this.forcedAddressFamily.Value;
        }

        if (this.addressFamilyCache.TryGetValue(context.DnsEndPoint, out var cachedValue))
        {
            // TODO: Find some way to delete this after a while. It shouldn't stick around _forever_.
            return cachedValue;
        }

        return null;
    }

    private async Task<NetworkStream> AttemptConnection(
        AddressFamily family, SocketsHttpConnectionContext context, CancellationToken token, int delayMillis = 0)
    {
        if (delayMillis > 0)
        {
            await Task.Delay(delayMillis, token);
            token.ThrowIfCancellationRequested();
        }

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
