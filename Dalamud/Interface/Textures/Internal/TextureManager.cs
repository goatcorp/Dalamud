using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Textures.Internal.SharedImmediateTextures;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Services;
using Dalamud.Utility;

using Lumina.Data;
using Lumina.Data.Files;

using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

using TerraFX.Interop.DirectX;

namespace Dalamud.Interface.Textures.Internal;

/// <summary>Service responsible for loading and disposing ImGui texture wraps.</summary>
[PluginInterface]
[InterfaceVersion("1.0")]
[ServiceManager.EarlyLoadedService]
#pragma warning disable SA1015
[ResolveVia<ITextureProvider>]
[ResolveVia<ITextureSubstitutionProvider>]
#pragma warning restore SA1015
internal sealed partial class TextureManager : IServiceType, IDisposable, ITextureProvider, ITextureSubstitutionProvider
{
    private static readonly ModuleLog Log = new(nameof(TextureManager));

    [ServiceManager.ServiceDependency]
    private readonly Dalamud dalamud = Service<Dalamud>.Get();

    [ServiceManager.ServiceDependency]
    private readonly DataManager dataManager = Service<DataManager>.Get();

    [ServiceManager.ServiceDependency]
    private readonly Framework framework = Service<Framework>.Get();

    [ServiceManager.ServiceDependency]
    private readonly InterfaceManager interfaceManager = Service<InterfaceManager>.Get();

    [ServiceManager.ServiceDependency]
    private readonly TextureLoadThrottler textureLoadThrottler = Service<TextureLoadThrottler>.Get();

    private SharedTextureManager? sharedTextureManager;
    private WicManager? wicManager;
    private bool disposing;

    [SuppressMessage(
        "StyleCop.CSharp.LayoutRules",
        "SA1519:Braces should not be omitted from multi-line child statement",
        Justification = "Multiple fixed blocks")]
    [ServiceManager.ServiceConstructor]
    private TextureManager()
    {
        this.sharedTextureManager = new(this);
        this.wicManager = new(this);
    }

    /// <summary>Gets the shared texture manager.</summary>
    public SharedTextureManager Shared =>
        this.sharedTextureManager ??
        throw new ObjectDisposedException(nameof(this.sharedTextureManager));

    /// <summary>Gets the WIC manager.</summary>
    public WicManager Wic =>
        this.wicManager ??
        throw new ObjectDisposedException(nameof(this.sharedTextureManager));

    /// <inheritdoc/>
    public void Dispose()
    {
        if (this.disposing)
            return;

        this.disposing = true;

        this.drawsOneSquare?.Dispose();
        this.drawsOneSquare = null;

        Interlocked.Exchange(ref this.sharedTextureManager, null)?.Dispose();
        Interlocked.Exchange(ref this.wicManager, null)?.Dispose();
    }

    /// <inheritdoc/>
    public Task<IDalamudTextureWrap> CreateFromImageAsync(
        ReadOnlyMemory<byte> bytes,
        CancellationToken cancellationToken = default) =>
        this.textureLoadThrottler.LoadTextureAsync(
            new TextureLoadThrottler.ReadOnlyThrottleBasisProvider(),
            ct => Task.Run(() => this.NoThrottleCreateFromImage(bytes.ToArray(), ct), ct),
            cancellationToken);

