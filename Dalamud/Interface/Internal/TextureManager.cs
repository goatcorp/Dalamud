using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;

using Dalamud.Data;
using Dalamud.Game;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Services;
using ImGuiScene;
using Lumina.Data.Files;

namespace Dalamud.Interface.Internal;

/// <summary>
/// Service responsible for loading and disposing ImGui texture wraps.
/// </summary>
[PluginInterface]
[InterfaceVersion("1.0")]
[ServiceManager.BlockingEarlyLoadedService]
#pragma warning disable SA1015
[ResolveVia<ITextureSubstitutionProvider>]
#pragma warning restore SA1015
internal class TextureManager : IDisposable, IServiceType, ITextureSubstitutionProvider
{
    private const string IconFileFormat = "ui/icon/{0:D3}000/{1}{2:D6}.tex";
    private const string HighResolutionIconFileFormat = "ui/icon/{0:D3}000/{1}{2:D6}_hr1.tex";

    private const uint MillisecondsEvictionTime = 2000;
    
    private static readonly ModuleLog Log = new("TEXM");

    private readonly Framework framework;
    private readonly DataManager dataManager;
    private readonly InterfaceManager im;
    private readonly DalamudStartInfo startInfo;
    
    private readonly Dictionary<string, TextureInfo> activeTextures = new();

    private TextureWrap? fallbackTextureWrap;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextureManager"/> class.
    /// </summary>
    /// <param name="framework">Framework instance.</param>
    /// <param name="dataManager">DataManager instance.</param>
    /// <param name="im">InterfaceManager instance.</param>
    /// <param name="startInfo">DalamudStartInfo instance.</param>
    [ServiceManager.ServiceConstructor]
    public TextureManager(Framework framework, DataManager dataManager, InterfaceManager im, DalamudStartInfo startInfo)
    {
        this.framework = framework;
        this.dataManager = dataManager;
        this.im = im;
        this.startInfo = startInfo;

        this.framework.Update += this.FrameworkOnUpdate;

        Service<InterfaceManager.InterfaceManagerWithScene>.GetAsync().ContinueWith(_ => this.CreateFallbackTexture());
    }

    /// <inheritdoc/>
    public event ITextureSubstitutionProvider.TextureDataInterceptorDelegate? InterceptTexDataLoad;
    
    /// <summary>
    /// Get a texture handle for a specific icon.
    /// </summary>
    /// <param name="iconId">The ID of the icon to load.</param>
    /// <param name="flags">Options to be considered when loading the icon.</param>
    /// <param name="language">
    /// The language to be considered when loading the icon, if the icon has versions for multiple languages.
    /// If null, default to the game's current language.
    /// </param>
    /// <param name="keepAlive">
    /// Prevent Dalamud from automatically unloading this icon to save memory. Usually does not need to be set.
    /// </param>
    /// <returns>
    /// Null, if the icon does not exist in the specified configuration, or a texture wrap that can be used
    /// to render the icon.
    /// </returns>
    public TextureManagerTextureWrap? GetIcon(uint iconId, ITextureProvider.IconFlags flags = ITextureProvider.IconFlags.HiRes, ClientLanguage? language = null, bool keepAlive = false)
    {
        var path = this.GetIconPath(iconId, flags, language);
        return path == null ? null : this.CreateWrap(path, keepAlive);
    }

