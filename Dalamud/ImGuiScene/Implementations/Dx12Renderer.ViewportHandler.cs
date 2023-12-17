using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Dalamud.ImGuiScene.Helpers;
using Dalamud.Interface.Utility;
using Dalamud.Utility;

using ImGuiNET;

using TerraFX.Interop;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

using static TerraFX.Interop.Windows.Windows;

namespace Dalamud.ImGuiScene.Implementations;

/// <summary>
/// Deals with rendering ImGui using DirectX 12.
/// See https://github.com/ocornut/imgui/blob/master/examples/imgui_impl_dx12.cpp for the original implementation.
/// </summary>
[SuppressMessage(
    "StyleCop.CSharp.LayoutRules",
    "SA1519:Braces should not be omitted from multi-line child statement",
    Justification = "Multiple fixed/using scopes")]
internal unsafe partial class Dx12Renderer
{
    private static readonly Vector4 ClearColor = default;

    /// <summary>
    /// MULTI-VIEWPORT / PLATFORM INTERFACE SUPPORT
    /// This is an _advanced_ and _optional_ feature, allowing the backend to create and handle multiple viewports simultaneously.
    /// If you are new to dear imgui or creating a new binding for dear imgui, it is recommended that you completely ignore this section first..
    /// </summary>
    private class ViewportHandler : IDisposable
    {
        private readonly Dx12Renderer renderer;

        [SuppressMessage("ReSharper", "NotAccessedField.Local", Justification = "Keeping reference alive")]
        private readonly ImGuiViewportHelpers.CreateWindowDelegate cwd;

        public ViewportHandler(Dx12Renderer renderer)
        {
            this.renderer = renderer;

            var pio = ImGui.GetPlatformIO();
            pio.Renderer_CreateWindow = Marshal.GetFunctionPointerForDelegate(this.cwd = this.OnCreateWindow);
            pio.Renderer_DestroyWindow = (nint)(delegate* unmanaged<ImGuiViewportPtr, void>)&OnDestroyWindow;
            pio.Renderer_SetWindowSize = (nint)(delegate* unmanaged<ImGuiViewportPtr, Vector2, void>)&OnSetWindowSize;
            pio.Renderer_RenderWindow = (nint)(delegate* unmanaged<ImGuiViewportPtr, nint, void>)&OnRenderWindow;
            pio.Renderer_SwapBuffers = (nint)(delegate* unmanaged<ImGuiViewportPtr, nint, void>)&OnSwapBuffers;
        }

        ~ViewportHandler() => ReleaseUnmanagedResources();

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        private static void ReleaseUnmanagedResources()
        {
            var pio = ImGui.GetPlatformIO();
            pio.Renderer_CreateWindow = nint.Zero;
            pio.Renderer_DestroyWindow = nint.Zero;
            pio.Renderer_SetWindowSize = nint.Zero;
            pio.Renderer_RenderWindow = nint.Zero;
            pio.Renderer_SwapBuffers = nint.Zero;
        }

        [UnmanagedCallersOnly]
        private static void OnDestroyWindow(ImGuiViewportPtr viewport)
        {
            if (viewport.RendererUserData == nint.Zero)
                return;
            ViewportData.AttachFromAddress(viewport.RendererUserData).Release();
            viewport.RendererUserData = nint.Zero;
        }

        [UnmanagedCallersOnly]
        private static void OnSetWindowSize(ImGuiViewportPtr viewport, Vector2 size) =>
            ViewportData.AttachFromAddress(viewport.RendererUserData).ResizeBuffers((int)size.X, (int)size.Y, true);

        [UnmanagedCallersOnly]
        private static void OnRenderWindow(ImGuiViewportPtr viewport, nint unused) =>
            ViewportData.AttachFromAddress(viewport.RendererUserData)
                        .Draw(viewport.DrawData, true);

        [UnmanagedCallersOnly]
        private static void OnSwapBuffers(ImGuiViewportPtr viewport, nint v)
        {
            var data = ViewportData.AttachFromAddress(viewport.RendererUserData);
            data.PresentIfSwapChainAvailable();
            data.WaitForPendingOperations();
        }

        private void OnCreateWindow(ImGuiViewportPtr viewport)
        {
            // PlatformHandleRaw should always be a HWND, whereas PlatformHandle might be a higher-level handle (e.g. GLFWWindow*, SDL_Window*).
            // Some backend will leave PlatformHandleRaw NULL, in which case we assume PlatformHandle will contain the HWND.
            var hWnd = viewport.PlatformHandleRaw;
            if (hWnd == 0)
                hWnd = viewport.PlatformHandle;
            try
            {
                viewport.RendererUserData =
                    ViewportData.CreateDComposition(this.renderer, (HWND)hWnd, $"ID#{viewport.ID}").AsHandle();
            }
            catch
            {
                viewport.RendererUserData =
                    ViewportData.Create(this.renderer, (HWND)hWnd, $"ID#{viewport.ID}").AsHandle();                
            }
        }
    }

