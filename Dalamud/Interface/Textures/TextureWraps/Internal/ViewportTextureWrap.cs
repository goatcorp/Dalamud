using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Game;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Textures.Internal;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Storage.Assets;
using Dalamud.Utility;

using ImGuiNET;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

using NotSupportedException = System.NotSupportedException;

namespace Dalamud.Interface.Textures.TextureWraps.Internal;

/// <summary>A texture wrap that takes its buffer from the frame buffer (of swap chain).</summary>
internal sealed class ViewportTextureWrap : IDalamudTextureWrap, IDeferredDisposable
{
    private readonly string? debugName;
    private readonly LocalPlugin? ownerPlugin;
    private readonly CancellationToken cancellationToken;
    private readonly TaskCompletionSource<IDalamudTextureWrap> firstUpdateTaskCompletionSource = new();

    private ImGuiViewportTextureArgs args;
    private D3D11_TEXTURE2D_DESC desc;
    private ComPtr<ID3D11Texture2D> tex;
    private ComPtr<ID3D11ShaderResourceView> srv;
    private ComPtr<ID3D11RenderTargetView> rtv;

    private bool disposed;

    /// <summary>Initializes a new instance of the <see cref="ViewportTextureWrap"/> class.</summary>
    /// <param name="args">The arguments for creating a texture.</param>
    /// <param name="debugName">Name for debug display purposes.</param>
    /// <param name="ownerPlugin">The owner plugin.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public ViewportTextureWrap(
        ImGuiViewportTextureArgs args, string? debugName, LocalPlugin? ownerPlugin, CancellationToken cancellationToken)
    {
        this.args = args;
        this.debugName = debugName;
        this.ownerPlugin = ownerPlugin;
        this.cancellationToken = cancellationToken;
    }

    /// <summary>Finalizes an instance of the <see cref="ViewportTextureWrap"/> class.</summary>
    ~ViewportTextureWrap() => this.Dispose(false);

    /// <inheritdoc/>
    public unsafe nint ImGuiHandle
    {
        get
        {
            var t = (nint)this.srv.Get();
            return t == nint.Zero ? Service<DalamudAssetManager>.Get().Empty4X4.ImGuiHandle : t;
        }
    }

    /// <inheritdoc/>
    public int Width => (int)this.desc.Width;

    /// <inheritdoc/>
    public int Height => (int)this.desc.Height;

    /// <summary>Gets the task representing the first <see cref="Update"/> call.</summary>
    public Task<IDalamudTextureWrap> FirstUpdateTask => this.firstUpdateTaskCompletionSource.Task;

    /// <summary>Updates the texture from the source viewport.</summary>
    public unsafe void Update()
    {
        if (this.cancellationToken.IsCancellationRequested || this.disposed)
        {
            this.firstUpdateTaskCompletionSource.TrySetCanceled();
            return;
        }

        try
        {
            ThreadSafety.AssertMainThread();

            using var backBuffer = GetImGuiViewportBackBuffer(this.args.ViewportId);
            D3D11_TEXTURE2D_DESC newDesc;
            backBuffer.Get()->GetDesc(&newDesc);

            if (newDesc.SampleDesc.Count > 1)
                throw new NotSupportedException("Multisampling is not expected");

            using var device = default(ComPtr<ID3D11Device>);
            backBuffer.Get()->GetDevice(device.GetAddressOf());

            using var context = default(ComPtr<ID3D11DeviceContext>);
            device.Get()->GetImmediateContext(context.GetAddressOf());

            var copyBox = new D3D11_BOX
            {
                left = (uint)MathF.Round(newDesc.Width * this.args.Uv0.X),
                top = (uint)MathF.Round(newDesc.Height * this.args.Uv0.Y),
                right = (uint)MathF.Round(newDesc.Width * this.args.Uv1Effective.X),
                bottom = (uint)MathF.Round(newDesc.Height * this.args.Uv1Effective.Y),
                front = 0,
                back = 1,
            };

            if (this.desc.Width != copyBox.right - copyBox.left
                || this.desc.Height != copyBox.bottom - copyBox.top
                || this.desc.Format != newDesc.Format)
            {
                var texDesc = new D3D11_TEXTURE2D_DESC
                {
                    Width = copyBox.right - copyBox.left,
                    Height = copyBox.bottom - copyBox.top,
                    MipLevels = 1,
                    ArraySize = 1,
                    Format = newDesc.Format,
                    SampleDesc = new(1, 0),
                    Usage = D3D11_USAGE.D3D11_USAGE_DEFAULT,
                    BindFlags = (uint)(D3D11_BIND_FLAG.D3D11_BIND_SHADER_RESOURCE |
                                       D3D11_BIND_FLAG.D3D11_BIND_RENDER_TARGET),
                    CPUAccessFlags = 0u,
                    MiscFlags = 0u,
                };

                using var texTemp = default(ComPtr<ID3D11Texture2D>);
                device.Get()->CreateTexture2D(&texDesc, null, texTemp.GetAddressOf()).ThrowOnError();

                var rtvDesc = new D3D11_RENDER_TARGET_VIEW_DESC(
                    texTemp,
                    D3D11_RTV_DIMENSION.D3D11_RTV_DIMENSION_TEXTURE2D);
                using var rtvTemp = default(ComPtr<ID3D11RenderTargetView>);
                device.Get()->CreateRenderTargetView(
                    (ID3D11Resource*)texTemp.Get(),
                    &rtvDesc,
                    rtvTemp.GetAddressOf()).ThrowOnError();

                var srvDesc = new D3D11_SHADER_RESOURCE_VIEW_DESC(
                    texTemp,
                    D3D_SRV_DIMENSION.D3D11_SRV_DIMENSION_TEXTURE2D);
                using var srvTemp = default(ComPtr<ID3D11ShaderResourceView>);
                device.Get()->CreateShaderResourceView(
                        (ID3D11Resource*)texTemp.Get(),
                        &srvDesc,
                        srvTemp.GetAddressOf())
                    .ThrowOnError();

                this.desc = texDesc;
                srvTemp.Swap(ref this.srv);
                rtvTemp.Swap(ref this.rtv);
                texTemp.Swap(ref this.tex);

                Service<TextureManager>.Get().Blame(this, this.ownerPlugin);
                Service<TextureManager>.Get().BlameSetName(
                    this,
                    this.debugName ?? $"{nameof(ViewportTextureWrap)}({this.args})");
            }

            // context.Get()->CopyResource((ID3D11Resource*)this.tex.Get(), (ID3D11Resource*)backBuffer.Get());
            context.Get()->CopySubresourceRegion(
                (ID3D11Resource*)this.tex.Get(),
                0,
                0,
                0,
                0,
                (ID3D11Resource*)backBuffer.Get(),
                0,
                &copyBox);

            if (!this.args.KeepTransparency)
            {
                var rtvLocal = this.rtv.Get();
                context.Get()->OMSetRenderTargets(1u, &rtvLocal, null);
                Service<TextureManager>.Get().SimpleDrawer.StripAlpha(context.Get());

                var dummy = default(ID3D11RenderTargetView*);
                context.Get()->OMSetRenderTargets(1u, &dummy, null);
            }

            this.firstUpdateTaskCompletionSource.TrySetResult(this);
        }
        catch (Exception e)
        {
            this.firstUpdateTaskCompletionSource.TrySetException(e);
        }

        if (this.args.AutoUpdate)
            this.QueueUpdate();
    }

    /// <summary>Queues a call to <see cref="Update"/>.</summary>
    public void QueueUpdate() =>
        Service<Framework>.Get().RunOnTick(
            () =>
            {
                if (this.args.TakeBeforeImGuiRender)
                    Service<InterfaceManager>.Get().RunBeforeImGuiRender(this.Update);
                else
                    Service<InterfaceManager>.Get().RunAfterImGuiRender(this.Update);
            },
            cancellationToken: this.cancellationToken);

    /// <summary>Queue the texture to be disposed once the frame ends. </summary>
    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Actually dispose the wrapped texture.</summary>
    void IDeferredDisposable.RealDispose()
    {
        _ = this.FirstUpdateTask.Exception;
        this.tex.Reset();
        this.srv.Reset();
        this.rtv.Reset();
    }

    private static unsafe ComPtr<ID3D11Texture2D> GetImGuiViewportBackBuffer(uint viewportId)
    {
        ThreadSafety.AssertMainThread();
        var viewports = ImGui.GetPlatformIO().Viewports;
        var viewportIndex = 0;
        for (; viewportIndex < viewports.Size; viewportIndex++)
        {
            if (viewports[viewportIndex].ID == viewportId)
                break;
        }

        if (viewportIndex >= viewports.Size)
        {
            throw new ArgumentOutOfRangeException(
                nameof(viewportId),
                viewportId,
                "Could not find a viewport with the given ID.");
        }

        var texture = default(ComPtr<ID3D11Texture2D>);

        Debug.Assert(viewports[0].ID == ImGui.GetMainViewport().ID, "ImGui has changed");
        if (viewportId == viewports[0].ID)
        {
            var device = FFXIVClientStructs.FFXIV.Client.Graphics.Kernel.Device.Instance();
            fixed (Guid* piid = &IID.IID_ID3D11Texture2D)
            {
                ((IDXGISwapChain*)device->SwapChain->DXGISwapChain)->GetBuffer(0, piid, (void**)texture.GetAddressOf())
                    .ThrowOnError();
            }
        }
        else
        {
            // See: ImGui_Impl_DX11.ImGuiViewportDataDx11
            var rud = (nint*)viewports[viewportIndex].RendererUserData;
            if (rud == null || rud[0] == nint.Zero || rud[1] == nint.Zero)
                throw new InvalidOperationException();

            using var resource = default(ComPtr<ID3D11Resource>);
            ((ID3D11RenderTargetView*)rud[1])->GetResource(resource.GetAddressOf());
            resource.As(&texture).ThrowOnError();
        }

        return texture;
    }

    private void Dispose(bool disposing)
    {
        this.disposed = true;
        this.args.AutoUpdate = false;
        if (disposing)
            Service<InterfaceManager>.GetNullable()?.EnqueueDeferredDispose(this);
        else
            ((IDeferredDisposable)this).RealDispose();
    }
}
