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
            if (processModule.FileName == null || !processModule.FileName.EndsWith("game\\dxgi.dll"))
                continue;
            
            try
            {
                var fileInfo = FileVersionInfo.GetVersionInfo(processModule.FileName);

                if (fileInfo.FileDescription == null)
                    break;

                if (!fileInfo.FileDescription.Contains("GShade") && !fileInfo.FileDescription.Contains("ReShade"))
                    break;
     
                // warning: these comments may no longer be accurate.
                // reshade master@4232872 RVA
                // var p = processModule.BaseAddress + 0x82C7E0; // DXGISwapChain::Present
                // var p = processModule.BaseAddress + 0x82FAC0; // DXGISwapChain::runtime_present
                // DXGISwapChain::handle_device_loss =>df DXGISwapChain::Present => DXGISwapChain::runtime_present
                // 5.2+ - F6 C2 01 0F 85
                // 6.0+ - F6 C2 01 0F 85 88

                var scanner = new SigScanner(processModule);
                var reShadeDxgiPresent = IntPtr.Zero;

                if (fileInfo.FileVersion?.StartsWith("6.") == true)
                {
                    // No Addon
                    if (scanner.TryScanText("F6 C2 01 0F 85 A8", out reShadeDxgiPresent)) 
                    {
                        Log.Information("Hooking present for ReShade 6 No-Addon");
                    }

                    // Addon
                    else if (scanner.TryScanText("F6 C2 01 0F 85 88", out reShadeDxgiPresent)) 
                    {
                        Log.Information("Hooking present for ReShade 6 Addon");
                    }
                            
                    // Fallback
                    else
                    {
                        Log.Error("Failed to get ReShade 6 DXGISwapChain::on_present offset!");
                    }
                }

                // Looks like this sig only works for GShade 4
                if (reShadeDxgiPresent == IntPtr.Zero && fileInfo.FileDescription?.Contains("GShade 4.") == true)
                {
                    if (scanner.TryScanText("E8 ?? ?? ?? ?? 45 0F B6 5E ??", out reShadeDxgiPresent))
                    {
                        Log.Information("Hooking present for GShade 4");
                    }
                    else
                    {
                        Log.Error("Failed to find GShade 4 DXGISwapChain::on_present offset!");
                    }
                }

                if (reShadeDxgiPresent == IntPtr.Zero)
                {
                    if (scanner.TryScanText("F6 C2 01 0F 85", out reShadeDxgiPresent))
                    {
                        Log.Information("Hooking present for ReShade with fallback 5.X sig");
                    }
                    else
                    {
                        Log.Error("Failed to find ReShade DXGISwapChain::on_present offset with fallback sig!");
                    }
                }

                Log.Information("ReShade DLL: {FileName} ({Info} - {Version}) with DXGISwapChain::on_present at {Address}",
                                processModule.FileName,
                                fileInfo.FileDescription ?? "Unknown",
                                fileInfo.FileVersion ?? "Unknown",
                                reShadeDxgiPresent.ToString("X"));

                if (reShadeDxgiPresent != IntPtr.Zero)
                {
                    this.Present = reShadeDxgiPresent;
                    this.IsReshade = true;
                }

                break;
            }
            catch (Exception e)
            {
                Log.Error(e, "Failed to get ReShade version info");
                break;
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
