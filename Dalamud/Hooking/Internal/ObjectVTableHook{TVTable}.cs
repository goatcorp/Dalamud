using System;
using System.Runtime.InteropServices;

namespace Dalamud.Hooking.Internal;

/// <summary>
/// Typed version of <see cref="ObjectVTableHook"/>.
/// </summary>
/// <typeparam name="TVTableEnum">Type of VTable enum.</typeparam>
internal unsafe class ObjectVTableHook<TVTableEnum> : ObjectVTableHook
    where TVTableEnum : unmanaged, Enum
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectVTableHook{TVTable}"/> class.
    /// </summary>
    /// <param name="ppVtbl">Address to vtable. Usually the address of the object itself.</param>
    public ObjectVTableHook(TVTableEnum** ppVtbl)
        : base(ppVtbl, Enum.GetValues(typeof(TVTableEnum)).Length)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectVTableHook{TVTable}"/> class.
    /// </summary>
    /// <param name="ppVtbl">Address to vtable. Usually the address of the object itself.</param>
    public ObjectVTableHook(void* ppVtbl)
        : base(ppVtbl, Enum.GetValues(typeof(TVTableEnum)).Length)
    {
    }

    /// <summary>
    /// Gets the original method address of the given method index.
    /// </summary>
    /// <param name="methodIndex">The method index.</param>
    /// <returns>Address of the original method.</returns>
    public nint GetOriginalMethodAddress(TVTableEnum methodIndex) =>
        this.GetOriginalMethodAddress((int)(object)methodIndex);

    /// <summary>
    /// Gets the original method of the given method index, as a delegate of given type.
    /// </summary>
    /// <param name="methodIndex">The method index.</param>
    /// <typeparam name="T">Type of delegate.</typeparam>
    /// <returns>Delegate to the original method.</returns>
    public T GetOriginalMethodDelegate<T>(TVTableEnum methodIndex)
        where T : Delegate
        => this.GetOriginalMethodDelegate<T>((int)(object)methodIndex);

    /// <summary>
    /// Resets a method to the original function.
    /// </summary>
    /// <param name="methodIndex">The method index.</param>
    public void ResetVtableEntry(TVTableEnum methodIndex)
        => this.ResetVtableEntry((int)(object)methodIndex);

    /// <summary>
    /// Sets a method in vtable to the given address of function.
    /// </summary>
    /// <param name="methodIndex">The method index.</param>
    /// <param name="pfn">Address of the detour function.</param>
    /// <param name="refkeep">Additional reference to keep in memory.</param>
    public void SetVtableEntry(TVTableEnum methodIndex, nint pfn, object? refkeep)
        => this.SetVtableEntry((int)(object)methodIndex, pfn, refkeep);

    /// <summary>
    /// Sets a method in vtable to the given delegate.
    /// </summary>
    /// <param name="methodIndex">The method index.</param>
    /// <param name="detourDelegate">Detour delegate.</param>
    /// <typeparam name="T">Type of delegate.</typeparam>
    public void SetVtableEntry<T>(TVTableEnum methodIndex, T detourDelegate)
        where T : Delegate =>
        this.SetVtableEntry(methodIndex, Marshal.GetFunctionPointerForDelegate(detourDelegate), detourDelegate);

    /// <summary>
    /// Sets a method in vtable to the given delegate.
    /// </summary>
    /// <param name="methodIndex">The method index.</param>
    /// <param name="detourDelegate">Detour delegate.</param>
    /// <param name="originalMethodDelegate">Original method delegate.</param>
    /// <typeparam name="T">Type of delegate.</typeparam>
    public void SetVtableEntry<T>(TVTableEnum methodIndex, T detourDelegate, out T originalMethodDelegate)
        where T : Delegate
        => this.SetVtableEntry((int)(object)methodIndex, detourDelegate, out originalMethodDelegate);
}
