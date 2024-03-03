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

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Dalamud.Interface.Textures.Internal;

/// <summary>Service responsible for loading and disposing ImGui texture wraps.</summary>
[PluginInterface]
[InterfaceVersion("1.0")]
[ServiceManager.EarlyLoadedService]
#pragma warning disable SA1015
[ResolveVia<ITextureProvider>]
[ResolveVia<ITextureSubstitutionProvider>]
#pragma warning restore SA1015
internal sealed partial class TextureManager
    : IServiceType, IDisposable, ITextureProvider, ITextureSubstitutionProvider, ITextureReadbackProvider
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

    /// <summary>Gets the D3D11 Device used to create textures. Ownership is not transferred.</summary>
    public unsafe ComPtr<ID3D11Device> Device
    {
        get
        {
            if (this.interfaceManager.Scene is not { } scene)
            {
                _ = Service<InterfaceManager.InterfaceManagerWithScene>.Get();
                scene = this.interfaceManager.Scene ?? throw new InvalidOperationException();
            }

            var device = default(ComPtr<ID3D11Device>);
            device.Attach((ID3D11Device*)scene.Device.NativePointer);
            return device;
        }
    }

    /// <summary>Gets a simpler drawer.</summary>
    public SimpleDrawerImpl SimpleDrawer =>
        this.simpleDrawer ?? throw new ObjectDisposedException(nameof(TextureManager));

    /// <summary>Gets the shared texture manager.</summary>
    public SharedTextureManager Shared =>
        this.sharedTextureManager ??
        throw new ObjectDisposedException(nameof(TextureManager));

    /// <summary>Gets the WIC manager.</summary>
    public WicManager Wic =>
        this.wicManager ??
        throw new ObjectDisposedException(nameof(TextureManager));

    /// <inheritdoc/>
    public void Dispose()
    {
        if (this.disposing)
            return;

        this.disposing = true;

        Interlocked.Exchange(ref this.simpleDrawer, null)?.Dispose();
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
    public unsafe bool IsDxgiFormatSupported(DXGI_FORMAT dxgiFormat)
    {
        D3D11_FORMAT_SUPPORT supported;
        if (this.Device.Get()->CheckFormatSupport(dxgiFormat, (uint*)&supported).FAILED)
            return false;

        const D3D11_FORMAT_SUPPORT required = D3D11_FORMAT_SUPPORT.D3D11_FORMAT_SUPPORT_TEXTURE2D;
        return (supported & required) == required;
    }

    /// <inheritdoc cref="ITextureProvider.CreateFromRaw"/>
    internal unsafe IDalamudTextureWrap NoThrottleCreateFromRaw(
        RawImageSpecification specs,
        ReadOnlySpan<byte> bytes)
    {
        var device = this.Device;

        var texd = new D3D11_TEXTURE2D_DESC
        {
            Width = (uint)specs.Width,
            Height = (uint)specs.Height,
            MipLevels = 1,
            ArraySize = 1,
            Format = (DXGI_FORMAT)specs.DxgiFormat,
            SampleDesc = new(1, 0),
            Usage = D3D11_USAGE.D3D11_USAGE_IMMUTABLE,
            BindFlags = (uint)D3D11_BIND_FLAG.D3D11_BIND_SHADER_RESOURCE,
            CPUAccessFlags = 0,
            MiscFlags = 0,
        };
        using var texture = default(ComPtr<ID3D11Texture2D>);
        fixed (void* dataPtr = bytes)
        {
            var subrdata = new D3D11_SUBRESOURCE_DATA { pSysMem = dataPtr, SysMemPitch = (uint)specs.Pitch };
            device.Get()->CreateTexture2D(&texd, &subrdata, texture.GetAddressOf()).ThrowOnError();
        }

        var viewDesc = new D3D11_SHADER_RESOURCE_VIEW_DESC
        {
            Format = texd.Format,
            ViewDimension = D3D_SRV_DIMENSION.D3D11_SRV_DIMENSION_TEXTURE2D,
            Texture2D = new() { MipLevels = texd.MipLevels },
        };
        using var view = default(ComPtr<ID3D11ShaderResourceView>);
        device.Get()->CreateShaderResourceView((ID3D11Resource*)texture.Get(), &viewDesc, view.GetAddressOf())
            .ThrowOnError();

        return new UnknownTextureWrap((IUnknown*)view.Get(), specs.Width, specs.Height, true);
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
            dxgiFormat = (int)DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM;
            buffer = buffer.Filter(0, 0, TexFile.TextureFormat.B8G8R8A8);
        }

        return this.NoThrottleCreateFromRaw(
            new(buffer.Width, buffer.Height, dxgiFormat),
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
