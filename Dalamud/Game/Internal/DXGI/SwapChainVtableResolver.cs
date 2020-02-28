using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SharpDX.Windows;
using Device = SharpDX.Direct3D11.Device;

namespace Dalamud.Game.Internal.DXGI
{
    /*
     * This method of getting the SwapChain Addresses is currently not used.
     * If the normal AddressResolver(SigScanner) fails, we should use it as a fallback.(Linux?)
     */
    public class SwapChainVtableResolver : BaseAddressResolver, ISwapChainAddressResolver
    {
        private const int DxgiSwapchainMethodCount = 18;
        private const int D3D11DeviceMethodCount = 43;

        private static SwapChainDescription CreateSwapChainDescription(IntPtr renderForm) {
            return new SwapChainDescription {
                BufferCount = 1,
                Flags = SwapChainFlags.None,
                IsWindowed = true,
                ModeDescription = new ModeDescription(100, 100, new Rational(60, 1), Format.R8G8B8A8_UNorm),
                OutputHandle = renderForm,
                SampleDescription = new SampleDescription(1, 0),
                SwapEffect = SwapEffect.Discard,
                Usage = Usage.RenderTargetOutput
            };
        }

        private IntPtr[] GetVTblAddresses(IntPtr pointer, int numberOfMethods)
        {
            return GetVTblAddresses(pointer, 0, numberOfMethods);
        }

        private IntPtr[] GetVTblAddresses(IntPtr pointer, int startIndex, int numberOfMethods)
        {
            List<IntPtr> vtblAddresses = new List<IntPtr>();
            IntPtr vTable = Marshal.ReadIntPtr(pointer);
            for (int i = startIndex; i < startIndex + numberOfMethods; i++)
                vtblAddresses.Add(Marshal.ReadIntPtr(vTable, i * IntPtr.Size)); // using IntPtr.Size allows us to support both 32 and 64-bit processes

            return vtblAddresses.ToArray();
        }

        private List<IntPtr> d3d11VTblAddresses = null;
        private List<IntPtr> dxgiSwapChainVTblAddresses = null;

        #region Internal device resources

        private Device device;
        private SwapChain swapChain;
        private RenderForm renderForm;
        #endregion

        #region Addresses

        public IntPtr Present { get; set; }
        public IntPtr ResizeBuffers { get; set; }

        #endregion

        protected override void Setup64Bit(SigScanner sig) {
            if (this.d3d11VTblAddresses == null) {
                this.d3d11VTblAddresses = new List<IntPtr>();
                this.dxgiSwapChainVTblAddresses = new List<IntPtr>();

                #region Get Device and SwapChain method addresses

                // Create temporary device + swapchain and determine method addresses
                this.renderForm = new RenderForm();
                Device.CreateWithSwapChain(
                    DriverType.Hardware,
                    DeviceCreationFlags.BgraSupport,
                    CreateSwapChainDescription(this.renderForm.Handle),
                    out this.device,
                    out this.swapChain
                );

                if (this.device != null && this.swapChain != null) {
                    this.d3d11VTblAddresses.AddRange(
                        GetVTblAddresses(this.device.NativePointer, D3D11DeviceMethodCount));
                    this.dxgiSwapChainVTblAddresses.AddRange(
                        GetVTblAddresses(this.swapChain.NativePointer, DxgiSwapchainMethodCount));
                }

                this.device?.Dispose();
                this.swapChain?.Dispose();

                #endregion
            }

            Present = this.dxgiSwapChainVTblAddresses[8];
            ResizeBuffers = this.dxgiSwapChainVTblAddresses[13];
        }
    }
}
