using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

using Dalamud.Data;
using Dalamud.Game;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Services;
using ImGuiScene;
using Lumina.Data;
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
    private readonly DalamudStartInfo startInfo;
    
    private readonly Dictionary<string, TextureInfo> activeTextures = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="TextureManager"/> class.
    /// </summary>
    /// <param name="framework">Framework instance.</param>
    /// <param name="dataManager">DataManager instance.</param>
    /// <param name="startInfo">DalamudStartInfo instance.</param>
    [ServiceManager.ServiceConstructor]
    public TextureManager(Framework framework, DataManager dataManager, DalamudStartInfo startInfo)
    {
        this.framework = framework;
        this.dataManager = dataManager;
        this.startInfo = startInfo;

        this.framework.Update += this.FrameworkOnUpdate;
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
        var hiRes = flags.HasFlag(ITextureProvider.IconFlags.HiRes);
        
        // 1. Item
        var path = FormatIconPath(
            iconId,
            flags.HasFlag(ITextureProvider.IconFlags.ItemHighQuality) ? "hq/" : string.Empty,
            hiRes);
        if (this.dataManager.FileExists(path))
            return this.CreateWrap(path, keepAlive);
        
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
            return this.CreateWrap(path, keepAlive);

        if (hiRes)
        {
            // 3. Regular icon, with language, no hi-res
            path = FormatIconPath(
                iconId,
                languageFolder,
                false);
            if (this.dataManager.FileExists(path))
                return this.CreateWrap(path, keepAlive);
        }

        // 4. Regular icon, without language, hi-res
        path = FormatIconPath(
            iconId,
            null,
            hiRes);
        if (this.dataManager.FileExists(path))
            return this.CreateWrap(path, keepAlive);
        
        // 4. Regular icon, without language, no hi-res
        if (hiRes)
        {
            path = FormatIconPath(
                iconId,
                null,
                false);
            if (this.dataManager.FileExists(path))
                return this.CreateWrap(path, keepAlive);
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
    public TextureManagerTextureWrap? GetTextureFromGamePath(string path, bool keepAlive)
    {
        return !this.dataManager.FileExists(path) ? null : this.CreateWrap(path, keepAlive);
    }
    
    /// <inheritdoc/>
    public void Dispose()
    {
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
    /// <param name="refresh">Whether or not the texture should be reloaded if it was unloaded.</param>
    /// <returns>Info object storing texture metadata.</returns>
    internal TextureInfo GetInfo(string path, bool refresh = true)
    {
        TextureInfo? info;
        lock (this.activeTextures)
        {
            this.activeTextures.TryGetValue(path, out info);
        }

        if (info == null)
        {
            info = new TextureInfo();
            lock (this.activeTextures)
            {
                if (!this.activeTextures.TryAdd(path, info))
                    Log.Warning("Texture {Path} tracked twice, this might not be an issue", path);
            }
        }
        
        if (refresh && info.KeepAliveCount == 0)
            info.LastAccess = DateTime.Now;
        
        if (info is { Wrap: not null })
            return info;

        if (refresh)
        {
            byte[]? interceptData = null;
            this.InterceptTexDataLoad?.Invoke(path, ref interceptData);

            // TODO: Do we also want to support loading from actual fs here? Doesn't seem to be a big deal, collect interest
            
            TexFile? file;
            if (interceptData != null)
            {
                // TODO: upstream to lumina
                file = Activator.CreateInstance<TexFile>();
                var type = typeof(TexFile);
                type.GetProperty("Data", BindingFlags.NonPublic | BindingFlags.Instance)!.GetSetMethod()!
                    .Invoke(file, new object[] { interceptData });
                type.GetProperty("Reader", BindingFlags.NonPublic | BindingFlags.Instance)!.GetSetMethod()!
                    .Invoke(file, new object[] { new LuminaBinaryReader(file.Data) });
                file.LoadFile();
            }
            else
            {
                file = this.dataManager.GetFile<TexFile>(path);
            }
            
            var wrap = this.dataManager.GetImGuiTexture(file);
            info.Wrap = wrap;
        }

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
        var info = this.GetInfo(path, false);
        info.RefCount--;

        if (keepAlive)
            info.KeepAliveCount--;

        // Clean it up by the next update. If it's re-requested in-between, we don't reload it.
        if (info.RefCount <= 0)
            info.LastAccess = default;
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
        // This will create the texture.
        // That's fine, it's probably used immediately and this will let the plugin catch load errors.
        var info = this.GetInfo(path, keepAlive);
        info.RefCount++;

        if (keepAlive)
            info.KeepAliveCount++;

        return new TextureManagerTextureWrap(path, keepAlive, this);
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

                if (DateTime.Now - texInfo.Value.LastAccess > TimeSpan.FromMilliseconds(MillisecondsEvictionTime))
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
    private readonly DataManager dataManager;
    private readonly TextureManager textureManager;

    private readonly List<TextureManagerTextureWrap> trackedTextures = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="TextureManagerPluginScoped"/> class.
    /// </summary>
    /// <param name="dataManager">DataManager instance.</param>
    /// <param name="textureManager">TextureManager instance.</param>
    public TextureManagerPluginScoped(DataManager dataManager, TextureManager textureManager)
    {
        this.dataManager = dataManager;
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
    public IDalamudTextureWrap? GetTextureFromGamePath(string path, bool keepAlive = false)
    {
        var wrap = this.textureManager.GetTextureFromGamePath(path, keepAlive);
        if (wrap == null)
            return null;
        
        this.trackedTextures.Add(wrap);
        return wrap;
    }

    /// <inheritdoc/>
    public IDalamudTextureWrap GetTexture(TexFile file)
    {
        return this.dataManager.GetImGuiTexture(file) as DalamudTextureWrap ?? throw new ArgumentException("Could not load texture");
    }

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

    private int? width;
    private int? height;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextureManagerTextureWrap"/> class.
    /// </summary>
    /// <param name="path">The path to the texture.</param>
    /// <param name="keepAlive">Keep alive or not.</param>
    /// <param name="manager">Manager that we obtained this from.</param>
    internal TextureManagerTextureWrap(string path, bool keepAlive, TextureManager manager)
    {
        this.path = path;
        this.keepAlive = keepAlive;
        this.manager = manager;
    }

    /// <inheritdoc/>
    public IntPtr ImGuiHandle => this.manager.GetInfo(this.path).Wrap!.ImGuiHandle;

    /// <inheritdoc/>
    public int Width => this.width ??= this.manager.GetInfo(this.path).Wrap!.Width;

    /// <inheritdoc/>
    public int Height => this.height ??= this.manager.GetInfo(this.path).Wrap!.Height;

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