    /// <summary>
    /// Get a path for a specific icon's .tex file.
    /// </summary>
    /// <param name="iconId">The ID of the icon to look up.</param>
    /// <param name="flags">Options to be considered when loading the icon.</param>
    /// <param name="language">
    /// The language to be considered when loading the icon, if the icon has versions for multiple languages.
    /// If null, default to the game's current language.
    /// </param>
    /// <returns>
    /// Null, if the icon does not exist in the specified configuration, or the path to the texture's .tex file,
    /// which can be loaded via IDataManager.
    /// </returns>
    public string? GetIconPath(uint iconId, ITextureProvider.IconFlags flags = ITextureProvider.IconFlags.HiRes, ClientLanguage? language = null)
    {
        var hiRes = flags.HasFlag(ITextureProvider.IconFlags.HiRes);
        
        // 1. Item
        var path = FormatIconPath(
            iconId,
            flags.HasFlag(ITextureProvider.IconFlags.ItemHighQuality) ? "hq/" : string.Empty,
            hiRes);
        if (this.dataManager.FileExists(path))
            return path;
        
        language ??= this.startInfo.Language;
        var languageFolder = language switch
        {
            ClientLanguage.Japanese => "ja/",
            ClientLanguage.English => "en/",
            ClientLanguage.German => "de/",
            ClientLanguage.French => "fr/",
            _ => throw new ArgumentOutOfRangeException(nameof(language), $"Unknown Language: {language}"),
        };
        
        // 2. Regular icon, with language, hi-res
        path = FormatIconPath(
            iconId,
            languageFolder,
            hiRes);
        if (this.dataManager.FileExists(path))
            return path;

        if (hiRes)
        {
            // 3. Regular icon, with language, no hi-res
            path = FormatIconPath(
                iconId,
                languageFolder,
                false);
            if (this.dataManager.FileExists(path))
                return path;
        }

        // 4. Regular icon, without language, hi-res
        path = FormatIconPath(
            iconId,
            null,
            hiRes);
        if (this.dataManager.FileExists(path))
            return path;
        
        // 4. Regular icon, without language, no hi-res
        if (hiRes)
        {
            path = FormatIconPath(
                iconId,
                null,
                false);
            if (this.dataManager.FileExists(path))
                return path;
        }

        return null;
    }

    /// <summary>
    /// Get a texture handle for the texture at the specified path.
    /// You may only specify paths in the game's VFS.
    /// </summary>
    /// <param name="path">The path to the texture in the game's VFS.</param>
    /// <param name="keepAlive">Prevent Dalamud from automatically unloading this texture to save memory. Usually does not need to be set.</param>
    /// <returns>Null, if the icon does not exist, or a texture wrap that can be used to render the texture.</returns>
    public TextureManagerTextureWrap? GetTextureFromGame(string path, bool keepAlive = false)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        if (Path.IsPathRooted(path))
            throw new ArgumentException("Use GetTextureFromFile() to load textures directly from a file.", nameof(path));
        
