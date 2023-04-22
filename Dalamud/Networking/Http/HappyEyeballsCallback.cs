﻿using System;
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
        var race = AsyncUtils.FirstSuccessfulTask(new List<Task<NetworkStream>>
        {
            this.AttemptConnection(AddressFamily.InterNetworkV6, context, linkedToken.Token),
            this.AttemptConnection(AddressFamily.InterNetwork, context, linkedToken.Token, this.ipv4WaitMillis),
        });

        var stream = await race.ConfigureAwait(false);

        // Only cache the address family if a stream was successfully created and returned.
        // Note that this cache is *in addition* to HttpClient keepalives, and really exists to share IPv6 state across
        // multiple HttpClients.
        if (race.IsCompletedSuccessfully)
        {
            this.addressFamilyCache[context.DnsEndPoint] = stream.Socket.AddressFamily;
        }

        return stream;
    }

    private AddressFamily? GetAddressFamilyOverride(SocketsHttpConnectionContext context)
    {
        if (this.forcedAddressFamily.HasValue)
        {
            return this.forcedAddressFamily.Value;
        }

        if (this.addressFamilyCache.TryGetValue(context.DnsEndPoint, out var cachedValue))
        {
            // TODO: Find some way to delete this after a while.
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
