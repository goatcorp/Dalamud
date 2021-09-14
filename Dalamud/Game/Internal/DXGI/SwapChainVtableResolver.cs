using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Dalamud.Game.Internal.DXGI.Definitions;
using Serilog;
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
        /// <inheritdoc/>
        public IntPtr Present { get; set; }

        /// <inheritdoc/>
        public IntPtr ResizeBuffers { get; set; }

        /// <inheritdoc/>
        protected override unsafe void Setup64Bit(SigScanner sig)
        {
            var kernelDev = FFXIVClientStructs.FFXIV.Client.Graphics.Kernel.Device.Instance();

            var scVtbl = GetVTblAddresses(new IntPtr(kernelDev->SwapChain->DXGISwapChain), Enum.GetValues(typeof(IDXGISwapChainVtbl)).Length);

            this.Present = scVtbl[(int)IDXGISwapChainVtbl.Present];
            this.ResizeBuffers = scVtbl[(int)IDXGISwapChainVtbl.ResizeBuffers];
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
