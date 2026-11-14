using System.Diagnostics;
using System.Runtime.InteropServices;

using Dalamud.Support;

using TerraFX.Interop.DirectX;

namespace Dalamud.Interface.Internal.Unwrapper;

/// <summary>Recognizes COM wrappers whose IUnknown methods are implemented by NvPresent.</summary>
internal unsafe class NvPresentUnwrapper : ComHookUnwrapper
{
    private const int GetDescSlot = 12;

    /// <inheritdoc/>
    protected override bool FailOnUnwrapFailure => true;

    /// <summary>Recognizes a bounded forwarding path from an x64 swap-chain GetDesc implementation.</summary>
    /// <param name="code">The function's initial instruction bytes.</param>
    /// <param name="offsets">The pointer fields traversed to reach its forwarded receiver.</param>
    /// <returns>Whether the supported instructions establish an underlying receiver.</returns>
    internal static bool TryGetSwapChainOffsets(ReadOnlySpan<byte> code, out int[] offsets) =>
        SwapChainForwardingResolver.TryResolve(code, out offsets);

    /// <inheritdoc/>
    protected override bool TryGetCandidateAddress<T>(T* obj, int candidateIndex, out nint candidateAddress)
    {
        candidateAddress = 0;

        // Derive a single candidate from GetDesc forwarding without probing nearby object fields.
        if (candidateIndex != 0 || typeof(T) != typeof(IDXGISwapChain) ||
            !IsValidReadableMemoryAddress((nint)obj, nint.Size))
            return false;

        var vtable = *(nint**)obj;
        if (!IsValidReadableMemoryAddress((nint)vtable, (GetDescSlot + 1) * nint.Size))
            return false;

        // Decode only a readable, executable range contained entirely within NvPresent.
        var getDesc = vtable[GetDescSlot];
        if (!BelongsInNvPresentDll(getDesc, SwapChainForwardingResolver.MaximumCodeLength) ||
            !IsValidReadableMemoryAddress(getDesc, SwapChainForwardingResolver.MaximumCodeLength) ||
            !IsValidExecutableMemoryAddress(getDesc, SwapChainForwardingResolver.MaximumCodeLength) ||
            !TryGetSwapChainOffsets(new ReadOnlySpan<byte>((void*)getDesc, SwapChainForwardingResolver.MaximumCodeLength), out var offsets))
            return false;

        // Follow the decoded receiver path, validating each pointer field before reading it.
        var current = (nint)obj;
        nint fieldAddress = 0;
        foreach (var offset in offsets)
        {
            if (current <= 0 || current > nint.MaxValue - offset)
                return false;

            fieldAddress = current + offset;
            if (!IsValidReadableMemoryAddress(fieldAddress, nint.Size))
                return false;

            current = *(nint*)fieldAddress;
        }

        // Return the final pointer field's address for validation by the base unwrapper.
        candidateAddress = fieldAddress;
        return candidateAddress != 0;
    }

    /// <inheritdoc/>
    protected override bool IsRelevantComObject<T>(T* obj)
    {
        if (!IsValidReadableMemoryAddress((nint)obj, sizeof(nint)))
            return false;

        try
        {
            var vtbl = (nint**)Marshal.ReadIntPtr((nint)obj);
            if (!IsValidReadableMemoryAddress((nint)vtbl, sizeof(nint) * 3))
                return false;

            for (var i = 0; i < 3; i++)
            {
                var pfn = Marshal.ReadIntPtr((nint)(vtbl + i));
                if (!IsValidExecutableMemoryAddress(pfn, 1))
                    return false;
                if (!BelongsInNvPresentDll(pfn))
                    return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Checks whether the entire code range belongs to a loaded NvPresent module.</summary>
    /// <param name="ptr">The beginning of the code range.</param>
    /// <param name="length">The number of bytes in the code range.</param>
    /// <returns>Whether the range is within an NvPresent module.</returns>
    private static bool BelongsInNvPresentDll(nint ptr, int length = 1)
    {
        foreach (ProcessModule processModule in CurrentProcessModules.ModuleCollection)
        {
            if (length <= 0 || processModule.ModuleMemorySize < length || ptr < processModule.BaseAddress ||
                ptr - processModule.BaseAddress > processModule.ModuleMemorySize - length ||
                !processModule.ModuleName.Contains("NvPresent", StringComparison.OrdinalIgnoreCase))
                continue;

            return true;
        }

        return false;
    }
}