    /// <summary>
    /// Helper structure we store in the void* RendererUserData field of each ImGuiViewport to easily retrieve our backend data.
    /// Main viewport created by application will only use the Resources field.
    /// Secondary viewports created by this backend will use all the fields (including Window fields.)
    /// </summary>
    private class ViewportData : ManagedComObjectBase<ViewportData>, INativeGuid
    {
        public static readonly Guid MyGuid =
            new(0xb4e47b38, 0x61fd, 0x4f4c, 0xb1, 0x49, 0x45, 0x47, 0x81, 0x19, 0x1c, 0x16);

        private readonly Dx12Renderer parent;
        private readonly string debugName;
        private readonly int numBackBuffers;
        private readonly ViewportFrame?[]? frames;
        private readonly CommandQueueWrapper? queue;

        private ComPtr<ID3D12DescriptorHeap> rtvDescHeap;
        private ComPtr<IDXGISwapChain3> swapChain;
        private ComPtr<IDCompositionVisual> dcompVisual;
        private ComPtr<IDCompositionTarget> dcompTarget;

        private int width;
        private int height;
        
        private ViewportData(
            Dx12Renderer parent,
            IDXGISwapChain3* swapChain3,
            CommandQueueWrapper queue,
            int width,
            int height,
            int numBackBuffers,
            string debugName,
            IDCompositionVisual* dcompVisual,
            IDCompositionTarget* dcompTarget)
        {
            try
            {
                // Set up basic information.
                this.parent = parent;
                this.width = width;
                this.height = height;
                this.numBackBuffers = numBackBuffers;
                this.CurrentFrameIndex = this.numBackBuffers - 1;
                this.debugName = debugName;
                this.swapChain = swapChain3 != null ? new(swapChain3) : default;
                this.queue = queue;
                this.frames = new ViewportFrame[numBackBuffers];
                if (dcompVisual is not null)
                    this.dcompVisual = new(dcompVisual);
                if (dcompTarget is not null)
                    this.dcompTarget = new(dcompTarget);
                
                // Create the descriptor heap to store the render targets.
                fixed (Guid* piidDescHeap = &IID.IID_ID3D12DescriptorHeap)
                fixed (void* pName = $"{nameof(ViewportData)}[{debugName}].{nameof(this.rtvDescHeap)}")
                {
                    var desc = new D3D12_DESCRIPTOR_HEAP_DESC
                    {
                        Type = D3D12_DESCRIPTOR_HEAP_TYPE.D3D12_DESCRIPTOR_HEAP_TYPE_RTV,
                        NumDescriptors = (uint)numBackBuffers,
                        Flags = D3D12_DESCRIPTOR_HEAP_FLAGS.D3D12_DESCRIPTOR_HEAP_FLAG_NONE,
                        NodeMask = 1,
                    };

                    this.Device->CreateDescriptorHeap(
                        &desc,
                        piidDescHeap,
                        (void**)this.rtvDescHeap.GetAddressOf()).ThrowHr();

                    this.rtvDescHeap.Get()->SetName((ushort*)pName).ThrowHr();
                }

                // Create the frame buffers for each one of them.
                var rtvDescriptorSize = this.Device->GetDescriptorHandleIncrementSize(
                    D3D12_DESCRIPTOR_HEAP_TYPE.D3D12_DESCRIPTOR_HEAP_TYPE_RTV);
                var rtvHandle = this.rtvDescHeap.Get()->GetCPUDescriptorHandleForHeapStart();
                for (var i = 0; i < numBackBuffers; i++)
                {
                    var frameDebugName = $"{this.debugName}.{nameof(this.frames)}[{i}]";
                    this.frames[i] = new(this.Device, i, rtvHandle, frameDebugName);
                    rtvHandle.ptr += rtvDescriptorSize;
                }
            }
            catch
            {
                this.Release();
                throw;
            }
        }

        public static Guid* NativeGuid => (Guid*)Unsafe.AsPointer(ref Unsafe.AsRef(in MyGuid));

        public int CurrentFrameIndex { get; private set; }

        public ViewportFrame CurrentFrame =>
            this.frames?[this.CurrentFrameIndex] ?? throw new ObjectDisposedException(nameof(ViewportHandler));

        public IDXGISwapChain3* SwapChain => this.swapChain;

        private ID3D12Device* Device => this.parent.device;

