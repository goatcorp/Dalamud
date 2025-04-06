using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;

using Dalamud.Interface.ImGuiBackend.Helpers;
using Dalamud.Utility;

using Dalamud.Bindings.ImGui;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

using static TerraFX.Interop.Windows.Windows;

namespace Dalamud.Interface.ImGuiBackend.Renderers;

/// <summary>
/// Deals with rendering ImGui using DirectX 11.
/// See https://github.com/ocornut/imgui/blob/master/examples/imgui_impl_dx11.cpp for the original implementation.
/// </summary>
[SuppressMessage(
    "StyleCop.CSharp.LayoutRules",
    "SA1519:Braces should not be omitted from multi-line child statement",
    Justification = "Multiple fixed/using scopes")]
internal unsafe partial class Dx11Renderer
{
    private class ViewportHandler : IDisposable
    {
        private readonly Dx11Renderer renderer;

        [SuppressMessage("ReSharper", "NotAccessedField.Local", Justification = "Keeping reference alive")]
        private readonly ImGuiViewportHelpers.CreateWindowDelegate cwd;

        public ViewportHandler(Dx11Renderer renderer)
        {
            this.renderer = renderer;

            var pio = ImGui.GetPlatformIO();
            pio.RendererCreateWindow = Marshal.GetFunctionPointerForDelegate(this.cwd = this.OnCreateWindow).ToPointer();
            pio.RendererDestroyWindow = (delegate* unmanaged<ImGuiViewportPtr, void>)&OnDestroyWindow;
            pio.RendererSetWindowSize = (delegate* unmanaged<ImGuiViewportPtr, Vector2, void>)&OnSetWindowSize;
            pio.RendererRenderWindow = (delegate* unmanaged<ImGuiViewportPtr, nint, void>)&OnRenderWindow;
            pio.RendererSwapBuffers = (delegate* unmanaged<ImGuiViewportPtr, nint, void>)&OnSwapBuffers;
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
            pio.RendererCreateWindow = null;
            pio.RendererDestroyWindow = null;
            pio.RendererSetWindowSize = null;
            pio.RendererRenderWindow = null;
            pio.RendererSwapBuffers = null;
        }

        [UnmanagedCallersOnly]
        private static void OnDestroyWindow(ImGuiViewportPtr viewport)
        {
            if (viewport.RendererUserData == null)
                return;
            ViewportData.Attach(viewport.RendererUserData).Dispose();
            viewport.RendererUserData = null;
        }

        [UnmanagedCallersOnly]
        private static void OnSetWindowSize(ImGuiViewportPtr viewport, Vector2 size) =>
            ViewportData.Attach(viewport.RendererUserData).ResizeBuffers((int)size.X, (int)size.Y, true);

        [UnmanagedCallersOnly]
        private static void OnRenderWindow(ImGuiViewportPtr viewport, nint v) =>
            ViewportData.Attach(viewport.RendererUserData).Draw(viewport.DrawData, true);

        [UnmanagedCallersOnly]
        private static void OnSwapBuffers(ImGuiViewportPtr viewport, nint v) =>
            ViewportData.Attach(viewport.RendererUserData).PresentIfSwapChainAvailable();

        private void OnCreateWindow(ImGuiViewportPtr viewport)
        {
            // PlatformHandleRaw should always be a HWND, whereas PlatformHandle might be a higher-level handle (e.g. GLFWWindow*, SDL_Window*).
            // Some backend will leave PlatformHandleRaw NULL, in which case we assume PlatformHandle will contain the HWND.
            var hWnd = viewport.PlatformHandleRaw;
            if (hWnd == null)
                hWnd = viewport.PlatformHandle;
            try
            {
                viewport.RendererUserData = ViewportData.CreateDComposition(this.renderer, (HWND)hWnd).Handle;
            }
            catch
            {
                viewport.RendererUserData = ViewportData.Create(this.renderer, (HWND)hWnd).Handle;
            }
        }
    }

    private sealed class ViewportData : IDisposable
    {
        private readonly Dx11Renderer parent;

        private GCHandle selfGcHandle;
        private ComPtr<IDXGISwapChain> swapChain;
        private ComPtr<ID3D11Texture2D> renderTarget;
        private ComPtr<ID3D11RenderTargetView> renderTargetView;
        private ComPtr<IDCompositionVisual> dcompVisual;
        private ComPtr<IDCompositionTarget> dcompTarget;

        private int width;
        private int height;

