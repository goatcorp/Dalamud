using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using Dalamud.Support;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Dalamud.Interface.Internal.Unwrapper;

/// <summary>Detects NvPresent wrappers without changing cached swap-chain or presentation-hook targets.</summary>
internal static unsafe class NvPresentDetector
{
    /// <summary>Describes the evidence available from recognized swap-chain implementations.</summary>
    internal enum DetectionResult
    {
        /// <summary>A system DXGI/D3D11 terminal was reached without recognizing NvPresent.</summary>
        NotDetected,

        /// <summary>The current interface's IUnknown methods belong to NvPresent.</summary>
        Detected,

        /// <summary>An opaque, unresolved, cyclic, or unsupported chain prevented inspection.</summary>
        Unknown,
    }

    /// <summary>Checks the original chain for NvPresent, independently of the selected presentation hook.</summary>
    /// <param name="swapChain">The borrowed live original game swap chain, before global unwrapping.</param>
    /// <returns>Wrapper participation, not the driver's Smooth Motion setting.</returns>
    internal static DetectionResult Inspect(IDXGISwapChain* swapChain)
    {
        if (!NativeSwapChainRecognizer.IsInspectable(swapChain))
            return DetectionResult.Unknown;

        using var inspected = new ComPtr<IDXGISwapChain>(swapChain);
        var nvPresent = new NvPresentUnwrapper();
        var reShade = new ReShadeUnwrapper();
        var native = new NativeSwapChainRecognizer();
        var visited = new HashSet<nint>();

        while (true)
        {
            // IUnknown module ownership establishes participation without decoding private GetDesc forwarding.
            if (nvPresent.Recognizes(inspected.Get()))
                return DetectionResult.Detected;

            if (!reShade.Recognizes(inspected.Get()))
                return native.Recognizes(inspected.Get()) ? DetectionResult.NotDetected : DetectionResult.Unknown;

            try
            {
                // Peel only this owned reference, with shared cycle detection and bounded traversal.
                if (!reShade.Unwrap(&inspected, visited))
                    return DetectionResult.Unknown;
            }
            catch (InvalidOperationException)
            {
                return DetectionResult.Unknown;
            }
        }
    }

    private sealed class NativeSwapChainRecognizer : ComHookUnwrapper
    {
        public static bool IsInspectable<T>(T* obj)
            where T : unmanaged, IUnknown.Interface
        {
            if (!IsValidReadableMemoryAddress((nint)obj, nint.Size))
                return false;

            try
            {
                var vtable = *(nint**)obj;
                if (!IsValidReadableMemoryAddress((nint)vtable, 3 * nint.Size))
                    return false;

                for (var i = 0; i < 3; i++)
                {
                    if (!IsValidExecutableMemoryAddress(vtable[i], 1))
                        return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        protected override bool IsRelevantComObject<T>(T* obj)
        {
            if (!IsInspectable(obj))
                return false;

            try
            {
                var vtable = *(nint**)obj;
                for (var i = 0; i < 3; i++)
                {
                    if (!BelongsToNativeRuntime(vtable[i]))
                        return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool BelongsToNativeRuntime(nint pointer)
        {
            foreach (ProcessModule module in CurrentProcessModules.ModuleCollection)
            {
                if (pointer < module.BaseAddress || pointer - module.BaseAddress >= module.ModuleMemorySize)
                    continue;

                // Proxy DLLs may share the runtime filename; require the operating-system runtime path.
                var path = Path.GetFullPath(module.FileName);
                return string.Equals(path, Path.Combine(Environment.SystemDirectory, "dxgi.dll"), StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(path, Path.Combine(Environment.SystemDirectory, "d3d11.dll"), StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }
    }
}