        private DXGI_FORMAT RtvFormat => this.parent.rtvFormat;

        public static ViewportData Create(
            Dx12Renderer renderer,
            IDXGISwapChain3* swapChain3,
            ID3D12CommandQueue* commandQueue,
            string debugName,
            IDCompositionVisual* dcompVisual,
            IDCompositionTarget* dcompTarget)
        {
            DXGI_SWAP_CHAIN_DESC desc;
            swapChain3->GetDesc(&desc).ThrowHr();
            return new(
                renderer,
                swapChain3,
                new(renderer.device, commandQueue, $"{debugName}.{nameof(queue)}"),
                (int)desc.BufferDesc.Width,
                (int)desc.BufferDesc.Height,
                (int)desc.BufferCount,
                debugName,
                dcompVisual,
                dcompTarget);
        }

        public static ViewportData CreateDComposition(Dx12Renderer renderer, HWND hWnd, string debugName)
        {
            if (renderer.dcompDevice.IsEmpty())
                throw new NotSupportedException();

            // Create command queue.
            using var queue = default(ComPtr<ID3D12CommandQueue>);
            fixed (Guid* piid = &IID.IID_ID3D12CommandQueue)
            {
                var queueDesc = new D3D12_COMMAND_QUEUE_DESC
                {
                    Flags = D3D12_COMMAND_QUEUE_FLAGS.D3D12_COMMAND_QUEUE_FLAG_NONE,
                    Type = D3D12_COMMAND_LIST_TYPE.D3D12_COMMAND_LIST_TYPE_DIRECT,
                };

                renderer.device.Get()->CreateCommandQueue(&queueDesc, piid, (void**)queue.GetAddressOf()).ThrowHr();
            }

            using var dxgiFactory = default(ComPtr<IDXGIFactory4>);
            fixed (Guid* piidFactory = &IID.IID_IDXGIFactory4)
            {
#if DEBUG
                DirectX.CreateDXGIFactory2(
                    DXGI.DXGI_CREATE_FACTORY_DEBUG,
                    piidFactory,
                    (void**)dxgiFactory.GetAddressOf()).ThrowHr();
#else
                DirectX.CreateDXGIFactory1(piidFactory, (void**)dxgiFactory.GetAddressOf()).ThrowHr();
#endif
            }

            RECT rc;
            if (!GetWindowRect(hWnd, &rc) || rc.right == rc.left || rc.bottom == rc.top)
                rc = new(0, 0, 4, 4);

            using var swapChain1 = default(ComPtr<IDXGISwapChain1>);
            var sd1 = new DXGI_SWAP_CHAIN_DESC1
            {
                Width = (uint)(rc.right - rc.left),
                Height = (uint)(rc.bottom - rc.top),
                Format = renderer.rtvFormat,
                Stereo = false,
                SampleDesc = new(1, 0),
                BufferUsage = DXGI.DXGI_USAGE_RENDER_TARGET_OUTPUT,
                BufferCount = (uint)renderer.NumBackBuffers,
                Scaling = DXGI_SCALING.DXGI_SCALING_STRETCH,
                SwapEffect = DXGI_SWAP_EFFECT.DXGI_SWAP_EFFECT_FLIP_SEQUENTIAL,
                AlphaMode = DXGI_ALPHA_MODE.DXGI_ALPHA_MODE_PREMULTIPLIED,
                Flags = 0,
            };
            dxgiFactory.Get()->CreateSwapChainForComposition(
                (IUnknown*)queue.Get(),
                &sd1,
                null,
                swapChain1.GetAddressOf()).ThrowHr();

            if (ReShadePeeler.PeelSwapChain(&swapChain1))
            {
                swapChain1.Get()->ResizeBuffers(sd1.BufferCount, sd1.Width, sd1.Height, sd1.Format, sd1.Flags)
                    .ThrowHr();
            }

            using var dcTarget = default(ComPtr<IDCompositionTarget>);
            renderer.dcompDevice.Get()->CreateTargetForHwnd(hWnd, BOOL.TRUE, dcTarget.GetAddressOf());

            using var dcVisual = default(ComPtr<IDCompositionVisual>);
            renderer.dcompDevice.Get()->CreateVisual(dcVisual.GetAddressOf()).ThrowHr();

            dcVisual.Get()->SetContent((IUnknown*)swapChain1.Get()).ThrowHr();
            dcTarget.Get()->SetRoot(dcVisual).ThrowHr();
            renderer.dcompDevice.Get()->Commit().ThrowHr();
            
            using var swapChain3 = default(ComPtr<IDXGISwapChain3>);
            swapChain1.As(&swapChain3).ThrowHr();
            return Create(renderer, swapChain3, queue, debugName, dcVisual, dcTarget);
        }

