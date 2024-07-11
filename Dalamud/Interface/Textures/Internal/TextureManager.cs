using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Data;
using Dalamud.Game;
using Dalamud.ImGuiScene;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Textures.Internal.SharedImmediateTextures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Services;
using Dalamud.Utility;

using Lumina.Data;
using Lumina.Data.Files;

using TerraFX.Interop.DirectX;

namespace Dalamud.Interface.Textures.Internal;

/// <summary>Service responsible for loading and disposing ImGui texture wraps.</summary>
[ServiceManager.EarlyLoadedService]
internal sealed partial class TextureManager
    : IInternalDisposableService,
      ITextureProvider,
      ITextureSubstitutionProvider,
      ITextureReadbackProvider
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

    private DynamicPriorityQueueLoader? dynamicPriorityTextureLoader;
    private SharedTextureManager? sharedTextureManager;
    private WicManager? wicManager;
    private bool disposing;

    [ServiceManager.ServiceConstructor]
    private TextureManager(InterfaceManager.InterfaceManagerWithScene withScene)
    {
        using var failsafe = new DisposeSafety.ScopedFinalizer();
        failsafe.Add(this.dynamicPriorityTextureLoader = new(Math.Max(1, Environment.ProcessorCount - 1)));
        failsafe.Add(this.sharedTextureManager = new(this));
        failsafe.Add(this.wicManager = new(this));

        failsafe.Cancel();
    }

    /// <summary>Gets the dynamic-priority queue texture loader.</summary>
    public DynamicPriorityQueueLoader DynamicPriorityTextureLoader
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.dynamicPriorityTextureLoader ?? throw new ObjectDisposedException(nameof(TextureManager));
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

    /// <summary>Gets the active instance of <see cref="IImGuiScene"/>.</summary>
    internal IImGuiScene Scene => this.interfaceManager.Scene ?? throw new InvalidOperationException("Not yet ready.");

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        if (this.disposing)
            return;

        this.disposing = true;

        Interlocked.Exchange(ref this.dynamicPriorityTextureLoader, null)?.Dispose();
        Interlocked.Exchange(ref this.sharedTextureManager, null)?.Dispose();
        Interlocked.Exchange(ref this.wicManager, null)?.Dispose();
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
                    this.NoThrottleCreateFromImage(
                        bytes.ToArray(),
                        debugName ?? $"{nameof(this.CreateFromImageAsync)}({bytes.Length:n0}b)",
                        ct),
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
                return this.NoThrottleCreateFromImage(
                    ms.GetBuffer(),
                    debugName ?? $"{nameof(this.CreateFromImageAsync)}(stream)",
                    ct);
            },
            cancellationToken,
            leaveOpen ? null : stream);

    /// <inheritdoc/>
    // It probably doesn't make sense to throttle this, as it copies the passed bytes to GPU without any transformation.
    public IDalamudTextureWrap CreateFromRaw(
        RawImageSpecification specs,
        ReadOnlySpan<byte> bytes,
        string? debugName = null) =>
        this.NoThrottleCreateFromRaw(
            specs,
            bytes,
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
                this.NoThrottleCreateFromRaw(
                    specs,
                    bytes.Span,
                    debugName ?? $"{nameof(this.CreateFromRawAsync)}({specs}, {bytes.Length:n0})")),
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
                return this.NoThrottleCreateFromRaw(
                    specs,
                    ms.GetBuffer().AsSpan(0, (int)ms.Length),
                    debugName ?? $"{nameof(this.CreateFromRawAsync)}({specs}, stream)");
            },
            cancellationToken,
            leaveOpen ? null : stream);

    /// <inheritdoc/>
    public IDalamudTextureWrap CreateFromTexFile(TexFile file) =>
        this.CreateFromTexFileAsync(file, $"{nameof(this.CreateFromTexFile)}({nameof(file)})").Result;

    /// <inheritdoc/>
    public Task<IDalamudTextureWrap> CreateFromTexFileAsync(
        TexFile file,
        string? debugName = null,
        CancellationToken cancellationToken = default)
    {
        return this.DynamicPriorityTextureLoader.LoadAsync(
            null,
            _ => Task.FromResult(
                this.NoThrottleCreateFromTexFile(
                    file,
                    debugName ?? $"{nameof(this.CreateFromTexFile)}({ForceNullable(file.FilePath)?.Path})")),
            cancellationToken);

        static T? ForceNullable<T>(T s) => s;
    }

    /// <inheritdoc/>
    public IDalamudTextureWrap CreateEmpty(
        RawImageSpecification specs,
        bool cpuRead,
        bool cpuWrite,
        string? debugName = null) =>
        this.Scene.CreateTexture2D(
            default,
            specs,
            cpuRead,
            cpuWrite,
            true,
            debugName ?? $"{nameof(this.CreateEmpty)}({specs})");

    /// <inheritdoc/>
    bool ITextureProvider.IsDxgiFormatSupported(int dxgiFormat) =>
        this.IsDxgiFormatSupported((DXGI_FORMAT)dxgiFormat);

    /// <inheritdoc cref="ITextureProvider.IsDxgiFormatSupported"/>
    public bool IsDxgiFormatSupported(DXGI_FORMAT dxgiFormat)
    {
        if (this.interfaceManager.Scene is not { } scene)
            throw new InvalidOperationException("Not yet ready.");
        return scene.SupportsTextureFormat((int)dxgiFormat);
    }

    /// <inheritdoc cref="ITextureProvider.CreateFromRaw"/>
    internal IDalamudTextureWrap NoThrottleCreateFromRaw(
        RawImageSpecification specs,
        ReadOnlySpan<byte> bytes,
        string? debugName = null) =>
        this.Scene.CreateTexture2D(
            bytes,
            specs,
            false,
            false,
            false,
            debugName ?? $"{nameof(this.NoThrottleCreateFromRaw)}({specs}, {bytes.Length:n0})");

    /// <summary>Creates a texture from the given <see cref="TexFile"/>. Skips the load throttler; intended to be used
    /// from implementation of <see cref="SharedImmediateTexture"/>s.</summary>
    /// <param name="file">The data.</param>
    /// <param name="debugName">Name for debugging.</param>
    /// <returns>The loaded texture.</returns>
    internal IDalamudTextureWrap NoThrottleCreateFromTexFile(TexFile file, string? debugName = null)
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

        var wrap = this.NoThrottleCreateFromRaw(
            new(buffer.Width, buffer.Height, dxgiFormat),
            buffer.RawData,
            debugName ?? $"{nameof(this.NoThrottleCreateFromTexFile)}({ForceNullable(file.FilePath).Path})");
        return wrap;

        static T? ForceNullable<T>(T s) => s;
    }

    /// <summary>Creates a texture from the given <paramref name="fileBytes"/>, trying to interpret it as a
    /// <see cref="TexFile"/>.</summary>
    /// <param name="fileBytes">The file bytes.</param>
    /// <param name="debugName">Name for debugging.</param>
    /// <returns>The loaded texture.</returns>
    internal IDalamudTextureWrap NoThrottleCreateFromTexFile(ReadOnlySpan<byte> fileBytes, string? debugName = null)
    {
        ObjectDisposedException.ThrowIf(this.disposing, this);

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

        return this.NoThrottleCreateFromTexFile(
            tf,
            debugName ?? $"{nameof(this.NoThrottleCreateFromTexFile)}({fileBytes.Length:n0})");
    }

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
