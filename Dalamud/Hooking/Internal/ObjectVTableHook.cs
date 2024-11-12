using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Dalamud.Utility;

using Serilog;

namespace Dalamud.Hooking.Internal;

/// <summary>Manages a hook that works by replacing the vtable of target object.</summary>
internal unsafe class ObjectVTableHook : IDisposable
{
    private readonly nint** ppVtbl;
    private readonly int numMethods;

    private readonly nint* pVtblOriginal;
    private readonly nint[] vtblOverriden;

    /// <summary>Extra data for overriden vtable entries, primarily for keeping references to delegates that are used
    /// with <see cref="Marshal.GetFunctionPointerForDelegate"/>.</summary>
    private readonly object?[] vtblOverridenTag;

    private bool released;

    /// <summary>Initializes a new instance of the <see cref="ObjectVTableHook"/> class.</summary>
    /// <param name="ppVtbl">Address to vtable. Usually the address of the object itself.</param>
    /// <param name="numMethods">Number of methods in this vtable.</param>
    public ObjectVTableHook(nint ppVtbl, int numMethods)
    {
        this.ppVtbl = (nint**)ppVtbl;
        this.numMethods = numMethods;
        this.vtblOverridenTag = new object?[numMethods];
        this.pVtblOriginal = *this.ppVtbl;
        this.vtblOverriden = GC.AllocateArray<nint>(numMethods, true);
        this.OriginalVTableSpan.CopyTo(this.vtblOverriden);
    }

    /// <summary>Initializes a new instance of the <see cref="ObjectVTableHook"/> class.</summary>
    /// <param name="ppVtbl">Address to vtable. Usually the address of the object itself.</param>
    /// <param name="numMethods">Number of methods in this vtable.</param>
    public ObjectVTableHook(void* ppVtbl, int numMethods)
        : this((nint)ppVtbl, numMethods)
    {
    }

    /// <summary>Finalizes an instance of the <see cref="ObjectVTableHook"/> class.</summary>
    ~ObjectVTableHook() => this.ReleaseUnmanagedResources();

    /// <summary>Gets the span view of original vtable.</summary>
    public ReadOnlySpan<nint> OriginalVTableSpan => new(this.pVtblOriginal, this.numMethods);

    /// <summary>Gets the span view of overriden vtable.</summary>
    public ReadOnlySpan<nint> OverridenVTableSpan => this.vtblOverriden.AsSpan();

    /// <summary>Gets the address of the pointer to the vtable.</summary>
    public nint Address => (nint)this.ppVtbl;

    /// <summary>Gets the address of the original vtable.</summary>
    public nint OriginalVTableAddress => (nint)this.pVtblOriginal;

    /// <summary>Gets the address of the overriden vtable.</summary>
    public nint OverridenVTableAddress => (nint)Unsafe.AsPointer(ref this.vtblOverriden[0]);

