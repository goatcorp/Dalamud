using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Textures.TextureWraps.Internal;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using Dalamud.Utility.TerraFxCom;

using Lumina.Data.Files;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Dalamud.Interface.Textures.Internal;

/// <summary>Service responsible for loading and disposing ImGui texture wraps.</summary>
internal sealed partial class TextureManager
{
    /// <inheritdoc/>
    unsafe nint ITextureProvider.ConvertToKernelTexture(IDalamudTextureWrap wrap, bool leaveWrapOpen) =>
        (nint)this.ConvertToKernelTexture(wrap, leaveWrapOpen);

    /// <inheritdoc cref="ITextureProvider.ConvertToKernelTexture"/>
    public unsafe FFXIVClientStructs.FFXIV.Client.Graphics.Kernel.Texture* ConvertToKernelTexture(
        IDalamudTextureWrap wrap,
        bool leaveWrapOpen = false)
    {
        using var wrapAux = new WrapAux(wrap, leaveWrapOpen);

        var flags = TexFile.Attribute.TextureType2D;
        if (wrapAux.Desc.Usage == D3D11_USAGE.D3D11_USAGE_IMMUTABLE)
            flags |= TexFile.Attribute.Immutable;
        if (wrapAux.Desc.Usage == D3D11_USAGE.D3D11_USAGE_DYNAMIC)
            flags |= TexFile.Attribute.ReadWrite;
        if ((wrapAux.Desc.CPUAccessFlags & (uint)D3D11_CPU_ACCESS_FLAG.D3D11_CPU_ACCESS_READ) != 0)
            flags |= TexFile.Attribute.CpuRead;
        if ((wrapAux.Desc.BindFlags & (uint)D3D11_BIND_FLAG.D3D11_BIND_RENDER_TARGET) != 0)
            flags |= TexFile.Attribute.TextureRenderTarget;
        if ((wrapAux.Desc.BindFlags & (uint)D3D11_BIND_FLAG.D3D11_BIND_DEPTH_STENCIL) != 0)
            flags |= TexFile.Attribute.TextureDepthStencil;
        if (wrapAux.Desc.ArraySize != 1)
            throw new NotSupportedException("TextureArray2D is currently not supported.");

        var gtex = FFXIVClientStructs.FFXIV.Client.Graphics.Kernel.Texture.CreateTexture2D(
            (int)wrapAux.Desc.Width,
            (int)wrapAux.Desc.Height,
            (byte)wrapAux.Desc.MipLevels,
            (uint)TexFile.TextureFormat.Null, // instructs the game to skip preprocessing it seems
            (uint)flags,
            0);

        // Kernel::Texture owns these resources. We're passing the ownership to them.
        wrapAux.TexPtr->AddRef();
        wrapAux.SrvPtr->AddRef();

        // Not sure this is needed
        var ltf = wrapAux.Desc.Format switch
        {
            DXGI_FORMAT.DXGI_FORMAT_R32G32B32A32_FLOAT => TexFile.TextureFormat.R32G32B32A32F,
            DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_FLOAT => TexFile.TextureFormat.R16G16B16A16F,
            DXGI_FORMAT.DXGI_FORMAT_R32G32_FLOAT => TexFile.TextureFormat.R32G32F,
            DXGI_FORMAT.DXGI_FORMAT_R16G16_FLOAT => TexFile.TextureFormat.R16G16F,
            DXGI_FORMAT.DXGI_FORMAT_R32_FLOAT => TexFile.TextureFormat.R32F,
            DXGI_FORMAT.DXGI_FORMAT_R24G8_TYPELESS => TexFile.TextureFormat.D24S8,
            DXGI_FORMAT.DXGI_FORMAT_R16_TYPELESS => TexFile.TextureFormat.D16,
            DXGI_FORMAT.DXGI_FORMAT_A8_UNORM => TexFile.TextureFormat.A8,
            DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM => TexFile.TextureFormat.BC1,
            DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM => TexFile.TextureFormat.BC2,
            DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM => TexFile.TextureFormat.BC3,
            DXGI_FORMAT.DXGI_FORMAT_BC5_UNORM => TexFile.TextureFormat.BC5,
            DXGI_FORMAT.DXGI_FORMAT_B4G4R4A4_UNORM => TexFile.TextureFormat.B4G4R4A4,
            DXGI_FORMAT.DXGI_FORMAT_B5G5R5A1_UNORM => TexFile.TextureFormat.B5G5R5A1,
            DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM => TexFile.TextureFormat.B8G8R8A8,
            DXGI_FORMAT.DXGI_FORMAT_B8G8R8X8_UNORM => TexFile.TextureFormat.B8G8R8X8,
            DXGI_FORMAT.DXGI_FORMAT_BC7_UNORM => TexFile.TextureFormat.BC7,
            _ => TexFile.TextureFormat.Null,
        };
        gtex->TextureFormat = (FFXIVClientStructs.FFXIV.Client.Graphics.Kernel.TextureFormat)ltf;

        gtex->D3D11Texture2D = wrapAux.TexPtr;
        gtex->D3D11ShaderResourceView = wrapAux.SrvPtr;
        return gtex;
    }

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
        if (this.device.Get()->CheckFormatSupport(dxgiFormat, (uint*)&supported).FAILED)
            return false;