        return !this.dataManager.FileExists(path) ? null : this.CreateWrap(path, keepAlive);
    }

    /// <summary>
    /// Get a texture handle for the image or texture, specified by the passed FileInfo.
    /// You may only specify paths on the native file system.
    ///
    /// This API can load .png and .tex files.
    /// </summary>
    /// <param name="file">The FileInfo describing the image or texture file.</param>
    /// <param name="keepAlive">Prevent Dalamud from automatically unloading this texture to save memory. Usually does not need to be set.</param>
    /// <returns>Null, if the file does not exist, or a texture wrap that can be used to render the texture.</returns>
    public TextureManagerTextureWrap? GetTextureFromFile(FileInfo file, bool keepAlive = false)
    {
        ArgumentNullException.ThrowIfNull(file);
        return !file.Exists ? null : this.CreateWrap(file.FullName, keepAlive);
    }

    /// <summary>
    /// Get a texture handle for the specified Lumina TexFile.
    /// </summary>
    /// <param name="file">The texture to obtain a handle to.</param>
    /// <returns>A texture wrap that can be used to render the texture.</returns>
    public IDalamudTextureWrap? GetTexture(TexFile file)
    {
        ArgumentNullException.ThrowIfNull(file);

        if (!this.im.IsReady)
            throw new InvalidOperationException("Cannot create textures before scene is ready");
        
#pragma warning disable CS0618
        return (IDalamudTextureWrap)this.dataManager.GetImGuiTexture(file);
#pragma warning restore CS0618
    }

    /// <inheritdoc/>
    public string GetSubstitutedPath(string originalPath)
    {
        if (this.InterceptTexDataLoad == null)
            return originalPath;
        
        string? interceptPath = null;
        this.InterceptTexDataLoad.Invoke(originalPath, ref interceptPath);

        if (interceptPath != null)
        {
            Log.Verbose("Intercept: {OriginalPath} => {ReplacePath}", originalPath, interceptPath);
            return interceptPath;
        }

        return originalPath;
    }

    /// <inheritdoc/>
    public void InvalidatePaths(IEnumerable<string> paths)
    {
        lock (this.activeTextures)
        {
            foreach (var path in paths)
            {
                if (!this.activeTextures.TryGetValue(path, out var info) || info == null)
                    continue;

                info.Wrap = null;
            }
        }
    }
    
    /// <inheritdoc/>
    public void Dispose()
    {
        this.fallbackTextureWrap?.Dispose();
        this.framework.Update -= this.FrameworkOnUpdate;
        
        Log.Verbose("Disposing {Num} left behind textures.");
        
        foreach (var activeTexture in this.activeTextures)
        {
            activeTexture.Value.Wrap?.Dispose();
        }
        
        this.activeTextures.Clear();
    }

    /// <summary>
    /// Get texture info.
    /// </summary>
    /// <param name="path">Path to the texture.</param>
    /// <param name="rethrow">
    /// If true, exceptions caused by texture load will not be caught.
    /// If false, exceptions will be caught and a dummy texture will be returned to prevent plugins from using invalid texture handles.
    /// </param>
    /// <returns>Info object storing texture metadata.</returns>
    internal TextureInfo GetInfo(string path, bool rethrow = false)
    {
        TextureInfo? info;
        lock (this.activeTextures)
        {
            if (!this.activeTextures.TryGetValue(path, out info))
            {
                Debug.Assert(rethrow, "This should never run when getting outside of creator");

                info = new TextureInfo();
                this.activeTextures.Add(path, info);
            }

            if (info == null)
                throw new Exception("null info in activeTextures");
        }

        if (info.KeepAliveCount == 0)
            info.LastAccess = DateTime.UtcNow;
        
        if (info is { Wrap: not null })
            return info;

        if (!this.im.IsReady)
                throw new InvalidOperationException("Cannot create textures before scene is ready");

        // Substitute the path here for loading, instead of when getting the respective TextureInfo
        path = this.GetSubstitutedPath(path);
        
        TextureWrap? wrap;
        try
        {
            // We want to load this from the disk, probably, if the path has a root
            // Not sure if this can cause issues with e.g. network drives, might have to rethink
            // and add a flag instead if it does.
            if (Path.IsPathRooted(path))
            {
                if (Path.GetExtension(path) == ".tex")
                {
                    // Attempt to load via Lumina
                    var file = this.dataManager.GameData.GetFileFromDisk<TexFile>(path);
                    wrap = this.GetTexture(file);
                    Log.Verbose("Texture {Path} loaded FS via Lumina", path);
                }
                else
                {
                    // Attempt to load image
                    wrap = this.im.LoadImage(path);
                    Log.Verbose("Texture {Path} loaded FS via LoadImage", path);
                }
            }
            else
            {
                // Load regularly from dats
                var file = this.dataManager.GetFile<TexFile>(path);
                if (file == null)
                    throw new Exception("Could not load TexFile from dat.");
                    
                wrap = this.GetTexture(file);
                Log.Verbose("Texture {Path} loaded from SqPack", path);
            }
                
            if (wrap == null)
                throw new Exception("Could not create texture");

            // TODO: We could support this, but I don't think it's worth it at the moment.
            var extents = new Vector2(wrap.Width, wrap.Height);
            if (info.Extents != Vector2.Zero && info.Extents != extents)
                Log.Warning("Texture at {Path} changed size between reloads, this is currently not supported.", path);

            info.Extents = extents;
        }
        catch (Exception e)
        {
            Log.Error(e, "Could not load texture from {Path}", path);

            // When creating the texture initially, we want to be able to pass errors back to the plugin
            if (rethrow)
                throw;
                
            // This means that the load failed due to circumstances outside of our control,
            // and we can't do anything about it. Return a dummy texture so that the plugin still
            // has something to draw.
            wrap = this.fallbackTextureWrap;
                
            // Prevent divide-by-zero
            if (info.Extents == Vector2.Zero)
                info.Extents = Vector2.One;
        }

        info.Wrap = wrap;
        return info;
    }

    /// <summary>
    /// Notify the system about an instance of a texture wrap being disposed.
    /// If required conditions are met, the texture will be unloaded at the next update.
    /// </summary>
    /// <param name="path">The path to the texture.</param>
    /// <param name="keepAlive">Whether or not this handle was created in keep-alive mode.</param>
    internal void NotifyTextureDisposed(string path, bool keepAlive)
    {
        lock (this.activeTextures)
        {
            if (!this.activeTextures.TryGetValue(path, out var info))
            {
                Log.Warning("Disposing texture that didn't exist: {Path}", path);
                return;
            }
            
            info.RefCount--;

            if (keepAlive)
                info.KeepAliveCount--;

            // Clean it up by the next update. If it's re-requested in-between, we don't reload it.
            if (info.RefCount <= 0)
                info.LastAccess = default;
        }
    }

    private static string FormatIconPath(uint iconId, string? type, bool highResolution)
    {
        var format = highResolution ? HighResolutionIconFileFormat : IconFileFormat;
        
        type ??= string.Empty;
        if (type.Length > 0 && !type.EndsWith("/"))
            type += "/";

        return string.Format(format, iconId / 1000, type, iconId);
    }
    
    private TextureManagerTextureWrap? CreateWrap(string path, bool keepAlive)
    {
        lock (this.activeTextures)
        {
            // This will create the texture.
            // That's fine, it's probably used immediately and this will let the plugin catch load errors.
            var info = this.GetInfo(path, rethrow: true);

            // We need to increase the refcounts here while locking the collection!
            // Otherwise, if this is loaded from a task, cleanup might already try to delete it
            // before it can be increased.
            info.RefCount++;

            if (keepAlive)
                info.KeepAliveCount++;
            
            return new TextureManagerTextureWrap(path, info.Extents, keepAlive, this);
        }
    }

    private void FrameworkOnUpdate(Framework fw)
    {
        lock (this.activeTextures)
        {
            var toRemove = new List<string>();

            foreach (var texInfo in this.activeTextures)
            {
                if (texInfo.Value.RefCount == 0)
                {
                    Log.Verbose("Evicting {Path} since no refs", texInfo.Key);

                    Debug.Assert(texInfo.Value.KeepAliveCount == 0, "texInfo.Value.KeepAliveCount == 0");
                    
                    texInfo.Value.Wrap?.Dispose();
                    texInfo.Value.Wrap = null;
                    toRemove.Add(texInfo.Key);
                    continue;
                }
                
                if (texInfo.Value.KeepAliveCount > 0 || texInfo.Value.Wrap == null)
                    continue;

                if (DateTime.UtcNow - texInfo.Value.LastAccess > TimeSpan.FromMilliseconds(MillisecondsEvictionTime))
                {
                    Log.Verbose("Evicting {Path} since too old", texInfo.Key);
                    texInfo.Value.Wrap.Dispose();
                    texInfo.Value.Wrap = null;
                }
            }

            foreach (var path in toRemove)
            {
                this.activeTextures.Remove(path);
            }
        }
    }

    private void CreateFallbackTexture()
    {
        var fallbackTexBytes = new byte[] { 0xFF, 0x00, 0xDC, 0xFF };
        this.fallbackTextureWrap = this.im.LoadImageRaw(fallbackTexBytes, 1, 1, 4);
        Debug.Assert(this.fallbackTextureWrap != null, "this.fallbackTextureWrap != null");
    }

    /// <summary>
    /// Internal representation of a managed texture.
    /// </summary>
    internal class TextureInfo
    {
        /// <summary>
        /// Gets or sets the actual texture wrap. May be unpopulated.
        /// </summary>
        public TextureWrap? Wrap { get; set; }
        
        /// <summary>
        /// Gets or sets the time the texture was last accessed.
        /// </summary>
        public DateTime LastAccess { get; set; }

        /// <summary>
        /// Gets or sets the number of active holders of this texture.
        /// </summary>
        public uint RefCount { get; set; }
        
        /// <summary>
        /// Gets or sets the number of active holders that want this texture to stay alive forever.
        /// </summary>
        public uint KeepAliveCount { get; set; }
        
        /// <summary>
        /// Gets or sets the extents of the texture.
        /// </summary>
        public Vector2 Extents { get; set; }
    }
}

