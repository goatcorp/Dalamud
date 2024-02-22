using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using BitFaster.Caching.Lru;

using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Interface.Internal.SharableTextures;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Services;
using Dalamud.Storage.Assets;
using Dalamud.Utility;

using Lumina.Data.Files;

namespace Dalamud.Interface.Internal;

// TODO API10: Remove keepAlive from public APIs

/// <summary>
/// Service responsible for loading and disposing ImGui texture wraps.
/// </summary>
[PluginInterface]
[InterfaceVersion("1.0")]
[ServiceManager.BlockingEarlyLoadedService]
#pragma warning disable SA1015
[ResolveVia<ITextureProvider>]
[ResolveVia<ITextureSubstitutionProvider>]
#pragma warning restore SA1015
internal sealed class TextureManager : IServiceType, IDisposable, ITextureProvider, ITextureSubstitutionProvider
{
    private const int PathLookupLruCount = 8192;

    private const string IconFileFormat = "ui/icon/{0:D3}000/{1}{2:D6}.tex";
    private const string HighResolutionIconFileFormat = "ui/icon/{0:D3}000/{1}{2:D6}_hr1.tex";

    private static readonly ModuleLog Log = new("TEXM");

    [ServiceManager.ServiceDependency]
    private readonly Dalamud dalamud = Service<Dalamud>.Get();

    [ServiceManager.ServiceDependency]
    private readonly DalamudAssetManager dalamudAssetManager = Service<DalamudAssetManager>.Get();

    [ServiceManager.ServiceDependency]
    private readonly DataManager dataManager = Service<DataManager>.Get();

    [ServiceManager.ServiceDependency]
    private readonly Framework framework = Service<Framework>.Get();

    [ServiceManager.ServiceDependency]
    private readonly InterfaceManager interfaceManager = Service<InterfaceManager>.Get();

    [ServiceManager.ServiceDependency]
    private readonly TextureLoadThrottler textureLoadThrottler = Service<TextureLoadThrottler>.Get();

    private readonly ConcurrentLru<GameIconLookup, string> lookupToPath = new(PathLookupLruCount);
    private readonly ConcurrentDictionary<string, SharableTexture> gamePathTextures = new();
    private readonly ConcurrentDictionary<string, SharableTexture> fileSystemTextures = new();
    private readonly HashSet<SharableTexture> invalidatedTextures = new();

    private bool disposing;

    [ServiceManager.ServiceConstructor]
    private TextureManager()
    {
        this.framework.Update += this.FrameworkOnUpdate;
    }

    /// <inheritdoc/>
    public event ITextureSubstitutionProvider.TextureDataInterceptorDelegate? InterceptTexDataLoad;

    /// <summary>
    /// Gets all the loaded textures from the game resources. Debug use only.
    /// </summary>
    public ICollection<SharableTexture> GamePathTextures => this.gamePathTextures.Values;

    /// <summary>
    /// Gets all the loaded textures from the game resources. Debug use only.
    /// </summary>
    public ICollection<SharableTexture> FileSystemTextures => this.fileSystemTextures.Values;

    /// <summary>
    /// Gets all the loaded textures that are invalidated from <see cref="InvalidatePaths"/>. Debug use only.
    /// </summary>
    /// <remarks><c>lock</c> on use of the value returned from this property.</remarks>
    [SuppressMessage(
        "ReSharper",
        "InconsistentlySynchronizedField",
        Justification = "Debug use only; users are expected to lock around this")]
    public ICollection<SharableTexture> InvalidatedTextures => this.invalidatedTextures;

    /// <inheritdoc/>
    public void Dispose()
    {
        if (this.disposing)
            return;

        this.disposing = true;
        foreach (var v in this.gamePathTextures.Values)
            v.ReleaseSelfReference(true);
        foreach (var v in this.fileSystemTextures.Values)
            v.ReleaseSelfReference(true);

        this.lookupToPath.Clear();
        this.gamePathTextures.Clear();
        this.fileSystemTextures.Clear();
    }

#pragma warning disable CS0618 // Type or member is obsolete
    /// <inheritdoc/>
    [Api10ToDo(Api10ToDoAttribute.DeleteCompatBehavior)]
    [Obsolete("See interface definition.")]
    public string? GetIconPath(
        uint iconId,
        ITextureProvider.IconFlags flags = ITextureProvider.IconFlags.HiRes,
        ClientLanguage? language = null)
        => this.TryGetIconPath(
               new(
                   iconId,
                   (flags & ITextureProvider.IconFlags.ItemHighQuality) != 0,
                   (flags & ITextureProvider.IconFlags.HiRes) != 0,
                   language),
               out var path)
               ? path
               : null;

