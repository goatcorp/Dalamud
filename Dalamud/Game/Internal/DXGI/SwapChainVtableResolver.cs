using Dalamud.Game.Internal.DXGI.Definitions;
using Dalamud.ImGuiScene.Helpers;

using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Dalamud.Game.Internal.DXGI;

/// <summary>
/// This class attempts to determine the D3D11 SwapChain vtable addresses via instantiating a new form and inspecting it.
/// </summary>
/// <remarks>
/// If the normal signature based method of resolution fails, this is the backup.
/// </remarks>
internal class SwapChainVtableResolver : BaseAddressResolver, ISwapChainAddressResolver
{
    /// <inheritdoc/>
    public IntPtr Present { get; set; }

    /// <inheritdoc/>
    public IntPtr ResizeBuffers { get; set; }

    /// <inheritdoc/>
    protected override unsafe void Setup64Bit(ISigScanner sig)
    {
        void* dxgiSwapChain;

        while (true)
        {
            var kernelDev = Device.Instance();
            if (kernelDev == null)
                continue;

            var swapChain = kernelDev->SwapChain;
            if (swapChain == null)
                continue;

            dxgiSwapChain = swapChain->DXGISwapChain;
            if (dxgiSwapChain == null)
                continue;

            break;
        }

        using var sc = new ComPtr<IDXGISwapChain>((IDXGISwapChain*)dxgiSwapChain);
        ReShadePeeler.PeelSwapChain(&sc);

        this.Present = (nint)sc.Get()->lpVtbl[(int)IDXGISwapChainVtbl.Present];
        this.ResizeBuffers = (nint)sc.Get()->lpVtbl[(int)IDXGISwapChainVtbl.ResizeBuffers];
    }
}
