using System;
using System.Runtime.InteropServices;

namespace Dalamud.Hooking.Internal;

/// <summary>
/// Manages a hook that works by replacing the vtable of target object.
/// </summary>
internal unsafe class ObjectVTableHook : IDisposable
{
    private readonly nint** ppVtbl;
    private readonly int numMethods;

    private readonly nint* pVtblOriginal;
    private readonly nint* pVtblOverriden;

    private readonly object?[] detourDelegates;

    private bool released;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectVTableHook"/> class.
    /// </summary>
    /// <param name="ppVtbl">Address to vtable. Usually the address of the object itself.</param>
    /// <param name="numMethods">Number of methods in this vtable.</param>
    public ObjectVTableHook(nint ppVtbl, int numMethods)
    {
        this.ppVtbl = (nint**)ppVtbl;
        this.numMethods = numMethods;
        this.detourDelegates = new object?[numMethods];
        this.pVtblOriginal = *this.ppVtbl;
        this.pVtblOverriden = (nint*)Marshal.AllocHGlobal(sizeof(void*) * numMethods);
        this.VtblOriginal.CopyTo(this.VtblOverriden);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectVTableHook"/> class.
    /// </summary>
    /// <param name="ppVtbl">Address to vtable. Usually the address of the object itself.</param>
    /// <param name="numMethods">Number of methods in this vtable.</param>
    public ObjectVTableHook(void* ppVtbl, int numMethods)
        : this((nint)ppVtbl, numMethods)
    {
    }

    /// <summary>
    /// Finalizes an instance of the <see cref="ObjectVTableHook"/> class.
    /// </summary>
    ~ObjectVTableHook() => this.ReleaseUnmanagedResources();

    /// <summary>
    /// Gets the span view of original vtable.
    /// </summary>
    private Span<nint> VtblOriginal => new(this.pVtblOriginal, this.numMethods);

    /// <summary>
    /// Gets the span view of overriden vtable.
    /// </summary>
    private Span<nint> VtblOverriden => new(this.pVtblOverriden, this.numMethods);

    /// <summary>
    /// Disables the hook.
    /// </summary>
    public void Disable() => *this.ppVtbl = this.pVtblOriginal;

    /// <inheritdoc />
    public void Dispose()
    {
        this.ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Enables the hook.
    /// </summary>
    public void Enable() => *this.ppVtbl = this.pVtblOverriden;

    /// <summary>
    /// Gets the original method address of the given method index.
    /// </summary>
    /// <param name="methodIndex">The method index.</param>
    /// <returns>Address of the original method.</returns>
    public nint GetOriginalMethodAddress(int methodIndex)
    {
        this.EnsureMethodIndex(methodIndex);
        return this.pVtblOriginal[methodIndex];
    }

    /// <summary>
    /// Gets the original method of the given method index, as a delegate of given type.
    /// </summary>
    /// <param name="methodIndex">The method index.</param>
    /// <typeparam name="T">Type of delegate.</typeparam>
    /// <returns>Delegate to the original method.</returns>
    public T GetOriginalMethodDelegate<T>(int methodIndex)
        where T : Delegate
    {
        this.EnsureMethodIndex(methodIndex);
        return Marshal.GetDelegateForFunctionPointer<T>(this.pVtblOriginal[methodIndex]);
    }

    /// <summary>
    /// Resets a method to the original function.
    /// </summary>
    /// <param name="methodIndex">The method index.</param>
    public void ResetVtableEntry(int methodIndex)
    {
        this.EnsureMethodIndex(methodIndex);
        this.VtblOverriden[methodIndex] = this.pVtblOriginal[methodIndex];
        this.detourDelegates[methodIndex] = null;
    }

    /// <summary>
    /// Sets a method in vtable to the given address of function.
    /// </summary>
    /// <param name="methodIndex">The method index.</param>
    /// <param name="pfn">Address of the detour function.</param>
    /// <param name="refkeep">Additional reference to keep in memory.</param>
    public void SetVtableEntry(int methodIndex, nint pfn, object? refkeep)
    {
        this.EnsureMethodIndex(methodIndex);
        this.VtblOverriden[methodIndex] = pfn;
        this.detourDelegates[methodIndex] = refkeep;
    }

    /// <summary>
    /// Sets a method in vtable to the given delegate.
    /// </summary>
    /// <param name="methodIndex">The method index.</param>
    /// <param name="detourDelegate">Detour delegate.</param>
    /// <typeparam name="T">Type of delegate.</typeparam>
    public void SetVtableEntry<T>(int methodIndex, T detourDelegate)
        where T : Delegate =>
        this.SetVtableEntry(methodIndex, Marshal.GetFunctionPointerForDelegate(detourDelegate), detourDelegate);

    /// <summary>
    /// Sets a method in vtable to the given delegate.
    /// </summary>
    /// <param name="methodIndex">The method index.</param>
    /// <param name="detourDelegate">Detour delegate.</param>
    /// <param name="originalMethodDelegate">Original method delegate.</param>
    /// <typeparam name="T">Type of delegate.</typeparam>
    public void SetVtableEntry<T>(int methodIndex, T detourDelegate, out T originalMethodDelegate)
        where T : Delegate
    {
        originalMethodDelegate = this.GetOriginalMethodDelegate<T>(methodIndex);
        this.SetVtableEntry(methodIndex, Marshal.GetFunctionPointerForDelegate(detourDelegate), detourDelegate);
    }

    private void EnsureMethodIndex(int methodIndex)
    {
        if (methodIndex < 0 || methodIndex >= this.numMethods)
        {
            throw new ArgumentOutOfRangeException(nameof(methodIndex), methodIndex, null);
        }
    }

    private void ReleaseUnmanagedResources()
    {
        if (!this.released)
        {
            this.Disable();
            Marshal.FreeHGlobal((nint)this.pVtblOverriden);
            this.released = true;
        }
    }
}