    /// <summary>Disables the hook.</summary>
    public void Disable()
    {
        // already disabled
        if (*this.ppVtbl == this.pVtblOriginal)
            return;

        if (*this.ppVtbl != Unsafe.AsPointer(ref this.vtblOverriden[0]))
        {
            Log.Warning(
                "[{who}]: the object was hooked by something else; disabling may result in a crash.",
                this.GetType().Name);
        }

        *this.ppVtbl = this.pVtblOriginal;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        this.ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    /// <summary>Enables the hook.</summary>
    public void Enable()
    {
        // already enabled
        if (*this.ppVtbl == Unsafe.AsPointer(ref this.vtblOverriden[0]))
            return;

        if (*this.ppVtbl != this.pVtblOriginal)
        {
            Log.Warning(
                "[{who}]: the object was hooked by something else; enabling may result in a crash.",
                this.GetType().Name);
        }

        *this.ppVtbl = (nint*)Unsafe.AsPointer(ref this.vtblOverriden[0]);
    }

    /// <summary>Gets the original method address of the given method index.</summary>
    /// <param name="methodIndex">Index of the method.</param>
    /// <returns>Address of the original method.</returns>
    public nint GetOriginalMethodAddress(int methodIndex)
    {
        this.EnsureMethodIndex(methodIndex);
        return this.pVtblOriginal[methodIndex];
    }

    /// <summary>Gets the original method of the given method index, as a delegate of given type.</summary>
    /// <param name="methodIndex">Index of the method.</param>
    /// <typeparam name="T">Type of delegate.</typeparam>
    /// <returns>Delegate to the original method.</returns>
    public T GetOriginalMethodDelegate<T>(int methodIndex)
        where T : Delegate
    {
        this.EnsureMethodIndex(methodIndex);
        return Marshal.GetDelegateForFunctionPointer<T>(this.pVtblOriginal[methodIndex]);
    }

    /// <summary>Resets a method to the original function.</summary>
    /// <param name="methodIndex">Index of the method.</param>
    public void ResetVtableEntry(int methodIndex)
    {
        this.EnsureMethodIndex(methodIndex);
        this.vtblOverriden[methodIndex] = this.pVtblOriginal[methodIndex];
        this.vtblOverridenTag[methodIndex] = null;
    }

    /// <summary>Sets a method in vtable to the given address of function.</summary>
    /// <param name="methodIndex">Index of the method.</param>
    /// <param name="pfn">Address of the detour function.</param>
    /// <param name="refkeep">Additional reference to keep in memory.</param>
    public void SetVtableEntry(int methodIndex, nint pfn, object? refkeep)
    {
        this.EnsureMethodIndex(methodIndex);
        this.vtblOverriden[methodIndex] = pfn;
        this.vtblOverridenTag[methodIndex] = refkeep;
    }

    /// <summary>Sets a method in vtable to the given delegate.</summary>
    /// <param name="methodIndex">Index of the method.</param>
    /// <param name="detourDelegate">Detour delegate.</param>
    /// <typeparam name="T">Type of delegate.</typeparam>
    public void SetVtableEntry<T>(int methodIndex, T detourDelegate)
        where T : Delegate =>
        this.SetVtableEntry(methodIndex, Marshal.GetFunctionPointerForDelegate(detourDelegate), detourDelegate);

    /// <summary>Sets a method in vtable to the given delegate.</summary>
    /// <param name="methodIndex">Index of the method.</param>
    /// <param name="detourDelegate">Detour delegate.</param>
    /// <param name="originalMethodDelegate">Original method delegate.</param>
    /// <typeparam name="T">Type of delegate.</typeparam>
    public void SetVtableEntry<T>(int methodIndex, T detourDelegate, out T originalMethodDelegate)
        where T : Delegate
    {
        originalMethodDelegate = this.GetOriginalMethodDelegate<T>(methodIndex);
        this.SetVtableEntry(methodIndex, Marshal.GetFunctionPointerForDelegate(detourDelegate), detourDelegate);
    }

    /// <summary>Creates a new instance of <see cref="Hook{T}"/> that manages one entry in the vtable hook.</summary>
    /// <param name="methodIndex">Index of the method.</param>
    /// <param name="detourDelegate">Detour delegate.</param>
    /// <typeparam name="T">Type of delegate.</typeparam>
    /// <returns>A new instance of <see cref="Hook{T}"/>.</returns>
    /// <remarks>Even if a single hook is enabled, without <see cref="Enable"/>, the hook will remain disabled.
    /// </remarks>
    public Hook<T> CreateHook<T>(int methodIndex, T detourDelegate) where T : Delegate =>
        new SingleHook<T>(this, methodIndex, detourDelegate);

    private void EnsureMethodIndex(int methodIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(methodIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(methodIndex, this.numMethods);
    }

    private void ReleaseUnmanagedResources()
    {
        if (!this.released)
        {
            this.Disable();
            this.released = true;
        }
    }

    private sealed class SingleHook<T>(ObjectVTableHook hook, int methodIndex, T detourDelegate)
        : Hook<T>((nint)hook.ppVtbl)
        where T : Delegate
    {
        /// <inheritdoc/>
        public override T Original { get; } = hook.GetOriginalMethodDelegate<T>(methodIndex);

        /// <inheritdoc/>
        public override bool IsEnabled =>
            hook.OriginalVTableSpan[methodIndex] != hook.OverridenVTableSpan[methodIndex];

        /// <inheritdoc/>
        public override string BackendName => nameof(ObjectVTableHook);

        /// <inheritdoc/>
        public override void Enable() => hook.SetVtableEntry(methodIndex, detourDelegate);

        /// <inheritdoc/>
        public override void Disable() => hook.ResetVtableEntry(methodIndex);
    }
}

/// <summary>Typed version of <see cref="ObjectVTableHook"/>.</summary>
/// <typeparam name="TVTable">VTable struct.</typeparam>
internal unsafe class ObjectVTableHook<TVTable> : ObjectVTableHook
    where TVTable : unmanaged
{
    private static readonly string[] Fields =
        typeof(TVTable).GetFields(BindingFlags.Instance | BindingFlags.Public).Select(x => x.Name).ToArray();

    /// <summary>Initializes a new instance of the <see cref="ObjectVTableHook{TVTable}"/> class.</summary>
    /// <param name="ppVtbl">Address to vtable. Usually the address of the object itself.</param>
    public ObjectVTableHook(void* ppVtbl)
        : base(ppVtbl, Fields.Length)
    {
    }

    /// <summary>Gets the original vtable.</summary>
    public ref readonly TVTable OriginalVTable => ref MemoryMarshal.Cast<nint, TVTable>(this.OriginalVTableSpan)[0];

    /// <summary>Gets the overriden vtable.</summary>
    public ref readonly TVTable OverridenVTable => ref MemoryMarshal.Cast<nint, TVTable>(this.OverridenVTableSpan)[0];

    /// <summary>Gets the index of the method by method name.</summary>
    /// <param name="methodName">Name of the method.</param>
    /// <returns>Index of the method.</returns>
    public int GetMethodIndex(string methodName) => Fields.IndexOf(methodName);

    /// <summary>Gets the original method address of the given method index.</summary>
    /// <param name="methodName">Name of the method.</param>
    /// <returns>Address of the original method.</returns>
    public nint GetOriginalMethodAddress(string methodName) =>
        this.GetOriginalMethodAddress(this.GetMethodIndex(methodName));

    /// <summary>Gets the original method of the given method index, as a delegate of given type.</summary>
    /// <param name="methodName">Name of the method.</param>
    /// <typeparam name="T">Type of delegate.</typeparam>
    /// <returns>Delegate to the original method.</returns>
    public T GetOriginalMethodDelegate<T>(string methodName)
        where T : Delegate
        => this.GetOriginalMethodDelegate<T>(this.GetMethodIndex(methodName));

    /// <summary>Resets a method to the original function.</summary>
    /// <param name="methodName">Name of the method.</param>
    public void ResetVtableEntry(string methodName)
        => this.ResetVtableEntry(this.GetMethodIndex(methodName));

    /// <summary>Sets a method in vtable to the given address of function.</summary>
    /// <param name="methodName">Name of the method.</param>
    /// <param name="pfn">Address of the detour function.</param>
    /// <param name="refkeep">Additional reference to keep in memory.</param>
    public void SetVtableEntry(string methodName, nint pfn, object? refkeep)
        => this.SetVtableEntry(this.GetMethodIndex(methodName), pfn, refkeep);

    /// <summary>Sets a method in vtable to the given delegate.</summary>
    /// <param name="methodName">Name of the method.</param>
    /// <param name="detourDelegate">Detour delegate.</param>
    /// <typeparam name="T">Type of delegate.</typeparam>
    public void SetVtableEntry<T>(string methodName, T detourDelegate)
        where T : Delegate =>
        this.SetVtableEntry(
            this.GetMethodIndex(methodName),
            Marshal.GetFunctionPointerForDelegate(detourDelegate),
            detourDelegate);

    /// <summary>Sets a method in vtable to the given delegate.</summary>
    /// <param name="methodName">Name of the method.</param>
    /// <param name="detourDelegate">Detour delegate.</param>
    /// <param name="originalMethodDelegate">Original method delegate.</param>
    /// <typeparam name="T">Type of delegate.</typeparam>
    public void SetVtableEntry<T>(string methodName, T detourDelegate, out T originalMethodDelegate)
        where T : Delegate
        => this.SetVtableEntry(this.GetMethodIndex(methodName), detourDelegate, out originalMethodDelegate);

    /// <summary>Creates a new instance of <see cref="Hook{T}"/> that manages one entry in the vtable hook.</summary>
    /// <param name="methodName">Name of the method.</param>
    /// <param name="detourDelegate">Detour delegate.</param>
    /// <typeparam name="T">Type of delegate.</typeparam>
    /// <returns>A new instance of <see cref="Hook{T}"/>.</returns>
    /// <remarks>Even if a single hook is enabled, without <see cref="ObjectVTableHook.Enable"/>, the hook will remain
    /// disabled.</remarks>
    public Hook<T> CreateHook<T>(string methodName, T detourDelegate) where T : Delegate =>
        this.CreateHook(this.GetMethodIndex(methodName), detourDelegate);
}
