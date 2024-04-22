using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Interface.Internal;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Networking.Http;
using Dalamud.Utility;
using Dalamud.Utility.Timing;

using JetBrains.Annotations;

using Serilog;

namespace Dalamud.Storage.Assets;

/// <summary>
/// A concrete class for <see cref="IDalamudAssetManager"/>.
/// </summary>
[PluginInterface]
[ServiceManager.BlockingEarlyLoadedService(
    "Ensuring that it is worth continuing loading Dalamud, by checking if all required assets are properly available.")]
#pragma warning disable SA1015
[ResolveVia<IDalamudAssetManager>]
#pragma warning restore SA1015
internal sealed class DalamudAssetManager : IInternalDisposableService, IDalamudAssetManager
{
    private const int DownloadAttemptCount = 10;
    private const int RenameAttemptCount = 10;

    private readonly object syncRoot = new();
    private readonly DisposeSafety.ScopedFinalizer scopedFinalizer = new();
    private readonly Dictionary<DalamudAsset, Task<FileStream>?> fileStreams;
    private readonly Dictionary<DalamudAsset, Task<IDalamudTextureWrap>?> textureWraps;
    private readonly Dalamud dalamud;
    private readonly HappyHttpClient httpClient;
    private readonly string localSourceDirectory;
    private readonly CancellationTokenSource cancellationTokenSource;

    private bool isDisposed;

    [ServiceManager.ServiceConstructor]
    private DalamudAssetManager(
        Dalamud dalamud,
        HappyHttpClient httpClient,
        ServiceManager.RegisterStartupBlockerDelegate registerStartupBlocker)
    {
        this.dalamud = dalamud;
        this.httpClient = httpClient;
        this.localSourceDirectory = Path.Combine(this.dalamud.AssetDirectory.FullName, "..", "local");
        Directory.CreateDirectory(this.localSourceDirectory);
        this.scopedFinalizer.Add(this.cancellationTokenSource = new());

        this.fileStreams = Enum.GetValues<DalamudAsset>().ToDictionary(x => x, _ => (Task<FileStream>?)null);
        this.textureWraps = Enum.GetValues<DalamudAsset>().ToDictionary(x => x, _ => (Task<IDalamudTextureWrap>?)null);
        
        // Block until all the required assets to be ready.
        var loadTimings = Timings.Start("DAM LoadAll");
        registerStartupBlocker(
            Task.WhenAll(
                    Enum.GetValues<DalamudAsset>()
                        .Where(x => x is not DalamudAsset.Empty4X4)
                        .Where(x => x.GetAttribute<DalamudAssetAttribute>()?.Required is true)
                        .Select(this.CreateStreamAsync)
                        .Select(x => x.ToContentDisposedTask()))
                .ContinueWith(
                    r =>
                    {
                        loadTimings.Dispose();
                        return r;
                    })
                .Unwrap(),
            "Prevent Dalamud from loading more stuff, until we've ensured that all required assets are available.");

        Task.WhenAll(
            Enum.GetValues<DalamudAsset>()
                .Where(x => x is not DalamudAsset.Empty4X4)
                .Where(x => x.GetAttribute<DalamudAssetAttribute>()?.Required is false)
                .Select(this.CreateStreamAsync)
                .Select(x => x.ToContentDisposedTask(true)))
            .ContinueWith(r => Log.Verbose($"Optional assets load state: {r}"));
    }

    /// <inheritdoc/>
    public IDalamudTextureWrap Empty4X4 => this.GetDalamudTextureWrap(DalamudAsset.Empty4X4);

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        lock (this.syncRoot)
        {
            if (this.isDisposed)
                return;

            this.isDisposed = true;
        }

