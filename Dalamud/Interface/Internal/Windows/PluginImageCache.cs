using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Game;
using Dalamud.Plugin.Internal;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Utility;
using ImGuiScene;
using Serilog;

namespace Dalamud.Interface.Internal.Windows
{
    /// <summary>
    /// A cache for plugin icons and images.
    /// </summary>
    [ServiceManager.EarlyLoadedService]
    internal class PluginImageCache : IDisposable, IServiceType
    {
        /// <summary>
        /// Maximum number of concurrent image downloads.
        /// </summary>
        public const int MaxConcurrentDownloads = 8;

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

        [ServiceManager.ServiceDependency]
        private readonly Framework framework = Service<Framework>.Get();

        private readonly BlockingCollection<Tuple<ulong, Func<Task>>> downloadQueue = new();
        private readonly BlockingCollection<Action> loadQueue = new();
        private readonly CancellationTokenSource downloadToken = new();
        private readonly Thread downloadThread;

        private readonly Dictionary<string, TextureWrap?> pluginIconMap = new();
        private readonly Dictionary<string, TextureWrap?[]> pluginImagesMap = new();

        private readonly Task<TextureWrap> emptyTextureTask;
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
            var imwst = Service<InterfaceManager.InterfaceManagerWithScene>.GetAsync();

            Task<TextureWrap>? TaskWrapIfNonNull(TextureWrap? tw) => tw == null ? null : Task.FromResult(tw!);

            this.emptyTextureTask = imwst.ContinueWith(task => task.Result.Manager.LoadImageRaw(new byte[64], 8, 8, 4)!);
            this.defaultIconTask = imwst.ContinueWith(task => TaskWrapIfNonNull(task.Result.Manager.LoadImage(Path.Combine(dalamud.AssetDirectory.FullName, "UIRes", "defaultIcon.png"))) ?? this.emptyTextureTask).Unwrap();
            this.troubleIconTask = imwst.ContinueWith(task => TaskWrapIfNonNull(task.Result.Manager.LoadImage(Path.Combine(dalamud.AssetDirectory.FullName, "UIRes", "troubleIcon.png"))) ?? this.emptyTextureTask).Unwrap();
            this.updateIconTask = imwst.ContinueWith(task => TaskWrapIfNonNull(task.Result.Manager.LoadImage(Path.Combine(dalamud.AssetDirectory.FullName, "UIRes", "updateIcon.png"))) ?? this.emptyTextureTask).Unwrap();
            this.installedIconTask = imwst.ContinueWith(task => TaskWrapIfNonNull(task.Result.Manager.LoadImage(Path.Combine(dalamud.AssetDirectory.FullName, "UIRes", "installedIcon.png"))) ?? this.emptyTextureTask).Unwrap();
            this.thirdIconTask = imwst.ContinueWith(task => TaskWrapIfNonNull(task.Result.Manager.LoadImage(Path.Combine(dalamud.AssetDirectory.FullName, "UIRes", "thirdIcon.png"))) ?? this.emptyTextureTask).Unwrap();
            this.thirdInstalledIconTask = imwst.ContinueWith(task => TaskWrapIfNonNull(task.Result.Manager.LoadImage(Path.Combine(dalamud.AssetDirectory.FullName, "UIRes", "thirdInstalledIcon.png"))) ?? this.emptyTextureTask).Unwrap();
            this.corePluginIconTask = imwst.ContinueWith(task => TaskWrapIfNonNull(task.Result.Manager.LoadImage(Path.Combine(dalamud.AssetDirectory.FullName, "UIRes", "tsmLogo.png"))) ?? this.emptyTextureTask).Unwrap();

            this.downloadThread = new Thread(this.DownloadTask);
            this.downloadThread.Start();

            this.framework.Update += this.FrameworkOnUpdate;
        }

        /// <summary>
        /// Gets the fallback empty texture.
        /// </summary>
        public TextureWrap EmptyTexture => this.emptyTextureTask.IsCompleted
                                               ? this.emptyTextureTask.Result
                                               : this.emptyTextureTask.GetAwaiter().GetResult();

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
            this.framework.Update -= this.FrameworkOnUpdate;

            this.DefaultIcon.Dispose();
            this.TroubleIcon.Dispose();
            this.UpdateIcon.Dispose();
            this.InstalledIcon.Dispose();
            this.ThirdIcon.Dispose();
            this.ThirdInstalledIcon.Dispose();
            this.CorePluginIcon.Dispose();

            this.downloadToken.Cancel();

            if (!this.downloadThread.Join(4000))
            {
                Log.Error("Plugin Image Download thread has not cancelled in time");
            }

            this.downloadToken.Dispose();
            this.downloadQueue.CompleteAdding();
            this.downloadQueue.Dispose();

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
            if (this.pluginIconMap.TryGetValue(manifest.InternalName, out iconTexture))
                return true;

            iconTexture = null;
            this.pluginIconMap.Add(manifest.InternalName, iconTexture);

