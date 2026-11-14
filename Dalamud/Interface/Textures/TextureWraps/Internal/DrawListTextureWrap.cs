using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Textures.Internal;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Plugin.Services;
using Dalamud.Utility;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Dalamud.Interface.Textures.TextureWraps.Internal;

/// <inheritdoc cref="IDrawListTextureWrap"/>
internal sealed unsafe partial class DrawListTextureWrap : IDrawListTextureWrap, IDeferredDisposable
{
    private readonly TextureManager textureManager;
    private readonly IDalamudTextureWrap emptyTexture;
    private readonly LocalPlugin? plugin;
    private readonly string debugName;

    private ComPtr<ID3D11Device> device;
    private ComPtr<ID3D11DeviceContext> deviceContext;
    private ComPtr<ID3D11Texture2D> tex;
    private ComPtr<ID3D11ShaderResourceView> srv;
    private ComPtr<ID3D11RenderTargetView> rtv;

    private ComPtr<ID3D11Texture2D> texPremultiplied;
    private ComPtr<ID3D11ShaderResourceView> srvPremultiplied;
    private ComPtr<ID3D11RenderTargetView> rtvPremultiplied;

    private int width;
    private int height;
    private DXGI_FORMAT format = DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM;

    /// <summary>Initializes a new instance of the <see cref="DrawListTextureWrap"/> class.</summary>
    /// <param name="device">Pointer to a D3D11 device. Ownership is taken.</param>
    /// <param name="textureManager">Instance of the <see cref="ITextureProvider"/> class.</param>
    /// <param name="emptyTexture">Texture to use, if <see cref="Width"/> or <see cref="Height"/> is <c>0</c>.</param>
    /// <param name="plugin">Plugin that holds responsible for this texture.</param>
    /// <param name="debugName">Name for debug display purposes.</param>
    public DrawListTextureWrap(
        ComPtr<ID3D11Device> device,
        TextureManager textureManager,
        IDalamudTextureWrap emptyTexture,
        LocalPlugin? plugin,
        string debugName)
    {
        this.textureManager = textureManager;
        this.emptyTexture = emptyTexture;
        this.plugin = plugin;
        this.debugName = debugName;

        if (device.IsEmpty())
            throw new ArgumentNullException(nameof(device));

        this.device.Swap(ref device);
        fixed (ID3D11DeviceContext** pdc = &this.deviceContext.GetPinnableReference())
            this.device.Get()->GetImmediateContext(pdc);

        this.emptyTexture = emptyTexture;
        this.srv = new((ID3D11ShaderResourceView*)emptyTexture.Handle.Handle);
    }

    /// <summary>Finalizes an instance of the <see cref="DrawListTextureWrap"/> class.</summary>
    ~DrawListTextureWrap() => this.RealDispose();

    /// <inheritdoc/>
    public ImTextureID Handle => new(this.srv.Get());

