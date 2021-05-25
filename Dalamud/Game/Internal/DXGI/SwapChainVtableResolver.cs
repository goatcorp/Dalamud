using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

using Device = SharpDX.Direct3D11.Device;

namespace Dalamud.Game.Internal.DXGI
{
    /// <summary>
    /// This class attempts to determine the D3D11 SwapChain vtable addresses via instantiating a new form and inspecting it.
    /// </summary>
    /// <remarks>
    /// If the normal signature based method of resolution fails, this is the backup.
    /// </remarks>
    public class SwapChainVtableResolver : BaseAddressResolver, ISwapChainAddressResolver
    {
        private const int DxgiSwapchainMethodCount = 18;
        private const int D3D11DeviceMethodCount = 43;

        private List<IntPtr> d3d11VTblAddresses;
        private List<IntPtr> dxgiSwapChainVTblAddresses;

        /// <inheritdoc/>
        public IntPtr Present { get; set; }

        /// <inheritdoc/>
        public IntPtr ResizeBuffers { get; set; }

        /// <inheritdoc/>
        protected override void Setup64Bit(SigScanner sig)
        {
            if (this.d3d11VTblAddresses == null)
            {
                // Create temporary device + swapchain and determine method addresses
                var renderForm = new Form();

                Device.CreateWithSwapChain(
                    DriverType.Hardware,
                    DeviceCreationFlags.BgraSupport,
                    CreateSwapChainDescription(renderForm.Handle),
                    out var device,
                    out var swapChain);

                if (device != null && swapChain != null)
                {
                    this.d3d11VTblAddresses = this.GetVTblAddresses(device.NativePointer, D3D11DeviceMethodCount);
                    this.dxgiSwapChainVTblAddresses = this.GetVTblAddresses(swapChain.NativePointer, DxgiSwapchainMethodCount);
                }

                device?.Dispose();
                swapChain?.Dispose();
            }

            this.Present = this.dxgiSwapChainVTblAddresses[8];
            this.ResizeBuffers = this.dxgiSwapChainVTblAddresses[13];
        }

        private static SwapChainDescription CreateSwapChainDescription(IntPtr renderForm)
        {
            return new SwapChainDescription
            {
                BufferCount = 1,
                Flags = SwapChainFlags.None,
                IsWindowed = true,
                ModeDescription = new ModeDescription(100, 100, new Rational(60, 1), Format.R8G8B8A8_UNorm),
                OutputHandle = renderForm,
                SampleDescription = new SampleDescription(1, 0),
                SwapEffect = SwapEffect.Discard,
                Usage = Usage.RenderTargetOutput,
            };
        }

        private List<IntPtr> GetVTblAddresses(IntPtr pointer, int numberOfMethods)
        {
            return this.GetVTblAddresses(pointer, 0, numberOfMethods);
        }

        private List<IntPtr> GetVTblAddresses(IntPtr pointer, int startIndex, int numberOfMethods)
        {
            var vtblAddresses = new List<IntPtr>();
            var vTable = Marshal.ReadIntPtr(pointer);
            for (var i = startIndex; i < startIndex + numberOfMethods; i++)
                vtblAddresses.Add(Marshal.ReadIntPtr(vTable, i * IntPtr.Size)); // using IntPtr.Size allows us to support both 32 and 64-bit processes

            return vtblAddresses;
        }
    }
}