        public ViewportData(
            Dx11Renderer parent,
            IDXGISwapChain* swapChain,
            int width,
            int height,
            IDCompositionVisual* dcompVisual,
            IDCompositionTarget* dcompTarget)
        {
            this.parent = parent;
            this.swapChain = new(swapChain);
            this.width = width;
            this.height = height;
            if (dcompVisual is not null)
                this.dcompVisual = new(dcompVisual);
            if (dcompTarget is not null)
                this.dcompTarget = new(dcompTarget);
            this.selfGcHandle = GCHandle.Alloc(this);
        }

        public IDXGISwapChain* SwapChain => this.swapChain;

        public void* Handle => GCHandle.ToIntPtr(this.selfGcHandle).ToPointer();

        private DXGI_FORMAT RtvFormat => this.parent.rtvFormat;

        public static ViewportData Attach(void* handle) =>
            (ViewportData)GCHandle.FromIntPtr(new IntPtr(handle)).Target ?? throw new InvalidOperationException();

        public static ViewportData Create(
            Dx11Renderer renderer,
            IDXGISwapChain* swapChain,
            IDCompositionVisual* dcompVisual,
            IDCompositionTarget* dcompTarget)
        {
            DXGI_SWAP_CHAIN_DESC desc;
            swapChain->GetDesc(&desc).ThrowOnError();
            return new(
                renderer,
                swapChain,
                (int)desc.BufferDesc.Width,
                (int)desc.BufferDesc.Height,
                dcompVisual,
                dcompTarget);
        }

        public static ViewportData CreateDComposition(Dx11Renderer renderer, HWND hWnd)
        {
            if (renderer.dcompDevice.IsEmpty())
                throw new NotSupportedException();

            var mvsd = default(DXGI_SWAP_CHAIN_DESC);
            renderer.mainViewport.SwapChain->GetDesc(&mvsd).ThrowOnError();

            using var dxgiFactory = default(ComPtr<IDXGIFactory4>);
            fixed (Guid* piidFactory = &IID.IID_IDXGIFactory4)
            {
#if DEBUG
                DirectX.CreateDXGIFactory2(
                    DXGI.DXGI_CREATE_FACTORY_DEBUG,
                    piidFactory,
                    (void**)dxgiFactory.GetAddressOf()).ThrowOnError();
#else
                DirectX.CreateDXGIFactory1(piidFactory, (void**)dxgiFactory.GetAddressOf()).ThrowOnError();
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
                BufferCount = Math.Max(2u, mvsd.BufferCount),
                Scaling = DXGI_SCALING.DXGI_SCALING_STRETCH,
                SwapEffect = DXGI_SWAP_EFFECT.DXGI_SWAP_EFFECT_FLIP_SEQUENTIAL,
                AlphaMode = DXGI_ALPHA_MODE.DXGI_ALPHA_MODE_PREMULTIPLIED,
                Flags = 0,
            };
            dxgiFactory.Get()->CreateSwapChainForComposition(
                (IUnknown*)renderer.device.Get(),
                &sd1,
                null,
                swapChain1.GetAddressOf()).ThrowOnError();

            if (ReShadePeeler.PeelSwapChain(&swapChain1))
            {
                swapChain1.Get()->ResizeBuffers(sd1.BufferCount, sd1.Width, sd1.Height, sd1.Format, sd1.Flags)
                    .ThrowOnError();
            }

            using var dcTarget = default(ComPtr<IDCompositionTarget>);
            renderer.dcompDevice.Get()->CreateTargetForHwnd(hWnd, BOOL.TRUE, dcTarget.GetAddressOf());

            using var dcVisual = default(ComPtr<IDCompositionVisual>);
            renderer.dcompDevice.Get()->CreateVisual(dcVisual.GetAddressOf()).ThrowOnError();

            dcVisual.Get()->SetContent((IUnknown*)swapChain1.Get()).ThrowOnError();
            dcTarget.Get()->SetRoot(dcVisual).ThrowOnError();
            renderer.dcompDevice.Get()->Commit().ThrowOnError();

            using var swapChain = default(ComPtr<IDXGISwapChain>);
            swapChain1.As(&swapChain).ThrowOnError();
            return Create(renderer, swapChain, dcVisual, dcTarget);
        }

