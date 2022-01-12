using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

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
    internal class PluginImageCache : IDisposable
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

        private const string MainRepoImageUrl = "https://raw.githubusercontent.com/goatcorp/DalamudPlugins/api5/{0}/{1}/images/{2}";

        private BlockingCollection<Func<Task>> downloadQueue = new();
        private CancellationTokenSource downloadToken = new();
        private Thread downloadThread;

        private Dictionary<string, TextureWrap?> pluginIconMap = new();
        private Dictionary<string, TextureWrap?[]> pluginImagesMap = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginImageCache"/> class.
        /// </summary>
        public PluginImageCache()
        {
            var dalamud = Service<Dalamud>.Get();
            var interfaceManager = Service<InterfaceManager>.Get();

            this.DefaultIcon = interfaceManager.LoadImage(Path.Combine(dalamud.AssetDirectory.FullName, "UIRes", "defaultIcon.png"))!;
            this.TroubleIcon = interfaceManager.LoadImage(Path.Combine(dalamud.AssetDirectory.FullName, "UIRes", "troubleIcon.png"))!;
            this.UpdateIcon = interfaceManager.LoadImage(Path.Combine(dalamud.AssetDirectory.FullName, "UIRes", "updateIcon.png"))!;
            this.InstalledIcon = interfaceManager.LoadImage(Path.Combine(dalamud.AssetDirectory.FullName, "UIRes", "installedIcon.png"))!;

            this.downloadThread = new Thread(this.DownloadTask);
            this.downloadThread.Start();
        }

        /// <summary>
        /// Gets the default plugin icon.
        /// </summary>
        public TextureWrap DefaultIcon { get; }

        /// <summary>
        /// Gets the plugin trouble icon overlay.
        /// </summary>
        public TextureWrap TroubleIcon { get; }

        /// <summary>
        /// Gets the plugin update icon overlay.
        /// </summary>
        public TextureWrap UpdateIcon { get; }

        /// <summary>
        /// Gets the plugin installed icon overlay.
        /// </summary>
        public TextureWrap InstalledIcon { get; set; }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.DefaultIcon?.Dispose();
            this.TroubleIcon?.Dispose();
            this.UpdateIcon?.Dispose();
            this.InstalledIcon?.Dispose();

            this.downloadToken?.Cancel();

            if (!this.downloadThread.Join(4000))
            {
                Log.Error("Plugin Image Download thread has not cancelled in time.");
            }

            this.downloadToken?.Dispose();
            this.downloadQueue?.CompleteAdding();
            this.downloadQueue?.Dispose();

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

            if (!this.downloadQueue.IsCompleted)
            {
                this.downloadQueue.Add(async () => await this.DownloadPluginIconAsync(plugin, manifest, isThirdParty));
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

            if (!this.downloadQueue.IsCompleted)
            {
                this.downloadQueue.Add(async () => await this.DownloadPluginImagesAsync(plugin, manifest, isThirdParty));
            }

            return false;
        }

        private async void DownloadTask()
        {
            while (!this.downloadToken.Token.IsCancellationRequested)
            {
                try
                {
                    if (!this.downloadQueue.TryTake(out var task, -1, this.downloadToken.Token))
                        return;

                    await task.Invoke();
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
            }

            Log.Debug("Plugin image downloader has shutdown");
        }

        private async Task DownloadPluginIconAsync(LocalPlugin? plugin, PluginManifest manifest, bool isThirdParty)
        {
            var interfaceManager = Service<InterfaceManager>.Get();
            var pluginManager = Service<PluginManager>.Get();

            static bool TryLoadIcon(byte[] bytes, string loc, PluginManifest manifest, InterfaceManager interfaceManager, out TextureWrap icon)
            {
                icon = interfaceManager.LoadImage(bytes);

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
                    if (!TryLoadIcon(bytes, file.FullName, manifest, interfaceManager, out var icon))
                        return;

                    this.pluginIconMap[manifest.InternalName] = icon;
                    Log.Verbose($"Plugin icon for {manifest.InternalName} loaded from disk");

                    return;
                }

                // Dev plugins are likely going to look like a main repo plugin, the InstalledFrom field is going to be null.
                // So instead, set the value manually so we download from the urls specified.
                isThirdParty = true;
            }

            var useTesting = pluginManager.UseTesting(manifest);
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
                if (!TryLoadIcon(bytes, url, manifest, interfaceManager, out var icon))
                    return;

                this.pluginIconMap[manifest.InternalName] = icon;
                Log.Verbose($"Plugin icon for {manifest.InternalName} downloaded");

                return;
            }

            Log.Verbose($"Plugin icon for {manifest.InternalName} is not available");
        }

        private async Task DownloadPluginImagesAsync(LocalPlugin? plugin, PluginManifest manifest, bool isThirdParty)
        {
            var interfaceManager = Service<InterfaceManager>.Get();
            var pluginManager = Service<PluginManager>.Get();

            static bool TryLoadImage(int i, byte[] bytes, string loc, PluginManifest manifest, InterfaceManager interfaceManager, out TextureWrap image)
            {
                image = interfaceManager.LoadImage(bytes);

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

            if (plugin != null && plugin.IsDev)
            {
                var files = this.GetPluginImageFileInfos(plugin);
                if (files != null)
                {
                    var didAny = false;
                    var pluginImages = new TextureWrap[files.Count];
                    for (var i = 0; i < files.Count; i++)
                    {
                        var file = files[i];

                        if (file == null)
                            continue;

                        Log.Verbose($"Loading image{i + 1} for {manifest.InternalName} from {file.FullName}");
                        var bytes = await File.ReadAllBytesAsync(file.FullName);

                        if (!TryLoadImage(i, bytes, file.FullName, manifest, interfaceManager, out var image))
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
                }

                // Dev plugins are likely going to look like a main repo plugin, the InstalledFrom field is going to be null.
                // So instead, set the value manually so we download from the urls specified.
                isThirdParty = true;
            }

            var useTesting = pluginManager.UseTesting(manifest);
            var urls = this.GetPluginImageUrls(manifest, isThirdParty, useTesting);
            if (urls != null)
            {
                var didAny = false;
                var pluginImages = new TextureWrap[urls.Count];
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
                    if (!TryLoadImage(i, bytes, url, manifest, interfaceManager, out var image))
                        continue;

                    Log.Verbose($"Plugin image{i + 1} for {manifest.InternalName} downloaded");
                    pluginImages[i] = image;

                    didAny = true;
                }

                if (didAny)
                {
                    Log.Verbose($"Plugin images for {manifest.InternalName} downloaded");

                    if (pluginImages.Contains(null))
                        pluginImages = pluginImages.Where(image => image != null).ToArray();

                    this.pluginImagesMap[manifest.InternalName] = pluginImages;

                    return;
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
            var pluginDir = plugin.DllFile.Directory;

            var devUrl = new FileInfo(Path.Combine(pluginDir.FullName, "images", "icon.png"));
            if (devUrl.Exists)
                return devUrl;

            return null;
        }

        private List<FileInfo?> GetPluginImageFileInfos(LocalPlugin? plugin)
        {
            var pluginDir = plugin.DllFile.Directory;
            var output = new List<FileInfo>();
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
