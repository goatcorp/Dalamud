using System.Numerics;

using Dalamud.Interface.Internal;
using Dalamud.Utility.Numerics;

using ImGuiNET;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Dalamud.Interface.Spannables.Rendering.Internal;

/// <summary>A texture that can be drawn from an <see cref="ImDrawList"/>.</summary>
internal sealed unsafe partial class DrawListTexture : IDalamudTextureWrap
{
    private ComPtr<ID3D11Device> device;
    private ComPtr<ID3D11DeviceContext> deviceContext;
    private ComPtr<ID3D11Texture2D> tex;
    private ComPtr<ID3D11ShaderResourceView> srv;
    private ComPtr<ID3D11RenderTargetView> rtv;
    private ComPtr<ID3D11UnorderedAccessView> uav;

    /// <summary>Initializes a new instance of the <see cref="DrawListTexture"/> class.</summary>
    /// <param name="device11">The pointer to a D3D11 device.</param>
    public DrawListTexture(nint device11)
    {
        this.device = new((ID3D11Device*)device11);
        fixed (ID3D11DeviceContext** pdc = &this.deviceContext.GetPinnableReference())
            this.device.Get()->GetImmediateContext(pdc);
    }

    /// <summary>Finalizes an instance of the <see cref="DrawListTexture"/> class.</summary>
    ~DrawListTexture() => this.ReleaseUnmanagedResources();

    /// <inheritdoc/>
    public nint ImGuiHandle => (nint)this.srv.Get();

    /// <inheritdoc/>
    public int Width { get; private set; }

    /// <inheritdoc/>
    public int Height { get; private set; }

    /// <inheritdoc/>
    public Vector2 Size => new(this.Width, this.Height);

    /// <summary>Gets the last update time.</summary>
    public DateTime LastUpdate { get; private set; }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    /// <summary>Draws the draw list to the texture, resizing as necessary.</summary>
    /// <param name="drawListPtr">The pointer to the draw list.</param>
    /// <param name="clipRect">The clip rect.</param>
    /// <param name="clearColor">The color to clear with.</param>
    /// <param name="scale">The texture size scaling.</param>
    /// <param name="clipRectUv">The UV for the clip rect.</param>
    /// <returns>A <see cref="HRESULT"/> that represents the operation result.</returns>
    public HRESULT Draw(
        ImDrawListPtr drawListPtr,
        RectVector4 clipRect,
        Vector4 clearColor,
        Vector2 scale,
        out RectVector4 clipRectUv)
    {
        clipRectUv = default;
        if (clipRect.Width <= 0 || clipRect.Height <= 0
            || drawListPtr.NativePtr is null || drawListPtr.IdxBuffer.Size == 0 || drawListPtr.VtxBuffer.Size == 0)
            return S.S_FALSE;

        HRESULT hr = S.S_OK;
        if (this.Width < clipRect.Width * scale.X || this.Height < clipRect.Height * scale.Y)
            hr = this.Resize(Vector2.Max(this.Size, clipRect.Size * scale));
        if (hr.FAILED)
            return hr;

        clipRectUv = new(Vector2.Zero, clipRect.Size / this.Size);

        var dd = new ImDrawData
        {
            Valid = 1,
            CmdListsCount = 1,
            TotalIdxCount = drawListPtr.IdxBuffer.Size,
            TotalVtxCount = drawListPtr.VtxBuffer.Size,
            CmdLists = (ImDrawList**)(&drawListPtr),
            DisplayPos = clipRect.LeftTop,
            DisplaySize = clipRect.Size,
            FramebufferScale = scale,
        };

        using var bkup = new DeviceContextStateBackup(this.device.Get()->GetFeatureLevel(), this.deviceContext);

        this.deviceContext.Get()->ClearRenderTargetView(this.rtv.Get(), (float*)&clearColor);
        Service<Renderer>.Get().RenderDrawData(this.rtv.Get(), &dd);
        Service<Renderer>.Get().MakeStraight(this.uav.Get(), new(Vector2.Zero, clipRect.Size));

        this.LastUpdate = DateTime.Now;
        return S.S_OK;
    }

