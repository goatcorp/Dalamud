using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Windows.Win32;
using Windows.Win32.System.Threading;

namespace Dalamud.Injector.Win32;

internal sealed class ProcThreadAttributeList : IDisposable
{
    private IntPtr mAttributeListData;

    public unsafe LPPROC_THREAD_ATTRIBUTE_LIST AsPointer() => (LPPROC_THREAD_ATTRIBUTE_LIST)(void*)this.mAttributeListData;

    public ProcThreadAttributeList(int count)
    {
        nuint attributeAllocSize = default;

        // First call is to ask for its size
        PInvoke.InitializeProcThreadAttributeList(
            (LPPROC_THREAD_ATTRIBUTE_LIST)null,
            (uint)count,
            0,
            ref attributeAllocSize);

        // Note that array is pinned as it needs to be transmuted into LPPROC_THREAD_ATTRIBUTE_LIST as needed
        this.mAttributeListData = Marshal.AllocCoTaskMem((int)attributeAllocSize);
        GC.AllocateArray<byte>((int)attributeAllocSize, true);

        // Initialize it for real this time
        var ok = PInvoke.InitializeProcThreadAttributeList(
            this.AsPointer(),
            (uint)count,
            0,
            ref attributeAllocSize);
        if (!ok)
        {
            throw new Win32Exception();
        }
    }

    ~ProcThreadAttributeList()
    {
        this.DisposeUnmanaged();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        this.DisposeUnmanaged();
    }
    
    private void DisposeUnmanaged()
    {
        PInvoke.DeleteProcThreadAttributeList(this.AsPointer());
        Marshal.FreeCoTaskMem(this.mAttributeListData);
    }

    public unsafe void Add(nuint attribute, void* value, int cbSize)
    {
        var ok = PInvoke.UpdateProcThreadAttribute(
            this.AsPointer(),
            0, // reserved and must be 0 at all times
            attribute,
            value,
            (nuint)cbSize);
        if (!ok)
        {
            throw new Win32Exception();
        }
    }
}