    /// <inheritdoc/>
    [Api10ToDo(Api10ToDoAttribute.DeleteCompatBehavior)]
    [Obsolete("See interface definition.")]
    public IDalamudTextureWrap? GetIcon(
        uint iconId,
        ITextureProvider.IconFlags flags = ITextureProvider.IconFlags.HiRes,
        ClientLanguage? language = null,
        bool keepAlive = false) =>
        this.GetTextureFromGame(
            this.lookupToPath.GetOrAdd(
                new(
                    iconId,
                    (flags & ITextureProvider.IconFlags.ItemHighQuality) != 0,
                    (flags & ITextureProvider.IconFlags.HiRes) != 0,
                    language),
                this.GetIconPathByValue));

    /// <inheritdoc/>
    [Api10ToDo(Api10ToDoAttribute.DeleteCompatBehavior)]
    [Obsolete("See interface definition.")]
    public IDalamudTextureWrap? GetTextureFromGame(string path, bool keepAlive = false) =>
        this.gamePathTextures.GetOrAdd(path, GamePathSharableTexture.CreateImmediate)
            .GetAvailableOnAccessWrapForApi9();

    /// <inheritdoc/>
    [Api10ToDo(Api10ToDoAttribute.DeleteCompatBehavior)]
    [Obsolete("See interface definition.")]
    public IDalamudTextureWrap? GetTextureFromFile(FileInfo file, bool keepAlive = false) =>
        this.fileSystemTextures.GetOrAdd(file.FullName, FileSystemSharableTexture.CreateImmediate)
            .GetAvailableOnAccessWrapForApi9();
#pragma warning restore CS0618 // Type or member is obsolete

    /// <inheritdoc/>
    public IDalamudTextureWrap ImmediateGetFromGameIcon(in GameIconLookup lookup) =>
        this.ImmediateGetFromGame(this.lookupToPath.GetOrAdd(lookup, this.GetIconPathByValue));

    /// <inheritdoc/>
    public IDalamudTextureWrap ImmediateGetFromGame(string path) =>
        this.ImmediateTryGetFromGame(path, out var texture, out _)
            ? texture
            : this.dalamudAssetManager.Empty4X4;

    /// <inheritdoc/>
    public IDalamudTextureWrap ImmediateGetFromFile(string file) =>
        this.ImmediateTryGetFromFile(file, out var texture, out _)
            ? texture
            : this.dalamudAssetManager.Empty4X4;

    /// <inheritdoc/>
    public bool ImmediateTryGetFromGameIcon(
        in GameIconLookup lookup,
        [NotNullWhen(true)] out IDalamudTextureWrap? texture,
        out Exception? exception) =>
        this.ImmediateTryGetFromGame(
            this.lookupToPath.GetOrAdd(lookup, this.GetIconPathByValue),
            out texture,
            out exception);

    /// <inheritdoc/>
    public bool ImmediateTryGetFromGame(
        string path,
        [NotNullWhen(true)] out IDalamudTextureWrap? texture,
        out Exception? exception)
    {
        ThreadSafety.AssertMainThread();
        var t = this.gamePathTextures.GetOrAdd(path, GamePathSharableTexture.CreateImmediate);
        texture = t.GetImmediate();
        exception = t.UnderlyingWrap?.Exception;
        return texture is not null;
    }

    /// <inheritdoc/>
    public bool ImmediateTryGetFromFile(
        string file,
        [NotNullWhen(true)] out IDalamudTextureWrap? texture,
        out Exception? exception)
    {
        ThreadSafety.AssertMainThread();
        var t = this.fileSystemTextures.GetOrAdd(file, FileSystemSharableTexture.CreateImmediate);
        texture = t.GetImmediate();
        exception = t.UnderlyingWrap?.Exception;
        return texture is not null;
    }

