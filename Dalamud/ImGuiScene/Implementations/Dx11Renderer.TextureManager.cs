using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Utility;
using Dalamud.Utility.TerraFxCom;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Dalamud.ImGuiScene.Implementations;

/// <summary>
/// Deals with rendering ImGui using DirectX 11.
/// See https://github.com/ocornut/imgui/blob/master/examples/imgui_impl_dx11.cpp for the original implementation.
/// </summary>
[SuppressMessage(
    "StyleCop.CSharp.LayoutRules",
    "SA1519:Braces should not be omitted from multi-line child statement",
    Justification = "Multiple fixed/using scopes")]
internal partial class Dx11Renderer
{
    private sealed class Dx11TextureManager : ISceneTextureManager, IDisposable
    {
        private readonly Dx11Renderer renderer;

        public Dx11TextureManager(Dx11Renderer renderer) => this.renderer = renderer;

        ~Dx11TextureManager() => this.ReleaseUnmanagedResources();

        public void Dispose()
        {
            this.ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        public unsafe IDalamudTextureWrap Create(
            ReadOnlySpan<byte> data,
            RawImageSpecification specs,
            bool cpuRead,
            bool cpuWrite,
            bool immutable,
            string? debugName = null)
        {
            if (cpuRead && cpuWrite)
                throw new ArgumentException("cpuRead and cpuWrite cannot be set at the same time.");
            if (immutable && cpuWrite)
                throw new ArgumentException("immutable and cpuWrite cannot be set at the same time.");

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
            else if (immutable)
                usage = D3D11_USAGE.D3D11_USAGE_IMMUTABLE;
            else
                usage = D3D11_USAGE.D3D11_USAGE_DEFAULT;

            var texd = new D3D11_TEXTURE2D_DESC
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
            };
            using var texture = default(ComPtr<ID3D11Texture2D>);
            if (data.IsEmpty)
            {
                this.renderer.device.Get()->CreateTexture2D(&texd, null, texture.GetAddressOf()).ThrowOnError();
            }
            else
            {
                fixed (void* dataPtr = data)
                {
                    var subrdata = new D3D11_SUBRESOURCE_DATA { pSysMem = dataPtr, SysMemPitch = (uint)specs.Pitch };
                    this.renderer.device.Get()->CreateTexture2D(&texd, &subrdata, texture.GetAddressOf())
                        .ThrowOnError();
                }
            }

            SetDebugName(texture, $"Texture:{debugName}:Tex2D");
            return TextureWrap.TakeOwnership(
                new(texture, this.CreateSrvFromTex2D(texture, debugName), specs.Width, specs.Height, specs.Format));
        }

        public async Task<IDalamudTextureWrap> CreateFromExistingTextureAsync(
            IDalamudTextureWrap wrap,
            TextureModificationArgs args = default,
            string? debugName = null,
            CancellationToken cancellationToken = default)
        {
            var texture = await this.CreateTex2DFromWrap(wrap, args, debugName, cancellationToken);
            return TextureWrap.TakeOwnership(
                new(texture, this.CreateSrvFromTex2D(texture, debugName), args.NewWidth, args.NewHeight, args.Format));
        }

        public Task<IDalamudTextureWrap> CreateFromImGuiViewportAsync(
            ImGuiViewportTextureArgs args,
            LocalPlugin? ownerPlugin,
            string? debugName = null,
            CancellationToken cancellationToken = default)
        {
            args.ThrowOnInvalidValues();
            var t = new ViewportTextureWrap(this.renderer, args, debugName, ownerPlugin, cancellationToken);
            t.QueueUpdate();
            return t.FirstUpdateTask;
        }

        public unsafe bool IsDxgiFormatSupported(int dxgiFormat)
        {
            D3D11_FORMAT_SUPPORT supported;
            if (this.renderer.device.Get()->CheckFormatSupport((DXGI_FORMAT)dxgiFormat, (uint*)&supported).FAILED)
                return false;

            const D3D11_FORMAT_SUPPORT required = D3D11_FORMAT_SUPPORT.D3D11_FORMAT_SUPPORT_TEXTURE2D;
            return (supported & required) == required;
        }

        public unsafe bool IsDxgiFormatSupportedForCreateFromExistingTextureAsync(int dxgiFormat)
        {
            switch ((DXGI_FORMAT)dxgiFormat)
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
            if (this.renderer.device.Get()->CheckFormatSupport((DXGI_FORMAT)dxgiFormat, (uint*)&supported).FAILED)
                return false;

            const D3D11_FORMAT_SUPPORT required =
                D3D11_FORMAT_SUPPORT.D3D11_FORMAT_SUPPORT_TEXTURE2D
                | D3D11_FORMAT_SUPPORT.D3D11_FORMAT_SUPPORT_RENDER_TARGET;
            return (supported & required) == required;
        }

