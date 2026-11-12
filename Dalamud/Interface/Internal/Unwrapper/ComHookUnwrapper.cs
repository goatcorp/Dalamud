using System.Collections.Generic;
using System.Runtime.CompilerServices;

using TerraFX.Interop.Windows;

using static TerraFX.Interop.Windows.Windows;

namespace Dalamud.Interface.Internal.Unwrapper;

/// <summary>Attempts to locate an underlying COM interface in a recognized graphics wrapper.</summary>
/// <remarks>Memory checks reject unsuitable candidates but do not establish COM identity or preserve object lifetime.</remarks>
internal abstract unsafe class ComHookUnwrapper
{
    /// <summary>Gets a value indicating whether an unresolved recognized wrapper causes unwrapping to throw.</summary>
    protected virtual bool FailOnUnwrapFailure => false;

    /// <summary>Checks whether an interface is recognized without inspecting its forwarding layout.</summary>
    /// <param name="obj">The interface to recognize.</param>
    /// <typeparam name="T">A COM type that is or extends <see cref="IUnknown"/>.</typeparam>
    /// <returns>Whether the interface belongs to this wrapper implementation.</returns>
    public bool Recognizes<T>(T* obj)
        where T : unmanaged, IUnknown.Interface => this.IsRelevantComObject(obj);

    /// <summary>Removes consecutive wrappers recognized by <see cref="IsRelevantComObject{T}"/>.</summary>
    /// <param name="comptr">[inout] The COM pointer to an instance of <typeparamref name="T"/>.</param>
    /// <param name="visited">Removed wrapper addresses shared across discovery passes.</param>
    /// <typeparam name="T">A COM type that is or extends <see cref="IUnknown"/>.</typeparam>
    /// <returns><c>true</c> if at least one interface replacement succeeded.</returns>
    /// <exception cref="InvalidOperationException">
    /// A cycle or discovery limit is reached, or a required wrapper cannot be resolved.
    /// </exception>
    public bool Unwrap<T>(ComPtr<T>* comptr, HashSet<nint>? visited = null)
        where T : unmanaged, IUnknown.Interface
    {
        if (typeof(T).GetNestedType("Vtbl`1") is not { } vtblType)
            return false;

        nint vtblSize = vtblType.GetFields().Length * nint.Size;
        var changed = false;

        // Bound discovery and detect cycles across wrapper types.
        visited ??= new HashSet<nint>();
        while (comptr->Get() != null && this.IsRelevantComObject(comptr->Get()))
        {
            if (visited.Count >= 64)
                throw new InvalidOperationException("Graphics middleware exceeded the wrapper-discovery limit.");

            var currentObject = (nint)comptr->Get();
            if (!visited.Add(currentObject))
                throw new InvalidOperationException("A cycle was detected while unwrapping graphics middleware.");

            // Validate candidate fields and vtables before calling QueryInterface.
            var peeled = false;
            for (var i = 0; this.TryGetCandidateAddress(comptr->Get(), i, out var ppObjectBehind); i++)
            {
                if (!IsValidReadableMemoryAddress(ppObjectBehind, 8))
                    continue;

                var pObjectBehind = *(nint*)ppObjectBehind;

                if (!IsValidReadableMemoryAddress(pObjectBehind, nint.Size))
                    continue;
                var pObjectBehindVtbl = *(nint*)pObjectBehind;

                if (!IsValidReadableMemoryAddress(pObjectBehindVtbl, vtblSize))
                    continue;

                var valid = true;
                for (var j = 0; valid && j < vtblSize; j += nint.Size)
                    valid &= IsValidExecutableMemoryAddress(*(nint*)(pObjectBehindVtbl + j), 1);
                if (!valid)
                    continue;

                // Borrow the candidate without AddRef; do not dispose this temporary pointer.
                // As acquires an owned reference, which is transferred to comptr on a successful replacement.
                var punk = default(ComPtr<IUnknown>);
                punk.Attach((IUnknown*)pObjectBehind);

                using var comptr2 = default(ComPtr<T>);
                if (punk.As(&comptr2).Value != 0 || comptr2.Get() == null)
                    continue;

                // Self-references do not advance discovery.
                if ((nint)comptr2.Get() == currentObject)
                    continue;

                comptr2.Swap(comptr);
                changed = true;
                peeled = true;
                break;
            }

            if (!peeled)
            {
                if (this.FailOnUnwrapFailure)
                {
                    throw new InvalidOperationException(
                        $"Could not validate the underlying swap chain for {this.GetType().Name}. " +
                        "Presentation hooks were refused; no arbitrary-field fallback was used.");
                }

                // Keep only removed wrappers in the cycle set so other unwrappers can retry this terminal.
                visited.Remove(currentObject);
                break;
            }
        }

        return changed;
    }

    /// <summary>
    /// Checks whether the requested userspace range is currently mapped with readable memory protection.
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

            var regionEnd = (nint)mbi.BaseAddress + (nint)mbi.RegionSize;
            var checkedSize = regionEnd - p;
            if (checkedSize <= 0)
                return false;
            checkedSize = Math.Min(checkedSize, size);
            size -= checkedSize;
            p += checkedSize;
        }

        return true;
    }

    /// <summary>
    /// Checks whether the requested userspace range is currently mapped with executable memory protection.
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

            var regionEnd = (nint)mbi.BaseAddress + (nint)mbi.RegionSize;
            var checkedSize = regionEnd - p;
            if (checkedSize <= 0)
                return false;
            checkedSize = Math.Min(checkedSize, size);
            size -= checkedSize;
            p += checkedSize;
        }

        return true;
    }

    /// <summary>Finds a field holding an underlying interface candidate.</summary>
    /// <param name="obj">The recognized wrapper.</param>
    /// <param name="candidateIndex">Zero-based candidate index.</param>
    /// <param name="candidateAddress">Address of the pointer field, if available.</param>
    /// <typeparam name="T">The requested COM interface.</typeparam>
    /// <returns>Whether another candidate field is available.</returns>
    protected virtual bool TryGetCandidateAddress<T>(T* obj, int candidateIndex, out nint candidateAddress)
        where T : unmanaged, IUnknown.Interface
    {
        // The default layout probes four pointer fields after the vtable pointer.
        candidateAddress = 0;
        if (candidateIndex is < 0 or >= 4)
            return false;

        candidateAddress = (nint)obj + ((candidateIndex + 1) * nint.Size);
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
