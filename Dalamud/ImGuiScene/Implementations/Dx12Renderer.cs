using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Dalamud.ImGuiScene.Helpers;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Utility;
using Dalamud.Utility;

using ImGuiNET;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Dalamud.ImGuiScene.Implementations;

/// <summary>
/// Deals with rendering ImGui using DirectX 12.
/// See https://github.com/ocornut/imgui/blob/master/examples/imgui_impl_dx12.cpp for the original implementation.
/// </summary>
[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "DX12")]
[SuppressMessage(
    "StyleCop.CSharp.LayoutRules",
    "SA1519:Braces should not be omitted from multi-line child statement",
    Justification = "Multiple fixed/using scopes")]
internal unsafe partial class Dx12Renderer : IImGuiRenderer
{
    private readonly Dictionary<nint, IImGuiRenderer.DrawCmdUserCallbackDelegate> userCallbacks = new();
    private readonly List<IDalamudTextureWrap> fontTextures = new();

    private readonly TextureManager textureManager;
    private readonly ViewportHandler viewportHandler;
    private readonly nint renderNamePtr;

    private readonly DXGI_FORMAT rtvFormat;
    private readonly ViewportData mainViewport;

    private bool releaseUnmanagedResourceCalled;

    private ComPtr<ID3D12Device> device;
    private ComPtr<IDCompositionDevice> dcompDevice;

    private TexturePipeline? defaultPipeline;

