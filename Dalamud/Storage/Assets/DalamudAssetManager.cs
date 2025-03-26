using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Interface.Textures.Internal;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Textures.TextureWraps.Internal;
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

    private readonly DisposeSafety.ScopedFinalizer scopedFinalizer = new();
    private readonly Task<FileStream>?[] fileStreams;
    private readonly Task<IDalamudTextureWrap>?[] textureWraps;
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

        var numDalamudAssetSlots = Enum.GetValues<DalamudAsset>().Max(x => (int)x) + 1;
        this.fileStreams = new Task<FileStream>?[numDalamudAssetSlots];
        this.textureWraps = new Task<IDalamudTextureWrap>?[numDalamudAssetSlots];

        // Block until all the required assets to be ready.
        var loadTimings = Timings.Start("DAM LoadAll");
        registerStartupBlocker(
            Task.WhenAll(
                    Enum.GetValues<DalamudAsset>()
                        .Where(static x => x.GetAssetAttribute() is { Required: true, Data: null })
                        .Select(this.CreateStreamAsync)
                        .Select(static x => x.ToContentDisposedTask()))
                .ContinueWith(
                    r =>
                    {
                        loadTimings.Dispose();
                        return r;
                    })
                .Unwrap(),
            "Prevent Dalamud from loading more stuff, until we've ensured that all required assets are available.");

        // Begin preloading optional(non-required) assets.
        Task.WhenAll(
                Enum.GetValues<DalamudAsset>()
                    .Where(static x => x.GetAssetAttribute() is { Required: false, Data: null })
                    .Select(this.CreateStreamAsync)
                    .Select(static x => x.ToContentDisposedTask(true)))
            .ContinueWith(static r => Log.Verbose($"Optional assets load state: {r}"));
    }

    /// <inheritdoc/>
    public IDalamudTextureWrap Empty4X4 => this.GetDalamudTextureWrap(DalamudAsset.Empty4X4);

    /// <inheritdoc/>
    public IDalamudTextureWrap White4X4 => this.GetDalamudTextureWrap(DalamudAsset.White4X4);

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        if (this.isDisposed)
            return;

        this.isDisposed = true;

        this.cancellationTokenSource.Cancel();
        Task.WaitAll(
            Array.Empty<Task>()
                 .Concat(this.fileStreams)
                 .Concat(this.textureWraps)
                 .Where(static x => x is not null)
                 .Select(static x => x.ContinueWith(static r => _ = r.Exception))
                 .ToArray<Task>());
        this.scopedFinalizer.Dispose();
    }

    /// <inheritdoc/>
    [Pure]
    public bool IsStreamImmediatelyAvailable(DalamudAsset asset) =>
        asset.GetAssetAttribute().Data is not null
        || this.fileStreams[(int)asset]?.IsCompletedSuccessfully is true;

    /// <inheritdoc/>
    [Pure]
    public Stream CreateStream(DalamudAsset asset) => this.CreateStreamAsync(asset).Result;

    /// <inheritdoc/>
    [Pure]
    public Task<Stream> CreateStreamAsync(DalamudAsset asset)
    {
        ObjectDisposedException.ThrowIf(this.isDisposed, this);

        var attribute = asset.GetAssetAttribute();

        // The corresponding asset does not exist.
        if (attribute.Purpose is DalamudAssetPurpose.Empty)
            return Task.FromException<Stream>(new ArgumentOutOfRangeException(nameof(asset), asset, null));

        // Special case: raw data is specified from asset definition.
        if (attribute.Data is not null)
            return Task.FromResult<Stream>(new MemoryStream(attribute.Data, false));

        // Range is guaranteed to be satisfied if the asset has a purpose; get the slot for the stream task.
        ref var streamTaskRef = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(this.fileStreams), (int)asset);

        // The stream task is already set.
        if (streamTaskRef is not null)
            return CloneFileStreamAsync(streamTaskRef);

        var tcs = new TaskCompletionSource<FileStream>();
        if (Interlocked.CompareExchange(ref streamTaskRef, tcs.Task, null) is not { } streamTask)
        {
            // The stream task has just been set. Actually start the operation.
            // In case it did not correctly finish the task in tcs, set the task to a failed state.
            // Do not pass cancellation token here; we always want to touch tcs.
            Task.Run(
                async () =>
                {
                    try
                    {
                        tcs.SetResult(await CreateInnerAsync(this, asset));
                    }
                    catch (Exception e)
                    {
                        tcs.SetException(e);
                    }
                },
                default);
            return CloneFileStreamAsync(tcs.Task);
        }

        // Discard the new task, and return the already created task.
        tcs.SetCanceled();
        return CloneFileStreamAsync(streamTask);

        static async Task<Stream> CloneFileStreamAsync(Task<FileStream> fileStreamTask) =>
            new FileStream(
                (await fileStreamTask).Name,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                4096,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

        static async Task<FileStream> CreateInnerAsync(DalamudAssetManager dam, DalamudAsset asset)
        {
            string path;
            List<Exception?> exceptions = null;
            foreach (var name in asset.GetAttributes<DalamudAssetPathAttribute>().Select(static x => x.FileName))
            {
                if (!File.Exists(path = Path.Combine(dam.dalamud.AssetDirectory.FullName, name)))
                    continue;

                try
                {
                    return File.OpenRead(path);
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    exceptions ??= [];
                    exceptions.Add(e);
                }
            }

            if (File.Exists(path = Path.Combine(dam.localSourceDirectory, asset.ToString())))
            {
                try
                {
                    return File.OpenRead(path);
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    exceptions ??= [];
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
                                    dam.httpClient.SharedHttpClient,
                                    tempPathStream,
                                    dam.cancellationTokenSource.Token);
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
                                    await Task.Delay(1000, dam.cancellationTokenSource.Token);
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
                    await Task.Delay(delay * 1000, dam.cancellationTokenSource.Token);
                }

                throw new FileNotFoundException($"Failed to load the asset {asset}.", asset.ToString());
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                exceptions ??= [];
                exceptions.Add(e);
                try
                {
                    File.Delete(tempPath);
                }
                catch
                {
                    // don't care
                }

                throw new AggregateException(exceptions);
            }
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
        ObjectDisposedException.ThrowIf(this.isDisposed, this);

        // Check if asset is a texture asset.
        if (asset.GetPurpose() is not DalamudAssetPurpose.TextureFromPng and not DalamudAssetPurpose.TextureFromRaw)
        {
            return Task.FromException<IDalamudTextureWrap>(
                new ArgumentOutOfRangeException(
                    nameof(asset),
                    asset,
                    "The asset does not exist or cannot be taken as a Texture2D."));
        }

        // Range is guaranteed to be satisfied if the asset has a purpose; get the slot for the wrap task.
        ref var wrapTaskRef = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(this.textureWraps), (int)asset);

        // The wrap task is already set.
        if (wrapTaskRef is not null)
            return wrapTaskRef;

        var tcs = new TaskCompletionSource<IDalamudTextureWrap>();
        if (Interlocked.CompareExchange(ref wrapTaskRef, tcs.Task, null) is not { } wrapTask)
        {
            // The stream task has just been set. Actually start the operation.
            // In case it did not correctly finish the task in tcs, set the task to a failed state.
            // Do not pass cancellation token here; we always want to touch tcs.
            Task.Run(
                async () =>
                {
                    try
                    {
                        tcs.SetResult(await CreateInnerAsync(this, asset));
                    }
                    catch (Exception e)
                    {
                        tcs.SetException(e);
                    }
                },
                default);
            return tcs.Task;
        }

        // Discard the new task, and return the already created task.
        tcs.SetCanceled();
        return wrapTask;

        static async Task<IDalamudTextureWrap> CreateInnerAsync(DalamudAssetManager dam, DalamudAsset asset)
        {
            var buf = Array.Empty<byte>();
            try
            {
                var tm = await Service<TextureManager>.GetAsync();
                await using var stream = await dam.CreateStreamAsync(asset);
                var length = checked((int)stream.Length);
                buf = ArrayPool<byte>.Shared.Rent(length);
                stream.ReadExactly(buf, 0, length);
                var name = $"{nameof(DalamudAsset)}[{Enum.GetName(asset)}]";
                var texture = asset.GetPurpose() switch
                {
                    DalamudAssetPurpose.TextureFromPng => await tm.CreateFromImageAsync(buf, name),
                    DalamudAssetPurpose.TextureFromRaw =>
                        asset.GetAttribute<DalamudAssetRawTextureAttribute>() is { } raw
                            ? await tm.CreateFromRawAsync(raw.Specification, buf, name)
                            : throw new InvalidOperationException(
                                  "TextureFromRaw must accompany a DalamudAssetRawTextureAttribute."),
                    _ => throw new InvalidOperationException(), // cannot happen
                };
                return new DisposeSuppressingTextureWrap(dam.scopedFinalizer.Add(texture));
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buf);
            }
        }
    }
}