        public static ViewportData Create(Dx11Renderer renderer, HWND hWnd)
        {
            using var dxgiFactory = default(ComPtr<IDXGIFactory>);
            fixed (Guid* piidFactory = &IID.IID_IDXGIFactory)
            {
#if DEBUG
                DirectX.CreateDXGIFactory2(
                    DXGI.DXGI_CREATE_FACTORY_DEBUG,
                    piidFactory,
                    (void**)dxgiFactory.GetAddressOf()).ThrowOnError();
#else
                DirectX.CreateDXGIFactory(piidFactory, (void**)dxgiFactory.GetAddressOf()).ThrowOnError();
#endif
            }

            // Create swapchain
            using var swapChain = default(ComPtr<IDXGISwapChain>);
            var desc = new DXGI_SWAP_CHAIN_DESC
            {
                BufferDesc =
                {
                    Format = DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM,
                },
                SampleDesc = new(1, 0),
                BufferUsage = DXGI.DXGI_USAGE_RENDER_TARGET_OUTPUT,
                BufferCount = 1,
                OutputWindow = hWnd,
                Windowed = true,
                SwapEffect = DXGI_SWAP_EFFECT.DXGI_SWAP_EFFECT_DISCARD,
            };
            dxgiFactory.Get()->CreateSwapChain((IUnknown*)renderer.device.Get(), &desc, swapChain.GetAddressOf())
                .ThrowOnError();

            if (ReShadePeeler.PeelSwapChain(&swapChain))
            {
                swapChain.Get()->ResizeBuffers(
                        desc.BufferCount,
                        desc.BufferDesc.Width,
                        desc.BufferDesc.Height,
                        desc.BufferDesc.Format,
                        desc.Flags)
                    .ThrowOnError();
            }

            return Create(renderer, swapChain, null, null);
        }

        public void Dispose()
        {
            if (!this.selfGcHandle.IsAllocated)
                return;

            this.ResetBuffers();
            this.dcompVisual.Reset();
            this.dcompTarget.Reset();
            this.swapChain.Reset();
            this.selfGcHandle.Free();
        }

        public void Draw(ImDrawDataPtr drawData, bool clearRenderTarget)
        {
            if (this.width < 1 || this.height < 1)
                return;

            this.EnsureRenderTarget();
            this.parent.RenderDrawDataInternal(this.renderTargetView, drawData, clearRenderTarget);
        }

        public void PresentIfSwapChainAvailable()
        {
            if (this.width < 1 || this.height < 1)
                return;

            if (!this.swapChain.IsEmpty())
                this.swapChain.Get()->Present(0, 0).ThrowOnError();
        }

        public void ResetBuffers()
        {
            this.renderTargetView.Reset();
            this.renderTarget.Reset();
        }

        public void ResizeBuffers(int newWidth, int newHeight, bool resizeSwapChain)
        {
            this.ResetBuffers();

            this.width = newWidth;
            this.height = newHeight;
            if (this.width < 1 || this.height < 1)
                return;

            if (resizeSwapChain && !this.swapChain.IsEmpty())
            {
                DXGI_SWAP_CHAIN_DESC desc;
                this.swapChain.Get()->GetDesc(&desc).ThrowOnError();
                this.swapChain.Get()->ResizeBuffers(
                    desc.BufferCount,
                    (uint)newWidth,
                    (uint)newHeight,
                    DXGI_FORMAT.DXGI_FORMAT_UNKNOWN,
                    desc.Flags).ThrowOnError();
            }
        }

        private void EnsureRenderTarget()
        {
            if (!this.renderTarget.IsEmpty() && !this.renderTargetView.IsEmpty())
                return;

            this.ResetBuffers();

            fixed (ID3D11Texture2D** pprt = &this.renderTarget.GetPinnableReference())
            fixed (ID3D11RenderTargetView** pprtv = &this.renderTargetView.GetPinnableReference())
            {
                if (this.swapChain.IsEmpty())
                {
                    var desc = new D3D11_TEXTURE2D_DESC
                    {
                        Width = (uint)this.width,
                        Height = (uint)this.height,
                        MipLevels = 1,
                        ArraySize = 1,
                        Format = this.RtvFormat,
                        SampleDesc = new(1, 0),
                        Usage = D3D11_USAGE.D3D11_USAGE_DEFAULT,
                        BindFlags = (uint)D3D11_BIND_FLAG.D3D11_BIND_SHADER_RESOURCE,
                        CPUAccessFlags = 0,
                        MiscFlags = (uint)D3D11_RESOURCE_MISC_FLAG.D3D11_RESOURCE_MISC_SHARED_NTHANDLE,
                    };
                    this.parent.device.Get()->CreateTexture2D(&desc, null, pprt).ThrowOnError();
                }
                else
                {
                    fixed (Guid* piid = &IID.IID_ID3D11Texture2D)
                    {
                        this.swapChain.Get()->GetBuffer(0u, piid, (void**)pprt)
                            .ThrowOnError();
                    }
                }

                this.parent.device.Get()->CreateRenderTargetView((ID3D11Resource*)*pprt, null, pprtv).ThrowOnError();
            }
        }
    }
}
