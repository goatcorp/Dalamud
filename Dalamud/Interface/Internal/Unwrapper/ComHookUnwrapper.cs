using System.Collections.Generic;
using System.Runtime.CompilerServices;

using TerraFX.Interop.Windows;

using static TerraFX.Interop.Windows.Windows;

namespace Dalamud.Interface.Internal.Unwrapper;

/// <summary>Unwraps IUnknown wrapped by other graphics injector.</summary>
internal abstract unsafe class ComHookUnwrapper
{
    /// <summary>Unwraps <typeparamref name="T"/> if it is wrapped by ReShade.</summary>
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

        // Track objects we've already peeled so a wrapper whose pointer scan resolves back to an
        // already-visited object (or to itself) cannot spin forever -> game hang.
        var visited = new HashSet<nint>();
        while (comptr->Get() != null && this.IsRelevantComObject(comptr->Get()))
        {
            var currentObject = (nint)comptr->Get();
            if (!visited.Add(currentObject))
                break; // already seen this object -> cycle, stop to avoid infinite loop

            // Expectation: the pointer to the underlying object should come early after the overriden vtable.
            var peeled = false;
            for (nint i = sizeof(nint); i <= 0x20; i += sizeof(nint))
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

                // The candidate resolved back to the object we're currently peeling; swapping to
                // self is not real progress and would let the outer loop spin. Skip it.
                if ((nint)comptr2.Get() == currentObject)
                    continue;

                comptr2.Swap(comptr);
                changed = true;
                peeled = true;
                break;
            }

            // Use a per-iteration flag: once 'changed' is true it stays true, so the outer
            // loop exit condition must track whether *this* iteration succeeded, not any prior one.
            if (!peeled)
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
        if (size < 0)
            return false;

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

            // Advance by the actual region remaining from p, clamped to what's left to check.
            var regionEnd = (nint)mbi.BaseAddress + (nint)mbi.RegionSize;
            var checkedSize = regionEnd - p;          // bytes validated in this region from p
            if (checkedSize <= 0)
                return false;                          // no forward progress -> bail instead of looping
            checkedSize = Math.Min(checkedSize, size); // never advance past the requested range
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
        if (size < 0)
            return false;

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

            // Advance by the actual region remaining from p, clamped to what's left to check.
            var regionEnd = (nint)mbi.BaseAddress + (nint)mbi.RegionSize;
            var checkedSize = regionEnd - p;          // bytes validated in this region from p
            if (checkedSize <= 0)
                return false;                          // no forward progress -> bail instead of looping
            checkedSize = Math.Min(checkedSize, size); // never advance past the requested range
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