    /// <summary>
    /// Initializes a new instance of the <see cref="Dx12Renderer"/> class,
    /// from existing swap chain, device, and command queue.
    /// </summary>
    /// <param name="swapChain">The swap chain.</param>
    /// <param name="device">The device.</param>
    /// <param name="commandQueue">The command queue.</param>
    public Dx12Renderer(IDXGISwapChain3* swapChain, ID3D12Device* device, ID3D12CommandQueue* commandQueue)
    {
        if (swapChain is null)
            throw new NullReferenceException($"{nameof(swapChain)} cannot be null.");
        if (device is null)
            throw new NullReferenceException($"{nameof(device)} cannot be null.");
        if (commandQueue is null)
            throw new NullReferenceException($"{nameof(commandQueue)} cannot be null.");

        using var mySwapChain = new ComPtr<IDXGISwapChain3>(swapChain);
        using var myDevice = new ComPtr<ID3D12Device>(device);
        using var myCommandQueue = new ComPtr<ID3D12CommandQueue>(commandQueue);
        ReShadePeeler.PeelSwapChain(&mySwapChain);
        ReShadePeeler.PeelD3D12Device(&myDevice);
        ReShadePeeler.PeelD3D12CommandQueue(&myCommandQueue);

        var io = ImGui.GetIO();
        if (ImGui.GetIO().NativePtr->BackendRendererName is not null)
            throw new InvalidOperationException("ImGui backend renderer seems to be have been already attached.");

        DXGI_SWAP_CHAIN_DESC desc;
        mySwapChain.Get()->GetDesc(&desc).ThrowOnError();
        this.NumBackBuffers = (int)desc.BufferCount;
        this.rtvFormat = desc.BufferDesc.Format;
        myDevice.Swap(ref this.device);

        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset | ImGuiBackendFlags.RendererHasViewports;

        this.renderNamePtr = Marshal.StringToHGlobalAnsi("imgui_impl_dx12_c#");
        io.NativePtr->BackendRendererName = (byte*)this.renderNamePtr;

        try
        {
            if (io.ConfigFlags.HasFlag(ImGuiConfigFlags.ViewportsEnable))
            {
                try
                {
                    fixed (IDCompositionDevice** pp = &this.dcompDevice.GetPinnableReference())
                    fixed (Guid* piidDCompositionDevice = &IID.IID_IDCompositionDevice)
                        DirectX.DCompositionCreateDevice(null, piidDCompositionDevice, (void**)pp).ThrowOnError();

                    ImGuiViewportHelpers.EnableViewportWindowBackgroundAlpha();
                }
                catch
                {
                    // don't care; not using DComposition then
                }

                this.viewportHandler = new(this);
            }

            this.mainViewport = ViewportData.Create(
                this,
                mySwapChain,
                myCommandQueue,
                nameof(this.mainViewport),
                null,
                null);
            ImGui.GetPlatformIO().Viewports[0].RendererUserData = this.mainViewport.AsHandle();
            this.textureManager = new(this.device);
        }
        catch
        {
            this.textureManager?.Dispose();
            this.viewportHandler?.Dispose();
            this.ReleaseUnmanagedResources();
            throw;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Dx12Renderer"/> class,
    /// without using any swap buffer, for offscreen rendering.
    /// </summary>
    /// <param name="device">The device.</param>
    /// <param name="rtvFormat">The format of render target.</param>
    /// <param name="numBackBuffers">Number of back buffers.</param>
    /// <param name="width">The width.</param>
    /// <param name="height">The height of render target.</param>
    public Dx12Renderer(ID3D12Device* device, DXGI_FORMAT rtvFormat, int numBackBuffers, int width, int height)
    {
        if (device is null)
            throw new NullReferenceException($"{nameof(device)} cannot be null.");
        if (rtvFormat == DXGI_FORMAT.DXGI_FORMAT_UNKNOWN)
            throw new ArgumentOutOfRangeException(nameof(rtvFormat), rtvFormat, "Cannot be unknown.");
        if (numBackBuffers is < 2 or > 16)
            throw new ArgumentOutOfRangeException(nameof(numBackBuffers), numBackBuffers, "Must be between 2 and 16.");
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), width, "Must be a positive number.");
        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height), height, "Must be a positive number.");

        using var myDevice = new ComPtr<ID3D12Device>(device);
        ReShadePeeler.PeelD3D12Device(&myDevice);

        var io = ImGui.GetIO();
        if (ImGui.GetIO().NativePtr->BackendRendererName is not null)
            throw new InvalidOperationException("ImGui backend renderer seems to be have been already attached.");

        this.NumBackBuffers = numBackBuffers;
        this.rtvFormat = rtvFormat;
        myDevice.Swap(ref this.device);

        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset | ImGuiBackendFlags.RendererHasViewports;

        this.renderNamePtr = Marshal.StringToHGlobalAnsi("imgui_impl_dx12_c#");
        io.NativePtr->BackendRendererName = (byte*)this.renderNamePtr;

        try
        {
            if (io.ConfigFlags.HasFlag(ImGuiConfigFlags.ViewportsEnable))
            {
                try
                {
                    fixed (IDCompositionDevice** pp = &this.dcompDevice.GetPinnableReference())
                    fixed (Guid* piidDCompositionDevice = &IID.IID_IDCompositionDevice)
                        DirectX.DCompositionCreateDevice(null, piidDCompositionDevice, (void**)pp).ThrowOnError();

                    ImGuiViewportHelpers.EnableViewportWindowBackgroundAlpha();
                }
                catch
                {
                    // don't care; not using DComposition then
                }

                this.viewportHandler = new(this);
            }

            this.mainViewport = ViewportData.Create(this, width, height, nameof(this.mainViewport));
            ImGui.GetPlatformIO().Viewports[0].RendererUserData = this.mainViewport.AsHandle();
            this.textureManager = new(this.device);
        }
        catch
        {
            this.textureManager?.Dispose();
            this.viewportHandler?.Dispose();
            this.ReleaseUnmanagedResources();
            throw;
        }
    }

    /// <summary>
    /// Finalizes an instance of the <see cref="Dx12Renderer"/> class.
    /// </summary>
    ~Dx12Renderer() => this.ReleaseUnmanagedResources();

    /// <summary>
    /// Gets the number of back buffers.
    /// </summary>
    public int NumBackBuffers { get; }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.ReleaseUnmanagedResources();
        foreach (var t in this.fontTextures)
            t.Dispose();
        this.textureManager.Dispose();
        this.viewportHandler.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    public void OnNewFrame()
    {
        this.EnsureDeviceObjects();
    }

    /// <inheritdoc/>
    public void OnPreResize() => this.mainViewport.ResetBuffers();

    /// <inheritdoc/>
    public void OnPostResize(int width, int height) => this.mainViewport.ResizeBuffers(width, height, false);

    /// <inheritdoc/>
    public void RenderDrawData(ImDrawDataPtr drawData)
    {
        var noSwapChain = this.mainViewport.SwapChain == null;
        this.textureManager.FlushPendingTextureUploads();
        this.mainViewport.Draw(drawData, noSwapChain);
        if (noSwapChain)
            this.mainViewport.WaitForPendingOperations();
    }

    /// <summary>
    /// Gets the current main render target, and the index of it, out of <see cref="NumBackBuffers"/> back buffers.
    /// </summary>
    /// <param name="resource">The resource. Reference count is not increased. Do not release.</param>
    /// <param name="backBufferIndex">The back buffer index.</param>
    public void GetCurrentMainViewportRenderTarget(out ID3D12Resource* resource, out int backBufferIndex)
    {
        resource = this.mainViewport.CurrentFrame.RenderTarget;
        backBufferIndex = this.mainViewport.CurrentFrameIndex;
    }

    /// <summary>
    /// Creates a new texture pipeline.
    /// </summary>
    /// <param name="ps">The pixel shader data.</param>
    /// <param name="samplerDesc">The sampler description.</param>
    /// <param name="debugName">Name for debugging.</param>
    /// <returns>The handle to the new texture pipeline.</returns>
    public ITexturePipelineWrap CreateTexturePipeline(
        ReadOnlySpan<byte> ps,
        in D3D12_STATIC_SAMPLER_DESC samplerDesc,
        [CallerMemberName] string debugName = "")
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var streamVs = assembly.GetManifestResourceStream("imgui-vertex.hlsl.bytes")!;
        var vs = ArrayPool<byte>.Shared.Rent((int)streamVs.Length);
        try
        {
            streamVs.ReadExactly(vs, 0, (int)streamVs.Length);
            return TexturePipelineWrap.TakeOwnership(
                TexturePipeline.From(
                    this.device,
                    this.rtvFormat,
                    vs.AsSpan(0, (int)streamVs.Length),
                    ps,
                    samplerDesc,
                    debugName));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(vs);
        }
    }

    /// <inheritdoc/>
    public ITexturePipelineWrap? GetTexturePipeline(IDalamudTextureWrap texture) =>
        TextureData.AttachFromAddress(texture.ImGuiHandle).CustomPipeline is { } cp
            ? TexturePipelineWrap.NewReference(cp)
            : null;

    /// <inheritdoc/>
    public void SetTexturePipeline(IDalamudTextureWrap texture, ITexturePipelineWrap? pipeline) =>
        TextureData.AttachFromAddress(texture.ImGuiHandle).CustomPipeline = pipeline switch
        {
            TexturePipelineWrap tpw => tpw.Data,
            null => default,
            _ => throw new ArgumentException("Not a compatible texture pipeline wrap.", nameof(pipeline)),
        };

    /// <inheritdoc/>
    public nint AddDrawCmdUserCallback(IImGuiRenderer.DrawCmdUserCallbackDelegate @delegate)
    {
        if (this.userCallbacks.FirstOrDefault(x => x.Value == @delegate).Key is not 0 and var key)
            return key;

        key = Marshal.GetFunctionPointerForDelegate(@delegate);
        this.userCallbacks.Add(key, @delegate);
        return key;
    }

    /// <inheritdoc/>
    public void RemoveDrawCmdUserCallback(IImGuiRenderer.DrawCmdUserCallbackDelegate @delegate)
    {
        foreach (var key in this.userCallbacks
                                .Where(x => x.Value == @delegate)
                                .Select(x => x.Key)
                                .ToArray())
        {
            this.userCallbacks.Remove(key);
        }
    }

    /// <summary>
    /// Rebuilds font texture.
    /// </summary>
    public void RebuildFontTexture()
    {
        foreach (var fontResourceView in this.fontTextures)
            fontResourceView.Dispose();
        this.fontTextures.Clear();

        this.CreateFontsTexture();
    }

    /// <inheritdoc/>
    public IDalamudTextureWrap LoadTexture(
        ReadOnlySpan<byte> data,
        int pitch,
        int width,
        int height,
        int format,
        [CallerMemberName] string debugName = "")
    {
        try
        {
            return this.textureManager.CreateTexture(data, pitch, width, height, (DXGI_FORMAT)format, debugName);
        }
        catch (COMException e) when (e.HResult == unchecked((int)0x887a0005))
        {
            throw new AggregateException(
                Marshal.GetExceptionForHR(this.device.Get()->GetDeviceRemovedReason()) ?? new(),
                e);
        }
    }

    private void RenderDrawDataInternal(ViewportFrame frameData, ImDrawDataPtr drawData, ID3D12GraphicsCommandList* ctx)
    {
        // Avoid rendering when minimized
        if (drawData.DisplaySize.X <= 0 || drawData.DisplaySize.Y <= 0)
            return;

        if (!drawData.Valid || drawData.CmdListsCount == 0)
            return;

        var cmdLists = new Span<ImDrawListPtr>(drawData.NativePtr->CmdLists, drawData.NativePtr->CmdListsCount);

        frameData.EnsureVertexBufferCapacity(this.device, drawData.TotalVtxCount);
        frameData.EnsureIndexBufferCapacity(this.device, drawData.TotalIdxCount);
        frameData.OverwriteVertexBuffer(cmdLists);
        frameData.OverwriteIndexBuffer(cmdLists);
        frameData.BindIndexVertexBuffers(ctx);

        // Setup viewport
        var viewport = new D3D12_VIEWPORT
        {
            Width = drawData.DisplaySize.X,
            Height = drawData.DisplaySize.Y,
            MinDepth = 0f,
            MaxDepth = 1f,
            TopLeftX = 0f,
            TopLeftY = 0f,
        };
        ctx->RSSetViewports(1, &viewport);

        // Setup blend factor
        var blendFactor = default(Vector4);
        ctx->OMSetBlendFactor((float*)&blendFactor);

        // Setup orthographic projection matrix into our constant buffer.
        // Our visible imgui space lies from DisplayPos (LT) to DisplayPos+DisplaySize (RB).
        // DisplayPos is (0,0) for single viewport apps.
        var projMtx = Matrix4x4.CreateOrthographicOffCenter(
            drawData.DisplayPos.X,
            drawData.DisplayPos.X + drawData.DisplaySize.X,
            drawData.DisplayPos.Y + drawData.DisplaySize.Y,
            drawData.DisplayPos.Y,
            1f,
            0f);

        // Ensure that heap is of sufficient size.
        // We're overshooting it; a texture may be bound to the same heap multiple times.
        frameData.ResetHeap();
        var ensuringHeapSize = 0;
        foreach (ref var cmdList in cmdLists)
            ensuringHeapSize += cmdList.CmdBuffer.Size;
        frameData.EnsureHeapCapacity(this.device, ensuringHeapSize);

        // Render command lists
        // (Because we merged all buffers into a single one, we maintain our own offset into them)
        var vertexOffset = 0;
        var indexOffset = 0;
        var clipOff = new Vector4(drawData.DisplayPos, drawData.DisplayPos.X, drawData.DisplayPos.Y);
        foreach (ref var cmdList in cmdLists)
        {
            var cmds = new ImVectorWrapper<ImDrawCmd>(&cmdList.NativePtr->CmdBuffer);
            foreach (ref var cmd in cmds.DataSpan)
            {
                var clipV4 = cmd.ClipRect - clipOff;
                var clipRect = new RECT((int)clipV4.X, (int)clipV4.Y, (int)clipV4.Z, (int)clipV4.W);

                // Skip the draw if nothing would be visible
                if (clipRect.left >= clipRect.right || clipRect.top >= clipRect.bottom)
                    continue;
                
                ctx->RSSetScissorRects(1, &clipRect);

                if (cmd.UserCallback == nint.Zero)
                {
                    // Bind texture and draw
                    var ptcd = TextureData.AttachFromAddress(cmd.TextureId);

                    (ptcd.CustomPipeline ?? this.defaultPipeline!).BindTo(ctx);

                    ctx->SetGraphicsRoot32BitConstants(0, 16, &projMtx, 0);
                    frameData.BindResourceUsingHeap(this.device, ctx, ptcd.Texture);
                    ctx->DrawIndexedInstanced(
                        cmd.ElemCount,
                        1,
                        (uint)(cmd.IdxOffset + indexOffset),
                        (int)(cmd.VtxOffset + vertexOffset),
                        0);
                }
                else if (this.userCallbacks.TryGetValue(cmd.UserCallback, out var cb))
                {
                    // Use custom callback
                    cb(drawData, (ImDrawCmd*)Unsafe.AsPointer(ref cmd));
                }
            }

            indexOffset += cmdList.IdxBuffer.Size;
            vertexOffset += cmdList.VtxBuffer.Size;
        }
    }

    /// <summary>
    /// Builds fonts as necessary, and uploads the built data onto the GPU.<br />
    /// No-op if it has already been done.
    /// </summary>
    private void CreateFontsTexture()
    {
        if (this.fontTextures.Any())
            return;

        var io = ImGui.GetIO();
        if (io.Fonts.Textures.Size == 0)
            io.Fonts.Build();

        for (int textureIndex = 0, textureCount = io.Fonts.Textures.Size;
             textureIndex < textureCount;
             textureIndex++)
        {
            // Build texture atlas
            io.Fonts.GetTexDataAsRGBA32(
                textureIndex,
                out byte* fontPixels,
                out var width,
                out var height,
                out var bytespp);

            var tex = this.LoadTexture(
                new(fontPixels, width * height * bytespp),
                width * bytespp,
                width,
                height,
                (int)DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM,
                $"Font#{textureIndex}");
            io.Fonts.SetTexID(textureIndex, tex.ImGuiHandle);
            this.fontTextures.Add(tex);
        }

        io.Fonts.ClearTexData();
    }

    private void EnsureDeviceObjects()
    {
        if (this.defaultPipeline is null)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var streamVs = assembly.GetManifestResourceStream("imgui-vertex.hlsl.bytes")!;
            using var streamPs = assembly.GetManifestResourceStream("imgui-frag.hlsl.bytes")!;
            var vs = ArrayPool<byte>.Shared.Rent((int)streamVs.Length);
            var ps = ArrayPool<byte>.Shared.Rent((int)streamPs.Length);
            streamVs.ReadExactly(vs, 0, (int)streamVs.Length);
            streamPs.ReadExactly(ps, 0, (int)streamPs.Length);
            this.defaultPipeline = TexturePipeline.From(
                this.device,
                this.rtvFormat,
                vs.AsSpan(0, (int)streamVs.Length),
                ps.AsSpan(0, (int)streamPs.Length),
                nameof(this.defaultPipeline));
            ArrayPool<byte>.Shared.Return(vs);
            ArrayPool<byte>.Shared.Return(ps);
        }

        this.CreateFontsTexture();
    }

    private void ReleaseUnmanagedResources()
    {
        if (this.releaseUnmanagedResourceCalled)
            return;
        this.releaseUnmanagedResourceCalled = true;

        this.mainViewport.Release();
        ImGui.GetPlatformIO().Viewports[0].RendererUserData = nint.Zero;
        ImGui.DestroyPlatformWindows();

        var io = ImGui.GetIO();
        if (io.NativePtr->BackendRendererName == (void*)this.renderNamePtr)
            io.NativePtr->BackendRendererName = null;
        if (this.renderNamePtr != 0)
            Marshal.FreeHGlobal(this.renderNamePtr);

        foreach (var i in Enumerable.Range(0, io.Fonts.Textures.Size))
            io.Fonts.SetTexID(i, nint.Zero);

        this.device.Reset();
        this.dcompDevice.Reset();
    }
}