/// <summary>
/// Plugin-scoped version of a texture manager.
/// </summary>
[PluginInterface]
[InterfaceVersion("1.0")]
[ServiceManager.ScopedService]
#pragma warning disable SA1015
[ResolveVia<ITextureProvider>]
#pragma warning restore SA1015
internal class TextureManagerPluginScoped : ITextureProvider, IServiceType, IDisposable
{
    private readonly TextureManager textureManager;

    private readonly List<TextureManagerTextureWrap> trackedTextures = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="TextureManagerPluginScoped"/> class.
    /// </summary>
    /// <param name="textureManager">TextureManager instance.</param>
    public TextureManagerPluginScoped(TextureManager textureManager)
    {
        this.textureManager = textureManager;
    }

    /// <inheritdoc/>
    public IDalamudTextureWrap? GetIcon(
        uint iconId,
        ITextureProvider.IconFlags flags = ITextureProvider.IconFlags.ItemHighQuality,
        ClientLanguage? language = null,
        bool keepAlive = false)
    {
        var wrap = this.textureManager.GetIcon(iconId, flags, language, keepAlive);
        if (wrap == null)
            return null;
        
        this.trackedTextures.Add(wrap);
        return wrap;
    }

    /// <inheritdoc/>
    public string? GetIconPath(uint iconId, ITextureProvider.IconFlags flags = ITextureProvider.IconFlags.HiRes, ClientLanguage? language = null)
        => this.textureManager.GetIconPath(iconId, flags, language);

