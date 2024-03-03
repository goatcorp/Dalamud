using System.Threading;
using System.Threading.Tasks;

using Dalamud.Interface.Internal;
using Dalamud.Plugin.Services;
using Dalamud.Utility;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Dalamud.Interface.Textures.Internal;

/// <summary>Service responsible for loading and disposing ImGui texture wraps.</summary>
internal sealed partial class TextureManager
{
    /// <inheritdoc/>
    bool ITextureProvider.IsDxgiFormatSupportedForCreateFromExistingTextureAsync(int dxgiFormat) =>
        this.IsDxgiFormatSupportedForCreateFromExistingTextureAsync((DXGI_FORMAT)dxgiFormat);

    /// <inheritdoc cref="ITextureProvider.IsDxgiFormatSupportedForCreateFromExistingTextureAsync"/>
    public unsafe bool IsDxgiFormatSupportedForCreateFromExistingTextureAsync(DXGI_FORMAT dxgiFormat)
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

        D3D11_FORMAT_SUPPORT supported;
        if (this.Device.Get()->CheckFormatSupport(dxgiFormat, (uint*)&supported).FAILED)
            return false;

        const D3D11_FORMAT_SUPPORT required =
            D3D11_FORMAT_SUPPORT.D3D11_FORMAT_SUPPORT_TEXTURE2D
            | D3D11_FORMAT_SUPPORT.D3D11_FORMAT_SUPPORT_RENDER_TARGET;
        return (supported & required) == required;
    }

    /// <inheritdoc/>
    public Task<IDalamudTextureWrap> CreateFromExistingTextureAsync(
        IDalamudTextureWrap wrap,
        ExistingTextureModificationArgs args = default,
        bool leaveWrapOpen = false,
        CancellationToken cancellationToken = default)
    {
        return this.textureLoadThrottler.LoadTextureAsync(
            new TextureLoadThrottler.ReadOnlyThrottleBasisProvider(),
            ImmediateLoadFunction,
            cancellationToken,
            leaveWrapOpen ? null : wrap);

        async Task<IDalamudTextureWrap> ImmediateLoadFunction(CancellationToken ct)
        {
            using var tex = await this.NoThrottleCreateFromExistingTextureAsync(wrap, args);

            unsafe
            {
                var srvDesc = new D3D11_SHADER_RESOURCE_VIEW_DESC(
                    tex,
                    D3D_SRV_DIMENSION.D3D11_SRV_DIMENSION_TEXTURE2D);
                using var srv = default(ComPtr<ID3D11ShaderResourceView>);
                this.Device.Get()->CreateShaderResourceView(
                        (ID3D11Resource*)tex.Get(),
                        &srvDesc,
                        srv.GetAddressOf())
                    .ThrowOnError();

                var desc = default(D3D11_TEXTURE2D_DESC);
                tex.Get()->GetDesc(&desc);
                return new UnknownTextureWrap(
                    (IUnknown*)srv.Get(),
                    (int)desc.Width,
                    (int)desc.Height,
                    true);
            }
        }
    }

    /// <inheritdoc/>
    public async Task<IDalamudTextureWrap> CreateFromImGuiViewportAsync(
        ImGuiViewportTextureArgs args,
        CancellationToken cancellationToken = default)
    {
        // This constructor may throw; keep the function "async", to wrap the exception as a Task.
        var t = new ViewportTextureWrap(args, cancellationToken);
        t.QueueUpdate();
        return await t.FirstUpdateTask;
    }

    /// <inheritdoc/>
    public async Task<(RawImageSpecification Specification, byte[] RawData)> GetRawDataFromExistingTextureAsync(
        IDalamudTextureWrap wrap,
        ExistingTextureModificationArgs args = default,
        bool leaveWrapOpen = false,
        CancellationToken cancellationToken = default)
    {
        using var wrapDispose = leaveWrapOpen ? null : wrap;
        using var texSrv = default(ComPtr<ID3D11ShaderResourceView>);
        using var context = default(ComPtr<ID3D11DeviceContext>);
        using var tex2D = default(ComPtr<ID3D11Texture2D>);
        var texDesc = default(D3D11_TEXTURE2D_DESC);

        unsafe
        {
            fixed (Guid* piid = &IID.IID_ID3D11ShaderResourceView)
                ((IUnknown*)wrap.ImGuiHandle)->QueryInterface(piid, (void**)texSrv.GetAddressOf()).ThrowOnError();

            this.Device.Get()->GetImmediateContext(context.GetAddressOf());

            using (var texRes = default(ComPtr<ID3D11Resource>))
            {
                texSrv.Get()->GetResource(texRes.GetAddressOf());

                using var tex2DTemp = default(ComPtr<ID3D11Texture2D>);
                texRes.As(&tex2DTemp).ThrowOnError();
                tex2D.Swap(&tex2DTemp);
            }

            tex2D.Get()->GetDesc(&texDesc);
        }

        if (!args.IsCompleteSourceCopy(texDesc))
        {
            using var tmp = await this.NoThrottleCreateFromExistingTextureAsync(wrap, args);
            unsafe
            {
                tex2D.Swap(&tmp);
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        return await this.interfaceManager.RunBeforeImGuiRender(
                   () => ExtractMappedResource(this.Device, context, tex2D, cancellationToken));

        static unsafe (RawImageSpecification Specification, byte[] RawData) ExtractMappedResource(
            ComPtr<ID3D11Device> device,
            ComPtr<ID3D11DeviceContext> context,
            ComPtr<ID3D11Texture2D> tex2D,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ID3D11Resource* mapWhat = null;
            try
            {
                using var tmpTex = default(ComPtr<ID3D11Texture2D>);
                D3D11_TEXTURE2D_DESC desc;
                tex2D.Get()->GetDesc(&desc);
                if ((desc.CPUAccessFlags & (uint)D3D11_CPU_ACCESS_FLAG.D3D11_CPU_ACCESS_READ) == 0)
                {
                    var tmpTexDesc = desc with
                    {
                        MipLevels = 1,
                        ArraySize = 1,
                        SampleDesc = new(1, 0),
                        Usage = D3D11_USAGE.D3D11_USAGE_STAGING,
                        BindFlags = 0u,
                        CPUAccessFlags = (uint)D3D11_CPU_ACCESS_FLAG.D3D11_CPU_ACCESS_READ,
                        MiscFlags = 0u,
                    };
                    device.Get()->CreateTexture2D(&tmpTexDesc, null, tmpTex.GetAddressOf()).ThrowOnError();
                    context.Get()->CopyResource((ID3D11Resource*)tmpTex.Get(), (ID3D11Resource*)tex2D.Get());

                    cancellationToken.ThrowIfCancellationRequested();
                }

                D3D11_MAPPED_SUBRESOURCE mapped;
                mapWhat = (ID3D11Resource*)(tmpTex.IsEmpty() ? tex2D.Get() : tmpTex.Get());
                context.Get()->Map(
                    mapWhat,
                    0,
                    D3D11_MAP.D3D11_MAP_READ,
                    0,
                    &mapped).ThrowOnError();

                var specs = new RawImageSpecification(
                    (int)desc.Width,
                    (int)desc.Height,
                    (int)desc.Format,
                    (int)mapped.RowPitch);
                var bytes = new Span<byte>(mapped.pData, checked((int)mapped.DepthPitch)).ToArray();
                return (specs, bytes);
            }
            finally
            {
                if (mapWhat is not null)
                    context.Get()->Unmap(mapWhat, 0);
            }
        }
    }

    private async Task<ComPtr<ID3D11Texture2D>> NoThrottleCreateFromExistingTextureAsync(
        IDalamudTextureWrap wrap,
        ExistingTextureModificationArgs args)
    {
        args.ThrowOnInvalidValues();

        using var texSrv = default(ComPtr<ID3D11ShaderResourceView>);
        using var context = default(ComPtr<ID3D11DeviceContext>);
        using var tex2D = default(ComPtr<ID3D11Texture2D>);
        var texDesc = default(D3D11_TEXTURE2D_DESC);

        unsafe
        {
            fixed (Guid* piid = &IID.IID_ID3D11ShaderResourceView)
                ((IUnknown*)wrap.ImGuiHandle)->QueryInterface(piid, (void**)texSrv.GetAddressOf()).ThrowOnError();

            this.Device.Get()->GetImmediateContext(context.GetAddressOf());

            using (var texRes = default(ComPtr<ID3D11Resource>))
            {
                texSrv.Get()->GetResource(texRes.GetAddressOf());
                texRes.As(&tex2D).ThrowOnError();
            }

            tex2D.Get()->GetDesc(&texDesc);
        }

        if (args.Format == DXGI_FORMAT.DXGI_FORMAT_UNKNOWN)
            args = args with { Format = texDesc.Format };
        if (args.NewWidth == 0)
            args = args with { NewWidth = (int)MathF.Round((args.Uv1Effective.X - args.Uv0.X) * texDesc.Width) };
        if (args.NewHeight == 0)
            args = args with { NewHeight = (int)MathF.Round((args.Uv1Effective.Y - args.Uv0.Y) * texDesc.Height) };

        using var tex2DCopyTemp = default(ComPtr<ID3D11Texture2D>);
        unsafe
        {
            var tex2DCopyTempDesc = new D3D11_TEXTURE2D_DESC
            {
                Width = (uint)args.NewWidth,
                Height = (uint)args.NewHeight,
                MipLevels = 1,
                ArraySize = 1,
                Format = args.Format,
                SampleDesc = new(1, 0),
                Usage = D3D11_USAGE.D3D11_USAGE_DEFAULT,
                BindFlags = (uint)(D3D11_BIND_FLAG.D3D11_BIND_SHADER_RESOURCE |
                                   D3D11_BIND_FLAG.D3D11_BIND_RENDER_TARGET),
                CPUAccessFlags = 0u,
                MiscFlags = 0u,
            };
            this.Device.Get()->CreateTexture2D(&tex2DCopyTempDesc, null, tex2DCopyTemp.GetAddressOf()).ThrowOnError();
        }

        await this.interfaceManager.RunBeforeImGuiRender(
            () =>
            {
                unsafe
                {
                    using var rtvCopyTemp = default(ComPtr<ID3D11RenderTargetView>);
                    var rtvCopyTempDesc = new D3D11_RENDER_TARGET_VIEW_DESC(
                        tex2DCopyTemp,
                        D3D11_RTV_DIMENSION.D3D11_RTV_DIMENSION_TEXTURE2D);
                    this.Device.Get()->CreateRenderTargetView(
                        (ID3D11Resource*)tex2DCopyTemp.Get(),
                        &rtvCopyTempDesc,
                        rtvCopyTemp.GetAddressOf()).ThrowOnError();

                    context.Get()->OMSetRenderTargets(1u, rtvCopyTemp.GetAddressOf(), null);
                    this.SimpleDrawer.Draw(
                        context.Get(),
                        texSrv.Get(),
                        args.Uv0,
                        args.Uv1Effective);
                    if (args.MakeOpaque)
                        this.SimpleDrawer.StripAlpha(context.Get());

                    var dummy = default(ID3D11RenderTargetView*);
                    context.Get()->OMSetRenderTargets(1u, &dummy, null);
                }
            });

        return new(tex2DCopyTemp);
    }
}
