using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

using TerraFX.Interop;
using TerraFX.Interop.Windows;

namespace Dalamud.ImGuiScene.Helpers;

/// <summary>
/// Base class for implementing COM objects.
/// </summary>
internal abstract unsafe class ManagedComObjectBase : IDisposable
{
    private nint[] vtblAndHandle;
    private uint refCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedComObjectBase"/> class.
    /// </summary>
    /// <param name="numExtraFunctions">Number of extra functions in the vtable, extending IUnknown.</param>
    protected ManagedComObjectBase(int numExtraFunctions = 0)
    {
        this.vtblAndHandle = GC.AllocateArray<nint>(5 + numExtraFunctions, true);
        this.vtblAndHandle[0] = (nint)Unsafe.AsPointer(ref this.vtblAndHandle[2]);
        this.vtblAndHandle[1] = GCHandle.ToIntPtr(GCHandle.Alloc(this));
        this.vtblAndHandle[2] = (nint)(delegate* unmanaged<nint, Guid*, void**, HRESULT>)&StaticQueryInterface;
        this.vtblAndHandle[3] = (nint)(delegate* unmanaged<nint, uint>)&StaticAddRef;
        this.vtblAndHandle[4] = (nint)(delegate* unmanaged<nint, uint>)&StaticRelease;
        this.refCount = 1;
        return;

        [UnmanagedCallersOnly]
        static HRESULT StaticQueryInterface(nint punk, Guid* piid, void** ppvObject) =>
            AttachFromAddressOrNull(punk) is { } obj ? obj.QueryInterface(piid, ppvObject) : E.E_FAIL;

        [UnmanagedCallersOnly]
        static uint StaticAddRef(nint punk) => AttachFromAddressOrNull(punk) is { } obj ? obj.AddRef() : 0;

        [UnmanagedCallersOnly]
        static uint StaticRelease(nint punk) => AttachFromAddressOrNull(punk) is { } obj ? obj.Release() : 0;
    }

    /// <summary>
    /// Gets a value indicating whether this object has been fully released.
    /// </summary>
    public bool IsReleased => this.refCount == 0;
    
    /// <summary>
    /// Gets the current reference count.
    /// </summary>
    public uint RefCount => this.refCount;

    /// <summary>
    /// Gets the span of vtable.
    /// </summary>
    protected Span<nint> Vtbl => this.vtblAndHandle.AsSpan(2);

    /// <summary>
    /// Converts the specified address to the instance of the class.
    /// </summary>
    /// <param name="punk">The address.</param>
    /// <returns>The instance of the class.</returns>
    public static ManagedComObjectBase AttachFromAddress(nint punk) =>
        AttachFromAddressOrNull(punk) ?? throw new InvalidCastException();

    /// <summary>
    /// Converts the specified address to the instance of the class. Returns null if it's impossible.
    /// </summary>
    /// <param name="punk">The address.</param>
    /// <returns>The instance of the class.</returns>
    public static ManagedComObjectBase? AttachFromAddressOrNull(nint punk) =>
        punk == 0 ? null : GCHandle.FromIntPtr(((nint*)punk)![1]).Target as ManagedComObjectBase;

    /// <summary>
    /// Gets this class as a pointer of <see cref="IUnknown"/>.
    /// </summary>
    /// <returns>The pointer.</returns>
    public IUnknown* AsIUnknown() => (IUnknown*)Unsafe.AsPointer(ref this.vtblAndHandle[0]);

    /// <summary>
    /// Gets the COM pointer of this class as a <see cref="nint"/>.
    /// </summary>
    /// <returns>The pointer.</returns>
    public nint AsHandle() => (nint)Unsafe.AsPointer(ref this.vtblAndHandle[0]);

    /// <inheritdoc cref="IUnknown.QueryInterface"/>
    public HRESULT QueryInterface(Guid* piid, void** ppvObject)
    {
        if (ppvObject == null)
            return E.E_POINTER;

        *ppvObject = null;
        if (piid == null)
            return E.E_INVALIDARG;

        if (*piid == IID.IID_IUnknown)
        {
            this.AddRef();
            *ppvObject = Unsafe.AsPointer(ref this.vtblAndHandle[0]);
            return S.S_OK;
        }

        return E.E_NOINTERFACE;
    }

    /// <inheritdoc cref="IUnknown.AddRef"/>
    public uint AddRef() => Interlocked.Increment(ref this.refCount);

    /// <inheritdoc cref="IUnknown.Release"/>
    public uint Release()
    {
        var rc = Interlocked.Decrement(ref this.refCount);
        if (rc == 0)
        {
            this.FinalRelease();
            GCHandle.FromIntPtr(this.vtblAndHandle[1]).Free();
            this.vtblAndHandle.AsSpan().Fill(unchecked((nint)0xCCCCCCCCCCCCCCCC));
            this.vtblAndHandle = null!;
        }

        return rc;
    }

    /// <summary>
    /// Calls <see cref="Release"/>.
    /// </summary>
    public void Dispose() => this.Release();

    /// <summary>
    /// Attempt to cast this object as the given type, indicated with <paramref name="iid"/>.
    /// </summary>
    /// <param name="iid">The IID.</param>
    /// <returns>The casted object, or null if cast was not applicable.</returns>
    protected virtual void* DynamicCast(in Guid iid) => null;

    /// <summary>
    /// Called on the last release of this object.
    /// </summary>
    protected virtual void FinalRelease()
    {
    }
}

/// <summary>
/// Base class for implementing COM objects.
/// </summary>
/// <typeparam name="T">The implementor.</typeparam>
internal abstract unsafe class ManagedComObjectBase<T> : ManagedComObjectBase, ICloneable
    where T : ManagedComObjectBase<T>, INativeGuid
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedComObjectBase{T}"/> class.
    /// </summary>
    /// <param name="numExtraFunctions">Number of extra functions in the vtable, extending IUnknown.</param>
    protected ManagedComObjectBase(int numExtraFunctions = 0)
        : base(numExtraFunctions)
    {
    }

    /// <summary>
    /// Converts the specified address to the instance of the class.
    /// </summary>
    /// <param name="punk">The address.</param>
    /// <returns>The instance of the class.</returns>
    public static new T AttachFromAddress(nint punk) => AttachFromAddressOrNull(punk) ?? throw new InvalidCastException();

    /// <summary>
    /// Converts the specified address to the instance of the class. Returns null if it's impossible.
    /// </summary>
    /// <param name="punk">The address.</param>
    /// <returns>The instance of the class.</returns>
    public static new T? AttachFromAddressOrNull(nint punk) =>
        punk == 0 ? null : GCHandle.FromIntPtr(((nint*)punk)![1]).Target as T;

    /// <summary>
    /// Gets this class as a pointer of <see cref="ManagedComObject{T}"/>.
    /// </summary>
    /// <returns>The pointer.</returns>
    public ManagedComObject<T>* AsComInterface() => (ManagedComObject<T>*)this.AsHandle();

    /// <summary>
    /// Creates a new reference of this.
    /// </summary>
    /// <returns>This.</returns>
    public T CloneRef()
    {
        this.AddRef();
        return (T)this;
    }

    /// <summary>
    /// Creates a new reference of this.
    /// </summary>
    /// <returns>This.</returns>
    object ICloneable.Clone() => this.CloneRef();
}
