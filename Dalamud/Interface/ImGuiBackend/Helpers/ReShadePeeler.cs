using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

using static TerraFX.Interop.Windows.Windows;

namespace Dalamud.Interface.ImGuiBackend.Helpers;

/// <summary>
/// Peels ReShade off stuff.
/// </summary>
[SuppressMessage(
    "StyleCop.CSharp.LayoutRules",
    "SA1519:Braces should not be omitted from multi-line child statement",
    Justification = "Multiple fixed blocks")]
internal static unsafe class ReShadePeeler
{
    /// <summary>
    /// Peels <see cref="IDXGISwapChain"/> if it is wrapped by ReShade.
    /// </summary>
    /// <param name="comptr">[inout] The COM pointer to an instance of <see cref="IDXGISwapChain"/>.</param>
    /// <typeparam name="T">A COM type that is or extends <see cref="IDXGISwapChain"/>.</typeparam>
    /// <returns><c>true</c> if peeled.</returns>
    public static bool PeelSwapChain<T>(ComPtr<T>* comptr)
        where T : unmanaged, IDXGISwapChain.Interface =>
        PeelIUnknown(comptr, sizeof(IDXGISwapChain.Vtbl<IDXGISwapChain>));

    private static bool PeelIUnknown<T>(ComPtr<T>* comptr, nint vtblSize)
        where T : unmanaged, IUnknown.Interface
    {
        var changed = false;
        while (comptr->Get() != null && IsReShadedComObject(comptr->Get()))
        {
            // Expectation: the pointer to the underlying object should come early after the overriden vtable.
            for (nint i = 8; i <= 0x20; i += 8)
            {
                var ppObjectBehind = (nint)comptr->Get() + i;

                // Is the thing directly pointed from the address an actual something in the memory?
                if (!IsValidReadableMemoryAddress(ppObjectBehind, 8))
                    continue;

                var pObjectBehind = *(nint*)ppObjectBehind;

                // Is the address of vtable readable?
                if (!IsValidReadableMemoryAddress(pObjectBehind, sizeof(nint)))
                    continue;
                var pObjectBehindVtbl = *(nint*)pObjectBehind;

                // Is the vtable itself readable?
                if (!IsValidReadableMemoryAddress(pObjectBehindVtbl, vtblSize))
                    continue;

                // Are individual functions in vtable executable?
                var valid = true;
                for (var j = 0; valid && j < vtblSize; j += sizeof(nint))
                    valid &= IsValidExecutableMemoryAddress(*(nint*)(pObjectBehindVtbl + j), 1);
                if (!valid)
                    continue;

                // Interpret the object as an IUnknown.
                // Note that `using` is not used, and `Attach` is used. We do not alter the reference count yet.
                var punk = default(ComPtr<IUnknown>);
                punk.Attach((IUnknown*)pObjectBehind);

                // Is the IUnknown object also the type we want?
                using var comptr2 = default(ComPtr<T>);
                if (punk.As(&comptr2).FAILED)
                    continue;

                comptr2.Swap(comptr);
                changed = true;
                break;
            }

            if (!changed)
                break;
        }

        return changed;
    }

