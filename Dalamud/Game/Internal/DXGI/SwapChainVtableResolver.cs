using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Dalamud.Game.Internal.DXGI.Definitions;
using Serilog;

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

        /// <summary>
        /// Gets a value indicating whether or not ReShade is loaded/used.
        /// </summary>
        public bool IsReshade { get; private set; }

        /// <inheritdoc/>
        protected override unsafe void Setup64Bit(SigScanner sig)
        {
            var kernelDev = FFXIVClientStructs.FFXIV.Client.Graphics.Kernel.Device.Instance();

            var scVtbl = GetVTblAddresses(new IntPtr(kernelDev->SwapChain->DXGISwapChain), Enum.GetValues(typeof(IDXGISwapChainVtbl)).Length);

            this.Present = scVtbl[(int)IDXGISwapChainVtbl.Present];

            var modules = Process.GetCurrentProcess().Modules;
            foreach (ProcessModule processModule in modules)
            {
                if (processModule.FileName != null && (processModule.FileName.EndsWith("game\\dxgi.dll") || processModule.FileName.EndsWith("game\\d3d11.dll")))
                {
                    // reshade master@4232872 RVA
                    // var p = processModule.BaseAddress + 0x82C7E0; // DXGISwapChain::Present
                    // var p = processModule.BaseAddress + 0x82FAC0; // DXGISwapChain::runtime_present

                    var scanner = new SigScanner(processModule);
                    try
                    {
                        var p = scanner.ScanText("F6 C2 01 0F 85 ?? ?? ?? ??");
                        Log.Information($"ReShade DLL: {processModule.FileName} with DXGISwapChain::runtime_present at {p:X}");

                        this.Present = p;
                        this.IsReshade = true;
                        break;
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Could not find reshade DXGISwapChain::runtime_present offset!");
                    }
                }
            }

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