        public static ViewportData Create(
            Dx12Renderer renderer,
            HWND hWnd,
            string debugName)
        {
            // Create command queue.
            using var queue = default(ComPtr<ID3D12CommandQueue>);
            fixed (Guid* piid = &IID.IID_ID3D12CommandQueue)
            {
                var queueDesc = new D3D12_COMMAND_QUEUE_DESC
                {
                    Flags = D3D12_COMMAND_QUEUE_FLAGS.D3D12_COMMAND_QUEUE_FLAG_NONE,
                    Type = D3D12_COMMAND_LIST_TYPE.D3D12_COMMAND_LIST_TYPE_DIRECT,
                };

                renderer.device.Get()->CreateCommandQueue(&queueDesc, piid, (void**)queue.GetAddressOf()).ThrowHr();
            }

            // Create swap chain.
            using var swapChain3 = default(ComPtr<IDXGISwapChain3>);
            fixed (Guid* piidFactory2 = &IID.IID_IDXGIFactory2)
            fixed (Guid* piidSwapChain3 = &IID.IID_IDXGISwapChain3)
            {
                var sd1 = new DXGI_SWAP_CHAIN_DESC1
                {
                    Width = 0,
                    Height = 0,
                    Format = renderer.rtvFormat,
                    Stereo = false,
                    SampleDesc = new(1, 0),
                    BufferUsage = DXGI.DXGI_USAGE_RENDER_TARGET_OUTPUT,
                    BufferCount = (uint)renderer.NumBackBuffers,
                    Scaling = DXGI_SCALING.DXGI_SCALING_STRETCH,
                    SwapEffect = DXGI_SWAP_EFFECT.DXGI_SWAP_EFFECT_FLIP_DISCARD,
                    AlphaMode = DXGI_ALPHA_MODE.DXGI_ALPHA_MODE_UNSPECIFIED,
                    Flags = 0,
                };

                using var dxgiFactory2 = default(ComPtr<IDXGIFactory2>);
#if DEBUG
                DirectX.CreateDXGIFactory2(
                    DXGI.DXGI_CREATE_FACTORY_DEBUG,
                    piidFactory2,
                    (void**)dxgiFactory2.GetAddressOf()).ThrowHr();
#else
                DirectX.CreateDXGIFactory1(piidFactory2, (void**)dxgiFactory2.GetAddressOf()).ThrowHr();
#endif

                using var swapChainTmp = default(ComPtr<IDXGISwapChain1>);
                dxgiFactory2.Get()->CreateSwapChainForHwnd(
                    (IUnknown*)queue.Get(),
                    hWnd,
                    &sd1,
                    null,
                    null,
                    swapChainTmp.GetAddressOf()).ThrowHr();

                if (ReShadePeeler.PeelSwapChain(&swapChainTmp))
                {
                    swapChainTmp.Get()->ResizeBuffers(sd1.BufferCount, sd1.Width, sd1.Height, sd1.Format, sd1.Flags)
                        .ThrowHr();
                }

                swapChainTmp.Get()->QueryInterface(piidSwapChain3, (void**)swapChain3.GetAddressOf()).ThrowHr();
            }

            return Create(renderer, swapChain3, queue, debugName, null, null);
        }

        public static ViewportData Create(
            Dx12Renderer renderer,
            int width,
            int height,
            string debugName) =>
            new(
                renderer,
                null,
                new(
                    renderer.device,
                    new D3D12_COMMAND_QUEUE_DESC
                    {
                        Flags = D3D12_COMMAND_QUEUE_FLAGS.D3D12_COMMAND_QUEUE_FLAG_NONE,
                        Type = D3D12_COMMAND_LIST_TYPE.D3D12_COMMAND_LIST_TYPE_DIRECT,
                    },
                    $"{debugName}.{nameof(queue)}"),
                width,
                height,
                renderer.NumBackBuffers,
                debugName,
                null,
                null);

        public void Draw(ImDrawDataPtr drawData, bool clearRenderTarget)
        {
            if (this.width < 1 || this.height < 1)
                return;

            ObjectDisposedException.ThrowIf(this.frames is null || this.queue is null, nameof(ViewportHandler));
            
            this.EnsureRenderTarget();

            this.CurrentFrameIndex =
                this.swapChain.IsEmpty()
                          ? (this.CurrentFrameIndex + 1) % this.numBackBuffers
                          : (int)this.swapChain.Get()->GetCurrentBackBufferIndex();
            var currentFrame = this.CurrentFrame;
            
            // Draw
            using (currentFrame.Command.Record(out var cmdList))
            {
                currentFrame.BeginRenderTarget(cmdList, clearRenderTarget);
                this.parent.RenderDrawDataInternal(currentFrame, drawData, cmdList);
                currentFrame.EndRenderTarget(cmdList);
            }

            this.queue.Wait();
            this.queue.Submit(currentFrame.Command);
        }