    /// <inheritdoc/>
    public IDalamudTextureWrap? GetTextureFromGame(string path, bool keepAlive = false)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        
        var wrap = this.textureManager.GetTextureFromGame(path, keepAlive);
        if (wrap == null)
            return null;
        
        this.trackedTextures.Add(wrap);
        return wrap;
    }

    /// <inheritdoc/>
    public IDalamudTextureWrap? GetTextureFromFile(FileInfo file, bool keepAlive)
    {
        ArgumentNullException.ThrowIfNull(file);
        
        var wrap = this.textureManager.GetTextureFromFile(file, keepAlive);
        if (wrap == null)
            return null;
        
        this.trackedTextures.Add(wrap);
        return wrap;
    }

    /// <inheritdoc/>
    public IDalamudTextureWrap? GetTexture(TexFile file)
        => this.textureManager.GetTexture(file);

    /// <inheritdoc/>
    public void Dispose()
    {
        // Dispose all leaked textures
        foreach (var textureWrap in this.trackedTextures.Where(x => !x.IsDisposed))
        {
            textureWrap.Dispose();
        }
    }
}

/// <summary>
/// Wrap.
/// </summary>
internal class TextureManagerTextureWrap : IDalamudTextureWrap
{
    private readonly TextureManager manager;
    private readonly string path;
    private readonly bool keepAlive;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextureManagerTextureWrap"/> class.
    /// </summary>
    /// <param name="path">The path to the texture.</param>
    /// <param name="extents">The extents of the texture.</param>
    /// <param name="keepAlive">Keep alive or not.</param>
    /// <param name="manager">Manager that we obtained this from.</param>
    internal TextureManagerTextureWrap(string path, Vector2 extents, bool keepAlive, TextureManager manager)
    {
        this.path = path;
        this.keepAlive = keepAlive;
        this.manager = manager;
        this.Width = (int)extents.X;
        this.Height = (int)extents.Y;
    }

    /// <inheritdoc/>
    public IntPtr ImGuiHandle => !this.IsDisposed ?
                                     this.manager.GetInfo(this.path).Wrap!.ImGuiHandle :
                                     throw new InvalidOperationException("Texture already disposed. You may not render it.");

    /// <inheritdoc/>
    public int Width { get; private set; }

    /// <inheritdoc/>
    public int Height { get; private set; }

    /// <summary>
    /// Gets a value indicating whether or not this wrap has already been disposed.
    /// If true, the handle may be invalid.
    /// </summary>
    internal bool IsDisposed { get; private set; }

    /// <inheritdoc/>
    public void Dispose()
    {
        lock (this)
        {
            if (!this.IsDisposed)
                this.manager.NotifyTextureDisposed(this.path, this.keepAlive);
        
            this.IsDisposed = true;   
        }
    }
}
