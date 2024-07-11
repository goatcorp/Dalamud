using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Plugin.Services;
using Dalamud.Storage.Assets;

using TerraFX.Interop.DirectX;

namespace Dalamud.Interface.Textures.Internal;

/// <summary>Service responsible for loading and disposing ImGui texture wraps.</summary>
internal sealed partial class TextureManager
{
    /// <inheritdoc/>
    bool ITextureProvider.IsDxgiFormatSupportedForCreateFromExistingTextureAsync(int dxgiFormat) =>
        this.IsDxgiFormatSupportedForCreateFromExistingTextureAsync((DXGI_FORMAT)dxgiFormat);

    /// <inheritdoc cref="ITextureProvider.IsDxgiFormatSupportedForCreateFromExistingTextureAsync"/>
    public bool IsDxgiFormatSupportedForCreateFromExistingTextureAsync(DXGI_FORMAT dxgiFormat)
    {
        switch (dxgiFormat)
        {
            // https://learn.microsoft.com/en-us/windows/win32/api/dxgiformat/ne-dxgiformat-dxgi_format
            // Video formats requiring use of another DXGI_FORMAT when using with CreateRenderTarget
            case DXGI_FORMAT.DXGI_FORMAT_AYUV:
            case DXGI_FORMAT.DXGI_FORMAT_NV12:
            case DXGI_FORMAT.DXGI_FORMAT_P010:
            case DXGI_FORMAT.DXGI_FORMAT_P016:
            case DXGI_FORMAT.DXGI_FORMAT_NV11:
                return false;
        }

        if (this.interfaceManager.Scene is not { } scene)
            throw new InvalidOperationException("Not yet ready.");
        return scene.SupportsTextureFormat((int)dxgiFormat) &&
               scene.SupportsTextureFormatForRenderTarget((int)dxgiFormat);
    }

    /// <inheritdoc/>
    public Task<IDalamudTextureWrap> CreateFromExistingTextureAsync(
        IDalamudTextureWrap wrap,
        TextureModificationArgs args = default,
        bool leaveWrapOpen = false,
        string? debugName = null,
        CancellationToken cancellationToken = default) =>
        this.DynamicPriorityTextureLoader.LoadAsync(
            null,
            _ => this.NoThrottleCreateFromExistingTextureAsync(
                wrap,
                args,
                debugName ??
                $"{nameof(this.CreateFromExistingTextureAsync)}({wrap}, {args})"),
            cancellationToken,
            leaveWrapOpen ? null : wrap);

    /// <inheritdoc/>
    Task<IDalamudTextureWrap> ITextureProvider.CreateFromImGuiViewportAsync(
        ImGuiViewportTextureArgs args,
        string? debugName,
        CancellationToken cancellationToken) =>
        this.CreateFromImGuiViewportAsync(args, null, debugName, cancellationToken);

    /// <inheritdoc cref="ITextureProvider.CreateFromImGuiViewportAsync"/>
    public Task<IDalamudTextureWrap> CreateFromImGuiViewportAsync(
        ImGuiViewportTextureArgs args,
        LocalPlugin? ownerPlugin,
        string? debugName = null,
        CancellationToken cancellationToken = default) =>
        this.interfaceManager.RunAfterImGuiRender(
            () => this.Scene.CreateTextureFromImGuiViewport(args, ownerPlugin, debugName, cancellationToken));

    /// <inheritdoc/>
    public async Task<(RawImageSpecification Specification, byte[] RawData)> GetRawImageAsync(
        IDalamudTextureWrap wrap,
        TextureModificationArgs args = default,
        bool leaveWrapOpen = false,
        CancellationToken cancellationToken = default)
    {
        using var wrapCloser = leaveWrapOpen ? null : wrap;
        return await this.GetRawImageAsync(wrap, args, cancellationToken);
    }

    private async Task<(RawImageSpecification Specification, byte[] RawData)> GetRawImageAsync(
        IDalamudTextureWrap wrap,
        TextureModificationArgs args = default,
        CancellationToken cancellationToken = default)
    {
        using var tex2D =
            args.IsCompleteSourceCopy(this.Scene.GetTextureSpecification(wrap))
                ? wrap.CreateWrapSharingLowLevelResource()
                : await this.NoThrottleCreateFromExistingTextureAsync(wrap, args);

        cancellationToken.ThrowIfCancellationRequested();

        // ID3D11DeviceContext is not a threadsafe resource, and it must be used from the UI thread.
        return await this.RunDuringPresent(
                   () =>
                   {
                       var data = this.Scene.GetTextureData(tex2D, out var specs);
                       return (specs, data);
                   });
    }

    private async Task<IDalamudTextureWrap> NoThrottleCreateFromExistingTextureAsync(
        IDalamudTextureWrap source,
        TextureModificationArgs args,
        string? debugName = null)
    {
        args.ThrowOnInvalidValues();

        var sourceSpecs = this.Scene.GetTextureSpecification(source);
        if (args.Format == DXGI_FORMAT.DXGI_FORMAT_UNKNOWN)
            args = args with { Format = sourceSpecs.Format };
        if (args.NewWidth == 0)
            args = args with { NewWidth = (int)MathF.Round((args.Uv1Effective.X - args.Uv0.X) * sourceSpecs.Width) };
        if (args.NewHeight == 0)
            args = args with { NewHeight = (int)MathF.Round((args.Uv1Effective.Y - args.Uv0.Y) * sourceSpecs.Height) };

        var tex2DCopyTemp = this.Scene.CreateTexture2D(
            default,
            new(args.NewWidth, args.NewHeight, args.Format),
            false,
            false,
            true,
            debugName ?? $"{nameof(this.NoThrottleCreateFromExistingTextureAsync)}({args})");
        try
        {
            var dam = await Service<DalamudAssetManager>.GetAsync();
            await this.RunDuringPresent(
                () =>
                {
                    this.Scene.DrawTextureToTexture(
                        tex2DCopyTemp,
                        Vector2.Zero,
                        Vector2.One,
                        source,
                        args.Uv0,
                        args.Uv1Effective);
                    if (args.MakeOpaque)
                        this.Scene.DrawTextureToTexture(
                            tex2DCopyTemp,
                            Vector2.Zero,
                            Vector2.One,
                            dam.White4X4,
                            args.Uv0,
                            args.Uv1Effective,
                            true);
                });

            return tex2DCopyTemp;
        }
        catch
        {
            tex2DCopyTemp.Dispose();
            throw;
        }
    }
}
