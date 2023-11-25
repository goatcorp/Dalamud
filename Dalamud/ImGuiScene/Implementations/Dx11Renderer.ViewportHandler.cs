using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Dalamud.ImGuiScene.Helpers;
using Dalamud.Utility;

using ImGuiNET;

using TerraFX.Interop;
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
            ViewportData.AttachFromAddress(viewport.RendererUserData)
                        .ResizeBuffers((int)size.X, (int)size.Y, true);

        [UnmanagedCallersOnly]
        private static void OnRenderWindow(ImGuiViewportPtr viewport, nint v) =>
            ViewportData.AttachFromAddress(viewport.RendererUserData)
                        .Draw(viewport.DrawData, (viewport.Flags & ImGuiViewportFlags.NoRendererClear) == 0);

        [UnmanagedCallersOnly]
        private static void OnSwapBuffers(ImGuiViewportPtr viewport, nint v) =>
            ViewportData.AttachFromAddress(viewport.RendererUserData)
                        .PresentIfSwapChainAvailable();

        private void OnCreateWindow(ImGuiViewportPtr viewport)
        {
            // PlatformHandleRaw should always be a HWND, whereas PlatformHandle might be a higher-level handle (e.g. GLFWWindow*, SDL_Window*).
            // Some backend will leave PlatformHandleRaw NULL, in which case we assume PlatformHandle will contain the HWND.
            var hWnd = viewport.PlatformHandleRaw;
            if (hWnd == 0)
                hWnd = viewport.PlatformHandle;
            viewport.RendererUserData = ViewportData.Create(this.renderer, (HWND)hWnd).AsHandle();
        }
    }

    private class ViewportData : ManagedComObjectBase<ViewportData>, INativeGuid
    {
        public static readonly Guid MyGuid =
            new(0x98eaa0be, 0x9123, 0x4346, 0x94, 0x16, 0xe0, 0xd7, 0x54, 0xbe, 0x45, 0x8d);

        private readonly Dx11Renderer parent;

        private ComPtr<IDXGISwapChain> swapChain;
        private ComPtr<ID3D11Texture2D> renderTarget;
        private ComPtr<ID3D11RenderTargetView> renderTargetView;

        private int width;
        private int height;

        public ViewportData(
            Dx11Renderer parent,
            IDXGISwapChain* swapChain,
            int width,
            int height)
        {
            this.parent = parent;
            this.swapChain = new(swapChain);
            this.width = width;
            this.height = height;
        }

        public static Guid* NativeGuid => (Guid*)Unsafe.AsPointer(ref Unsafe.AsRef(in MyGuid));

        public IDXGISwapChain* SwapChain => this.swapChain;

        private ID3D11Device* Device => this.parent.device.Get();

        private DXGI_FORMAT RtvFormat => this.parent.rtvFormat;

        public static ViewportData Create(Dx11Renderer renderer, IDXGISwapChain* swapChain)
        {
            DXGI_SWAP_CHAIN_DESC desc;
            swapChain->GetDesc(&desc).ThrowHr();
            return new(renderer, swapChain, (int)desc.BufferDesc.Width, (int)desc.BufferDesc.Height);
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
                    (void**)dxgiFactory.GetAddressOf()).ThrowHr();
#else
                DirectX.CreateDXGIFactory(piidFactory, (void**)dxgiFactory.GetAddressOf()).ThrowHr();
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
                .ThrowHr();

            return Create(renderer, swapChain);
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
                this.swapChain.Get()->Present(0, 0).ThrowHr();
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
                this.swapChain.Get()->GetDesc(&desc).ThrowHr();
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
            this.swapChain.Reset();
            this.renderTarget.Reset();
            this.renderTargetView.Reset();
        }

        private void EnsureRenderTarget()
        {
            if (!this.renderTarget.IsEmpty() && !this.renderTargetView.IsEmpty())
                return;

            this.renderTarget.Reset();
            this.renderTargetView.Reset();

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
                    this.parent.device.Get()->CreateTexture2D(&desc, null, pprt).ThrowHr();
                }
                else
                {
                    fixed (Guid* piid = &IID.IID_ID3D11Texture2D)
                    {
                        this.swapChain.Get()->GetBuffer(0, piid, (void**)pprt)
                            .ThrowHr();
                    }
                }

                this.parent.device.Get()->CreateRenderTargetView((ID3D11Resource*)*pprt, null, pprtv).ThrowHr();
            }
        }
    }
}
