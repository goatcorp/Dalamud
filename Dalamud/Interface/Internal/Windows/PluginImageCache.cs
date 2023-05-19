using System;
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
using Dalamud.Utility;
using ImGuiScene;
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
    private readonly InterfaceManager.InterfaceManagerWithScene imWithScene = Service<InterfaceManager.InterfaceManagerWithScene>.Get();

    [ServiceManager.ServiceDependency]
    private readonly HappyHttpClient happyHttpClient = Service<HappyHttpClient>.Get();

    private readonly BlockingCollection<Tuple<ulong, Func<Task>>> downloadQueue = new();
    private readonly BlockingCollection<Func<Task>> loadQueue = new();
    private readonly CancellationTokenSource cancelToken = new();
    private readonly Task downloadTask;
    private readonly Task loadTask;

    private readonly ConcurrentDictionary<string, TextureWrap?> pluginIconMap = new();
    private readonly ConcurrentDictionary<string, TextureWrap?[]?> pluginImagesMap = new();

    private readonly Task<TextureWrap> emptyTextureTask;
    private readonly Task<TextureWrap> disabledIconTask;
    private readonly Task<TextureWrap> outdatedInstallableIconTask;
    private readonly Task<TextureWrap> defaultIconTask;
    private readonly Task<TextureWrap> troubleIconTask;
    private readonly Task<TextureWrap> updateIconTask;
    private readonly Task<TextureWrap> installedIconTask;
    private readonly Task<TextureWrap> thirdIconTask;
    private readonly Task<TextureWrap> thirdInstalledIconTask;
    private readonly Task<TextureWrap> corePluginIconTask;

    [ServiceManager.ServiceConstructor]
    private PluginImageCache(Dalamud dalamud)
    {
        Task<TextureWrap>? TaskWrapIfNonNull(TextureWrap? tw) => tw == null ? null : Task.FromResult(tw!);
        var imwst = Task.Run(() => this.imWithScene);

        this.emptyTextureTask = imwst.ContinueWith(task => task.Result.Manager.LoadImageRaw(new byte[64], 8, 8, 4)!);
        this.defaultIconTask = imwst.ContinueWith(task => TaskWrapIfNonNull(task.Result.Manager.LoadImage(Path.Combine(dalamud.AssetDirectory.FullName, "UIRes", "defaultIcon.png"))) ?? this.emptyTextureTask).Unwrap();
        this.disabledIconTask = imwst.ContinueWith(task => TaskWrapIfNonNull(task.Result.Manager.LoadImage(Path.Combine(dalamud.AssetDirectory.FullName, "UIRes", "disabledIcon.png"))) ?? this.emptyTextureTask).Unwrap();
        this.outdatedInstallableIconTask = imwst.ContinueWith(task => TaskWrapIfNonNull(task.Result.Manager.LoadImage(Path.Combine(dalamud.AssetDirectory.FullName, "UIRes", "outdatedInstallableIcon.png"))) ?? this.emptyTextureTask).Unwrap();
        this.troubleIconTask = imwst.ContinueWith(task => TaskWrapIfNonNull(task.Result.Manager.LoadImage(Path.Combine(dalamud.AssetDirectory.FullName, "UIRes", "troubleIcon.png"))) ?? this.emptyTextureTask).Unwrap();
        this.updateIconTask = imwst.ContinueWith(task => TaskWrapIfNonNull(task.Result.Manager.LoadImage(Path.Combine(dalamud.AssetDirectory.FullName, "UIRes", "updateIcon.png"))) ?? this.emptyTextureTask).Unwrap();
        this.installedIconTask = imwst.ContinueWith(task => TaskWrapIfNonNull(task.Result.Manager.LoadImage(Path.Combine(dalamud.AssetDirectory.FullName, "UIRes", "installedIcon.png"))) ?? this.emptyTextureTask).Unwrap();
        this.thirdIconTask = imwst.ContinueWith(task => TaskWrapIfNonNull(task.Result.Manager.LoadImage(Path.Combine(dalamud.AssetDirectory.FullName, "UIRes", "thirdIcon.png"))) ?? this.emptyTextureTask).Unwrap();
        this.thirdInstalledIconTask = imwst.ContinueWith(task => TaskWrapIfNonNull(task.Result.Manager.LoadImage(Path.Combine(dalamud.AssetDirectory.FullName, "UIRes", "thirdInstalledIcon.png"))) ?? this.emptyTextureTask).Unwrap();
        this.corePluginIconTask = imwst.ContinueWith(task => TaskWrapIfNonNull(task.Result.Manager.LoadImage(Path.Combine(dalamud.AssetDirectory.FullName, "UIRes", "tsmLogo.png"))) ?? this.emptyTextureTask).Unwrap();

        this.downloadTask = Task.Factory.StartNew(
            () => this.DownloadTask(8), TaskCreationOptions.LongRunning);
        this.loadTask = Task.Factory.StartNew(
            () => this.LoadTask(Environment.ProcessorCount), TaskCreationOptions.LongRunning);
    }

    /// <summary>
    /// Gets the fallback empty texture.
    /// </summary>
    public TextureWrap EmptyTexture => this.emptyTextureTask.IsCompleted
                                           ? this.emptyTextureTask.Result
                                           : this.emptyTextureTask.GetAwaiter().GetResult();

    /// <summary>
    /// Gets the disabled plugin icon.
    /// </summary>
    public TextureWrap DisabledIcon => this.disabledIconTask.IsCompleted
                                           ? this.disabledIconTask.Result
                                           : this.disabledIconTask.GetAwaiter().GetResult();

    /// <summary>
    /// Gets the outdated installable plugin icon.
    /// </summary>
    public TextureWrap OutdatedInstallableIcon => this.outdatedInstallableIconTask.IsCompleted
                                                      ? this.outdatedInstallableIconTask.Result
                                                      : this.outdatedInstallableIconTask.GetAwaiter().GetResult();

    /// <summary>
    /// Gets the default plugin icon.
    /// </summary>
    public TextureWrap DefaultIcon => this.defaultIconTask.IsCompleted
                                          ? this.defaultIconTask.Result
                                          : this.defaultIconTask.GetAwaiter().GetResult();

    /// <summary>
    /// Gets the plugin trouble icon overlay.
    /// </summary>
    public TextureWrap TroubleIcon => this.troubleIconTask.IsCompleted
                                          ? this.troubleIconTask.Result
                                          : this.troubleIconTask.GetAwaiter().GetResult();

    /// <summary>
    /// Gets the plugin update icon overlay.
    /// </summary>
    public TextureWrap UpdateIcon => this.updateIconTask.IsCompleted
                                         ? this.updateIconTask.Result
                                         : this.updateIconTask.GetAwaiter().GetResult();

    /// <summary>
    /// Gets the plugin installed icon overlay.
    /// </summary>
    public TextureWrap InstalledIcon => this.installedIconTask.IsCompleted
                                            ? this.installedIconTask.Result
                                            : this.installedIconTask.GetAwaiter().GetResult();

    /// <summary>
    /// Gets the third party plugin icon overlay.
    /// </summary>
    public TextureWrap ThirdIcon => this.thirdIconTask.IsCompleted
                                        ? this.thirdIconTask.Result
                                        : this.thirdIconTask.GetAwaiter().GetResult();

    /// <summary>
    /// Gets the installed third party plugin icon overlay.
    /// </summary>
    public TextureWrap ThirdInstalledIcon => this.thirdInstalledIconTask.IsCompleted
                                                 ? this.thirdInstalledIconTask.Result
                                                 : this.thirdInstalledIconTask.GetAwaiter().GetResult();

    /// <summary>
    /// Gets the core plugin icon.
    /// </summary>
    public TextureWrap CorePluginIcon => this.corePluginIconTask.IsCompleted
                                             ? this.corePluginIconTask.Result
                                             : this.corePluginIconTask.GetAwaiter().GetResult();

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

        foreach (var task in new[]
                 {
                     this.defaultIconTask,
                     this.troubleIconTask,
                     this.updateIconTask,
                     this.installedIconTask,
                     this.thirdIconTask,
                     this.thirdInstalledIconTask,
                     this.corePluginIconTask,
                 })
        {
            task.Wait();
            if (task.IsCompletedSuccessfully)
                task.Result.Dispose();
        }

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
    public bool TryGetIcon(LocalPlugin? plugin, PluginManifest manifest, bool isThirdParty, out TextureWrap? iconTexture)
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
    public bool TryGetImages(LocalPlugin? plugin, PluginManifest manifest, bool isThirdParty, out TextureWrap?[] imageTextures)
    {
        if (!this.pluginImagesMap.TryAdd(manifest.InternalName, null))
        {
            var found = this.pluginImagesMap[manifest.InternalName];
            imageTextures = found ?? Array.Empty<TextureWrap?>();
            return true;
        }

        var target = new TextureWrap?[5];
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

    private async Task<TextureWrap?> TryLoadImage(
        byte[]? bytes,
        string name,
        string? loc,
        PluginManifest manifest,
        int maxWidth,
        int maxHeight,
        bool requireSquare)
    {
        if (bytes == null)
            return null;

        var interfaceManager = this.imWithScene.Manager;
        var framework = await Service<Framework>.GetAsync();

        TextureWrap? image;
        // FIXME(goat): This is a hack around this call failing randomly in certain situations. Might be related to not being called on the main thread.
        try
        {
            image = interfaceManager.LoadImage(bytes);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Access violation during load plugin {name} from {Loc} (Async Thread)", name, loc);

            try
            {
                image = await framework.RunOnFrameworkThread(() => interfaceManager.LoadImage(bytes));
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

    private async Task<TextureWrap?> DownloadPluginIconAsync(LocalPlugin? plugin, PluginManifest manifest, bool isThirdParty, ulong requestedFrame)
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

    private async Task DownloadPluginImagesAsync(TextureWrap?[] pluginImages, LocalPlugin? plugin, PluginManifest manifest, bool isThirdParty, ulong requestedFrame)
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

    private string? GetPluginIconUrl(PluginManifest manifest, bool isThirdParty, bool isTesting)
    {
        if (isThirdParty)
            return manifest.IconUrl;

        if (manifest.IsDip17Plugin)
            return MainRepoDip17ImageUrl.Format(manifest.Dip17Channel!, manifest.InternalName, "icon.png");

        return MainRepoImageUrl.Format(isTesting ? "testing" : "plugins", manifest.InternalName, "icon.png");
    }

    private List<string?>? GetPluginImageUrls(PluginManifest manifest, bool isThirdParty, bool isTesting)
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
            if (manifest.IsDip17Plugin)
            {
                output.Add(MainRepoDip17ImageUrl.Format(manifest.Dip17Channel!, manifest.InternalName, $"image{i}.png"));
            }
            else
            {
                output.Add(MainRepoImageUrl.Format(isTesting ? "testing" : "plugins", manifest.InternalName, $"image{i}.png"));
            }
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
