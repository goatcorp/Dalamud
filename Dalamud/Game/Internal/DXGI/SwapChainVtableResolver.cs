using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Dalamud.Game.Internal.DXGI.Definitions;
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
        private List<IntPtr> d3d11VTblAddresses;
        private List<IntPtr> dxgiSwapChainVTblAddresses;

        /// <inheritdoc/>
        public IntPtr Present { get; set; }

        /// <inheritdoc/>
        public IntPtr ResizeBuffers { get; set; }

        /// <inheritdoc/>
        protected override void Setup64Bit(SigScanner sig)
        {
            // Create temporary device + swapchain and determine method addresses
            if (this.d3d11VTblAddresses == null)
            {
                // A renderable object isnt required, just a handle
                var handle = Marshal.AllocHGlobal(Marshal.SizeOf<IntPtr>());

                Device.CreateWithSwapChain(
                    DriverType.Hardware,
                    DeviceCreationFlags.BgraSupport,
                    CreateSwapChainDescription(handle),
                    out var device,
                    out var swapChain);

                if (device != null && swapChain != null)
                {
                    this.d3d11VTblAddresses = GetVTblAddresses(device.NativePointer, Enum.GetValues(typeof(ID3D11DeviceVtbl)).Length);
                    this.dxgiSwapChainVTblAddresses = GetVTblAddresses(swapChain.NativePointer, Enum.GetValues(typeof(IDXGISwapChainVtbl)).Length);
                }

                device?.Dispose();
                swapChain?.Dispose();

                Marshal.FreeHGlobal(handle);
            }

            this.Present = this.dxgiSwapChainVTblAddresses[(int)IDXGISwapChainVtbl.Present];
            this.ResizeBuffers = this.dxgiSwapChainVTblAddresses[(int)IDXGISwapChainVtbl.ResizeBuffers];
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

        private static List<IntPtr> GetVTblAddresses(IntPtr pointer, int numberOfMethods)
        {
            return GetVTblAddresses(pointer, 0, numberOfMethods);
        }

        private static List<IntPtr> GetVTblAddresses(IntPtr pointer, int startIndex, int numberOfMethods)
        {
            var vtblAddresses = new List<IntPtr>();
            var vTable = Marshal.ReadIntPtr(pointer);
            for (var i = startIndex; i < startIndex + numberOfMethods; i++)
                vtblAddresses.Add(Marshal.ReadIntPtr(vTable, i * IntPtr.Size)); // using IntPtr.Size allows us to support both 32 and 64-bit processes

            return vtblAddresses;
        }
    }
}
