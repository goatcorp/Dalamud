using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using BitFaster.Caching.Lru;

using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Interface.Internal.SharedImmediateTextures;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Services;
using Dalamud.Storage.Assets;
using Dalamud.Utility;

using Lumina.Data.Files;

using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

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
    private readonly ConcurrentDictionary<string, SharedImmediateTexture> gamePathTextures = new();
    private readonly ConcurrentDictionary<string, SharedImmediateTexture> fileSystemTextures = new();
    private readonly HashSet<SharedImmediateTexture> invalidatedTextures = new();

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
    public ICollection<SharedImmediateTexture> GamePathTextures => this.gamePathTextures.Values;

    /// <summary>
    /// Gets all the loaded textures from the game resources. Debug use only.
    /// </summary>
    public ICollection<SharedImmediateTexture> FileSystemTextures => this.fileSystemTextures.Values;

    /// <summary>
    /// Gets all the loaded textures that are invalidated from <see cref="InvalidatePaths"/>. Debug use only.
    /// </summary>
    /// <remarks><c>lock</c> on use of the value returned from this property.</remarks>
    [SuppressMessage(
        "ReSharper",
        "InconsistentlySynchronizedField",
        Justification = "Debug use only; users are expected to lock around this")]
    public ICollection<SharedImmediateTexture> InvalidatedTextures => this.invalidatedTextures;

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

#region API9 compat
#pragma warning disable CS0618 // Type or member is obsolete
    /// <inheritdoc/>
    [Api10ToDo(Api10ToDoAttribute.DeleteCompatBehavior)]
    [Obsolete("See interface definition.")]
    string? ITextureProvider.GetIconPath(uint iconId, ITextureProvider.IconFlags flags, ClientLanguage? language)
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
    IDalamudTextureWrap? ITextureProvider.GetIcon(
        uint iconId,
        ITextureProvider.IconFlags flags,
        ClientLanguage? language,
        bool keepAlive) =>
        this.GetFromGameIcon(
                new(
                    iconId,
                    (flags & ITextureProvider.IconFlags.ItemHighQuality) != 0,
                    (flags & ITextureProvider.IconFlags.HiRes) != 0,
                    language))
            .GetAvailableOnAccessWrapForApi9();

    /// <inheritdoc/>
    [Api10ToDo(Api10ToDoAttribute.DeleteCompatBehavior)]
    [Obsolete("See interface definition.")]
    IDalamudTextureWrap? ITextureProvider.GetTextureFromGame(string path, bool keepAlive) =>
        this.GetFromGame(path).GetAvailableOnAccessWrapForApi9();

    /// <inheritdoc/>
    [Api10ToDo(Api10ToDoAttribute.DeleteCompatBehavior)]
    [Obsolete("See interface definition.")]
    IDalamudTextureWrap? ITextureProvider.GetTextureFromFile(FileInfo file, bool keepAlive) =>
        this.GetFromFile(file.FullName).GetAvailableOnAccessWrapForApi9();