    private static bool BelongsInReShadeDll(nint ptr)
    {
        foreach (ProcessModule processModule in Process.GetCurrentProcess().Modules)
        {
            if (ptr < processModule.BaseAddress)
                continue;

            var dosh = (IMAGE_DOS_HEADER*)processModule.BaseAddress;
            var nth = (IMAGE_NT_HEADERS64*)(processModule.BaseAddress + dosh->e_lfanew);
            if (ptr >= processModule.BaseAddress + nth->OptionalHeader.SizeOfImage)
                continue;

            fixed (byte* pfn0 = "CreateDXGIFactory"u8)
            fixed (byte* pfn1 = "D2D1CreateDevice"u8)
            fixed (byte* pfn2 = "D3D10CreateDevice"u8)
            fixed (byte* pfn3 = "D3D11CreateDevice"u8)
            fixed (byte* pfn4 = "D3D12CreateDevice"u8)
            fixed (byte* pfn5 = "glBegin"u8)
            fixed (byte* pfn6 = "vkCreateDevice"u8)
            {
                if (GetProcAddress((HMODULE)dosh, (sbyte*)pfn0) == 0)
                    continue;
                if (GetProcAddress((HMODULE)dosh, (sbyte*)pfn1) == 0)
                    continue;
                if (GetProcAddress((HMODULE)dosh, (sbyte*)pfn2) == 0)
                    continue;
                if (GetProcAddress((HMODULE)dosh, (sbyte*)pfn3) == 0)
                    continue;
                if (GetProcAddress((HMODULE)dosh, (sbyte*)pfn4) == 0)
                    continue;
                if (GetProcAddress((HMODULE)dosh, (sbyte*)pfn5) == 0)
                    continue;
                if (GetProcAddress((HMODULE)dosh, (sbyte*)pfn6) == 0)
                    continue;
            }

            var fileInfo = FileVersionInfo.GetVersionInfo(processModule.FileName);

            if (fileInfo.FileDescription == null)
                continue;

            if (!fileInfo.FileDescription.Contains("GShade") && !fileInfo.FileDescription.Contains("ReShade"))
                continue;

            return true;
        }

        return false;
    }

    private static bool IsReShadedComObject<T>(T* obj)
        where T : unmanaged, IUnknown.Interface
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
                if (!BelongsInReShadeDll(pfn))
                    return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsValidReadableMemoryAddress(nint p, nint size)
    {
        while (size > 0)
        {
            if (!IsValidUserspaceMemoryAddress(p))
                return false;

            MEMORY_BASIC_INFORMATION mbi;
            if (VirtualQuery((void*)p, &mbi, (nuint)sizeof(MEMORY_BASIC_INFORMATION)) == 0)
                return false;

            if (mbi is not
                {
                    State: MEM.MEM_COMMIT,
                    Protect: PAGE.PAGE_READONLY or PAGE.PAGE_READWRITE or PAGE.PAGE_EXECUTE_READ
                    or PAGE.PAGE_EXECUTE_READWRITE,
                })
                return false;

            var regionSize = (nint)((mbi.RegionSize + 0xFFFUL) & ~0x1000UL);
            var checkedSize = ((nint)mbi.BaseAddress + regionSize) - p;
            size -= checkedSize;
            p += checkedSize;
        }

        return true;
    }

    private static bool IsValidExecutableMemoryAddress(nint p, nint size)
    {
        while (size > 0)
        {
            if (!IsValidUserspaceMemoryAddress(p))
                return false;

            MEMORY_BASIC_INFORMATION mbi;
            if (VirtualQuery((void*)p, &mbi, (nuint)sizeof(MEMORY_BASIC_INFORMATION)) == 0)
                return false;

            if (mbi is not
                {
                    State: MEM.MEM_COMMIT,
                    Protect: PAGE.PAGE_EXECUTE or PAGE.PAGE_EXECUTE_READ or PAGE.PAGE_EXECUTE_READWRITE
                    or PAGE.PAGE_EXECUTE_WRITECOPY,
                })
                return false;

            var regionSize = (nint)((mbi.RegionSize + 0xFFFUL) & ~0x1000UL);
            var checkedSize = ((nint)mbi.BaseAddress + regionSize) - p;
            size -= checkedSize;
            p += checkedSize;
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsValidUserspaceMemoryAddress(nint p)
    {
        // https://learn.microsoft.com/en-us/windows-hardware/drivers/gettingstarted/virtual-address-spaces
        // A 64-bit process on 64-bit Windows has a virtual address space within the 128-terabyte range
        // 0x000'00000000 through 0x7FFF'FFFFFFFF.
        return p >= 0x10000 && p <= unchecked((nint)0x7FFF_FFFFFFFFUL);
    }
}