            try
            {
                if (!this.downloadQueue.IsCompleted)
                {
                    this.downloadQueue.Add(
                        Tuple.Create(
                                Service<DalamudInterface>.GetNullable()?.FrameCount ?? 0,
                                () => this.DownloadPluginIconAsync(plugin, manifest, isThirdParty)),
                        this.downloadToken.Token);
                }
            }
            catch (ObjectDisposedException)
            {
                // pass
            }

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
            if (this.pluginImagesMap.TryGetValue(manifest.InternalName, out imageTextures))
                return true;

            imageTextures = Array.Empty<TextureWrap>();
            this.pluginImagesMap.Add(manifest.InternalName, imageTextures);

            try
            {
                if (!this.downloadQueue.IsCompleted)
                {
                    this.downloadQueue.Add(
                        Tuple.Create(
                            Service<DalamudInterface>.GetNullable()?.FrameCount ?? 0,
                            () => this.DownloadPluginIconAsync(plugin, manifest, isThirdParty)),
                        this.downloadToken.Token);
                }
            }
            catch (ObjectDisposedException)
            {
                // pass
            }

            return false;
        }

        private void FrameworkOnUpdate(Framework framework1)
        {
            try
            {
                if (!this.loadQueue.TryTake(out var loadAction, 0, this.downloadToken.Token))
                    return;

                loadAction.Invoke();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An unhandled exception occurred in image loader framework dispatcher");
            }
        }

        private async void DownloadTask()
        {
            var token = this.downloadToken.Token;
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
                        while (runningTasks.Count >= MaxConcurrentDownloads)
                        {
                            await Task.WhenAll(runningTasks);
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

                while (runningTasks.Count >= MaxConcurrentDownloads)
                {
                    await Task.WhenAny(runningTasks);
                    runningTasks.RemoveAll(task => task.IsCompleted);
                }
            }

            Log.Debug("Plugin image downloader has shutdown");
        }

        private async Task DownloadPluginIconAsync(LocalPlugin? plugin, PluginManifest manifest, bool isThirdParty)
        {
            static bool TryLoadIcon(byte[] bytes, string? loc, PluginManifest manifest, InterfaceManager interfaceManager, out TextureWrap? icon)
            {
                // FIXME(goat): This is a hack around this call failing randomly in certain situations. Might be related to not being called on the main thread.
                try
                {
                    icon = interfaceManager.LoadImage(bytes);
                }
                catch (AccessViolationException ex)
                {
                    Log.Error(ex, "Access violation during load plugin icon from {Loc}", loc);

                    icon = null;
                    return false;
                }

                if (icon == null)
                {
                    Log.Error($"Could not load icon for {manifest.InternalName} at {loc}");
                    return false;
                }

                if (icon.Width > PluginIconWidth || icon.Height > PluginIconHeight)
                {
                    Log.Error($"Icon for {manifest.InternalName} at {loc} was larger than the maximum allowed resolution ({PluginIconWidth}x{PluginIconHeight}).");
                    return false;
                }

                if (icon.Height != icon.Width)
                {
                    Log.Error($"Icon for {manifest.InternalName} at {loc} was not square.");
                    return false;
                }

                return true;
            }

            if (plugin != null && plugin.IsDev)
            {
                var file = this.GetPluginIconFileInfo(plugin);
                if (file != null)
                {
                    Log.Verbose($"Fetching icon for {manifest.InternalName} from {file.FullName}");

                    var bytes = await File.ReadAllBytesAsync(file.FullName);
                    if (!TryLoadIcon(bytes, file.FullName, manifest, Service<InterfaceManager.InterfaceManagerWithScene>.Get().Manager, out var icon))
                        return;

                    this.pluginIconMap[manifest.InternalName] = icon;
                    Log.Verbose($"Plugin icon for {manifest.InternalName} loaded from disk");

                    return;
                }

                // Dev plugins are likely going to look like a main repo plugin, the InstalledFrom field is going to be null.
                // So instead, set the value manually so we download from the urls specified.
                isThirdParty = true;
            }

            var useTesting = PluginManager.UseTesting(manifest);
            var url = this.GetPluginIconUrl(manifest, isThirdParty, useTesting);

            if (!url.IsNullOrEmpty())
            {
                Log.Verbose($"Downloading icon for {manifest.InternalName} from {url}");

                HttpResponseMessage data;
                try
                {
                    data = await Util.HttpClient.GetAsync(url);
                }
                catch (InvalidOperationException)
                {
                    Log.Error($"Plugin icon for {manifest.InternalName} has an Invalid URI");
                    return;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"An unexpected error occurred with the icon for {manifest.InternalName}");
                    return;
                }

                if (data.StatusCode == HttpStatusCode.NotFound)
                    return;

                data.EnsureSuccessStatusCode();

                var bytes = await data.Content.ReadAsByteArrayAsync();
                this.loadQueue.Add(() =>
                {
                    if (!TryLoadIcon(bytes, url, manifest, Service<InterfaceManager.InterfaceManagerWithScene>.Get().Manager, out var icon))
                        return;

                    this.pluginIconMap[manifest.InternalName] = icon;
                    Log.Verbose($"Plugin icon for {manifest.InternalName} downloaded");
                });

                return;
            }

            Log.Verbose($"Plugin icon for {manifest.InternalName} is not available");
        }

        private async Task DownloadPluginImagesAsync(LocalPlugin? plugin, PluginManifest manifest, bool isThirdParty)
        {
            static bool TryLoadImage(int i, byte[] bytes, string loc, PluginManifest manifest, InterfaceManager interfaceManager, out TextureWrap? image)
            {
                // FIXME(goat): This is a hack around this call failing randomly in certain situations. Might be related to not being called on the main thread.
                try
                {
                    image = interfaceManager.LoadImage(bytes);
                }
                catch (AccessViolationException ex)
                {
                    Log.Error(ex, "Access violation during load plugin image from {Loc}", loc);

                    image = null;
                    return false;
                }

                if (image == null)
                {
                    Log.Error($"Could not load image{i + 1} for {manifest.InternalName} at {loc}");
                    return false;
                }

                if (image.Width > PluginImageWidth || image.Height > PluginImageHeight)
                {
                    Log.Error($"Plugin image{i + 1} for {manifest.InternalName} at {loc} was larger than the maximum allowed resolution ({PluginImageWidth}x{PluginImageHeight}).");
                    return false;
                }

                return true;
            }

            if (plugin is { IsDev: true })
            {
                var files = this.GetPluginImageFileInfos(plugin);

                var didAny = false;
                var pluginImages = new TextureWrap[files.Count];
                for (var i = 0; i < files.Count; i++)
                {
                    var file = files[i];

                    if (file == null)
                        continue;

                    Log.Verbose($"Loading image{i + 1} for {manifest.InternalName} from {file.FullName}");
                    var bytes = await File.ReadAllBytesAsync(file.FullName);

                    if (!TryLoadImage(i, bytes, file.FullName, manifest, Service<InterfaceManager.InterfaceManagerWithScene>.Get().Manager, out var image) || image == null)
                        continue;

                    Log.Verbose($"Plugin image{i + 1} for {manifest.InternalName} loaded from disk");
                    pluginImages[i] = image;

                    didAny = true;
                }

                if (didAny)
                {
                    Log.Verbose($"Plugin images for {manifest.InternalName} loaded from disk");

                    if (pluginImages.Contains(null))
                        pluginImages = pluginImages.Where(image => image != null).ToArray();

                    this.pluginImagesMap[manifest.InternalName] = pluginImages;

                    return;
                }

                // Dev plugins are likely going to look like a main repo plugin, the InstalledFrom field is going to be null.
                // So instead, set the value manually so we download from the urls specified.
                isThirdParty = true;
            }

            var useTesting = PluginManager.UseTesting(manifest);
            var urls = this.GetPluginImageUrls(manifest, isThirdParty, useTesting);

            if (urls != null)
            {
                var imageBytes = new byte[urls.Count][];

                var didAny = false;

                for (var i = 0; i < urls.Count; i++)
                {
                    var url = urls[i];

                    if (url.IsNullOrEmpty())
                        continue;

                    Log.Verbose($"Downloading image{i + 1} for {manifest.InternalName} from {url}");

                    HttpResponseMessage data;
                    try
                    {
                        data = await Util.HttpClient.GetAsync(url);
                    }
                    catch (InvalidOperationException)
                    {
                        Log.Error($"Plugin image{i + 1} for {manifest.InternalName} has an Invalid URI");
                        continue;
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, $"An unexpected error occurred with image{i + 1} for {manifest.InternalName}");
                        continue;
                    }

                    if (data.StatusCode == HttpStatusCode.NotFound)
                        continue;

                    data.EnsureSuccessStatusCode();

                    var bytes = await data.Content.ReadAsByteArrayAsync();
                    imageBytes[i] = bytes;

                    Log.Verbose($"Plugin image{i + 1} for {manifest.InternalName} downloaded");

                    didAny = true;
                }

                if (didAny)
                {
                    this.loadQueue.Add(() =>
                    {
                        var pluginImages = new TextureWrap[urls.Count];

                        for (var i = 0; i < imageBytes.Length; i++)
                        {
                            var bytes = imageBytes[i];

                            if (!TryLoadImage(i, bytes, "queue", manifest, Service<InterfaceManager.InterfaceManagerWithScene>.Get().Manager, out var image) || image == null)
                                continue;

                            pluginImages[i] = image;
                        }

                        Log.Verbose($"Plugin images for {manifest.InternalName} downloaded");

                        if (pluginImages.Contains(null))
                            pluginImages = pluginImages.Where(image => image != null).ToArray();

                        this.pluginImagesMap[manifest.InternalName] = pluginImages;
                    });
                }
            }

            Log.Verbose($"Images for {manifest.InternalName} are not available");
        }

        private string? GetPluginIconUrl(PluginManifest manifest, bool isThirdParty, bool isTesting)
        {
            if (isThirdParty)
                return manifest.IconUrl;

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
                output.Add(MainRepoImageUrl.Format(isTesting ? "testing" : "plugins", manifest.InternalName, $"image{i}.png"));
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
}
