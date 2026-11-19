using System.Threading;
using System.Threading.Tasks;

using Dalamud.Bindings.ImGui;
using Dalamud.Game;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Textures.Internal;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Storage.Assets;
using Dalamud.Utility;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

using NotSupportedException = System.NotSupportedException;

namespace Dalamud.Interface.Textures.TextureWraps.Internal;

/// <summary>Owns a texture containing a copy of an ImGui viewport's back buffer.</summary>
internal sealed class ViewportTextureWrap : IDalamudTextureWrap, IDeferredDisposable
{
    private readonly string? debugName;
    private readonly LocalPlugin? ownerPlugin;
    private readonly CancellationToken cancellationToken;
    private readonly TaskCompletionSource<IDalamudTextureWrap> firstUpdateTaskCompletionSource =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

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
    public unsafe ImTextureID Handle
    {
        get
        {
            var t = (nint)this.srv.Get();
            return t == nint.Zero ? Service<DalamudAssetManager>.Get().Empty4X4.Handle : new ImTextureID(this.srv.Get());
        }
    }

    /// <inheritdoc/>
    public int Width => (int)this.desc.Width;

    /// <inheritdoc/>
    public int Height => (int)this.desc.Height;

    /// <summary>Gets the task for the initial viewport capture.</summary>
    public Task<IDalamudTextureWrap> FirstUpdateTask => this.firstUpdateTaskCompletionSource.Task;

    /// <summary>Schedules a viewport copy before or after a subsequent render through the framework queue.</summary>
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

    /// <summary>Queues the texture for disposal through <see cref="InterfaceManager.DeferUntilFrameRetired"/>.</summary>
    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Releases the wrapped D3D resources.</summary>
    void IDeferredDisposable.RealDispose()
    {
        _ = this.FirstUpdateTask.Exception;
        this.tex.Reset();
        this.srv.Reset();
        this.rtv.Reset();
    }

    /// <summary>Copies the viewport during a serialized presentation callback.</summary>
    private void Update()
    {
        if (this.disposed)
            return;

        if (this.cancellationToken.IsCancellationRequested || this.disposed)
        {
            this.firstUpdateTaskCompletionSource.TrySetCanceled();
            return;
        }

        try
        {
            var backend = Service<InterfaceManager>.Get().Backend
                          ?? throw new InvalidOperationException("The ImGui backend is not available.");
            var textureChanged = false;
            if (!backend.TryReadViewportBackBuffer(this.args.ViewportId, handle => textureChanged = this.UpdateFromBackBuffer(handle)))
            {
                // Retry suspended captures even when automatic updates are disabled.
                this.QueueUpdate();
                return;
            }

            // Keep tracking locks and task completion outside the backend graphics locks.
            if (textureChanged)
            {
                Service<TextureManager>.Get().Blame(this, this.ownerPlugin);
                Service<TextureManager>.Get().BlameSetName(
                    this,
                    this.debugName ?? $"{nameof(ViewportTextureWrap)}({this.args})");
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

    /// <summary>Copies a borrowed back buffer under the backend's graphics and snapshot locks.</summary>
    /// <param name="backBufferHandle">The texture pointer, valid only during this callback.</param>
    /// <returns>Whether the destination texture was replaced.</returns>
    private unsafe bool UpdateFromBackBuffer(nint backBufferHandle)
    {
        var backBuffer = (ID3D11Texture2D*)backBufferHandle;
        D3D11_TEXTURE2D_DESC newDesc;
        backBuffer->GetDesc(&newDesc);

        if (newDesc.SampleDesc.Count > 1)
            throw new NotSupportedException("Multisampling is not expected");

        using var device = default(ComPtr<ID3D11Device>);
        backBuffer->GetDevice(device.GetAddressOf());

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

        var textureChanged = this.desc.Width != copyBox.right - copyBox.left
                             || this.desc.Height != copyBox.bottom - copyBox.top
                             || this.desc.Format != newDesc.Format;
        if (textureChanged)
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

            if (this.srv.Get() is not null)
            {
                // Retain replaced resources until snapshots stop borrowing the old SRV.
                // Detach the fields to transfer their COM references to the retirement callback.
                var retiredSrv = this.srv;
                var retiredRtv = this.rtv;
                var retiredTex = this.tex;
                Service<InterfaceManager>.Get().DeferUntilFrameRetired(
                    () =>
                    {
                        retiredSrv.Dispose();
                        retiredRtv.Dispose();
                        retiredTex.Dispose();
                    });
                this.srv.Detach();
                this.rtv.Detach();
                this.tex.Detach();
            }

            this.desc = texDesc;
            srvTemp.Swap(ref this.srv);
            rtvTemp.Swap(ref this.rtv);
            texTemp.Swap(ref this.tex);
        }

        context.Get()->CopySubresourceRegion(
            (ID3D11Resource*)this.tex.Get(),
            0,
            0,
            0,
            0,
            (ID3D11Resource*)backBuffer,
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

        return textureChanged;
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
