using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Dalamud.Game.Internal.DXGI.Definitions;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using Serilog;

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

    /// <summary>
    /// Gets a value indicating whether or not ReShade is loaded/used.
    /// </summary>
    public bool IsReshade { get; private set; }

    /// <inheritdoc/>
    protected override unsafe void Setup64Bit(ISigScanner sig)
    {
        Device* kernelDev;
        SwapChain* swapChain;
        void* dxgiSwapChain;

        while (true)
        {
            kernelDev = Device.Instance();
            if (kernelDev == null)
                continue;

            swapChain = kernelDev->SwapChain;
            if (swapChain == null)
                continue;

            dxgiSwapChain = swapChain->DXGISwapChain;
            if (dxgiSwapChain == null)
                continue;

            break;
        }

        var scVtbl = GetVTblAddresses(new IntPtr(dxgiSwapChain), Enum.GetValues(typeof(IDXGISwapChainVtbl)).Length);

        this.Present = scVtbl[(int)IDXGISwapChainVtbl.Present];

        var modules = Process.GetCurrentProcess().Modules;
        foreach (ProcessModule processModule in modules)
        {
            if (processModule.FileName != null && processModule.FileName.EndsWith("game\\dxgi.dll"))
            {
                var fileInfo = FileVersionInfo.GetVersionInfo(processModule.FileName);

                if (fileInfo.FileDescription == null)
                    break;

                if (!fileInfo.FileDescription.Contains("GShade") && !fileInfo.FileDescription.Contains("ReShade"))
                    break;

                // reshade master@4232872 RVA
                // var p = processModule.BaseAddress + 0x82C7E0; // DXGISwapChain::Present
                // var p = processModule.BaseAddress + 0x82FAC0; // DXGISwapChain::runtime_present

                // DXGISwapChain::handle_device_loss => DXGISwapChain::Present => DXGISwapChain::runtime_present

                var scanner = new SigScanner(processModule);
                var runtimePresentSig = "F6 C2 01 0F 85 ?? ?? ?? ??";

                try
                {
                    // Looks like this sig only works for GShade 4
                    if (fileInfo.FileDescription?.Contains("GShade 4.") == true)
                    {
                        Log.Verbose("Hooking present for GShade 4");
                        runtimePresentSig = "E8 ?? ?? ?? ?? 45 0F B6 5E ??";
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to get reshade version info - falling back to default DXGISwapChain::runtime_present signature");
                }

                try
                {
                    var p = scanner.ScanText(runtimePresentSig);
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
