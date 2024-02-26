using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Game;
using Dalamud.Networking.Http;
using Dalamud.Plugin.Internal;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Plugin.Internal.Types.Manifest;
using Dalamud.Storage.Assets;
using Dalamud.Utility;

using Serilog;

namespace Dalamud.Interface.Internal.Windows;

/// <summary>
/// A cache for plugin icons and images.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal class PluginImageCache : IDisposable, IServiceType
{
    /// <summary>
    /// Maximum plugin image width.
    /// </summary>
    public const int PluginImageWidth = 730;

    /// <summary>
    /// Maximum plugin image height.
    /// </summary>
    public const int PluginImageHeight = 380;

    /// <summary>
    /// Maximum plugin icon width.
    /// </summary>
    public const int PluginIconWidth = 512;

    /// <summary>
    /// Maximum plugin height.
    /// </summary>
    public const int PluginIconHeight = 512;

    private const string MainRepoImageUrl = "https://raw.githubusercontent.com/goatcorp/DalamudPlugins/api6/{0}/{1}/images/{2}";
    private const string MainRepoDip17ImageUrl = "https://raw.githubusercontent.com/goatcorp/PluginDistD17/main/{0}/{1}/images/{2}";

    [ServiceManager.ServiceDependency]
    private readonly HappyHttpClient happyHttpClient = Service<HappyHttpClient>.Get();

    private readonly BlockingCollection<Tuple<ulong, Func<Task>>> downloadQueue = new();
    private readonly BlockingCollection<Func<Task>> loadQueue = new();
    private readonly CancellationTokenSource cancelToken = new();
    private readonly Task downloadTask;
    private readonly Task loadTask;

    private readonly ConcurrentDictionary<string, IDalamudTextureWrap?> pluginIconMap = new();
    private readonly ConcurrentDictionary<string, IDalamudTextureWrap?[]?> pluginImagesMap = new();
    private readonly DalamudAssetManager dalamudAssetManager;

    [ServiceManager.ServiceConstructor]
    private PluginImageCache(Dalamud dalamud, DalamudAssetManager dalamudAssetManager)
    {
        this.dalamudAssetManager = dalamudAssetManager;
        this.downloadTask = Task.Factory.StartNew(
            () => this.DownloadTask(8), TaskCreationOptions.LongRunning);
        this.loadTask = Task.Factory.StartNew(
            () => this.LoadTask(Environment.ProcessorCount), TaskCreationOptions.LongRunning);
    }

    /// <summary>
    /// Gets the fallback empty texture.
    /// </summary>
    public IDalamudTextureWrap EmptyTexture =>
        this.dalamudAssetManager.GetDalamudTextureWrap(DalamudAsset.Empty4X4);

    /// <summary>
    /// Gets the disabled plugin icon.
    /// </summary>
    public IDalamudTextureWrap DisabledIcon =>
        this.dalamudAssetManager.GetDalamudTextureWrap(DalamudAsset.DisabledIcon, this.EmptyTexture);

    /// <summary>
    /// Gets the outdated installable plugin icon.
    /// </summary>
    public IDalamudTextureWrap OutdatedInstallableIcon =>
        this.dalamudAssetManager.GetDalamudTextureWrap(DalamudAsset.OutdatedInstallableIcon, this.EmptyTexture);

    /// <summary>
    /// Gets the default plugin icon.
    /// </summary>
    public IDalamudTextureWrap DefaultIcon =>
        this.dalamudAssetManager.GetDalamudTextureWrap(DalamudAsset.DefaultIcon, this.EmptyTexture);

    /// <summary>
    /// Gets the plugin trouble icon overlay.
    /// </summary>
    public IDalamudTextureWrap TroubleIcon =>
        this.dalamudAssetManager.GetDalamudTextureWrap(DalamudAsset.TroubleIcon, this.EmptyTexture);
    
    /// <summary>
    /// Gets the devPlugin icon overlay.
    /// </summary>
    public IDalamudTextureWrap DevPluginIcon =>
        this.dalamudAssetManager.GetDalamudTextureWrap(DalamudAsset.DevPluginIcon, this.EmptyTexture);

    /// <summary>
    /// Gets the plugin update icon overlay.
    /// </summary>
    public IDalamudTextureWrap UpdateIcon =>
        this.dalamudAssetManager.GetDalamudTextureWrap(DalamudAsset.UpdateIcon, this.EmptyTexture);

    /// <summary>
    /// Gets the plugin installed icon overlay.
    /// </summary>
    public IDalamudTextureWrap InstalledIcon =>
        this.dalamudAssetManager.GetDalamudTextureWrap(DalamudAsset.InstalledIcon, this.EmptyTexture);

    /// <summary>
    /// Gets the third party plugin icon overlay.
    /// </summary>
    public IDalamudTextureWrap ThirdIcon =>
        this.dalamudAssetManager.GetDalamudTextureWrap(DalamudAsset.ThirdIcon, this.EmptyTexture);

    /// <summary>
    /// Gets the installed third party plugin icon overlay.
    /// </summary>
    public IDalamudTextureWrap ThirdInstalledIcon =>
        this.dalamudAssetManager.GetDalamudTextureWrap(DalamudAsset.ThirdInstalledIcon, this.EmptyTexture);

    /// <summary>
    /// Gets the core plugin icon.
    /// </summary>
    public IDalamudTextureWrap CorePluginIcon =>
        this.dalamudAssetManager.GetDalamudTextureWrap(DalamudAsset.LogoSmall, this.EmptyTexture);

    /// <inheritdoc/>
    public void Dispose()
    {
        this.cancelToken.Cancel();
        this.downloadQueue.CompleteAdding();
        this.loadQueue.CompleteAdding();

        if (!Task.WaitAll(new[] { this.loadTask, this.downloadTask }, 4000))
        {
            Log.Error("Plugin Image download/load thread has not cancelled in time");
        }

        this.cancelToken.Dispose();
        this.downloadQueue.Dispose();
        this.loadQueue.Dispose();

        foreach (var icon in this.pluginIconMap.Values)
        {
            icon?.Dispose();
        }

        foreach (var images in this.pluginImagesMap.Values)
        {
            foreach (var image in images)
            {
                image?.Dispose();
            }
        }

        this.pluginIconMap.Clear();
        this.pluginImagesMap.Clear();
    }

    /// <summary>
    /// Clear the cache of downloaded icons.
    /// </summary>
    public void ClearIconCache()
    {
        this.pluginIconMap.Clear();
        this.pluginImagesMap.Clear();
    }

    /// <summary>
    /// Try to get the icon associated with the internal name of a plugin.
    /// Uses the name within the manifest to search.
    /// </summary>
    /// <param name="plugin">The installed plugin, if available.</param>
    /// <param name="manifest">The plugin manifest.</param>
    /// <param name="isThirdParty">If the plugin was third party sourced.</param>
    /// <param name="iconTexture">Cached image textures, or an empty array.</param>
    /// <returns>True if an entry exists, may be null if currently downloading.</returns>
    public bool TryGetIcon(LocalPlugin? plugin, IPluginManifest manifest, bool isThirdParty, out IDalamudTextureWrap? iconTexture)
    {
        iconTexture = null;

        if (manifest == null || manifest.InternalName == null)
        {
            Log.Error("THIS SHOULD NEVER HAPPEN! manifest == null || manifest.InternalName == null");
            return false;
        }

        if (!this.pluginIconMap.TryAdd(manifest.InternalName, null))
        {
            iconTexture = this.pluginIconMap[manifest.InternalName];
            return true;
        }

        var requestedFrame = Service<DalamudInterface>.GetNullable()?.FrameCount ?? 0;
        Task.Run(async () =>
        {
            try
            {
                this.pluginIconMap[manifest.InternalName] =
                    await this.DownloadPluginIconAsync(plugin, manifest, isThirdParty, requestedFrame);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"An unexpected error occurred with the icon for {manifest.InternalName}");
            }
        });

        return false;
    }

    /// <summary>
    /// Try to get any images associated with the internal name of a plugin.
    /// Uses the name within the manifest to search.
    /// </summary>
    /// <param name="plugin">The installed plugin, if available.</param>
    /// <param name="manifest">The plugin manifest.</param>
    /// <param name="isThirdParty">If the plugin was third party sourced.</param>
    /// <param name="imageTextures">Cached image textures, or an empty array.</param>
    /// <returns>True if the image array exists, may be empty if currently downloading.</returns>
    public bool TryGetImages(LocalPlugin? plugin, IPluginManifest manifest, bool isThirdParty, out IDalamudTextureWrap?[] imageTextures)
    {
        if (!this.pluginImagesMap.TryAdd(manifest.InternalName, null))
        {
            var found = this.pluginImagesMap[manifest.InternalName];
            imageTextures = found ?? Array.Empty<IDalamudTextureWrap?>();
            return true;
        }

        var target = new IDalamudTextureWrap?[5];
        this.pluginImagesMap[manifest.InternalName] = target;
        imageTextures = target;

        var requestedFrame = Service<DalamudInterface>.GetNullable()?.FrameCount ?? 0;
        Task.Run(async () =>
        {
            try
            {
                await this.DownloadPluginImagesAsync(target, plugin, manifest, isThirdParty, requestedFrame);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"An unexpected error occurred with the images for {manifest.InternalName}");
            }
        });

        return false;
    }

    private async Task<IDalamudTextureWrap?> TryLoadImage(
        byte[]? bytes,
        string name,
        string? loc,
        IPluginManifest manifest,
        int maxWidth,
        int maxHeight,
        bool requireSquare)
    {
        if (bytes == null)
            return null;

        var interfaceManager = (await Service<InterfaceManager.InterfaceManagerWithScene>.GetAsync()).Manager;
        var framework = await Service<Framework>.GetAsync();

        IDalamudTextureWrap? image;
        // FIXME(goat): This is a hack around this call failing randomly in certain situations. Might be related to not being called on the main thread.
        try
        {
            image = interfaceManager.CreateTexture2DFromBytes(bytes, name);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Access violation during load plugin {name} from {Loc} (Async Thread)", name, loc);

            try
            {
                image = await framework.RunOnFrameworkThread(
                            () => interfaceManager.CreateTexture2DFromBytes(bytes, name));
            }
            catch (Exception ex2)
            {
                Log.Error(ex2, "Access violation during load plugin {name} from {Loc} (Framework Thread)", name, loc);
                return null;
            }
        }

        if (image == null)
        {
            Log.Error($"Could not load {name} for {manifest.InternalName} at {loc}");
            return null;
        }

        if (image.Width > maxWidth || image.Height > maxHeight)
        {
            Log.Error($"Plugin {name} for {manifest.InternalName} at {loc} was larger than the maximum allowed resolution ({image.Width}x{image.Height} > {maxWidth}x{maxHeight}).");
            image.Dispose();
            return null;
        }

        if (requireSquare && image.Height != image.Width)
        {
            Log.Error($"Plugin {name} for {manifest.InternalName} at {loc} was not square.");
            image.Dispose();
            return null;
        }

        return image!;
    }

    private Task<T> RunInDownloadQueue<T>(Func<Task<T>> func, ulong requestedFrame)
    {
        var tcs = new TaskCompletionSource<T>();
        this.downloadQueue.Add(Tuple.Create(requestedFrame, async () =>
        {
            try
            {
                tcs.SetResult(await func());
            }
            catch (Exception e)
            {
                tcs.SetException(e);
            }
        }));
        return tcs.Task;
    }

    private Task<T> RunInLoadQueue<T>(Func<Task<T>> func)
    {
        var tcs = new TaskCompletionSource<T>();
        this.loadQueue.Add(async () =>
        {
            try
            {
                tcs.SetResult(await func());
            }
            catch (Exception e)
            {
                tcs.SetException(e);
            }
        });
        return tcs.Task;
    }

    private async Task DownloadTask(int concurrency)
    {
        var token = this.cancelToken.Token;
        var runningTasks = new List<Task>();
        var pendingFuncs = new List<Tuple<ulong, Func<Task>>>();
        while (true)
        {
            try
            {
                token.ThrowIfCancellationRequested();
                if (!pendingFuncs.Any())
                {
                    if (!this.downloadQueue.TryTake(out var taskTuple, -1, token))
                        return;

                    pendingFuncs.Add(taskTuple);
                }

                token.ThrowIfCancellationRequested();
                while (this.downloadQueue.TryTake(out var taskTuple, 0, token))
                    pendingFuncs.Add(taskTuple);

                // Process most recently requested items first in terms of frame index.
                pendingFuncs = pendingFuncs.OrderBy(x => x.Item1).ToList();

                var item1 = pendingFuncs.Last().Item1;
                while (pendingFuncs.Any() && pendingFuncs.Last().Item1 == item1)
                {
                    token.ThrowIfCancellationRequested();
                    while (runningTasks.Count >= concurrency)
                    {
                        await Task.WhenAny(runningTasks);
                        runningTasks.RemoveAll(task => task.IsCompleted);
                    }

                    token.ThrowIfCancellationRequested();
                    runningTasks.Add(Task.Run(pendingFuncs.Last().Item2, token));
                    pendingFuncs.RemoveAt(pendingFuncs.Count - 1);
                }
            }
            catch (OperationCanceledException)
            {
                // Shutdown signal.
                break;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An unhandled exception occurred in the plugin image downloader");
            }

            while (runningTasks.Count >= concurrency)
            {
                await Task.WhenAny(runningTasks);
                runningTasks.RemoveAll(task => task.IsCompleted);
            }
        }

        await Task.WhenAll(runningTasks);
        Log.Debug("Plugin image downloader has shutdown");
    }

    private async Task LoadTask(int concurrency)
    {
        var token = this.cancelToken.Token;
        var runningTasks = new List<Task>();
        while (true)
        {
            try
            {
                token.ThrowIfCancellationRequested();
                while (runningTasks.Count >= concurrency)
                {
                    await Task.WhenAny(runningTasks);
                    runningTasks.RemoveAll(task => task.IsCompleted);
                }

                if (!this.loadQueue.TryTake(out var func, -1, token))
                    return;
                runningTasks.Add(Task.Run(func, token));
            }
            catch (OperationCanceledException)
            {
                // Shutdown signal.
                break;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An unhandled exception occurred in the plugin image loader");
            }
        }

        await Task.WhenAll(runningTasks);
        Log.Debug("Plugin image loader has shutdown");
    }

    private async Task<IDalamudTextureWrap?> DownloadPluginIconAsync(LocalPlugin? plugin, IPluginManifest manifest, bool isThirdParty, ulong requestedFrame)
    {
        if (plugin is { IsDev: true })
        {
            var file = this.GetPluginIconFileInfo(plugin);
            if (file != null)
            {
                Log.Verbose($"Fetching icon for {manifest.InternalName} from {file.FullName}");

                var fileBytes = await this.RunInDownloadQueue(
                                    () => File.ReadAllBytesAsync(file.FullName),
                                    requestedFrame);
                var fileIcon = await this.RunInLoadQueue(
                                   () => this.TryLoadImage(
                                       fileBytes,
                                       "icon",
                                       file.FullName,
                                       manifest,
                                       PluginIconWidth,
                                       PluginIconHeight,
                                       true));
                if (fileIcon != null)
                {
                    Log.Verbose($"Plugin icon for {manifest.InternalName} loaded from disk");
                    return fileIcon;
                }
            }

            // Dev plugins are likely going to look like a main repo plugin, the InstalledFrom field is going to be null.
            // So instead, set the value manually so we download from the urls specified.
            isThirdParty = true;
        }

        var useTesting = Service<PluginManager>.Get().UseTesting(manifest);
        var url = this.GetPluginIconUrl(manifest, isThirdParty, useTesting);

        if (url.IsNullOrEmpty())
        {
            Log.Verbose($"Plugin icon for {manifest.InternalName} is not available");
            return null;
        }

        Log.Verbose($"Downloading icon for {manifest.InternalName} from {url}");

        // ReSharper disable once RedundantTypeArgumentsOfMethod
        var bytes = await this.RunInDownloadQueue<byte[]?>(
                        async () =>
                        {
                            var data = await this.happyHttpClient.SharedHttpClient.GetAsync(url);
                            if (data.StatusCode == HttpStatusCode.NotFound)
                                return null;

                            data.EnsureSuccessStatusCode();
                            return await data.Content.ReadAsByteArrayAsync();
                        },
                        requestedFrame);

        if (bytes == null)
            return null;

        var icon = await this.RunInLoadQueue(
                       () => this.TryLoadImage(bytes, "icon", url, manifest, PluginIconWidth, PluginIconHeight, true));
        if (icon != null)
            Log.Verbose($"Plugin icon for {manifest.InternalName} loaded");
        return icon;
    }

    private async Task DownloadPluginImagesAsync(IDalamudTextureWrap?[] pluginImages, LocalPlugin? plugin, IPluginManifest manifest, bool isThirdParty, ulong requestedFrame)
    {
        if (plugin is { IsDev: true })
        {
            var fileTasks = new List<Task>();
            var files = this.GetPluginImageFileInfos(plugin)
                            .Where(x => x is { Exists: true })
                            .Select(x => (FileInfo)x!)
                            .ToList();
            for (var i = 0; i < files.Count && i < pluginImages.Length; i++)
            {
                var file = files[i];
                var i2 = i;
                fileTasks.Add(Task.Run(async () =>
                {
                    var bytes = await this.RunInDownloadQueue(
                                    () => File.ReadAllBytesAsync(file.FullName),
                                    requestedFrame);
                    var image = await this.RunInLoadQueue(
                                    () => this.TryLoadImage(
                                        bytes,
                                        $"image{i2 + 1}",
                                        file.FullName,
                                        manifest,
                                        PluginImageWidth,
                                        PluginImageHeight,
                                        false));
                    if (image == null)
                        return;

                    Log.Verbose($"Plugin image{i2 + 1} for {manifest.InternalName} loaded from disk");
                    pluginImages[i2] = image;
                }));
            }

            try
            {
                await Task.WhenAll(fileTasks);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to load at least one plugin image from filesystem");
            }

            if (pluginImages.Any(x => x != null))
                return;

            // Dev plugins are likely going to look like a main repo plugin, the InstalledFrom field is going to be null.
            // So instead, set the value manually so we download from the urls specified.
            isThirdParty = true;
        }

        var useTesting = Service<PluginManager>.Get().UseTesting(manifest);
        var urls = this.GetPluginImageUrls(manifest, isThirdParty, useTesting);
        urls = urls?.Where(x => !string.IsNullOrEmpty(x)).ToList();
        if (urls?.Any() != true)
        {
            Log.Verbose($"Images for {manifest.InternalName} are not available");
            return;
        }

        var tasks = new List<Task>();
        for (var i = 0; i < urls.Count && i < pluginImages.Length; i++)
        {
            var i2 = i;
            var url = urls[i];
            tasks.Add(Task.Run(async () =>
            {
                Log.Verbose($"Downloading image{i2 + 1} for {manifest.InternalName} from {url}");
                // ReSharper disable once RedundantTypeArgumentsOfMethod
                var bytes = await this.RunInDownloadQueue<byte[]?>(
                                async () =>
                                {
                                    var httpClient = Service<HappyHttpClient>.Get().SharedHttpClient;

                                    var data = await httpClient.GetAsync(url);
                                    if (data.StatusCode == HttpStatusCode.NotFound)
                                        return null;

                                    data.EnsureSuccessStatusCode();
                                    return await data.Content.ReadAsByteArrayAsync();
                                },
                                requestedFrame);

                if (bytes == null)
                    return;

                var image = await this.TryLoadImage(
                                bytes,
                                $"image{i2 + 1}",
                                "queue",
                                manifest,
                                PluginImageWidth,
                                PluginImageHeight,
                                false);
                if (image == null)
                    return;

                Log.Verbose($"Image{i2 + 1} for {manifest.InternalName} loaded");
                pluginImages[i2] = image;
            }));
        }

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load at least one plugin image from network.");
        }
    }

    private string? GetPluginIconUrl(IPluginManifest manifest, bool isThirdParty, bool isTesting)
    {
        if (isThirdParty)
            return manifest.IconUrl;

        return MainRepoDip17ImageUrl.Format(manifest.Dip17Channel!, manifest.InternalName, "icon.png");
    }

    private List<string?>? GetPluginImageUrls(IPluginManifest manifest, bool isThirdParty, bool isTesting)
    {
        if (isThirdParty)
        {
            if (manifest.ImageUrls?.Count > 5)
            {
                Log.Warning($"Plugin {manifest.InternalName} has too many images");
                return manifest.ImageUrls.Take(5).ToList();
            }

            return manifest.ImageUrls;
        }

        var output = new List<string>();
        for (var i = 1; i <= 5; i++)
        {
            output.Add(MainRepoDip17ImageUrl.Format(manifest.Dip17Channel!, manifest.InternalName, $"image{i}.png"));
        }

        return output;
    }

    private FileInfo? GetPluginIconFileInfo(LocalPlugin? plugin)
    {
        var pluginDir = plugin?.DllFile.Directory;
        if (pluginDir == null)
            return null;

        var devUrl = new FileInfo(Path.Combine(pluginDir.FullName, "images", "icon.png"));
        if (devUrl.Exists)
            return devUrl;

        return null;
    }

    private List<FileInfo?> GetPluginImageFileInfos(LocalPlugin? plugin)
    {
        var output = new List<FileInfo>();

        var pluginDir = plugin?.DllFile.Directory;
        if (pluginDir == null)
            return output;

        for (var i = 1; i <= 5; i++)
        {
            var devUrl = new FileInfo(Path.Combine(pluginDir.FullName, "images", $"image{i}.png"));
            if (devUrl.Exists)
            {
                output.Add(devUrl);
                continue;
            }

            output.Add(null);
        }

        return output;
    }
}
