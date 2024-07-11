using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Interface.Internal;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Plugin.Internal.Types.Manifest;
using Dalamud.Plugin.Services;
using Dalamud.Utility;

using Lumina.Data.Files;

using TerraFX.Interop.DirectX;

namespace Dalamud.Interface.Textures.Internal;

/// <summary>Plugin-scoped version of <see cref="TextureManager"/>.</summary>
[PluginInterface]
[ServiceManager.ScopedService]
#pragma warning disable SA1015
[ResolveVia<ITextureProvider>]
[ResolveVia<ITextureSubstitutionProvider>]
[ResolveVia<ITextureReadbackProvider>]
#pragma warning restore SA1015
internal sealed class TextureManagerPluginScoped
    : IInternalDisposableService,
      ITextureProvider,
      ITextureSubstitutionProvider,
      ITextureReadbackProvider
{
    private readonly LocalPlugin plugin;
    private readonly bool nonAsyncFunctionAccessDuringLoadIsError;

    private Task<TextureManager>? managerTaskNullable;

    /// <summary>Initializes a new instance of the <see cref="TextureManagerPluginScoped"/> class.</summary>
    /// <param name="plugin">The plugin.</param>
    [ServiceManager.ServiceConstructor]
    public TextureManagerPluginScoped(LocalPlugin plugin)
    {
        this.plugin = plugin;
        if (plugin.Manifest is LocalPluginManifest lpm)
            this.nonAsyncFunctionAccessDuringLoadIsError = lpm.LoadSync && lpm.LoadRequiredState != 0;

        this.managerTaskNullable =
            Service<TextureManager>
                .GetAsync()
                .ContinueWith(
                    r =>
                    {
                        if (r.IsCompletedSuccessfully)
                            r.Result.InterceptTexDataLoad += this.ResultOnInterceptTexDataLoad;
                        return r;
                    })
                .Unwrap();
    }

    /// <inheritdoc/>
    public event ITextureSubstitutionProvider.TextureDataInterceptorDelegate? InterceptTexDataLoad;

    /// <summary>Gets the task resulting in an instance of <see cref="TextureManager"/>.</summary>
    /// <exception cref="ObjectDisposedException">Thrown if disposed.</exception>
    private Task<TextureManager> ManagerTask
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.managerTaskNullable ?? throw new ObjectDisposedException(this.ToString());
    }

    /// <summary>Gets an instance of <see cref="TextureManager"/>.</summary>
    /// <exception cref="ObjectDisposedException">Thrown if disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown if called at an unfortune time.</exception>
    private TextureManager ManagerOrThrow
    {
        get
        {
            var task = this.ManagerTask;

            // Check for IMWS too, as TextureManager is constructed after IMWS, and UiBuilder.RunWhenUiPrepared gets
            // resolved when IMWS is constructed.
            if (!task.IsCompleted && Service<InterfaceManager.InterfaceManagerWithScene>.GetNullable() is null)
            {
                if (this.nonAsyncFunctionAccessDuringLoadIsError && this.plugin.State != PluginState.Loaded)
                {
                    throw new InvalidOperationException(
                        "The function you've called will wait for the drawing facilities to be available, and as " +
                        "Dalamud is already waiting for your plugin to be fully constructed before even attempting " +
                        "to initialize the drawing facilities, calling this function will stall the game until and " +
                        "is forbidden until your plugin has been fully loaded.\n" +
                        $"Consider using {nameof(UiBuilder.RunWhenUiPrepared)} to wait for the right moment.\n" +
                        "\n" +
                        $"Note that your plugin has {nameof(LocalPluginManifest.LoadSync)} set and " +
                        $"{nameof(LocalPluginManifest.LoadRequiredState)} that is nonzero.");
                }

                if (ThreadSafety.IsMainThread)
                {
                    throw new InvalidOperationException(
                        "The function you've called will wait for the drawing facilities to be available, and as " +
                        "the drawing facilities are initialized from the main thread, calling this function will " +
                        "stall the game until and is forbidden until your plugin has been fully loaded.\n" +
                        $"Consider using {nameof(UiBuilder.RunWhenUiPrepared)} to wait for the right moment.");
                }
            }

            return task.Result;
        }
    }

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        if (Interlocked.Exchange(ref this.managerTaskNullable, null) is not { } task)
            return;
        task.ContinueWith(
            r =>
            {
                if (r.IsCompletedSuccessfully)
                    r.Result.InterceptTexDataLoad -= this.ResultOnInterceptTexDataLoad;
            });
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return this.managerTaskNullable is null
                   ? $"{nameof(TextureManagerPluginScoped)}({this.plugin.Name}, disposed)"
                   : $"{nameof(TextureManagerPluginScoped)}({this.plugin.Name})";
    }

    /// <inheritdoc/>
    public IDalamudTextureWrap CreateEmpty(
        RawImageSpecification specs,
        bool cpuRead,
        bool cpuWrite,
        string? debugName = null)
    {
        var manager = this.ManagerOrThrow;
        var textureWrap = manager.CreateEmpty(specs, cpuRead, cpuWrite, debugName);
        return textureWrap;
    }

    /// <inheritdoc/>
    public async Task<IDalamudTextureWrap> CreateFromExistingTextureAsync(
        IDalamudTextureWrap wrap,
        TextureModificationArgs args = default,
        bool leaveWrapOpen = false,
        string? debugName = null,
        CancellationToken cancellationToken = default)
    {
        var manager = await this.ManagerTask;
        var textureWrap = await manager.CreateFromExistingTextureAsync(
                              wrap,
                              args,
                              leaveWrapOpen,
                              debugName,
                              cancellationToken);
        return textureWrap;
    }

    /// <inheritdoc/>
    public async Task<IDalamudTextureWrap> CreateFromImGuiViewportAsync(
        ImGuiViewportTextureArgs args,
        string? debugName = null,
        CancellationToken cancellationToken = default)
    {
        var manager = await this.ManagerTask;
        var textureWrap = await manager.CreateFromImGuiViewportAsync(args, this.plugin, debugName, cancellationToken);
        return textureWrap;
    }

    /// <inheritdoc/>
    public async Task<IDalamudTextureWrap> CreateFromImageAsync(
        ReadOnlyMemory<byte> bytes,
        string? debugName = null,
        CancellationToken cancellationToken = default)
    {
        var manager = await this.ManagerTask;
        var textureWrap = await manager.CreateFromImageAsync(bytes, debugName, cancellationToken);
        return textureWrap;
    }

    /// <inheritdoc/>
    public async Task<IDalamudTextureWrap> CreateFromImageAsync(
        Stream stream,
        bool leaveOpen = false,
        string? debugName = null,
        CancellationToken cancellationToken = default)
    {
        var manager = await this.ManagerTask;
        var textureWrap = await manager.CreateFromImageAsync(stream, leaveOpen, debugName, cancellationToken);
        return textureWrap;
    }

    /// <inheritdoc/>
    public IDalamudTextureWrap CreateFromRaw(
        RawImageSpecification specs,
        ReadOnlySpan<byte> bytes,
        string? debugName = null)
    {
        var manager = this.ManagerOrThrow;
        var textureWrap = manager.CreateFromRaw(specs, bytes, debugName);
        return textureWrap;
    }

    /// <inheritdoc/>
    public async Task<IDalamudTextureWrap> CreateFromRawAsync(
        RawImageSpecification specs,
        ReadOnlyMemory<byte> bytes,
        string? debugName = null,
        CancellationToken cancellationToken = default)
    {
        var manager = await this.ManagerTask;
        var textureWrap = await manager.CreateFromRawAsync(specs, bytes, debugName, cancellationToken);
        return textureWrap;
    }

    /// <inheritdoc/>
    public async Task<IDalamudTextureWrap> CreateFromRawAsync(
        RawImageSpecification specs,
        Stream stream,
        bool leaveOpen = false,
        string? debugName = null,
        CancellationToken cancellationToken = default)
    {
        var manager = await this.ManagerTask;
        var textureWrap = await manager.CreateFromRawAsync(specs, stream, leaveOpen, debugName, cancellationToken);
        return textureWrap;
    }

    /// <inheritdoc/>
    public IDalamudTextureWrap CreateFromTexFile(TexFile file)
    {
        var manager = this.ManagerOrThrow;
        var textureWrap = manager.CreateFromTexFile(file);
        return textureWrap;
    }

    /// <inheritdoc/>
    public async Task<IDalamudTextureWrap> CreateFromTexFileAsync(
        TexFile file,
        string? debugName = null,
        CancellationToken cancellationToken = default)
    {
        var manager = await this.ManagerTask;
        var textureWrap = await manager.CreateFromTexFileAsync(file, debugName, cancellationToken);
        return textureWrap;
    }

    /// <inheritdoc/>
    public IEnumerable<IBitmapCodecInfo> GetSupportedImageDecoderInfos() =>
        this.ManagerOrThrow.Wic.GetSupportedDecoderInfos();

    /// <inheritdoc/>
    public ISharedImmediateTexture GetFromGameIcon(in GameIconLookup lookup)
    {
        var shared = this.ManagerOrThrow.Shared.GetFromGameIcon(lookup);
        shared.AddOwnerPlugin(this.plugin);
        return shared;
    }

    /// <inheritdoc/>
    public bool TryGetFromGameIcon(in GameIconLookup lookup, [NotNullWhen(true)] out ISharedImmediateTexture? texture)
    {
        if (this.ManagerOrThrow.Shared.TryGetFromGameIcon(lookup, out var shared))
        {
            shared.AddOwnerPlugin(this.plugin);
            texture = shared;
            return true;
        }

        texture = null;
        return false;
    }
    
    /// <inheritdoc/>
    public ISharedImmediateTexture GetFromGame(string path)
    {
        var shared = this.ManagerOrThrow.Shared.GetFromGame(path);
        shared.AddOwnerPlugin(this.plugin);
        return shared;
    }

    /// <inheritdoc/>
    public ISharedImmediateTexture GetFromFile(string path)
    {
        var shared = this.ManagerOrThrow.Shared.GetFromFile(path);
        shared.AddOwnerPlugin(this.plugin);
        return shared;
    }
    
    /// <inheritdoc/>
    public ISharedImmediateTexture GetFromFile(FileInfo file)
    {
        var shared = this.ManagerOrThrow.Shared.GetFromFile(file);
        shared.AddOwnerPlugin(this.plugin);
        return shared;
    }

    /// <inheritdoc/>
    public ISharedImmediateTexture GetFromManifestResource(Assembly assembly, string name)
    {
        var shared = this.ManagerOrThrow.Shared.GetFromManifestResource(assembly, name);
        shared.AddOwnerPlugin(this.plugin);
        return shared;
    }

    /// <inheritdoc/>
    public string GetIconPath(in GameIconLookup lookup) => this.ManagerOrThrow.GetIconPath(lookup);

    /// <inheritdoc/>
    public bool TryGetIconPath(in GameIconLookup lookup, out string? path) =>
        this.ManagerOrThrow.TryGetIconPath(lookup, out path);

    /// <inheritdoc/>
    public bool IsDxgiFormatSupported(int dxgiFormat) =>
        this.ManagerOrThrow.IsDxgiFormatSupported((DXGI_FORMAT)dxgiFormat);

    /// <inheritdoc/>
    public bool IsDxgiFormatSupportedForCreateFromExistingTextureAsync(int dxgiFormat) =>
        this.ManagerOrThrow.IsDxgiFormatSupportedForCreateFromExistingTextureAsync((DXGI_FORMAT)dxgiFormat);

    /// <inheritdoc/>
    public string GetSubstitutedPath(string originalPath) =>
        this.ManagerOrThrow.GetSubstitutedPath(originalPath);

    /// <inheritdoc/>
    public void InvalidatePaths(IEnumerable<string> paths) =>
        this.ManagerOrThrow.InvalidatePaths(paths);

    /// <inheritdoc/>
    public async Task<(RawImageSpecification Specification, byte[] RawData)> GetRawImageAsync(
        IDalamudTextureWrap wrap,
        TextureModificationArgs args = default,
        bool leaveWrapOpen = false,
        CancellationToken cancellationToken = default)
    {
        var manager = await this.ManagerTask;
        return await manager.GetRawImageAsync(wrap, args, leaveWrapOpen, cancellationToken);
    }

    /// <inheritdoc/>
    public IEnumerable<IBitmapCodecInfo> GetSupportedImageEncoderInfos() =>
        this.ManagerOrThrow.Wic.GetSupportedEncoderInfos();

    /// <inheritdoc/>
    public async Task SaveToStreamAsync(
        IDalamudTextureWrap wrap,
        Guid containerGuid,
        Stream stream,
        IReadOnlyDictionary<string, object>? props = null,
        bool leaveWrapOpen = false,
        bool leaveStreamOpen = false,
        CancellationToken cancellationToken = default)
    {
        var manager = await this.ManagerTask;
        await manager.SaveToStreamAsync(
            wrap,
            containerGuid,
            stream,
            props,
            leaveWrapOpen,
            leaveStreamOpen,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task SaveToFileAsync(
        IDalamudTextureWrap wrap,
        Guid containerGuid,
        string path,
        IReadOnlyDictionary<string, object>? props = null,
        bool leaveWrapOpen = false,
        CancellationToken cancellationToken = default)
    {
        var manager = await this.ManagerTask;
        await manager.SaveToFileAsync(
            wrap,
            containerGuid,
            path,
            props,
            leaveWrapOpen,
            cancellationToken);
    }

    private void ResultOnInterceptTexDataLoad(string path, ref string? replacementPath) =>
        this.InterceptTexDataLoad?.Invoke(path, ref replacementPath);
}