    /// <inheritdoc cref="IDrawListTextureWrap.Width"/>
    public int Width
    {
        get => this.width;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value, nameof(value));
            this.Resize(value, this.height, this.format);
        }
    }

    /// <inheritdoc cref="IDrawListTextureWrap.Height"/>
    public int Height
    {
        get => this.height;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value, nameof(value));
            this.Resize(this.width, value, this.format).ThrowOnError();
        }
    }

    /// <inheritdoc cref="IDrawListTextureWrap.Size"/>
    public Vector2 Size
    {
        get => new(this.width, this.height);
        set
        {
            if (value.X is <= 0 or float.NaN)
                throw new ArgumentOutOfRangeException(nameof(value), value, "X component is invalid.");
            if (value.Y is <= 0 or float.NaN)
                throw new ArgumentOutOfRangeException(nameof(value), value, "Y component is invalid.");
            this.Resize((int)MathF.Ceiling(value.X), (int)MathF.Ceiling(value.Y), this.format).ThrowOnError();
        }
    }

    /// <inheritdoc/>
    public Vector4 ClearColor { get; set; }

    /// <summary>Gets or sets the <see cref="DXGI_FORMAT"/>.</summary>
    public int DxgiFormat
    {
        get => (int)this.format;
        set
        {
            if (!this.textureManager.IsDxgiFormatSupportedForCreateFromExistingTextureAsync((DXGI_FORMAT)value))
            {
                throw new ArgumentException(
                    "Specified format is not a supported rendering target format.",
                    nameof(value));
            }

            this.Resize(this.width, this.Height, (DXGI_FORMAT)value).ThrowOnError();
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (Service<InterfaceManager>.GetNullable() is { } im)
            im.EnqueueDeferredDispose(this);
        else
            this.RealDispose();
    }

    /// <inheritdoc/>
    public void RealDispose()
    {
        this.srv.Reset();
        this.tex.Reset();
        this.rtv.Reset();
        this.srvPremultiplied.Reset();
        this.texPremultiplied.Reset();
        this.rtvPremultiplied.Reset();
        this.device.Reset();
        this.deviceContext.Reset();

#pragma warning disable CA1816
        GC.SuppressFinalize(this);
#pragma warning restore CA1816
    }

    /// <inheritdoc/>
    public void Draw(ImDrawListPtr drawListPtr, Vector2 displayPos, Vector2 scale) =>
        this.Draw(
            new ImDrawData
            {
                Valid = 1,
                CmdListsCount = 1,
                TotalIdxCount = drawListPtr.IdxBuffer.Size,
                TotalVtxCount = drawListPtr.VtxBuffer.Size,
                CmdLists = (ImDrawList**)(&drawListPtr),
                DisplayPos = displayPos,
                DisplaySize = this.Size,
                FramebufferScale = scale,
            });

    /// <inheritdoc/>
    public void Draw(scoped in ImDrawData drawData)
    {
        fixed (ImDrawData* pDrawData = &drawData)
            this.Draw(new(pDrawData));
    }

    /// <inheritdoc/>
    public void Draw(ImDrawDataPtr drawData)
    {
        ThreadSafety.AssertMainThread();

        // Managed framework callbacks run outside native-update exclusion. Serialize clearing, drawing,
        // and state restoration with presentation work.
        using var drawScope = ThreadSafety.NativeFrameworkRenderSyncRoot.EnterScope();

        if (this.rtv.IsEmpty())
            return;

        // Clear even when there is no valid draw data.
        var clearColor = this.ClearColor;
        this.deviceContext.Get()->ClearRenderTargetView(this.rtvPremultiplied.Get(), (float*)&clearColor);

        if (!drawData.Valid
            || drawData.CmdListsCount < 1
            || drawData.TotalIdxCount < 1
            || drawData.TotalVtxCount < 1
            || drawData.CmdLists.IsNull
            || drawData.DisplaySize.X <= 0
            || drawData.DisplaySize.Y <= 0
            || drawData.FramebufferScale.X == 0
            || drawData.FramebufferScale.Y == 0)
            return;

        // Render with premultiplied alpha, then convert to the exposed straight-alpha texture.
        using (new DeviceContextStateBackup(this.device.Get()->GetFeatureLevel(), this.deviceContext))
        {
            Service<Renderer>.Get().RenderDrawData(this.rtvPremultiplied.Get(), drawData);
            Service<Renderer>.Get().MakeStraight(this.srvPremultiplied.Get(), this.rtv.Get());
        }
    }

    /// <summary>Resizes the render targets, or selects the empty texture if either dimension is zero.</summary>
    /// <param name="newWidth">New texture width.</param>
    /// <param name="newHeight">New texture height.</param>
    /// <param name="newFormat">New format.</param>
    /// <returns><see cref="S.S_OK"/> if new render targets were installed; <see cref="S.S_FALSE"/> if the empty texture
    /// was selected or the dimensions were unchanged; otherwise, a failing <see cref="HRESULT"/>.</returns>
    private HRESULT Resize(int newWidth, int newHeight, DXGI_FORMAT newFormat)
    {
        if (newWidth < 0 || newHeight < 0)
            return E.E_INVALIDARG;

        if (newWidth == 0 || newHeight == 0)
        {
            using var emptySrv = new ComPtr<ID3D11ShaderResourceView>(
                (ID3D11ShaderResourceView*)this.emptyTexture.Handle.Handle);
            this.RetireResources();
            emptySrv.Swap(ref this.srv);
            this.width = newWidth;
            this.height = newHeight;
            return S.S_FALSE;
        }

        if (this.width == newWidth && this.height == newHeight)
            return S.S_FALSE;

        // Allocate both alpha variants before replacing any current resources.
        using var tmptex = default(ComPtr<ID3D11Texture2D>);
        using var tmpsrv = default(ComPtr<ID3D11ShaderResourceView>);
        using var tmprtv = default(ComPtr<ID3D11RenderTargetView>);
        using var tmptexPremultiplied = default(ComPtr<ID3D11Texture2D>);
        using var tmpsrvPremultiplied = default(ComPtr<ID3D11ShaderResourceView>);
        using var tmprtvPremultiplied = default(ComPtr<ID3D11RenderTargetView>);

        var tmpTexDesc = new D3D11_TEXTURE2D_DESC
        {
            Width = (uint)newWidth,
            Height = (uint)newHeight,
            MipLevels = 1,
            ArraySize = 1,
            Format = newFormat,
            SampleDesc = new(1, 0),
            Usage = D3D11_USAGE.D3D11_USAGE_DEFAULT,
            BindFlags = (uint)(D3D11_BIND_FLAG.D3D11_BIND_SHADER_RESOURCE |
                               D3D11_BIND_FLAG.D3D11_BIND_RENDER_TARGET),
            CPUAccessFlags = 0u,
            MiscFlags = 0u,
        };
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

        hr = this.device.Get()->CreateTexture2D(&tmpTexDesc, null, tmptexPremultiplied.GetAddressOf());
        if (hr.FAILED)
            return hr;

        tmpres = (ID3D11Resource*)tmptexPremultiplied.Get();
        srvDesc = new(tmptexPremultiplied, D3D_SRV_DIMENSION.D3D11_SRV_DIMENSION_TEXTURE2D);
        hr = this.device.Get()->CreateShaderResourceView(tmpres, &srvDesc, tmpsrvPremultiplied.GetAddressOf());
        if (hr.FAILED)
            return hr;

        rtvDesc = new(tmptexPremultiplied, D3D11_RTV_DIMENSION.D3D11_RTV_DIMENSION_TEXTURE2D);
        hr = this.device.Get()->CreateRenderTargetView(tmpres, &rtvDesc, tmprtvPremultiplied.GetAddressOf());
        if (hr.FAILED)
            return hr;

        this.RetireResources();
        tmptex.Swap(ref this.tex);
        tmpsrv.Swap(ref this.srv);
        tmprtv.Swap(ref this.rtv);
        tmptexPremultiplied.Swap(ref this.texPremultiplied);
        tmpsrvPremultiplied.Swap(ref this.srvPremultiplied);
        tmprtvPremultiplied.Swap(ref this.rtvPremultiplied);
        this.width = newWidth;
        this.height = newHeight;
        this.format = newFormat;

        this.textureManager.BlameSetName(this, this.debugName);
        this.textureManager.Blame(this, this.plugin);
        return S.S_OK;
    }

    /// <summary>Retires both alpha variants and clears their owning fields.</summary>
    /// <remarks>Live draw commands and replayable snapshots may still borrow the retired resources.</remarks>
    private void RetireResources()
    {
        var retiredTex = this.tex;
        var retiredSrv = this.srv;
        var retiredRtv = this.rtv;
        var retiredTexPremultiplied = this.texPremultiplied;
        var retiredSrvPremultiplied = this.srvPremultiplied;
        var retiredRtvPremultiplied = this.rtvPremultiplied;
        Action release = () =>
        {
            retiredSrv.Dispose();
            retiredRtv.Dispose();
            retiredTex.Dispose();
            retiredSrvPremultiplied.Dispose();
            retiredRtvPremultiplied.Dispose();
            retiredTexPremultiplied.Dispose();
        };

        if (Service<InterfaceManager>.GetNullable() is { } im)
            im.DeferUntilFrameRetired(release);
        else
            release();

        // Transfer the copied COM references without releasing them from these fields.
        this.tex.Detach();
        this.srv.Detach();
        this.rtv.Detach();
        this.texPremultiplied.Detach();
        this.srvPremultiplied.Detach();
        this.rtvPremultiplied.Detach();
    }
}