        public void PresentIfSwapChainAvailable()
        {
            if (this.width < 1 || this.height < 1)
                return;

            if (!this.swapChain.IsEmpty())
                this.swapChain.Get()->Present(0, 0).ThrowHr();            
        }

        public void WaitForPendingOperations()
        {
            if (this.frames is null || this.queue is null)
                return;

            this.queue.WaitMostRecent();
        }

        public void ResetBuffers()
        {
            ObjectDisposedException.ThrowIf(this.frames is null || this.queue is null, nameof(ViewportHandler));

            this.WaitForPendingOperations();
            foreach (var f in this.frames)
                f?.ResetRenderTarget();
        }

        public void ResizeBuffers(int newWidth, int newHeight, bool resizeSwapChain)
        {
            ObjectDisposedException.ThrowIf(this.frames is null, this);

            this.ResetBuffers();

            this.width = newWidth;
            this.height = newHeight;
            if (this.width < 1 || this.height < 1)
                return;

            if (!this.swapChain.IsEmpty() && resizeSwapChain)
            {
                DXGI_SWAP_CHAIN_DESC1 desc;
                this.swapChain.Get()->GetDesc1(&desc).ThrowHr();
                this.swapChain.Get()->ResizeBuffers(
                    desc.BufferCount,
                    (uint)newWidth,
                    (uint)newHeight,
                    DXGI_FORMAT.DXGI_FORMAT_UNKNOWN,
                    desc.Flags).ThrowHr();
            }
        }

        protected override void FinalRelease()
        {
            this.WaitForPendingOperations();
            
            this.queue?.Dispose();
            foreach (var f in this.frames ?? Array.Empty<ViewportFrame?>())
                f?.Dispose();

            this.rtvDescHeap.Reset();
            this.dcompVisual.Reset();
            this.dcompTarget.Reset();
            this.swapChain.Reset();
        }

        private void EnsureRenderTarget()
        {
            ObjectDisposedException.ThrowIf(this.frames is null, this);

            for (var i = 0; i < this.numBackBuffers; i++)
            {
                var frame = this.frames[i];
                ObjectDisposedException.ThrowIf(frame is null, this);
                frame.EnsureRenderTarget(this.Device, this.swapChain, this.RtvFormat, this.width, this.height);
            }
        }
    }

    private class ViewportFrame : IDisposable
    {
        private const int HeapDefaultCapacity = 1024;

        private readonly string debugName;
        private readonly int bufferIndex;
        private readonly D3D12_CPU_DESCRIPTOR_HANDLE renderTargetCpuDescriptor;
        private readonly List<ComPtr<ID3D12DescriptorHeap>> deferredReleasingHeaps = new();

        // Buffers used for secondary viewports created by the multi-viewports systems.
        private ComPtr<ID3D12Resource> renderTarget;

        // Buffers used during the rendering of a frame.
        private ComPtr<ID3D12Resource> indexBuffer;
        private ComPtr<ID3D12Resource> vertexBuffer;
        private uint indexBufferSize;
        private uint vertexBufferSize;

        private ComPtr<ID3D12DescriptorHeap> heap;
        private int heapLength;
        private int heapCapacity;

        public ViewportFrame(
            ID3D12Device* device,
            int bufferIndex,
            D3D12_CPU_DESCRIPTOR_HANDLE renderTargetCpuDescriptor,
            string debugName)
        {
            this.debugName = debugName;
            this.bufferIndex = bufferIndex;
            this.renderTargetCpuDescriptor = renderTargetCpuDescriptor;
            this.Command = new(
                device,
                D3D12_COMMAND_LIST_TYPE.D3D12_COMMAND_LIST_TYPE_DIRECT,
                $"{debugName}.{nameof(this.Command)}");
        }

        ~ViewportFrame() => this.ReleaseUnmanagedResources();

        public GraphicsCommandListWrapper Command { get; }

        public ID3D12Resource* RenderTarget => this.renderTarget.Get();

        public ID3D12Resource* VertexBuffer => this.vertexBuffer;

        public int VertexBufferSize => (int)this.vertexBufferSize;

        public ID3D12Resource* IndexBuffer => this.indexBuffer;

        public int IndexBufferSize => (int)this.indexBufferSize;

