using System.Diagnostics;

using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using Serilog;

using TerraFX.Interop.DirectX;

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
    public nint Present { get; set; }

    /// <inheritdoc/>
    public nint ResizeBuffers { get; set; }

    /// <summary>
    /// Gets a value indicating whether or not ReShade is loaded/used.
    /// </summary>
    public bool IsReshade { get; private set; }

    /// <summary>Gets the game's active instance of IDXGISwapChain that is initialized.</summary>
    /// <returns>Address of the game's instance of IDXGISwapChain, or <c>null</c> if not available (yet.)</returns>
    public static unsafe IDXGISwapChain* GetGameDeviceSwapChain()
    {
        var kernelDev = Device.Instance();
        if (kernelDev == null)
            return null;

        var swapChain = kernelDev->SwapChain;
        if (swapChain == null)
            return null;

        // BackBuffer should be something from IDXGISwapChain->GetBuffer, which means that IDXGISwapChain itself
        // must have been fully initialized.
        if (swapChain->BackBuffer == null)
            return null;

        return (IDXGISwapChain*)swapChain->DXGISwapChain;
    }

    /// <inheritdoc/>
    protected override unsafe void Setup64Bit(ISigScanner sig)
    {
        IDXGISwapChain* dxgiSwapChain;
        do
        {
            dxgiSwapChain = GetGameDeviceSwapChain();
        }
        while (dxgiSwapChain is null);

        var vtbl = (IDXGISwapChain.Vtbl<IDXGISwapChain>*)dxgiSwapChain->lpVtbl;
        this.Present = (nint)vtbl->Present;

        var modules = Process.GetCurrentProcess().Modules;
        foreach (ProcessModule processModule in modules)
        {
            if (!processModule.FileName.EndsWith("game\\dxgi.dll", StringComparison.InvariantCultureIgnoreCase))
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
                var reShadeDxgiPresent = nint.Zero;

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
                if (reShadeDxgiPresent == nint.Zero && fileInfo.FileDescription?.Contains("GShade 4.") == true)
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

                if (reShadeDxgiPresent == nint.Zero)
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
                                Util.DescribeAddress(reShadeDxgiPresent));

                if (reShadeDxgiPresent != nint.Zero)
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

        this.ResizeBuffers = (nint)vtbl->ResizeBuffers;
    }
}
