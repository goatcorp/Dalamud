using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.System.Threading;

namespace Dalamud.Injector.Win32;

internal sealed class WellKnownSid : IDisposable
{
    private readonly IntPtr mSid;

    public unsafe PSID AsPointer() => (PSID)this.mSid.ToPointer();

    public WellKnownSid(WELL_KNOWN_SID_TYPE sidType)
    {
        uint sidAllocSize = 0;

        // First call is to ask its size
        PInvoke.CreateWellKnownSid(
            sidType,
            (PSID)null,
            (PSID)null,
            ref sidAllocSize);

        this.mSid = Marshal.AllocCoTaskMem((int)sidAllocSize);

        var ok = PInvoke.CreateWellKnownSid(
            sidType,
            (PSID)null,
            this.AsPointer(),
            ref sidAllocSize);
        if (!ok)
        {
            Marshal.FreeCoTaskMem(this.mSid);
            throw new Win32Exception();
        }
    }

    ~WellKnownSid()
    {
        this.DisposeUnmanaged();
    }

    public void Dispose()
    {
        this.DisposeUnmanaged();
        GC.SuppressFinalize(this);
    }

    private void DisposeUnmanaged()
    {
        Marshal.FreeCoTaskMem(this.mSid);
    }
}
