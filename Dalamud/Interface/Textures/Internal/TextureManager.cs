using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Configuration.Internal;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Interface.ImGuiSeStringRenderer.Internal;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Textures.Internal.SharedImmediateTextures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Textures.TextureWraps.Internal;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Plugin.Services;
using Dalamud.Storage.Assets;
using Dalamud.Utility;
using Dalamud.Utility.TerraFxCom;
using Lumina.Data;
using Lumina.Data.Files;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

using static TerraFX.Interop.Windows.COINIT;
using static TerraFX.Interop.Windows.SIIGBF;
using static TerraFX.Interop.Windows.Windows;

using BitmapData = System.Drawing.Imaging.BitmapData;
using HBITMAP = TerraFX.Interop.Windows.HBITMAP;
using ImageLockMode = System.Drawing.Imaging.ImageLockMode;
using IShellItemImageFactory = TerraFX.Interop.Windows.IShellItemImageFactory;
using SIZE = TerraFX.Interop.Windows.SIZE;

namespace Dalamud.Interface.Textures.Internal;

/// <summary>Service responsible for loading and disposing ImGui texture wraps.</summary>
[ServiceManager.EarlyLoadedService]
internal sealed partial class TextureManager
    : IInternalDisposableService,
      ITextureProvider,
      ITextureSubstitutionProvider,
      ITextureReadbackProvider
{
    private static readonly ModuleLog Log = ModuleLog.Create<TextureManager>();

    [ServiceManager.ServiceDependency]
    private readonly Dalamud dalamud = Service<Dalamud>.Get();

    [ServiceManager.ServiceDependency]
    private readonly DalamudConfiguration dalamudConfiguration = Service<DalamudConfiguration>.Get();

    [ServiceManager.ServiceDependency]
    private readonly DataManager dataManager = Service<DataManager>.Get();

    [ServiceManager.ServiceDependency]
    private readonly Framework framework = Service<Framework>.Get();

    [ServiceManager.ServiceDependency]
    private readonly InterfaceManager interfaceManager = Service<InterfaceManager>.Get();

    private readonly CancellationTokenSource disposeCts = new();

    private DynamicPriorityQueueLoader? dynamicPriorityTextureLoader;
    private SharedTextureManager? sharedTextureManager;
    private WicManager? wicManager;
    private ComPtr<ID3D11Device> device;

    [ServiceManager.ServiceConstructor]
    private unsafe TextureManager(InterfaceManager.InterfaceManagerWithScene withScene)
    {
        using var failsafe = new DisposeSafety.ScopedFinalizer();
        failsafe.Add(this.device = new((ID3D11Device*)withScene.Manager.Backend!.DeviceHandle));
        failsafe.Add(this.dynamicPriorityTextureLoader = new(Math.Max(1, Environment.ProcessorCount - 1)));
        failsafe.Add(this.sharedTextureManager = new(this));
        failsafe.Add(this.wicManager = new(this));
        failsafe.Add(this.simpleDrawer = new());
        this.framework.Update += this.BlameTrackerUpdate;
        failsafe.Add(() => this.framework.Update -= this.BlameTrackerUpdate);
        this.simpleDrawer.Setup(this.device.Get());

        failsafe.Cancel();
    }

    /// <summary>Finalizes an instance of the <see cref="TextureManager"/> class.</summary>
    ~TextureManager() => this.ReleaseUnmanagedResources();

    /// <summary>Gets the dynamic-priority queue texture loader.</summary>
    public DynamicPriorityQueueLoader DynamicPriorityTextureLoader
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.dynamicPriorityTextureLoader ?? throw new ObjectDisposedException(nameof(TextureManager));
    }

    /// <summary>Gets a simpler drawer.</summary>
    public SimpleDrawerImpl SimpleDrawer
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.simpleDrawer ?? throw new ObjectDisposedException(nameof(TextureManager));
    }

    /// <summary>Gets the shared texture manager.</summary>
    public SharedTextureManager Shared
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.sharedTextureManager ?? throw new ObjectDisposedException(nameof(TextureManager));
    }

    /// <summary>Gets the WIC manager.</summary>
    public WicManager Wic
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.wicManager ?? throw new ObjectDisposedException(nameof(TextureManager));
    }

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        if (this.disposeCts.IsCancellationRequested)
            return;

        this.disposeCts.Cancel();

        Interlocked.Exchange(ref this.dynamicPriorityTextureLoader, null)?.Dispose();
        Interlocked.Exchange(ref this.simpleDrawer, null)?.Dispose();
        Interlocked.Exchange(ref this.sharedTextureManager, null)?.Dispose();
        Interlocked.Exchange(ref this.wicManager, null)?.Dispose();
        this.ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    public Task<IDalamudTextureWrap> CreateFromImageAsync(
        ReadOnlyMemory<byte> bytes,
        string? debugName = null,
        CancellationToken cancellationToken = default) =>
        this.DynamicPriorityTextureLoader.LoadAsync(
            null,
            ct => Task.Run(
                () =>
                    this.BlameSetName(
                        this.NoThrottleCreateFromImage(bytes.ToArray(), ct),
                        debugName ??
                        $"{nameof(this.CreateFromImageAsync)}({bytes.Length:n0}b)"),
                ct),
            cancellationToken);

    /// <inheritdoc/>
    public Task<IDalamudTextureWrap> CreateFromImageAsync(
        Stream stream,
        bool leaveOpen = false,
        string? debugName = null,
        CancellationToken cancellationToken = default) =>
        this.DynamicPriorityTextureLoader.LoadAsync(
            null,
            async ct =>
            {
                await using var ms = stream.CanSeek ? new MemoryStream((int)stream.Length) : new();
                await stream.CopyToAsync(ms, ct).ConfigureAwait(false);
                return this.BlameSetName(
                    this.NoThrottleCreateFromImage(ms.GetBuffer(), ct),
                    debugName ??
                    $"{nameof(this.CreateFromImageAsync)}(stream)");
            },
            cancellationToken,
            leaveOpen ? null : stream);

    /// <inheritdoc/>
    // It probably doesn't make sense to throttle this, as it copies the passed bytes to GPU without any transformation.
    public IDalamudTextureWrap CreateFromRaw(
        RawImageSpecification specs,
        ReadOnlySpan<byte> bytes,
        string? debugName = null) =>
        this.BlameSetName(
            this.NoThrottleCreateFromRaw(specs, bytes),
            debugName ?? $"{nameof(this.CreateFromRaw)}({specs}, {bytes.Length:n0})");

    /// <inheritdoc/>
    public Task<IDalamudTextureWrap> CreateFromRawAsync(
        RawImageSpecification specs,
        ReadOnlyMemory<byte> bytes,
        string? debugName = null,
        CancellationToken cancellationToken = default) =>
        this.DynamicPriorityTextureLoader.LoadAsync(
            null,
            _ => Task.FromResult(
                this.BlameSetName(
                    this.NoThrottleCreateFromRaw(specs, bytes.Span),
                    debugName ??
                    $"{nameof(this.CreateFromRawAsync)}({specs}, {bytes.Length:n0})")),
            cancellationToken);

    /// <inheritdoc/>
    public Task<IDalamudTextureWrap> CreateFromRawAsync(
        RawImageSpecification specs,
        Stream stream,
        bool leaveOpen = false,
        string? debugName = null,
        CancellationToken cancellationToken = default) =>
        this.DynamicPriorityTextureLoader.LoadAsync(
            null,
            async ct =>
            {
                await using var ms = stream.CanSeek ? new MemoryStream((int)stream.Length) : new();
                await stream.CopyToAsync(ms, ct).ConfigureAwait(false);
                return this.BlameSetName(
                    this.NoThrottleCreateFromRaw(specs, ms.GetBuffer().AsSpan(0, (int)ms.Length)),
                    debugName ??
                    $"{nameof(this.CreateFromRawAsync)}({specs}, stream)");
            },
            cancellationToken,
            leaveOpen ? null : stream);

    /// <inheritdoc/>
    public IDalamudTextureWrap CreateFromTexFile(TexFile file) =>
        this.BlameSetName(
            this.CreateFromTexFileAsync(file).Result,
            $"{nameof(this.CreateFromTexFile)}({nameof(file)})");

    /// <inheritdoc/>
    public Task<IDalamudTextureWrap> CreateFromTexFileAsync(
        TexFile file,
        string? debugName = null,
        CancellationToken cancellationToken = default)
    {
        return this.DynamicPriorityTextureLoader.LoadAsync(
            null,
            _ => Task.FromResult(
                this.BlameSetName(
                    this.NoThrottleCreateFromTexFile(file),
                    debugName ?? $"{nameof(this.CreateFromTexFile)}({ForceNullable(file.FilePath)?.Path})")),
            cancellationToken);

        static T? ForceNullable<T>(T s) => s;
    }

    /// <inheritdoc/>
    public unsafe IDalamudTextureWrap CreateEmpty(
        RawImageSpecification specs,
        bool cpuRead,
        bool cpuWrite,
        string? debugName = null)
    {
        if (cpuRead && cpuWrite)
            throw new ArgumentException("cpuRead and cpuWrite cannot be set at the same time.");

        var cpuaf = default(D3D11_CPU_ACCESS_FLAG);
        if (cpuRead)
            cpuaf |= D3D11_CPU_ACCESS_FLAG.D3D11_CPU_ACCESS_READ;
        if (cpuWrite)
            cpuaf |= D3D11_CPU_ACCESS_FLAG.D3D11_CPU_ACCESS_WRITE;

        D3D11_USAGE usage;
        if (cpuRead)
            usage = D3D11_USAGE.D3D11_USAGE_STAGING;
        else if (cpuWrite)
            usage = D3D11_USAGE.D3D11_USAGE_DYNAMIC;
        else
            usage = D3D11_USAGE.D3D11_USAGE_DEFAULT;

        using var texture = this.device.CreateTexture2D(
            new()
            {
                Width = (uint)specs.Width,
                Height = (uint)specs.Height,
                MipLevels = 1,
                ArraySize = 1,
                Format = specs.Format,
                SampleDesc = new(1, 0),
                Usage = usage,
                BindFlags = (uint)D3D11_BIND_FLAG.D3D11_BIND_SHADER_RESOURCE,
                CPUAccessFlags = (uint)cpuaf,
                MiscFlags = 0,
            });
        using var view = this.device.CreateShaderResourceView(
            texture,
            new(texture, D3D_SRV_DIMENSION.D3D11_SRV_DIMENSION_TEXTURE2D));

        var wrap = new UnknownTextureWrap((IUnknown*)view.Get(), specs.Width, specs.Height, true);
        this.BlameSetName(wrap, debugName ?? $"{nameof(this.CreateEmpty)}({specs})");
        return wrap;
    }

    /// <inheritdoc/>
    public IDrawListTextureWrap CreateDrawListTexture(string? debugName = null) => this.CreateDrawListTexture(null, debugName);

    /// <summary><inheritdoc cref="CreateDrawListTexture(string?)" path="/summary"/></summary>
    /// <param name="plugin">Plugin that created the draw list.</param>
    /// <param name="debugName"><inheritdoc cref="CreateDrawListTexture(string?)" path="/param[name=debugName]"/></param>
    /// <returns><inheritdoc cref="CreateDrawListTexture(string?)" path="/returns"/></returns>
    public IDrawListTextureWrap CreateDrawListTexture(LocalPlugin? plugin, string? debugName = null) =>
        new DrawListTextureWrap(
            new(this.device),
            this,
            Service<DalamudAssetManager>.Get().Empty4X4,
            plugin,
            debugName ?? $"{nameof(this.CreateDrawListTexture)}");

    /// <inheritdoc/>
    bool ITextureProvider.IsDxgiFormatSupported(int dxgiFormat) =>
        this.IsDxgiFormatSupported((DXGI_FORMAT)dxgiFormat);

    /// <inheritdoc cref="ITextureProvider.IsDxgiFormatSupported"/>
    public unsafe bool IsDxgiFormatSupported(DXGI_FORMAT dxgiFormat)
    {
        D3D11_FORMAT_SUPPORT supported;
        if (this.device.Get()->CheckFormatSupport(dxgiFormat, (uint*)&supported).FAILED)
            return false;

        const D3D11_FORMAT_SUPPORT required = D3D11_FORMAT_SUPPORT.D3D11_FORMAT_SUPPORT_TEXTURE2D;
        return (supported & required) == required;
    }

    /// <inheritdoc/>
    public unsafe IDalamudTextureWrap? TryGetFileThumbnail(string path)
    {
        const int size = 256;
        HRESULT hr = CoInitializeEx(null, (uint)(COINIT_DISABLE_OLE1DDE | COINIT_MULTITHREADED));
        if (hr.FAILED)
            return null;

        IShellItemImageFactory* imageFact;
        fixed (char* pstr = path)
        {
            hr = SHCreateItemFromParsingName(pstr, null, __uuidof<IShellItemImageFactory>(), (void**)&imageFact);
        }

        if (hr.FAILED)
            return null;

        HBITMAP hbmp;
        imageFact->GetImage(new SIZE(cx: size, cy: size), (int)SIIGBF_BIGGERSIZEOK, &hbmp);
        var image = Image.FromHbitmap(hbmp);
        BitmapData bd = image.LockBits(new(Point.Empty, image.Size), ImageLockMode.ReadOnly, image.PixelFormat);
        var rowSize = bd.Stride < 0 ? -bd.Stride : bd.Stride;
        var pixels = new byte[bd.Height * rowSize];
        var iptr = bd.Scan0;

        for (int y = 0; y < bd.Height; y++)
        {
            Marshal.Copy(IntPtr.Add(iptr, y * bd.Stride),
                         pixels, y * rowSize,
                         rowSize);
        }

        image.UnlockBits(bd);
        return this.CreateFromRaw(new RawImageSpecification(bd.Width, bd.Height, (int)DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM), pixels.AsSpan());
    }

    /// <inheritdoc cref="ITextureProvider.CreateFromRaw"/>
    internal unsafe IDalamudTextureWrap NoThrottleCreateFromRaw(
        RawImageSpecification specs,
        ReadOnlySpan<byte> bytes)
    {
        var texd = new D3D11_TEXTURE2D_DESC
        {
            Width = (uint)specs.Width,
            Height = (uint)specs.Height,
            MipLevels = 1,
            ArraySize = 1,
            Format = specs.Format,
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
            this.device.Get()->CreateTexture2D(&texd, &subrdata, texture.GetAddressOf()).ThrowOnError();
        }

        var viewDesc = new D3D11_SHADER_RESOURCE_VIEW_DESC
        {
            Format = texd.Format,
            ViewDimension = D3D_SRV_DIMENSION.D3D11_SRV_DIMENSION_TEXTURE2D,
            Texture2D = new() { MipLevels = texd.MipLevels },
        };
        using var view = default(ComPtr<ID3D11ShaderResourceView>);
        this.device.Get()->CreateShaderResourceView((ID3D11Resource*)texture.Get(), &viewDesc, view.GetAddressOf())
            .ThrowOnError();

        var wrap = new UnknownTextureWrap((IUnknown*)view.Get(), specs.Width, specs.Height, true);
        this.BlameSetName(wrap, $"{nameof(this.NoThrottleCreateFromRaw)}({specs}, {bytes.Length:n0})");
        return wrap;
    }

    /// <summary>Creates a texture from the given <see cref="TexFile"/>. Skips the load throttler; intended to be used
    /// from implementation of <see cref="SharedImmediateTexture"/>s.</summary>
    /// <param name="file">The data.</param>
    /// <returns>The loaded texture.</returns>
    internal IDalamudTextureWrap NoThrottleCreateFromTexFile(TexFile file)
    {
        ObjectDisposedException.ThrowIf(this.disposeCts.IsCancellationRequested, this);

        var buffer = file.TextureBuffer;
        var (dxgiFormat, conversion) = TexFile.GetDxgiFormatFromTextureFormat(file.Header.Format, false);
        if (conversion != TexFile.DxgiFormatConversion.NoConversion ||
            !this.IsDxgiFormatSupported((DXGI_FORMAT)dxgiFormat))
        {
            dxgiFormat = (int)DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM;
            buffer = buffer.Filter(0, 0, TexFile.TextureFormat.B8G8R8A8);
        }

        var wrap = this.NoThrottleCreateFromRaw(new(buffer.Width, buffer.Height, dxgiFormat), buffer.RawData);
        this.BlameSetName(wrap, $"{nameof(this.NoThrottleCreateFromTexFile)}({ForceNullable(file.FilePath).Path})");
        return wrap;

        static T? ForceNullable<T>(T s) => s;
    }

    /// <summary>Creates a texture from the given <paramref name="fileBytes"/>, trying to interpret it as a
    /// <see cref="TexFile"/>.</summary>
    /// <param name="fileBytes">The file bytes.</param>
    /// <returns>The loaded texture.</returns>
    internal IDalamudTextureWrap NoThrottleCreateFromTexFile(ReadOnlySpan<byte> fileBytes)
    {
        ObjectDisposedException.ThrowIf(this.disposeCts.IsCancellationRequested, this);

        if (!TexFileExtensions.IsPossiblyTexFile2D(fileBytes))
            throw new InvalidDataException("The file is not a TexFile.");

        var bytesArray = fileBytes.ToArray();
        var tf = new TexFile();
        typeof(TexFile).GetProperty(nameof(tf.Data))!.GetSetMethod(true)!.Invoke(
            tf,
            [bytesArray]);
        typeof(TexFile).GetProperty(nameof(tf.Reader))!.GetSetMethod(true)!.Invoke(
            tf,
            [new LuminaBinaryReader(bytesArray)]);
        // Note: FileInfo and FilePath are not used from TexFile; skip it.

        var wrap = this.NoThrottleCreateFromTexFile(tf);
        this.BlameSetName(wrap, $"{nameof(this.NoThrottleCreateFromTexFile)}({fileBytes.Length:n0})");
        return wrap;
    }

    private void ReleaseUnmanagedResources() => this.device.Reset();

    /// <summary>Runs the given action in IDXGISwapChain.Present immediately or waiting as needed.</summary>
    /// <param name="action">The action to run.</param>
    // Not sure why this and the below can't be unconditional RunOnFrameworkThread
    private async Task RunDuringPresent(Action action)
    {
        if (this.interfaceManager.IsMainThreadInPresent && ThreadSafety.IsMainThread)
            action();
        else
            await this.interfaceManager.RunBeforeImGuiRender(action);
    }

    /// <summary>Runs the given function in IDXGISwapChain.Present immediately or waiting as needed.</summary>
    /// <typeparam name="T">The type of the return value.</typeparam>
    /// <param name="func">The function to run.</param>
    /// <returns>The return value from the function.</returns>
    private async Task<T> RunDuringPresent<T>(Func<T> func)
    {
        if (this.interfaceManager.IsMainThreadInPresent && ThreadSafety.IsMainThread)
            return func();
        return await this.interfaceManager.RunBeforeImGuiRender(func);
    }
}
