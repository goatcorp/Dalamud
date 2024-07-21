using System.Diagnostics;
using System.Threading;

using Dalamud.Game;
using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;

using Serilog;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Dalamud.Interface.Internal;

/// <summary>Helper for dealing with swap chains.</summary>
internal static unsafe class SwapChainHelper
{
    /// <summary>
    /// Gets the function pointer for ReShade's DXGISwapChain::on_present.
    /// <a href="https://github.com/crosire/reshade/blob/59eeecd0c902129a168cd772a63c46c5254ff2c5/source/dxgi/dxgi_swapchain.hpp#L88">Source.</a>
    /// </summary>
    public static delegate* unmanaged<nint, uint, nint, void> ReshadeOnPresent { get; private set; }

    /// <summary>Gets the game's active instance of IDXGISwapChain that is initialized.</summary>
    /// <value>Address of the game's instance of IDXGISwapChain, or <c>null</c> if not available (yet.)</value>
    public static IDXGISwapChain* GameDeviceSwapChain
    {
        get
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
    }

    /// <summary>Gets the vtable of <see cref="GameDeviceSwapChain"/>.</summary>
    public static IDXGISwapChain.Vtbl<IDXGISwapChain>* GameDeviceSwapChainVtbl
    {
        get
        {
            var s = GameDeviceSwapChain;
            return (IDXGISwapChain.Vtbl<IDXGISwapChain>*)(s is null ? null : s->lpVtbl);
        }
    }

    /// <inheritdoc cref="IsGameDeviceSwapChain{T}"/>
    public static bool IsGameDeviceSwapChain(nint punk) => IsGameDeviceSwapChain((IUnknown*)punk);

    /// <summary>Determines if the given instance of IUnknown is the game device's swap chain.</summary>
    /// <param name="punk">Object to check.</param>
    /// <typeparam name="T">Type of the object to check.</typeparam>
    /// <returns><c>true</c> if the object is the game's swap chain.</returns>
    public static bool IsGameDeviceSwapChain<T>(T* punk) where T : unmanaged, IUnknown.Interface
    {
        // https://learn.microsoft.com/en-us/windows/win32/api/unknwn/nf-unknwn-iunknown-queryinterface(refiid_void)
        // For any given COM object (also known as a COM component), a specific query for the IUnknown interface on any
        // of the object's interfaces must always return the same pointer value.

        var gdsc = GameDeviceSwapChain;
        if (gdsc is null || punk is null)
            return false;

        fixed (Guid* iid = &IID.IID_IUnknown)
        {
            using var u1 = default(ComPtr<IUnknown>);
            if (gdsc->QueryInterface(iid, (void**)u1.GetAddressOf()).FAILED)
                return false;

            using var u2 = default(ComPtr<IUnknown>);
            if (punk->QueryInterface(iid, (void**)u2.GetAddressOf()).FAILED)
                return false;

            return u1.Get() == u2.Get();
        }
    }

    /// <summary>Wait for the game to have finished initializing the IDXGISwapChain.</summary>
    public static void BusyWaitForGameDeviceSwapChain()
    {
        while (GameDeviceSwapChain is null)
            Thread.Yield();
    }

    /// <summary>Detects ReShade and populate <see cref="ReshadeOnPresent"/>.</summary>
    public static void DetectReShade()
    {
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

                Log.Information(
                    "ReShade DLL: {FileName} ({Info} - {Version}) with DXGISwapChain::on_present at {Address}",
                    processModule.FileName,
                    fileInfo.FileDescription ?? "Unknown",
                    fileInfo.FileVersion ?? "Unknown",
                    Util.DescribeAddress(reShadeDxgiPresent));

                if (reShadeDxgiPresent != nint.Zero)
                {
                    ReshadeOnPresent = (delegate* unmanaged<nint, uint, nint, void>)reShadeDxgiPresent;
                }

                break;
            }
            catch (Exception e)
            {
                Log.Error(e, "Failed to get ReShade version info");
                break;
            }
        }
    }
}