        public async Task<(RawImageSpecification Specification, byte[] RawData)> GetRawImageAsync(
            IDalamudTextureWrap wrap,
            TextureModificationArgs args = default,
            CancellationToken cancellationToken = default)
        {
            if (wrap is not TextureWrap { Data: { } data })
                throw new ArgumentException($"Given type {wrap.GetType()} is not a supported wrap type.", nameof(wrap));

            args.ThrowOnInvalidValues();

            using var tex2D =
                args.IsCompleteSourceCopy(data.Texture2D.GetDesc())
                ? new(data.Texture2D)
                : await this.CreateTex2DFromWrap(wrap, args, null, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            // ID3D11DeviceContext is not a threadsafe resource, and it must be used from the UI thread.
            (RawImageSpecification Specification, byte[] RawData) res = default;
            await this.renderer.RunDuringPresent(() => res = ExtractMappedResource(tex2D, cancellationToken));
            return res;

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

        private void ReleaseUnmanagedResources()
        {
            // TODO release unmanaged resources here
        }

        private async Task<ComPtr<ID3D11Texture2D>> CreateTex2DFromWrap(
            IDalamudTextureWrap wrap,
            TextureModificationArgs args = default,
            string? debugName = null,
            CancellationToken cancellationToken = default)
        {
            args.ThrowOnInvalidValues();

            if (wrap is not TextureWrap { Data: { } data })
                throw new ArgumentException($"Given type {wrap.GetType()} is not a supported wrap type.", nameof(wrap));

            if (args.Format == DXGI_FORMAT.DXGI_FORMAT_UNKNOWN)
                args = args with { Format = data.Format };
            if (args.NewWidth == 0)
                args = args with { NewWidth = (int)MathF.Round((args.Uv1Effective.X - args.Uv0.X) * data.Width) };
            if (args.NewHeight == 0)
                args = args with { NewHeight = (int)MathF.Round((args.Uv1Effective.Y - args.Uv0.Y) * data.Height) };

            using var target =
                this.renderer.device.CreateTexture2D(
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

            await this.renderer.RunDuringPresent(
                () =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    this.DrawSrvToTex2D(data.ShaderResourceView, args, target);
                });

            SetDebugName(target, $"Texture:{debugName}:Tex2D");
            return new(target);
        }

        private unsafe void DrawSrvToTex2D(
            ComPtr<ID3D11ShaderResourceView> sourceSrv,
            TextureModificationArgs args,
            ComPtr<ID3D11Texture2D> targetTex2D)
        {
            using var rtvCopyTemp = default(ComPtr<ID3D11RenderTargetView>);
            var rtvCopyTempDesc = new D3D11_RENDER_TARGET_VIEW_DESC(
                targetTex2D,
                D3D11_RTV_DIMENSION.D3D11_RTV_DIMENSION_TEXTURE2D);
            this.renderer.device.Get()->CreateRenderTargetView(
                    (ID3D11Resource*)targetTex2D.Get(),
                    &rtvCopyTempDesc,
                    rtvCopyTemp.GetAddressOf())
                .ThrowOnError();

            this.renderer.context.Get()->OMSetRenderTargets(1u, rtvCopyTemp.GetAddressOf(), null);
            this.renderer.rectangleDrawer.Draw(this.renderer.context, sourceSrv, args.Uv0, args.Uv1Effective);
            if (args.MakeOpaque)
                this.renderer.rectangleDrawer.StripAlpha(this.renderer.context);

            var dummy = default(ID3D11RenderTargetView*);
            this.renderer.context.Get()->OMSetRenderTargets(1u, &dummy, null);
        }

        private unsafe ComPtr<ID3D11ShaderResourceView> CreateSrvFromTex2D(
            ComPtr<ID3D11Texture2D> texture,
            string? debugName = null)
        {
            D3D11_TEXTURE2D_DESC desc;
            texture.Get()->GetDesc(&desc);
            var viewd = new D3D11_SHADER_RESOURCE_VIEW_DESC
            {
                Format = desc.Format,
                ViewDimension = D3D_SRV_DIMENSION.D3D11_SRV_DIMENSION_TEXTURE2D,
                Texture2D = new() { MipLevels = desc.MipLevels },
            };
            using var view = default(ComPtr<ID3D11ShaderResourceView>);
            this.renderer.device.Get()->CreateShaderResourceView(
                (ID3D11Resource*)texture.Get(),
                &viewd,
                view.GetAddressOf()).ThrowOnError();

            SetDebugName(view, $"Texture:{debugName}:SRV");
            return new(view);
        }
    }
}
