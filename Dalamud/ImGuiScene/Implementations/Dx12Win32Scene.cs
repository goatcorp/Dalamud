using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using Dalamud.ImGuiScene.Helpers;
using Dalamud.Interface.Internal;
using Dalamud.Utility;

using ImGuiNET;

using ImGuizmoNET;

using ImPlotNET;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Dalamud.ImGuiScene.Implementations;

/// <summary>
/// Backend for ImGui, using <see cref="Dx12Renderer"/> and <see cref="Win32InputHandler"/>.
/// </summary>
[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "DX12")]
[SuppressMessage(
    "StyleCop.CSharp.LayoutRules",
    "SA1519:Braces should not be omitted from multi-line child statement",
    Justification = "Multiple fixed/using scopes")]
internal sealed unsafe class Dx12Win32Scene : IWin32Scene
{
    private readonly Dx12Renderer imguiRenderer;
    private readonly Win32InputHandler imguiInput;
    private readonly WicEasy wicEasy;

    private ComPtr<IDXGISwapChain3> swapChainPossiblyWrapped;
    private ComPtr<IDXGISwapChain3> swapChain;
    private ComPtr<ID3D12Device> device;

    private int targetWidth;
    private int targetHeight;

    /// <summary>
    /// Initializes a new instance of the <see cref="Dx12Win32Scene"/> class.
    /// </summary>
    /// <param name="swapChain">The pointer to an instance of <see cref="IDXGISwapChain3"/>. The reference is copied.</param>
    /// <param name="commandQueue">The pointer to an instance of <see cref="ID3D12CommandQueue"/>. The reference is copied.</param>
    /// <param name="device">The pointer to an instance of <see cref="ID3D12Device"/>. The reference is copied.</param>
    public Dx12Win32Scene(IDXGISwapChain3* swapChain, ID3D12CommandQueue* commandQueue, ID3D12Device* device)
    {
        if (device is null || swapChain is null)
            throw new NullReferenceException();

        this.wicEasy = new();
        try
        {
            this.device = new(device);
            this.swapChainPossiblyWrapped = new(swapChain);
            this.swapChain = new(swapChain);
            fixed (ComPtr<IDXGISwapChain3>* ppSwapChain = &this.swapChain)
                ReShadePeeler.PeelSwapChain(ppSwapChain);
            fixed (ComPtr<ID3D12Device>* ppDevice = &this.device)
                ReShadePeeler.PeelD3D12Device(ppDevice);

            var desc = default(DXGI_SWAP_CHAIN_DESC);
            swapChain->GetDesc(&desc).ThrowOnError();
            this.targetWidth = (int)desc.BufferDesc.Width;
            this.targetHeight = (int)desc.BufferDesc.Height;
            this.WindowHandlePtr = desc.OutputWindow;

            var ctx = ImGui.CreateContext();
            ImGuizmo.SetImGuiContext(ctx);
            ImPlot.SetImGuiContext(ctx);
            ImPlot.CreateContext();

            ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DockingEnable | ImGuiConfigFlags.ViewportsEnable;

            this.imguiRenderer = new(swapChain, device, commandQueue);
            this.imguiInput = new(this.WindowHandlePtr);
        }
        catch
        {
            this.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Dx12Win32Scene"/> class.
    /// </summary>
    /// <param name="device">The pointer to an instance of <see cref="ID3D12Device"/>. The reference is copied.</param>
    /// <param name="hwnd">The window handle for input processing.</param>
    /// <param name="targetWidth">Initial target width.</param>
    /// <param name="targetHeight">Initial target height.</param>
    public Dx12Win32Scene(ID3D12Device* device, HWND hwnd, int targetWidth, int targetHeight)
    {
        if (device is null)
            throw new NullReferenceException();
        
        this.wicEasy = new();
        try
        {
            this.device = new(device);
            fixed (ComPtr<ID3D12Device>* ppDevice = &this.device)
                ReShadePeeler.PeelD3D12Device(ppDevice);

            this.targetWidth = targetWidth;
            this.targetHeight = targetHeight;
            this.WindowHandlePtr = hwnd;

            var ctx = ImGui.CreateContext();
            ImGuizmo.SetImGuiContext(ctx);
            ImPlot.SetImGuiContext(ctx);
            ImPlot.CreateContext();

            ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DockingEnable | ImGuiConfigFlags.ViewportsEnable;

            this.imguiRenderer = new(device, DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM, 2, targetWidth, targetHeight);
            this.imguiInput = new(this.WindowHandlePtr);
        }
        catch
        {
            this.wicEasy.Dispose();
            this.ReleaseUnmanagedResources();
            throw;
        }
    }

    /// <summary>
    /// Finalizes an instance of the <see cref="Dx12Win32Scene"/> class.
    /// </summary>
    ~Dx12Win32Scene() => this.ReleaseUnmanagedResources();

    /// <inheritdoc/>
    public event IImGuiScene.BuildUiDelegate? BuildUi;

    /// <inheritdoc/>
    public event IImGuiScene.NewInputFrameDelegate? NewInputFrame;

    /// <inheritdoc/>
    public event IImGuiScene.NewRenderFrameDelegate? NewRenderFrame;

    /// <inheritdoc/>
    public bool UpdateCursor
    {
        get => this.imguiInput.UpdateCursor;
        set => this.imguiInput.UpdateCursor = value;
    }

    /// <inheritdoc/>
    public string? IniPath
    {
        get => this.imguiInput.IniPath;
        set => this.imguiInput.IniPath = value;
    }

    /// <summary>
    /// Gets the pointer to an instance of <see cref="IDXGISwapChain3"/>.
    /// </summary>
    public IDXGISwapChain3* SwapChain => this.swapChain;

    /// <summary>
    /// Gets the pointer to an instance of <see cref="ID3D12Device"/>.
    /// </summary>
    public ID3D12Device* Device => this.device;

    /// <summary>
    /// Gets the pointer to an instance of <see cref="ID3D12Device"/>, in <see cref="nint"/>.
    /// </summary>
    public nint DeviceHandle => (nint)this.device.Get();

    /// <summary>
    /// Gets the window handle.
    /// </summary>
    public HWND WindowHandlePtr { get; private set; }

    /// <summary>
    /// Gets the input handler.
    /// </summary>
    public Win32InputHandler InputHandler => this.imguiInput;

    /// <summary>
    /// Gets the renderer.
    /// </summary>
    public Dx12Renderer Renderer => this.imguiRenderer;

    /// <inheritdoc/>
    public void Dispose()
    {
        this.wicEasy.Dispose();
        this.ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    public nint? ProcessWndProcW(HWND hWnd, uint msg, WPARAM wParam, LPARAM lParam) =>
        this.imguiInput.ProcessWndProcW(hWnd, msg, wParam, lParam);

    /// <inheritdoc/>
    public void Render()
    {
        this.imguiRenderer.OnNewFrame();
        this.NewRenderFrame?.Invoke();
        this.imguiInput.NewFrame(this.targetWidth, this.targetHeight);
        this.NewInputFrame?.Invoke();

        ImGui.NewFrame();
        ImGuizmo.BeginFrame();

        this.BuildUi?.Invoke();

        ImGui.Render();

        this.imguiRenderer.RenderDrawData(ImGui.GetDrawData());

        ImGui.UpdatePlatformWindows();
        ImGui.RenderPlatformWindowsDefault();
    }

    /// <inheritdoc/>
    public void OnPreResize() => this.imguiRenderer.OnPreResize();

    /// <inheritdoc/>
    public void OnPostResize(int newWidth, int newHeight)
    {
        this.imguiRenderer.OnPostResize(newWidth, newHeight);
        this.targetWidth = newWidth;
        this.targetHeight = newHeight;
    }

    /// <inheritdoc/>
    public void InvalidateFonts() => this.imguiRenderer.RebuildFontTexture();

    /// <inheritdoc/>
    public bool SupportsTextureFormat(int format) =>
        this.SupportsTextureFormat((DXGI_FORMAT)format);

    /// <inheritdoc/>
    public IDalamudTextureWrap CreateTexture2DFromFile(string path, [CallerMemberName] string debugName = "")
    {
        using var stream = WicEasyExtensions.CreateStreamFromFile(path);
        return this.LoadImage(stream, debugName);
    }

    /// <inheritdoc/>
    public IDalamudTextureWrap CreateTexture2DFromBytes(
        ReadOnlySpan<byte> data,
        [CallerMemberName] string debugName = "")
    {
        using var stream = default(ComPtr<IWICStream>);
        this.wicEasy.Factory->CreateStream(stream.ReleaseAndGetAddressOf()).ThrowOnError();
        fixed (byte* pData = data)
        {
            stream.Get()->InitializeFromMemory(pData, (uint)data.Length).ThrowOnError();
            return this.LoadImage((IStream*)stream.Get());
        }
    }

    /// <inheritdoc/>
    public IDalamudTextureWrap CreateTexture2DFromRaw(
        ReadOnlySpan<byte> data,
        int pitch,
        int width,
        int height,
        int format,
        [CallerMemberName] string debugName = "")
    {
        fixed (void* pData = data)
            return this.LoadImageRaw(pData, pitch, width, height, (DXGI_FORMAT)format, debugName);
    }

    /// <inheritdoc cref="Dx11Renderer.CreateTexturePipeline"/>
    public ITexturePipelineWrap CreateTexturePipeline(
        ReadOnlySpan<byte> ps,
        in D3D12_STATIC_SAMPLER_DESC samplerDesc,
        [CallerMemberName] string debugName = "")
        => this.imguiRenderer.CreateTexturePipeline(ps, samplerDesc, debugName);

    /// <inheritdoc/>
    public void SetTexturePipeline(IDalamudTextureWrap textureHandle, ITexturePipelineWrap? pipelineHandle) =>
        this.imguiRenderer.SetTexturePipeline(textureHandle, pipelineHandle);

    /// <inheritdoc/>
    public ITexturePipelineWrap? GetTexturePipeline(IDalamudTextureWrap textureHandle) =>
        this.imguiRenderer.GetTexturePipeline(textureHandle);

    /// <inheritdoc/>
    public bool IsImGuiCursor(nint cursorHandle) => this.imguiInput.IsImGuiCursor(cursorHandle);

    /// <inheritdoc/>
    public bool IsAttachedToPresentationTarget(nint targetHandle) =>
        this.swapChain.Get() == (void*)targetHandle
        || this.swapChainPossiblyWrapped.Get() == (void*)targetHandle;

    /// <inheritdoc/>
    public bool IsMainViewportFullScreen()
    {
        BOOL fullscreen;
        this.swapChain.Get()->GetFullscreenState(&fullscreen, null);
        return fullscreen;
    }

    /// <summary>
    /// Determines whether the current D3D12 Device supports the given DXGI format.
    /// </summary>
    /// <param name="dxgiFormat">DXGI format to check.</param>
    /// <param name="formatSupport1">First format to test.</param>
    /// <param name="formatSupport2">Second format to test.</param>
    /// <returns>Whether it is supported.</returns>
    public bool SupportsTextureFormat(
        DXGI_FORMAT dxgiFormat,
        D3D12_FORMAT_SUPPORT1 formatSupport1 = D3D12_FORMAT_SUPPORT1.D3D12_FORMAT_SUPPORT1_TEXTURE2D,
        D3D12_FORMAT_SUPPORT2 formatSupport2 = D3D12_FORMAT_SUPPORT2.D3D12_FORMAT_SUPPORT2_NONE)
    {
        var data = new D3D12_FEATURE_DATA_FORMAT_SUPPORT { Format = dxgiFormat };
        if (this.Device->CheckFeatureSupport(
                D3D12_FEATURE.D3D12_FEATURE_FORMAT_SUPPORT,
                &data,
                (uint)sizeof(D3D12_FEATURE_DATA_FORMAT_SUPPORT)).FAILED)
            return false;

        return (data.Support1 & formatSupport1) == formatSupport1
                && (data.Support2 & formatSupport2) == formatSupport2;
    }

    /// <summary>
    /// Loads an image from an <see cref="IStream"/>. The ownership is not transferred.
    /// </summary>
    /// <param name="stream">The stream.</param>
    /// <param name="debugName">The debug name.</param>
    /// <returns>The loaded image.</returns>
    public IDalamudTextureWrap LoadImage(IStream* stream, [CallerMemberName] string debugName = "")
    {
        using var source = this.wicEasy.CreateBitmapSource(stream);

        var dxgiFormat = source.Get()->GetPixelFormat().ToDxgiFormat();
        if (dxgiFormat == DXGI_FORMAT.DXGI_FORMAT_UNKNOWN || !this.SupportsTextureFormat(dxgiFormat))
        {
            using var converted = this.wicEasy.ConvertPixelFormat(source, GUID.GUID_WICPixelFormat32bppBGRA);
            converted.Swap(&source);
            dxgiFormat = DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM;
        }

        using var bitmap = default(ComPtr<IWICBitmap>);
        if (source.CopyTo(&bitmap).FAILED)
            this.wicEasy.CreateBitmap(source).Swap(&bitmap);
        
        using var l = bitmap.Get()->LockBits(WICBitmapLockFlags.WICBitmapLockRead, out var pb, out _, out _);
        uint stride, width, height;
        l.Get()->GetStride(&stride).ThrowOnError();
        l.Get()->GetSize(&width, &height).ThrowOnError();
        return this.LoadImageRaw(pb, (int)stride, (int)width, (int)height, dxgiFormat);
    }

    /// <summary>
    /// Load an image from a span of bytes of specified format.
    /// </summary>
    /// <param name="data">The data to load.</param>
    /// <param name="pitch">The pitch(stride) in bytes.</param>
    /// <param name="width">The width in pixels.</param>
    /// <param name="height">The height in pixels.</param>
    /// <param name="dxgiFormat">Format of the texture.</param>
    /// <param name="debugName">The debug name.</param>
    /// <returns>A texture, ready to use in ImGui.</returns>
    public IDalamudTextureWrap LoadImageRaw(
        void* data,
        int pitch,
        int width,
        int height,
        DXGI_FORMAT dxgiFormat,
        [CallerMemberName] string debugName = "") =>
        this.imguiRenderer.LoadTexture(new(data, pitch * height), pitch, width, height, (int)dxgiFormat);

    private void ReleaseUnmanagedResources()
    {
        if (this.device.IsEmpty())
            return;

        this.imguiRenderer.Dispose();
        this.imguiInput.Dispose();

        ImGui.DestroyContext();

        this.swapChain.Dispose();
        this.device.Dispose();
        this.swapChainPossiblyWrapped.Dispose();
    }
}