    /// <inheritdoc/>
    public Task<IDalamudTextureWrap> GetFromGameIconAsync(
        in GameIconLookup lookup,
        CancellationToken cancellationToken = default) =>
        this.GetFromGameAsync(this.lookupToPath.GetOrAdd(lookup, this.GetIconPathByValue), cancellationToken);

    /// <inheritdoc/>
    public Task<IDalamudTextureWrap> GetFromGameAsync(
        string path,
        CancellationToken cancellationToken = default) =>
        this.gamePathTextures.GetOrAdd(path, GamePathSharableTexture.CreateAsync)
            .CreateNewReference(cancellationToken);

    /// <inheritdoc/>
    public Task<IDalamudTextureWrap> GetFromFileAsync(
        string file,
        CancellationToken cancellationToken = default) =>
        this.fileSystemTextures.GetOrAdd(file, FileSystemSharableTexture.CreateAsync)
            .CreateNewReference(cancellationToken);

    /// <inheritdoc/>
    public Task<IDalamudTextureWrap> GetFromImageAsync(
        ReadOnlyMemory<byte> bytes,
        CancellationToken cancellationToken = default) =>
        this.textureLoadThrottler.CreateLoader(
            new TextureLoadThrottler.ReadOnlyThrottleBasisProvider(),
            _ => Task.FromResult(
                this.interfaceManager.LoadImage(bytes.ToArray())
                ?? throw new("Failed to load image because of an unknown reason.")),
            cancellationToken);

    /// <inheritdoc/>
    public Task<IDalamudTextureWrap> GetFromImageAsync(
        Stream stream,
        bool leaveOpen = false,
        CancellationToken cancellationToken = default) =>
        this.textureLoadThrottler.CreateLoader(
            new TextureLoadThrottler.ReadOnlyThrottleBasisProvider(),
            async ct =>
            {
                await using var streamDispose = leaveOpen ? null : stream;
                await using var ms = stream.CanSeek ? new MemoryStream((int)stream.Length) : new();
                await stream.CopyToAsync(ms, ct).ConfigureAwait(false);
                return await this.GetFromImageAsync(ms.GetBuffer(), ct);
            },
            cancellationToken);

    /// <inheritdoc/>
    public IDalamudTextureWrap GetFromRaw(
        RawImageSpecification specs,
        ReadOnlySpan<byte> bytes,
        CancellationToken cancellationToken = default) =>
        this.interfaceManager.LoadImageFromDxgiFormat(
            bytes,
            specs.Pitch,
            specs.Width,
            specs.Height,
            (SharpDX.DXGI.Format)specs.DxgiFormat);

    /// <inheritdoc/>
    public Task<IDalamudTextureWrap> GetFromRawAsync(
        RawImageSpecification specs,
        ReadOnlyMemory<byte> bytes,
        CancellationToken cancellationToken = default) =>
        this.textureLoadThrottler.CreateLoader(
            new TextureLoadThrottler.ReadOnlyThrottleBasisProvider(),
            ct => Task.FromResult(this.GetFromRaw(specs, bytes.Span, ct)),
            cancellationToken);

    /// <inheritdoc/>
    public Task<IDalamudTextureWrap> GetFromRawAsync(
        RawImageSpecification specs,
        Stream stream,
        bool leaveOpen = false,
        CancellationToken cancellationToken = default) =>
        this.textureLoadThrottler.CreateLoader(
            new TextureLoadThrottler.ReadOnlyThrottleBasisProvider(),
            async ct =>
            {
                await using var streamDispose = leaveOpen ? null : stream;
                await using var ms = stream.CanSeek ? new MemoryStream((int)stream.Length) : new();
                await stream.CopyToAsync(ms, ct).ConfigureAwait(false);
                return await this.GetFromRawAsync(specs, ms.GetBuffer(), ct);
            },
            cancellationToken);

    /// <inheritdoc/>
    public IDalamudTextureWrap GetTexture(TexFile file) => this.GetFromTexFileAsync(file).Result;