    /// <inheritdoc/>
    public Task<IDalamudTextureWrap> CreateFromImageAsync(
        Stream stream,
        bool leaveOpen = false,
        CancellationToken cancellationToken = default) =>
        this.textureLoadThrottler.LoadTextureAsync(
                new TextureLoadThrottler.ReadOnlyThrottleBasisProvider(),
                async ct =>
                {
                    await using var ms = stream.CanSeek ? new MemoryStream((int)stream.Length) : new();
                    await stream.CopyToAsync(ms, ct).ConfigureAwait(false);
                    return this.NoThrottleCreateFromImage(ms.GetBuffer(), ct);
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
    // It probably doesn't make sense to throttle this, as it copies the passed bytes to GPU without any transformation.
    public IDalamudTextureWrap CreateFromRaw(
        RawImageSpecification specs,
        ReadOnlySpan<byte> bytes) => this.NoThrottleCreateFromRaw(specs, bytes);

    /// <inheritdoc/>
    public Task<IDalamudTextureWrap> CreateFromRawAsync(
        RawImageSpecification specs,
        ReadOnlyMemory<byte> bytes,
        CancellationToken cancellationToken = default) =>
        this.textureLoadThrottler.LoadTextureAsync(
            new TextureLoadThrottler.ReadOnlyThrottleBasisProvider(),
            _ => Task.FromResult(this.NoThrottleCreateFromRaw(specs, bytes.Span)),
            cancellationToken);

    /// <inheritdoc/>
    public Task<IDalamudTextureWrap> CreateFromRawAsync(
        RawImageSpecification specs,
        Stream stream,
        bool leaveOpen = false,
        CancellationToken cancellationToken = default) =>
        this.textureLoadThrottler.LoadTextureAsync(
                new TextureLoadThrottler.ReadOnlyThrottleBasisProvider(),
                async ct =>
                {
                    await using var ms = stream.CanSeek ? new MemoryStream((int)stream.Length) : new();
                    await stream.CopyToAsync(ms, ct).ConfigureAwait(false);
                    return this.NoThrottleCreateFromRaw(specs, ms.GetBuffer().AsSpan(0, (int)ms.Length));
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
        this.textureLoadThrottler.LoadTextureAsync(
            new TextureLoadThrottler.ReadOnlyThrottleBasisProvider(),
            ct => Task.Run(() => this.NoThrottleCreateFromTexFile(file), ct),
            cancellationToken);

    /// <inheritdoc/>
    bool ITextureProvider.IsDxgiFormatSupported(int dxgiFormat) =>
        this.IsDxgiFormatSupported((DXGI_FORMAT)dxgiFormat);

    /// <inheritdoc cref="ITextureProvider.IsDxgiFormatSupported"/>
    public bool IsDxgiFormatSupported(DXGI_FORMAT dxgiFormat)
    {
        if (this.interfaceManager.Scene is not { } scene)
        {
            _ = Service<InterfaceManager.InterfaceManagerWithScene>.Get();
            scene = this.interfaceManager.Scene ?? throw new InvalidOperationException();
        }

        var format = (Format)dxgiFormat;
        var support = scene.Device.CheckFormatSupport(format);
        const FormatSupport required = FormatSupport.Texture2D;
        return (support & required) == required;
    }

    /// <inheritdoc cref="ITextureProvider.CreateFromRaw"/>
    internal IDalamudTextureWrap NoThrottleCreateFromRaw(
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

    /// <summary>Creates a texture from the given <see cref="TexFile"/>. Skips the load throttler; intended to be used
    /// from implementation of <see cref="SharedImmediateTexture"/>s.</summary>
    /// <param name="file">The data.</param>
    /// <returns>The loaded texture.</returns>
    internal IDalamudTextureWrap NoThrottleCreateFromTexFile(TexFile file)
    {
        ObjectDisposedException.ThrowIf(this.disposing, this);

        var buffer = file.TextureBuffer;
        var (dxgiFormat, conversion) = TexFile.GetDxgiFormatFromTextureFormat(file.Header.Format, false);
        if (conversion != TexFile.DxgiFormatConversion.NoConversion ||
            !this.IsDxgiFormatSupported((DXGI_FORMAT)dxgiFormat))
        {
            dxgiFormat = (int)Format.B8G8R8A8_UNorm;
            buffer = buffer.Filter(0, 0, TexFile.TextureFormat.B8G8R8A8);
        }

        return this.NoThrottleCreateFromRaw(
            RawImageSpecification.From(buffer.Width, buffer.Height, dxgiFormat),
            buffer.RawData);
    }

    /// <summary>Creates a texture from the given <paramref name="fileBytes"/>, trying to interpret it as a
    /// <see cref="TexFile"/>.</summary>
    /// <param name="fileBytes">The file bytes.</param>
    /// <returns>The loaded texture.</returns>
    internal IDalamudTextureWrap NoThrottleCreateFromTexFile(ReadOnlySpan<byte> fileBytes)
    {
        ObjectDisposedException.ThrowIf(this.disposing, this);

        if (!TexFileExtensions.IsPossiblyTexFile2D(fileBytes))
            throw new InvalidDataException("The file is not a TexFile.");

        var bytesArray = fileBytes.ToArray();
        var tf = new TexFile();
        typeof(TexFile).GetProperty(nameof(tf.Data))!.GetSetMethod(true)!.Invoke(
            tf,
            new object?[] { bytesArray });
        typeof(TexFile).GetProperty(nameof(tf.Reader))!.GetSetMethod(true)!.Invoke(
            tf,
            new object?[] { new LuminaBinaryReader(bytesArray) });
        // Note: FileInfo and FilePath are not used from TexFile; skip it.
        return this.NoThrottleCreateFromTexFile(tf);
    }
}