    /// <summary>Resizes the texture.</summary>
    /// <param name="dim">New texture dimensions.</param>
    /// <returns>A <see cref="HRESULT"/> that represents the operation result.</returns>
    public HRESULT Resize(Vector2 dim) => this.Resize((int)MathF.Ceiling(dim.X), (int)MathF.Ceiling(dim.Y));

    /// <summary>Resizes the texture.</summary>
    /// <param name="width">New texture width.</param>
    /// <param name="height">New texture height.</param>
    /// <returns>A <see cref="HRESULT"/> that represents the operation result.</returns>
    public HRESULT Resize(int width, int height)
    {
        if (width <= 0 || height <= 0)
            return S.S_FALSE;
        if (this.Width == width && this.Height == height)
            return S.S_FALSE;

        var tmpTexDesc = new D3D11_TEXTURE2D_DESC
        {
            Width = (uint)width,
            Height = (uint)height,
            MipLevels = 1,
            ArraySize = 1,
            Format = DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM,
            SampleDesc = new(1, 0),
            Usage = D3D11_USAGE.D3D11_USAGE_DEFAULT,
            BindFlags = (uint)(D3D11_BIND_FLAG.D3D11_BIND_SHADER_RESOURCE | D3D11_BIND_FLAG.D3D11_BIND_RENDER_TARGET |
                               D3D11_BIND_FLAG.D3D11_BIND_UNORDERED_ACCESS),
            CPUAccessFlags = 0u,
            MiscFlags = 0u,
        };
        using var tmptex = default(ComPtr<ID3D11Texture2D>);
        using var tmpsrv = default(ComPtr<ID3D11ShaderResourceView>);
        using var tmprtv = default(ComPtr<ID3D11RenderTargetView>);
        using var tmpuav = default(ComPtr<ID3D11UnorderedAccessView>);
        var hr = this.device.Get()->CreateTexture2D(&tmpTexDesc, null, tmptex.GetAddressOf());
        if (hr.FAILED)
            return hr;

        var tmpres = (ID3D11Resource*)tmptex.Get();
        var srvDesc = new D3D11_SHADER_RESOURCE_VIEW_DESC(tmptex, D3D_SRV_DIMENSION.D3D11_SRV_DIMENSION_TEXTURE2D);
        hr = this.device.Get()->CreateShaderResourceView(tmpres, &srvDesc, tmpsrv.GetAddressOf());
        if (hr.FAILED)
            return hr;

        var rtvDesc = new D3D11_RENDER_TARGET_VIEW_DESC(tmptex, D3D11_RTV_DIMENSION.D3D11_RTV_DIMENSION_TEXTURE2D);
        hr = this.device.Get()->CreateRenderTargetView(tmpres, &rtvDesc, tmprtv.GetAddressOf());
        if (hr.FAILED)
            return hr;

        var uavDesc = new D3D11_UNORDERED_ACCESS_VIEW_DESC(tmptex, D3D11_UAV_DIMENSION.D3D11_UAV_DIMENSION_TEXTURE2D);
        hr = this.device.Get()->CreateUnorderedAccessView(tmpres, &uavDesc, tmpuav.GetAddressOf());
        if (hr.FAILED)
            return hr;

        tmptex.Swap(ref this.tex);
        tmpsrv.Swap(ref this.srv);
        tmprtv.Swap(ref this.rtv);
        tmpuav.Swap(ref this.uav);
        this.Width = width;
        this.Height = height;

        return S.S_OK;
    }

    private void ReleaseUnmanagedResources()
    {
        this.srv.Reset();
        this.tex.Reset();
        this.rtv.Reset();
        this.uav.Reset();
        this.device.Reset();
        this.deviceContext.Reset();
    }
}