    /// <inheritdoc/>
    public Task<IDalamudTextureWrap> GetFromTexFileAsync(
        TexFile file,
        CancellationToken cancellationToken = default) =>
        this.textureLoadThrottler.CreateLoader(
            new TextureLoadThrottler.ReadOnlyThrottleBasisProvider(),
            _ => Task.FromResult<IDalamudTextureWrap>(this.interfaceManager.LoadImageFromTexFile(file)),
            cancellationToken);

    /// <inheritdoc/>
    public bool SupportsDxgiFormat(int dxgiFormat) =>
        this.interfaceManager.SupportsDxgiFormat((SharpDX.DXGI.Format)dxgiFormat);

    /// <inheritdoc/>
    public bool TryGetIconPath(in GameIconLookup lookup, out string path)
    {
        // 1. Item
        path = FormatIconPath(
            lookup.IconId,
            lookup.ItemHq ? "hq/" : string.Empty,
            lookup.HiRes);
        if (this.dataManager.FileExists(path))
            return true;

        var languageFolder = (lookup.Language ?? (ClientLanguage)(int)this.dalamud.StartInfo.Language) switch
        {
            ClientLanguage.Japanese => "ja/",
            ClientLanguage.English => "en/",
            ClientLanguage.German => "de/",
            ClientLanguage.French => "fr/",
            _ => null,
        };

        if (languageFolder is not null)
        {
            // 2. Regular icon, with language, hi-res
            path = FormatIconPath(
                lookup.IconId,
                languageFolder,
                lookup.HiRes);
            if (this.dataManager.FileExists(path))
                return true;

            if (lookup.HiRes)
            {
                // 3. Regular icon, with language, no hi-res
                path = FormatIconPath(
                    lookup.IconId,
                    languageFolder,
                    false);
                if (this.dataManager.FileExists(path))
                    return true;
            }
        }

        // 4. Regular icon, without language, hi-res
        path = FormatIconPath(
            lookup.IconId,
            null,
            lookup.HiRes);
        if (this.dataManager.FileExists(path))
            return true;

        // 4. Regular icon, without language, no hi-res
        if (lookup.HiRes)
        {
            path = FormatIconPath(
                lookup.IconId,
                null,
                false);
            if (this.dataManager.FileExists(path))
                return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public string GetIconPath(in GameIconLookup lookup) =>
        this.TryGetIconPath(lookup, out var path) ? path : throw new FileNotFoundException();

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
        foreach (var path in paths)
        {
            if (this.gamePathTextures.TryRemove(path, out var r))
            {
                if (r.ReleaseSelfReference(true) != 0 || r.HasRevivalPossibility)
                {
                    lock (this.invalidatedTextures)
                        this.invalidatedTextures.Add(r);
                }
            }

            if (this.fileSystemTextures.TryRemove(path, out r))
            {
                if (r.ReleaseSelfReference(true) != 0 || r.HasRevivalPossibility)
                {
                    lock (this.invalidatedTextures)
                        this.invalidatedTextures.Add(r);
                }
            }
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

    private void FrameworkOnUpdate(IFramework unused)
    {
        if (!this.gamePathTextures.IsEmpty)
        {
            foreach (var (k, v) in this.gamePathTextures)
            {
                if (TextureFinalReleasePredicate(v))
                    _ = this.gamePathTextures.TryRemove(k, out _);
            }
        }

        if (!this.fileSystemTextures.IsEmpty)
        {
            foreach (var (k, v) in this.fileSystemTextures)
            {
                if (TextureFinalReleasePredicate(v))
                    _ = this.fileSystemTextures.TryRemove(k, out _);
            }
        }

        // ReSharper disable once InconsistentlySynchronizedField
        if (this.invalidatedTextures.Count != 0)
        {
            lock (this.invalidatedTextures)
                this.invalidatedTextures.RemoveWhere(TextureFinalReleasePredicate);
        }

        return;

        static bool TextureFinalReleasePredicate(SharableTexture v) =>
            v.ReleaseSelfReference(false) == 0 && !v.HasRevivalPossibility;
    }

    private string GetIconPathByValue(GameIconLookup lookup) =>
        this.TryGetIconPath(lookup, out var path) ? path : throw new FileNotFoundException();
}