        this.cancellationTokenSource.Cancel();
        Task.WaitAll(
            Array.Empty<Task>()
                 .Concat(this.fileStreams.Values)
                 .Concat(this.textureWraps.Values)
                 .Where(x => x is not null)
                 .Select(x => x.ContinueWith(r => { _ = r.Exception; }))
                 .ToArray());
        this.scopedFinalizer.Dispose();
    }

    /// <inheritdoc/>
    [Pure]
    public bool IsStreamImmediatelyAvailable(DalamudAsset asset) =>
        asset.GetAttribute<DalamudAssetAttribute>()?.Data is not null
        || this.fileStreams[asset]?.IsCompletedSuccessfully is true;

    /// <inheritdoc/>
    [Pure]
    public Stream CreateStream(DalamudAsset asset)
    {
        var s = this.CreateStreamAsync(asset);
        s.Wait();
        if (s.IsCompletedSuccessfully)
            return s.Result;
        if (s.Exception is not null)
            throw new AggregateException(s.Exception.InnerExceptions);
        throw new OperationCanceledException();
    }

    /// <inheritdoc/>
    [Pure]
    public Task<Stream> CreateStreamAsync(DalamudAsset asset)
    {
        if (asset.GetAttribute<DalamudAssetAttribute>() is { Data: { } rawData })
            return Task.FromResult<Stream>(new MemoryStream(rawData, false));

        Task<FileStream> task;
        lock (this.syncRoot)
        {
            if (this.isDisposed)
                throw new ObjectDisposedException(nameof(DalamudAssetManager));

            task = this.fileStreams[asset] ??= CreateInnerAsync();
        }

        return this.TransformImmediate(
            task,
            x => (Stream)new FileStream(
                x.Name,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                4096,
                FileOptions.Asynchronous | FileOptions.SequentialScan));

        async Task<FileStream> CreateInnerAsync()
        {
            string path;
            List<Exception?> exceptions = null;
            foreach (var name in asset.GetAttributes<DalamudAssetPathAttribute>().Select(x => x.FileName))
            {
                if (!File.Exists(path = Path.Combine(this.dalamud.AssetDirectory.FullName, name)))
                    continue;

                try
                {
                    return File.OpenRead(path);
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    exceptions ??= new();
                    exceptions.Add(e);
                }
            }

            if (File.Exists(path = Path.Combine(this.localSourceDirectory, asset.ToString())))
            {
                try
                {
                    return File.OpenRead(path);
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    exceptions ??= new();
                    exceptions.Add(e);
                }
            }

            var tempPath = $"{path}.{Environment.ProcessId:x}.{Environment.CurrentManagedThreadId:x}";
            try
            {
                for (var i = 0; i < DownloadAttemptCount; i++)
                {
                    var attemptedAny = false;
                    foreach (var url in asset.GetAttributes<DalamudAssetOnlineSourceAttribute>())
                    {
                        Log.Information("[{who}] {asset}: Trying {url}", nameof(DalamudAssetManager), asset, url);
                        attemptedAny = true;

                        try
                        {
                            await using (var tempPathStream = File.Open(tempPath, FileMode.Create, FileAccess.Write))
                            {
                                await url.DownloadAsync(
                                    this.httpClient.SharedHttpClient,
                                    tempPathStream,
                                    this.cancellationTokenSource.Token);
                            }

                            for (var j = RenameAttemptCount; ; j--)
                            {
                                try
                                {
                                    File.Move(tempPath, path);
                                }
                                catch (IOException ioe)
                                {
                                    if (j == 0)
                                        throw;
                                    Log.Warning(
                                        ioe,
                                        "[{who}] {asset}: Renaming failed; trying again {n} more times",
                                        nameof(DalamudAssetManager),
                                        asset,
                                        j);
                                    await Task.Delay(1000, this.cancellationTokenSource.Token);
                                    continue;
                                }

                                return File.OpenRead(path);
                            }
                        }
                        catch (Exception e) when (e is not OperationCanceledException)
                        {
                            Log.Error(e, "[{who}] {asset}: Failed {url}", nameof(DalamudAssetManager), asset, url);
                        }
                    }

                    if (!attemptedAny)
                        throw new FileNotFoundException($"Failed to find the asset {asset}.", asset.ToString());

                    // Wait up to 5 minutes
                    var delay = Math.Min(300, (1 << i) * 1000);
                    Log.Error(
                        "[{who}] {asset}: Failed to download. Trying again in {sec} seconds...",
                        nameof(DalamudAssetManager),
                        asset,
                        delay);
                    await Task.Delay(delay * 1000, this.cancellationTokenSource.Token);
                }

                throw new FileNotFoundException($"Failed to load the asset {asset}.", asset.ToString());
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                exceptions ??= new();
                exceptions.Add(e);
                try
                {
                    File.Delete(tempPath);
                }
                catch
                {
                    // don't care
                }
            }

            throw new AggregateException(exceptions);
        }
    }

    /// <inheritdoc/>
    [Pure]
    public IDalamudTextureWrap GetDalamudTextureWrap(DalamudAsset asset) =>
        this.GetDalamudTextureWrapAsync(asset).Result;

    /// <inheritdoc/>
    [Pure]
    [return: NotNullIfNotNull(nameof(defaultWrap))]
    public IDalamudTextureWrap? GetDalamudTextureWrap(DalamudAsset asset, IDalamudTextureWrap? defaultWrap)
    {
        var task = this.GetDalamudTextureWrapAsync(asset);
        return task.IsCompletedSuccessfully ? task.Result : defaultWrap;
    }

    /// <inheritdoc/>
    [Pure]
    public Task<IDalamudTextureWrap> GetDalamudTextureWrapAsync(DalamudAsset asset)
    {
        var purpose = asset.GetPurpose();
        if (purpose is not DalamudAssetPurpose.TextureFromPng and not DalamudAssetPurpose.TextureFromRaw)
            throw new ArgumentOutOfRangeException(nameof(asset), asset, "The asset cannot be taken as a Texture2D.");

        Task<IDalamudTextureWrap> task;
        lock (this.syncRoot)
        {
            if (this.isDisposed)
                throw new ObjectDisposedException(nameof(DalamudAssetManager));

            task = this.textureWraps[asset] ??= CreateInnerAsync();
        }

        return task;

        async Task<IDalamudTextureWrap> CreateInnerAsync()
        {
            var buf = Array.Empty<byte>();
            try
            {
                var im = (await Service<InterfaceManager.InterfaceManagerWithScene>.GetAsync()).Manager;
                await using var stream = await this.CreateStreamAsync(asset);
                var length = checked((int)stream.Length);
                buf = ArrayPool<byte>.Shared.Rent(length);
                stream.ReadExactly(buf, 0, length);
                var image = purpose switch
                {
                    DalamudAssetPurpose.TextureFromPng => im.LoadImage(buf),
                    DalamudAssetPurpose.TextureFromRaw =>
                        asset.GetAttribute<DalamudAssetRawTextureAttribute>() is { } raw
                            ? im.LoadImageFromDxgiFormat(buf, raw.Pitch, raw.Width, raw.Height, raw.Format)
                            : throw new InvalidOperationException(
                                  "TextureFromRaw must accompany a DalamudAssetRawTextureAttribute."),
                    _ => null,
                };
                var disposeDeferred =
                    this.scopedFinalizer.Add(image)
                    ?? throw new InvalidOperationException("Something went wrong very badly");
                return new DisposeSuppressingDalamudTextureWrap(disposeDeferred);
            }
            catch (Exception e)
            {
                Log.Error(e, "[{name}] Failed to load {asset}.", nameof(DalamudAssetManager), asset);
                throw;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buf);
            }
        }
    }

    private Task<TOut> TransformImmediate<TIn, TOut>(Task<TIn> task, Func<TIn, TOut> transformer)
    {
        if (task.IsCompletedSuccessfully)
            return Task.FromResult(transformer(task.Result));
        if (task.Exception is { } exc)
            return Task.FromException<TOut>(exc);
        return task.ContinueWith(_ => this.TransformImmediate(task, transformer)).Unwrap();
    }

    private class DisposeSuppressingDalamudTextureWrap : IDalamudTextureWrap
    {
        private readonly IDalamudTextureWrap innerWrap;

        public DisposeSuppressingDalamudTextureWrap(IDalamudTextureWrap wrap) => this.innerWrap = wrap;

        /// <inheritdoc/>
        public IntPtr ImGuiHandle => this.innerWrap.ImGuiHandle;

        /// <inheritdoc/>
        public int Width => this.innerWrap.Width;

        /// <inheritdoc/>
        public int Height => this.innerWrap.Height;

        /// <inheritdoc/>
        public void Dispose()
        {
            // suppressed
        }
    }
}
