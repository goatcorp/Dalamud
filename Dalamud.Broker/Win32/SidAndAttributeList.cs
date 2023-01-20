using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Windows.Win32;
using Windows.Win32.Security;

namespace Dalamud.Broker.Win32;

internal sealed class SidAndAttributeList : IDisposable
{
    private readonly SID_AND_ATTRIBUTES[] mInner;

    public SidAndAttributeList(IReadOnlyList<(SecurityIdentifier, int)> list)
    {
        // Allocate capabilities in a way that Windows can understand. We also need to pin it on the memory.
        this.mInner = GC.AllocateArray<SID_AND_ATTRIBUTES>(list.Count, true);

        try
        {
            // Initialize sid&attribute list.
            // 
            // Since juggling with these low level APIs and kernel objects (also its lifetime) is already painful enough
            // we're going to take a shortcut here:
            // 1. We take the SecurityIdentifier object and convert it into SDDL string.
            // 2. Use that to construct PSID.
            for (var i = 0; i < list.Count; i++)
            {
                // First we convert sid into a SDDL format string.
                var (sid, attr) = list[i];
                var sidSddl = sid.ToString();

                // and we use *that* string to initialize an another sid object.
                var ok = PInvoke.ConvertStringSidToSid(sidSddl, out var psid);
                if (!ok)
                {
                    throw new Win32Exception();
                }

                // Set psid and associated attributes.
                this.mInner[i].Sid = psid;
                this.mInner[i].Attributes = (uint)attr;
            }
        }
        catch
        {
            // If there's an error while converting into a string, we need to destroy PSIDs already initialized.
            this.DisposeUnmanaged();
        }
    }

    ~SidAndAttributeList()
    {
        this.DisposeUnmanaged();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        this.DisposeUnmanaged();
    }

    private unsafe void DisposeUnmanaged()
    {
        // Destroy all initialized PSID.
        foreach (var entry in this.mInner)
        {
            var psid = entry.Sid.Value;
            if (psid != null)
            {
                PInvoke.LocalFree((nint)psid);
            }
        }
    }

    /// <summary>
    /// Returns a pointer to SID_AND_ATTRIBUTES that can be directly passed to Windows. 
    /// 
    /// The pointer is valid until either SidAndAttributeList goes out of scope or Dispose() has been called.
    /// </summary>
    public unsafe SID_AND_ATTRIBUTES* AsPointer()
    {
        // SAFETY:
        // This is **safe and only safe** because we already pinned the underlying array via
        // `GC.AllocateArray(count, pinned: true)`.
        return (SID_AND_ATTRIBUTES*)Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(this.mInner));
    }
}