#pragma warning restore CS0618 // Type or member is obsolete
#endregion

    /// <inheritdoc cref="ITextureProvider.GetFromGameIcon"/>
    public SharedImmediateTexture GetFromGameIcon(in GameIconLookup lookup) =>
        this.GetFromGame(this.lookupToPath.GetOrAdd(lookup, this.GetIconPathByValue));

    /// <inheritdoc cref="ITextureProvider.GetFromGame"/>
    public SharedImmediateTexture GetFromGame(string path) =>
        this.gamePathTextures.GetOrAdd(path, GamePathSharedImmediateTexture.CreateImmediate);

    /// <inheritdoc cref="ITextureProvider.GetFromFile"/>
    public SharedImmediateTexture GetFromFile(string path) =>
        this.fileSystemTextures.GetOrAdd(path, FileSystemSharedImmediateTexture.CreateImmediate);

    /// <inheritdoc/>
    ISharedImmediateTexture ITextureProvider.GetFromGameIcon(in GameIconLookup lookup) => this.GetFromGameIcon(lookup);

    /// <inheritdoc/>
    ISharedImmediateTexture ITextureProvider.GetFromGame(string path) => this.GetFromGame(path);

    /// <inheritdoc/>
    ISharedImmediateTexture ITextureProvider.GetFromFile(string path) => this.GetFromFile(path);

    /// <inheritdoc/>
    public Task<IDalamudTextureWrap> CreateFromImageAsync(
        ReadOnlyMemory<byte> bytes,
        CancellationToken cancellationToken = default) =>
        this.textureLoadThrottler.CreateLoader(
            new TextureLoadThrottler.ReadOnlyThrottleBasisProvider(),
            ct => Task.Run(() => this.NoThrottleGetFromImage(bytes.ToArray()), ct),
            cancellationToken);

    /// <inheritdoc/>
    public Task<IDalamudTextureWrap> CreateFromImageAsync(
        Stream stream,
        bool leaveOpen = false,
        CancellationToken cancellationToken = default) =>
        this.textureLoadThrottler.CreateLoader(
                new TextureLoadThrottler.ReadOnlyThrottleBasisProvider(),
                async ct =>
                {
                    await using var ms = stream.CanSeek ? new MemoryStream((int)stream.Length) : new();
                    await stream.CopyToAsync(ms, ct).ConfigureAwait(false);
                    return await this.CreateFromImageAsync(ms.GetBuffer(), ct);
                },
                cancellationToken)
            .ContinueWith(
                r =>
                {
                    if (!leaveOpen)
                        stream.Dispose();
                    return r;
                },
                default(CancellationToken))
            .Unwrap();

    /// <inheritdoc/>
    public IDalamudTextureWrap CreateFromRaw(
        RawImageSpecification specs,
        ReadOnlySpan<byte> bytes)
    {
        if (this.interfaceManager.Scene is not { } scene)
        {
            _ = Service<InterfaceManager.InterfaceManagerWithScene>.Get();
            scene = this.interfaceManager.Scene ?? throw new InvalidOperationException();
        }

        ShaderResourceView resView;
        unsafe
        {
            fixed (void* pData = bytes)
            {
                var texDesc = new Texture2DDescription
                {
                    Width = specs.Width,
                    Height = specs.Height,
                    MipLevels = 1,
                    ArraySize = 1,
                    Format = (Format)specs.DxgiFormat,
                    SampleDescription = new(1, 0),
                    Usage = ResourceUsage.Immutable,
                    BindFlags = BindFlags.ShaderResource,
                    CpuAccessFlags = CpuAccessFlags.None,
                    OptionFlags = ResourceOptionFlags.None,
                };

                using var texture = new Texture2D(scene.Device, texDesc, new DataRectangle(new(pData), specs.Pitch));
                resView = new(
                    scene.Device,
                    texture,
                    new()
                    {
                        Format = texDesc.Format,
                        Dimension = ShaderResourceViewDimension.Texture2D,
                        Texture2D = { MipLevels = texDesc.MipLevels },
                    });
            }
        }

        // no sampler for now because the ImGui implementation we copied doesn't allow for changing it
        return new DalamudTextureWrap(new D3DTextureWrap(resView, specs.Width, specs.Height));
    }

    /// <inheritdoc/>
    public Task<IDalamudTextureWrap> CreateFromRawAsync(
        RawImageSpecification specs,
        ReadOnlyMemory<byte> bytes,
        CancellationToken cancellationToken = default) =>
        this.textureLoadThrottler.CreateLoader(
            new TextureLoadThrottler.ReadOnlyThrottleBasisProvider(),
            _ => Task.FromResult(this.CreateFromRaw(specs, bytes.Span)),
            cancellationToken);

    /// <inheritdoc/>
    public Task<IDalamudTextureWrap> CreateFromRawAsync(
        RawImageSpecification specs,
        Stream stream,
        bool leaveOpen = false,
        CancellationToken cancellationToken = default) =>
        this.textureLoadThrottler.CreateLoader(
                new TextureLoadThrottler.ReadOnlyThrottleBasisProvider(),
                async ct =>
                {
                    await using var ms = stream.CanSeek ? new MemoryStream((int)stream.Length) : new();
                    await stream.CopyToAsync(ms, ct).ConfigureAwait(false);
                    return await this.CreateFromRawAsync(specs, ms.GetBuffer(), ct);
                },
                cancellationToken)
            .ContinueWith(
                r =>
                {
                    if (!leaveOpen)
                        stream.Dispose();
                    return r;
                },
                default(CancellationToken))
            .Unwrap();

    /// <inheritdoc/>
    public IDalamudTextureWrap CreateFromTexFile(TexFile file) => this.CreateFromTexFileAsync(file).Result;

    /// <inheritdoc/>
    public Task<IDalamudTextureWrap> CreateFromTexFileAsync(
        TexFile file,
        CancellationToken cancellationToken = default) =>
        this.textureLoadThrottler.CreateLoader(
            new TextureLoadThrottler.ReadOnlyThrottleBasisProvider(),
            ct => Task.Run(() => this.NoThrottleGetFromTexFile(file), ct),
            cancellationToken);

    /// <inheritdoc/>
    public bool SupportsDxgiFormat(int dxgiFormat)
    {
        if (this.interfaceManager.Scene is not { } scene)
        {
            _ = Service<InterfaceManager.InterfaceManagerWithScene>.Get();
            scene = this.interfaceManager.Scene ?? throw new InvalidOperationException();
        }

        return scene.Device.CheckFormatSupport((Format)dxgiFormat).HasFlag(FormatSupport.Texture2D);
    }

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

    /// <summary>
    /// Gets a texture from the given image. Skips the load throttler; intended to be used from implementation of
    /// <see cref="SharedImmediateTexture"/>s.
    /// </summary>
    /// <param name="bytes">The data.</param>
    /// <returns>The loaded texture.</returns>
    internal IDalamudTextureWrap NoThrottleGetFromImage(ReadOnlyMemory<byte> bytes)
    {
        if (this.interfaceManager.Scene is not { } scene)
        {
            _ = Service<InterfaceManager.InterfaceManagerWithScene>.Get();
            scene = this.interfaceManager.Scene ?? throw new InvalidOperationException();
        }

        return new DalamudTextureWrap(
            scene.LoadImage(bytes.ToArray())
            ?? throw new("Failed to load image because of an unknown reason."));
    }

    /// <summary>
    /// Gets a texture from the given <see cref="TexFile"/>. Skips the load throttler; intended to be used from
    /// implementation of <see cref="SharedImmediateTexture"/>s.
    /// </summary>
    /// <param name="file">The data.</param>
    /// <returns>The loaded texture.</returns>
    internal IDalamudTextureWrap NoThrottleGetFromTexFile(TexFile file)
    {
        var buffer = file.TextureBuffer;
        var (dxgiFormat, conversion) = TexFile.GetDxgiFormatFromTextureFormat(file.Header.Format, false);
        if (conversion != TexFile.DxgiFormatConversion.NoConversion || !this.SupportsDxgiFormat(dxgiFormat))
        {
            dxgiFormat = (int)Format.B8G8R8A8_UNorm;
            buffer = buffer.Filter(0, 0, TexFile.TextureFormat.B8G8R8A8);
        }

        return this.CreateFromRaw(
            RawImageSpecification.From(buffer.Width, buffer.Height, dxgiFormat),
            buffer.RawData);
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

        static bool TextureFinalReleasePredicate(SharedImmediateTexture v) =>
            v.ContentQueried && v.ReleaseSelfReference(false) == 0 && !v.HasRevivalPossibility;
    }

    private string GetIconPathByValue(GameIconLookup lookup) =>
        this.TryGetIconPath(lookup, out var path) ? path : throw new FileNotFoundException();
}