        const D3D11_FORMAT_SUPPORT required =
            D3D11_FORMAT_SUPPORT.D3D11_FORMAT_SUPPORT_TEXTURE2D
            | D3D11_FORMAT_SUPPORT.D3D11_FORMAT_SUPPORT_RENDER_TARGET;
        return (supported & required) == required;
    }

    /// <inheritdoc/>
    public Task<IDalamudTextureWrap> CreateFromExistingTextureAsync(
        IDalamudTextureWrap wrap,
        TextureModificationArgs args = default,
        bool leaveWrapOpen = false,
        string? debugName = null,
        CancellationToken cancellationToken = default) =>
        this.DynamicPriorityTextureLoader.LoadAsync<IDalamudTextureWrap>(
            null,
            async _ =>
            {
                // leaveWrapOpen is taken care from calling LoadTextureAsync
                using var wrapAux = new WrapAux(wrap, true);
                using var tex = await this.NoThrottleCreateFromExistingTextureAsync(wrapAux, args);

                unsafe
                {
                    using var srv = this.device.CreateShaderResourceView(
                        tex,
                        new(tex.Get(), D3D_SRV_DIMENSION.D3D11_SRV_DIMENSION_TEXTURE2D));

                    var desc = tex.GetDesc();

                    var outWrap = new UnknownTextureWrap(
                        (IUnknown*)srv.Get(),
                        (int)desc.Width,
                        (int)desc.Height,
                        true);
                    this.BlameSetName(
                        outWrap,
                        debugName ??
                        $"{nameof(this.CreateFromExistingTextureAsync)}({wrap}, {args})");
                    return outWrap;
                }
            },
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
        CancellationToken cancellationToken = default)
    {
        args.ThrowOnInvalidValues();
        var t = new ViewportTextureWrap(args, debugName, ownerPlugin, cancellationToken);
        t.QueueUpdate();
        return t.FirstUpdateTask;
    }

    /// <inheritdoc/>
    public async Task<(RawImageSpecification Specification, byte[] RawData)> GetRawImageAsync(
        IDalamudTextureWrap wrap,
        TextureModificationArgs args = default,
        bool leaveWrapOpen = false,
        CancellationToken cancellationToken = default)
    {
        using var wrapAux = new WrapAux(wrap, leaveWrapOpen);
        return await this.GetRawImageAsync(wrapAux, args, cancellationToken);
    }

    private async Task<(RawImageSpecification Specification, byte[] RawData)> GetRawImageAsync(
        WrapAux wrapAux,
        TextureModificationArgs args = default,
        CancellationToken cancellationToken = default)
    {
        using var tex2D =
            args.IsCompleteSourceCopy(wrapAux.Desc)
            ? wrapAux.NewTexRef()
            : await this.NoThrottleCreateFromExistingTextureAsync(wrapAux, args);

        cancellationToken.ThrowIfCancellationRequested();

        // ID3D11DeviceContext is not a threadsafe resource, and it must be used from the UI thread.
        return await this.RunDuringPresent(() => ExtractMappedResource(tex2D, cancellationToken));

        static unsafe (RawImageSpecification Specification, byte[] RawData) ExtractMappedResource(
            ComPtr<ID3D11Texture2D> tex2D,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var desc = tex2D.GetDesc();

            using var device = default(ComPtr<ID3D11Device>);
            tex2D.Get()->GetDevice(device.GetAddressOf());
            using var context = default(ComPtr<ID3D11DeviceContext>);
            device.Get()->GetImmediateContext(context.GetAddressOf());

            using var tmpTex =
                (desc.CPUAccessFlags & (uint)D3D11_CPU_ACCESS_FLAG.D3D11_CPU_ACCESS_READ) == 0
                    ? device.CreateTexture2D(
                        desc with
                        {
                            MipLevels = 1,
                            ArraySize = 1,
                            SampleDesc = new(1, 0),
                            Usage = D3D11_USAGE.D3D11_USAGE_STAGING,
                            BindFlags = 0u,
                            CPUAccessFlags = (uint)D3D11_CPU_ACCESS_FLAG.D3D11_CPU_ACCESS_READ,
                            MiscFlags = 0u,
                        },
                        tex2D)
                    : default;
            cancellationToken.ThrowIfCancellationRequested();

            var mapWhat = (ID3D11Resource*)(tmpTex.IsEmpty() ? tex2D.Get() : tmpTex.Get());

            D3D11_MAPPED_SUBRESOURCE mapped;
            context.Get()->Map(mapWhat, 0, D3D11_MAP.D3D11_MAP_READ, 0, &mapped).ThrowOnError();

            try
            {
                var specs = new RawImageSpecification(desc, mapped.RowPitch);
                var bytes = new Span<byte>(mapped.pData, checked((int)mapped.DepthPitch)).ToArray();
                return (specs, bytes);
            }
            finally
            {
                context.Get()->Unmap(mapWhat, 0);
            }
        }
    }

    private async Task<ComPtr<ID3D11Texture2D>> NoThrottleCreateFromExistingTextureAsync(
        WrapAux wrapAux,
        TextureModificationArgs args)
    {
        args.ThrowOnInvalidValues();

        if (args.Format == DXGI_FORMAT.DXGI_FORMAT_UNKNOWN)
            args = args with { Format = wrapAux.Desc.Format };
        if (args.NewWidth == 0)
            args = args with { NewWidth = (int)MathF.Round((args.Uv1Effective.X - args.Uv0.X) * wrapAux.Desc.Width) };
        if (args.NewHeight == 0)
            args = args with { NewHeight = (int)MathF.Round((args.Uv1Effective.Y - args.Uv0.Y) * wrapAux.Desc.Height) };

        using var tex2DCopyTemp =
            this.device.CreateTexture2D(
                new()
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
                });

        await this.RunDuringPresent(() => DrawSourceTextureToTarget(wrapAux, args, this.SimpleDrawer, tex2DCopyTemp));

        return new(tex2DCopyTemp);

        static unsafe void DrawSourceTextureToTarget(
            WrapAux wrapAux,
            TextureModificationArgs args,
            SimpleDrawerImpl simpleDrawer,
            ComPtr<ID3D11Texture2D> tex2DCopyTemp)
        {
            using var rtvCopyTemp = default(ComPtr<ID3D11RenderTargetView>);
            var rtvCopyTempDesc = new D3D11_RENDER_TARGET_VIEW_DESC(
                tex2DCopyTemp,
                D3D11_RTV_DIMENSION.D3D11_RTV_DIMENSION_TEXTURE2D);
            wrapAux.DevPtr->CreateRenderTargetView(
                    (ID3D11Resource*)tex2DCopyTemp.Get(),
                    &rtvCopyTempDesc,
                    rtvCopyTemp.GetAddressOf())
                .ThrowOnError();

            wrapAux.CtxPtr->OMSetRenderTargets(1u, rtvCopyTemp.GetAddressOf(), null);
            simpleDrawer.Draw(wrapAux.CtxPtr, wrapAux.SrvPtr, args.Uv0, args.Uv1Effective);
            if (args.MakeOpaque)
                simpleDrawer.StripAlpha(wrapAux.CtxPtr);

            var dummy = default(ID3D11RenderTargetView*);
            wrapAux.CtxPtr->OMSetRenderTargets(1u, &dummy, null);
        }
    }

    /// <summary>Auxiliary data from <see cref="IDalamudTextureWrap"/>.</summary>
    private unsafe struct WrapAux : IDisposable
    {
        public readonly D3D11_TEXTURE2D_DESC Desc;

        private IDalamudTextureWrap? wrapToClose;

        private ComPtr<ID3D11ShaderResourceView> srv;
        private ComPtr<ID3D11Resource> res;
        private ComPtr<ID3D11Texture2D> tex;
        private ComPtr<ID3D11Device> device;
        private ComPtr<ID3D11DeviceContext> context;

        public WrapAux(IDalamudTextureWrap wrap, bool leaveWrapOpen)
        {
            this.wrapToClose = leaveWrapOpen ? null : wrap;

            using var unk = new ComPtr<IUnknown>((IUnknown*)wrap.ImGuiHandle);

            using var srvTemp = default(ComPtr<ID3D11ShaderResourceView>);
            unk.As(&srvTemp).ThrowOnError();

            using var resTemp = default(ComPtr<ID3D11Resource>);
            srvTemp.Get()->GetResource(resTemp.GetAddressOf());

            using var texTemp = default(ComPtr<ID3D11Texture2D>);
            resTemp.As(&texTemp).ThrowOnError();

            using var deviceTemp = default(ComPtr<ID3D11Device>);
            texTemp.Get()->GetDevice(deviceTemp.GetAddressOf());

            using var contextTemp = default(ComPtr<ID3D11DeviceContext>);
            deviceTemp.Get()->GetImmediateContext(contextTemp.GetAddressOf());

            fixed (D3D11_TEXTURE2D_DESC* pDesc = &this.Desc)
                texTemp.Get()->GetDesc(pDesc);

            srvTemp.Swap(ref this.srv);
            resTemp.Swap(ref this.res);
            texTemp.Swap(ref this.tex);
            deviceTemp.Swap(ref this.device);
            contextTemp.Swap(ref this.context);
        }

        public ID3D11ShaderResourceView* SrvPtr
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.srv.Get();
        }

        public ID3D11Resource* ResPtr
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.res.Get();
        }

        public ID3D11Texture2D* TexPtr
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.tex.Get();
        }

        public ID3D11Device* DevPtr
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.device.Get();
        }

        public ID3D11DeviceContext* CtxPtr
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.context.Get();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ComPtr<ID3D11ShaderResourceView> NewSrvRef() => new(this.srv);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ComPtr<ID3D11Resource> NewResRef() => new(this.res);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ComPtr<ID3D11Texture2D> NewTexRef() => new(this.tex);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ComPtr<ID3D11Device> NewDevRef() => new(this.device);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ComPtr<ID3D11DeviceContext> NewCtxRef() => new(this.context);

        public void Dispose()
        {
            this.srv.Reset();
            this.res.Reset();
            this.tex.Reset();
            this.device.Reset();
            this.context.Reset();
            Interlocked.Exchange(ref this.wrapToClose, null)?.Dispose();
        }
    }
}
