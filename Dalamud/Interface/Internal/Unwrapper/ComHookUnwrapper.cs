using System.Runtime.CompilerServices;

using TerraFX.Interop.Windows;

using static TerraFX.Interop.Windows.Windows;

namespace Dalamud.Interface.Internal.Unwrapper;

/// <summary>Unwraps IUnknown wrapped by other graphics injectors.</summary>
internal abstract unsafe class ComHookUnwrapper
{
    /// <summary>Unwraps <typeparamref name="T"/> if it is wrapped by a relevant COM hook.</summary>
    /// <param name="comptr">[inout] The COM pointer to an instance of <typeparamref name="T"/>.</param>
    /// <typeparam name="T">A COM type that is or extends <see cref="IUnknown"/>.</typeparam>
    /// <returns><c>true</c> if peeled.</returns>
    public bool Unwrap<T>(ComPtr<T>* comptr)
        where T : unmanaged, IUnknown.Interface
    {
        if (typeof(T).GetNestedType("Vtbl`1") is not { } vtblType)
            return false;

        nint vtblSize = vtblType.GetFields().Length * sizeof(nint);
        var changed = false;
        while (comptr->Get() != null && this.IsRelevantComObject(comptr->Get()))
        {
            // Expectation: the pointer to the underlying object should come early after the overridden vtable.
            for (nint i = sizeof(nint); i <= 0x20; i += sizeof(nint))
            {
                var ppObjectBehind = (nint)comptr->Get() + i;

                if (!IsValidReadableMemoryAddress(ppObjectBehind, 8))
                    continue;

                var pObjectBehind = *(nint*)ppObjectBehind;

                if (!IsValidReadableMemoryAddress(pObjectBehind, sizeof(nint)))
                    continue;

                var pObjectBehindVtbl = *(nint*)pObjectBehind;

                if (!IsValidReadableMemoryAddress(pObjectBehindVtbl, vtblSize))
                    continue;

                var valid = true;
                for (var j = 0; valid && j < vtblSize; j += sizeof(nint))
                    valid &= IsValidExecutableMemoryAddress(*(nint*)(pObjectBehindVtbl + j), 1);

                if (!valid)
                    continue;

                var punk = default(ComPtr<IUnknown>);
                punk.Attach((IUnknown*)pObjectBehind);

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

    /// <summary>
    /// Whether the given memory address is a valid readable userspace memory region of the given size.
    /// </summary>
    /// <param name="p">Pointer to read from.</param>
    /// <param name="size">Size to read.</param>
    /// <returns>Whether the memory is readable.</returns>
    protected static bool IsValidReadableMemoryAddress(nint p, nint size)
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

    /// <summary>
    /// Whether the given memory address is a valid executable userspace memory region of the given size.
    /// </summary>
    /// <param name="p">Pointer to read from.</param>
    /// <param name="size">Size to read.</param>
    /// <returns>Whether the memory is executable.</returns>
    protected static bool IsValidExecutableMemoryAddress(nint p, nint size)
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

    /// <summary>
    /// Checks whether a given COM object is relevant to this unwrapper.
    /// </summary>
    /// <param name="obj">The object to check.</param>
    /// <typeparam name="T">The type of the COM object.</typeparam>
    /// <returns>Whether we should go ahead with the unwrap.</returns>
    protected abstract bool IsRelevantComObject<T>(T* obj)
        where T : unmanaged, IUnknown.Interface;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsValidUserspaceMemoryAddress(nint p)
    {
        // https://learn.microsoft.com/en-us/windows-hardware/drivers/gettingstarted/virtual-address-spaces
        // A 64-bit process on 64-bit Windows has a virtual address space within the 128-terabyte range
        // 0x000'00000000 through 0x7FFF'FFFFFFFF.
        return p >= 0x10000 && p <= unchecked((nint)0x7FFF_FFFFFFFFUL);
    }
}
