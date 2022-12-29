using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Windows.Win32;
using Windows.Win32.Security;

namespace Dalamud.Injector.Win32;

internal sealed class CapabilityList : IDisposable
{
    private readonly WellKnownSid?[] mSidList;
    private readonly SID_AND_ATTRIBUTES[] mCapabilityList;
    private int mCount;
    private readonly int mCapacity;

    public int Count => this.mCount;

    // SAFETY: This is safe and only safe because we **already pinned** the underlying array from .ctor
    public unsafe SID_AND_ATTRIBUTES* AsPointer() =>
        (SID_AND_ATTRIBUTES*)Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(this.mCapabilityList));

    public CapabilityList(int capacity)
    {
        this.mSidList = new WellKnownSid?[capacity];
        this.mCapabilityList = GC.AllocateArray<SID_AND_ATTRIBUTES>(capacity, true);
        this.mCount = 0;
        this.mCapacity = capacity;
    }

    public void Dispose()
    {
        this.mCount = 0;
        foreach (var sid in this.mSidList)
        {
            sid?.Dispose();
        }
    }

    public void Add(WELL_KNOWN_SID_TYPE capabilitySid)
    {
        // 1
        if (this.mCount >= this.mCapacity)
        {
            throw new IndexOutOfRangeException();
        }

        var capability = new WellKnownSid(capabilitySid);

        this.mSidList[this.mCount] = capability;
        this.mCapabilityList[this.mCount].Sid = capability.AsPointer();
        this.mCapabilityList[this.mCount].Attributes = PInvoke.SE_GROUP_ENABLED;
        this.mCount += 1;
    }
}