        public void Dispose()
        {
            this.Command.Dispose();
            this.ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        public void ResetRenderTarget() => this.renderTarget.Reset();

        public void EnsureRenderTarget(
            ID3D12Device* device,
            IDXGISwapChain3* swapChain,
            DXGI_FORMAT rtvFormat,
            int width,
            int height)
        {
            if (!this.renderTarget.IsEmpty())
                return;
                
            fixed (Guid* piidResource = &IID.IID_ID3D12Resource)
            {
                using var backBuffer = default(ComPtr<ID3D12Resource>);
                if (swapChain is null)
                {
                    var props = new D3D12_HEAP_PROPERTIES
                    {
                        Type = D3D12_HEAP_TYPE.D3D12_HEAP_TYPE_DEFAULT,
                        CPUPageProperty = D3D12_CPU_PAGE_PROPERTY.D3D12_CPU_PAGE_PROPERTY_UNKNOWN,
                        MemoryPoolPreference = D3D12_MEMORY_POOL.D3D12_MEMORY_POOL_UNKNOWN,
                    };
                    var desc = new D3D12_RESOURCE_DESC
                    {
                        Dimension = D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_TEXTURE2D,
                        Alignment = 0,
                        Width = (ulong)width,
                        Height = (uint)height,
                        DepthOrArraySize = 1,
                        MipLevels = 1,
                        Format = rtvFormat,
                        SampleDesc = { Count = 1, Quality = 0 },
                        Layout = D3D12_TEXTURE_LAYOUT.D3D12_TEXTURE_LAYOUT_UNKNOWN,
                        Flags = D3D12_RESOURCE_FLAGS.D3D12_RESOURCE_FLAG_ALLOW_RENDER_TARGET |
                                D3D12_RESOURCE_FLAGS.D3D12_RESOURCE_FLAG_ALLOW_SIMULTANEOUS_ACCESS,
                    };
                    var clearColor = ClearColor;
                    var clearValue = new D3D12_CLEAR_VALUE(rtvFormat, (float*)&clearColor);
                    device->CreateCommittedResource(
                        &props,
                        D3D12_HEAP_FLAGS.D3D12_HEAP_FLAG_SHARED,
                        &desc,
                        D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_COMMON,
                        &clearValue,
                        piidResource,
                        (void**)backBuffer.GetAddressOf()).ThrowHr();
                }
                else
                {
                    swapChain->GetBuffer(
                        (uint)this.bufferIndex,
                        piidResource,
                        (void**)backBuffer.GetAddressOf()).ThrowHr();
                }

                fixed (void* pName = $"{this.debugName}.{nameof(this.renderTarget)}")
                    backBuffer.Get()->SetName((ushort*)pName).ThrowHr();

                device->CreateRenderTargetView(backBuffer, null, this.renderTargetCpuDescriptor);
                this.renderTarget.Swap(&backBuffer);
            }
        }

        public void BeginRenderTarget(ID3D12GraphicsCommandList* cmdList, bool clearRenderTarget)
        {
            var barrier = new D3D12_RESOURCE_BARRIER
            {
                Type = D3D12_RESOURCE_BARRIER_TYPE.D3D12_RESOURCE_BARRIER_TYPE_TRANSITION,
                Flags = D3D12_RESOURCE_BARRIER_FLAGS.D3D12_RESOURCE_BARRIER_FLAG_NONE,
                Transition = new()
                {
                    pResource = this.RenderTarget,
                    Subresource = D3D12.D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES,
                    StateBefore = D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_PRESENT,
                    StateAfter = D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_RENDER_TARGET,
                },
            };
            cmdList->ResourceBarrier(1, &barrier);
            
            var rtcd = this.renderTargetCpuDescriptor;
            cmdList->OMSetRenderTargets(1, &rtcd, false, null);
            
            if (clearRenderTarget)
            {
                var clearColor = ClearColor;
                cmdList->ClearRenderTargetView(rtcd, (float*)&clearColor, 0, null);
            }
        }

        public void EndRenderTarget(ID3D12GraphicsCommandList* cmdList)
        {
            var barrier = new D3D12_RESOURCE_BARRIER
            {
                Type = D3D12_RESOURCE_BARRIER_TYPE.D3D12_RESOURCE_BARRIER_TYPE_TRANSITION,
                Flags = D3D12_RESOURCE_BARRIER_FLAGS.D3D12_RESOURCE_BARRIER_FLAG_NONE,
                Transition = new()
                {
                    pResource = this.RenderTarget,
                    Subresource = D3D12.D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES,
                    StateBefore = D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_RENDER_TARGET,
                    StateAfter = D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_PRESENT,
                },
            };
            cmdList->ResourceBarrier(1, &barrier);
        }
        
        public void EnsureVertexBufferCapacity(ID3D12Device* device, int capacity)
        {
            if (this.vertexBufferSize >= capacity)
                return;

            this.vertexBuffer.Reset();
            this.vertexBufferSize = (uint)(capacity + 5000);
            fixed (Guid* piid = &IID.IID_ID3D12Resource)
            {
                var props = new D3D12_HEAP_PROPERTIES
                {
                    Type = D3D12_HEAP_TYPE.D3D12_HEAP_TYPE_UPLOAD,
                    CPUPageProperty = D3D12_CPU_PAGE_PROPERTY.D3D12_CPU_PAGE_PROPERTY_UNKNOWN,
                    MemoryPoolPreference = D3D12_MEMORY_POOL.D3D12_MEMORY_POOL_UNKNOWN,
                };
                var desc = new D3D12_RESOURCE_DESC
                {
                    Dimension = D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_BUFFER,
                    Width = (ulong)(this.vertexBufferSize * sizeof(ImDrawVert)),
                    Height = 1,
                    DepthOrArraySize = 1,
                    MipLevels = 1,
                    Format = DXGI_FORMAT.DXGI_FORMAT_UNKNOWN,
                    SampleDesc = new(1, 0),
                    Layout = D3D12_TEXTURE_LAYOUT.D3D12_TEXTURE_LAYOUT_ROW_MAJOR,
                    Flags = D3D12_RESOURCE_FLAGS.D3D12_RESOURCE_FLAG_NONE,
                };
                device->CreateCommittedResource(
                    &props,
                    D3D12_HEAP_FLAGS.D3D12_HEAP_FLAG_NONE,
                    &desc,
                    D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_GENERIC_READ,
                    null,
                    piid,
                    (void**)this.vertexBuffer.GetAddressOf()).ThrowHr();

                fixed (void* pName = $"{this.debugName}.{nameof(this.vertexBuffer)}")
                    this.vertexBuffer.Get()->SetName((ushort*)pName).ThrowHr();
            }
        }

        public void EnsureIndexBufferCapacity(ID3D12Device* device, int capacity)
        {
            if (this.indexBufferSize >= capacity)
                return;

            this.indexBuffer.Reset();
            this.indexBufferSize = (uint)(capacity + 10000);
            fixed (Guid* piid = &IID.IID_ID3D12Resource)
            {
                var props = new D3D12_HEAP_PROPERTIES
                {
                    Type = D3D12_HEAP_TYPE.D3D12_HEAP_TYPE_UPLOAD,
                    CPUPageProperty = D3D12_CPU_PAGE_PROPERTY.D3D12_CPU_PAGE_PROPERTY_UNKNOWN,
                    MemoryPoolPreference = D3D12_MEMORY_POOL.D3D12_MEMORY_POOL_UNKNOWN,
                };
                var desc = new D3D12_RESOURCE_DESC
                {
                    Dimension = D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_BUFFER,
                    Width = this.indexBufferSize * sizeof(ushort),
                    Height = 1,
                    DepthOrArraySize = 1,
                    MipLevels = 1,
                    Format = DXGI_FORMAT.DXGI_FORMAT_UNKNOWN,
                    SampleDesc = new(1, 0),
                    Layout = D3D12_TEXTURE_LAYOUT.D3D12_TEXTURE_LAYOUT_ROW_MAJOR,
                    Flags = D3D12_RESOURCE_FLAGS.D3D12_RESOURCE_FLAG_NONE,
                };
                device->CreateCommittedResource(
                    &props,
                    D3D12_HEAP_FLAGS.D3D12_HEAP_FLAG_NONE,
                    &desc,
                    D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_GENERIC_READ,
                    null,
                    piid,
                    (void**)this.indexBuffer.GetAddressOf()).ThrowHr();

                fixed (void* pName = $"{this.debugName}.{nameof(this.indexBuffer)}")
                    this.indexBuffer.Get()->SetName((ushort*)pName).ThrowHr();
            }
        }

        public void OverwriteVertexBuffer(Span<ImDrawListPtr> cmdLists)
        {
            try
            {
                var range = default(D3D12_RANGE); // we don't care about what was in there before
                void* tmp;

                this.VertexBuffer->Map(0, &range, &tmp).ThrowHr();
                var targetVertices = new Span<ImDrawVert>(tmp, this.VertexBufferSize);
            
                foreach (ref var cmdList in cmdLists)
                {
                    var vertices = new ImVectorWrapper<ImDrawVert>(ref cmdList.NativePtr->VtxBuffer);
                    vertices.CopyTo(targetVertices);
                    targetVertices = targetVertices[vertices.Length..];
                }
            }
            finally
            {
                this.VertexBuffer->Unmap(0, null);
            }
        }

        public void OverwriteIndexBuffer(Span<ImDrawListPtr> cmdLists)
        {
            try
            {
                var range = default(D3D12_RANGE); // we don't care about what was in there before
                void* tmp;

                this.IndexBuffer->Map(0, &range, &tmp).ThrowHr();
                var targetIndices = new Span<ushort>(tmp, this.IndexBufferSize);
            
                foreach (ref var cmdList in cmdLists)
                {
                    var indices = new ImVectorWrapper<ushort>(ref cmdList.NativePtr->IdxBuffer);
                    indices.CopyTo(targetIndices);
                    targetIndices = targetIndices[indices.Length..];
                }
            }
            finally
            {
                this.IndexBuffer->Unmap(0, null);
            }
        }
        
        public void BindIndexVertexBuffers(ID3D12GraphicsCommandList* cmdList)
        {
            // Bind shader and vertex buffers
            var vbv = new D3D12_VERTEX_BUFFER_VIEW
            {
                BufferLocation = this.VertexBuffer->GetGPUVirtualAddress(),
                SizeInBytes = (uint)(this.VertexBufferSize * sizeof(ImDrawVert)),
                StrideInBytes = (uint)sizeof(ImDrawVert),
            };
            cmdList->IASetVertexBuffers(0, 1, &vbv);

            var ibv = new D3D12_INDEX_BUFFER_VIEW
            {
                BufferLocation = this.IndexBuffer->GetGPUVirtualAddress(),
                SizeInBytes = (uint)(this.IndexBufferSize * sizeof(ushort)),
                Format = DXGI_FORMAT.DXGI_FORMAT_R16_UINT,
            };
            cmdList->IASetIndexBuffer(&ibv);
            cmdList->IASetPrimitiveTopology(D3D_PRIMITIVE_TOPOLOGY.D3D_PRIMITIVE_TOPOLOGY_TRIANGLELIST);
        }
        
        public void ResetHeap()
        {
            this.heapLength = 0;
            foreach (ref var x in CollectionsMarshal.AsSpan(this.deferredReleasingHeaps))
                x.Reset();
            this.deferredReleasingHeaps.Clear();
        }

        public void EnsureHeapCapacity(ID3D12Device* device, int capacity)
        {
            if (this.heapCapacity >= capacity)
                return;

            if (!this.heap.IsEmpty())
            {
                this.deferredReleasingHeaps.Add(this.heap);
                this.heap.Detach();
            }

            var newCapacity = this.heapLength == 0 ? HeapDefaultCapacity : this.heapLength * 2;
            var desc = new D3D12_DESCRIPTOR_HEAP_DESC
            {
                Type = D3D12_DESCRIPTOR_HEAP_TYPE.D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV,
                NumDescriptors = (uint)newCapacity,
                Flags = D3D12_DESCRIPTOR_HEAP_FLAGS.D3D12_DESCRIPTOR_HEAP_FLAG_SHADER_VISIBLE,
            };
            fixed (Guid* guid = &IID.IID_ID3D12DescriptorHeap)
            fixed (ID3D12DescriptorHeap** ppHeap = &this.heap.GetPinnableReference())
                device->CreateDescriptorHeap(&desc, guid, (void**)ppHeap).ThrowHr();

            fixed (void* pName = $"{this.debugName}.{nameof(this.heap)}")
                this.heap.Get()->SetName((ushort*)pName).ThrowHr();
            this.heapCapacity = newCapacity;
        }

        public void BindResourceUsingHeap(
            ID3D12Device* device,
            ID3D12GraphicsCommandList* cmdList,
            ID3D12Resource* resource)
        {
            var entrySize = device->GetDescriptorHandleIncrementSize(
                D3D12_DESCRIPTOR_HEAP_TYPE.D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV);
            this.EnsureHeapCapacity(device, this.heapLength + 1);
            
            var h = this.heap.Get();
            var cpuh = h->GetCPUDescriptorHandleForHeapStart();
            var gpuh = h->GetGPUDescriptorHandleForHeapStart();
            cpuh.ptr += (nuint)(entrySize * this.heapLength);
            gpuh.ptr += (nuint)(entrySize * this.heapLength);
            device->CreateShaderResourceView(resource, null, cpuh);
            this.heapLength++;

            cmdList->SetDescriptorHeaps(1, &h);
            cmdList->SetGraphicsRootDescriptorTable(1, gpuh);
        }

        private void ReleaseUnmanagedResources()
        {
            this.ResetHeap();
            this.heap.Reset();
            this.indexBuffer.Reset();
            this.vertexBuffer.Reset();
            this.indexBufferSize = this.vertexBufferSize = 0u;
            this.renderTarget.Reset();
        }
    }
}
