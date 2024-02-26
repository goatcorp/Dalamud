using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using TerraFX.Interop;
using TerraFX.Interop.Windows;

namespace Dalamud.ImGuiScene.Helpers;

/// <summary>
/// Wraps a managed class for use with <see cref="ComPtr{T}"/>.
/// </summary>
/// <typeparam name="T">The contained type.</typeparam>
internal unsafe struct ManagedComObject<T> : IUnknown.Interface
    where T : ManagedComObjectBase<T>, INativeGuid
{
    private nint vtbl;

    /// <inheritdoc cref="INativeGuid.NativeGuid"/>
    public static Guid* NativeGuid => T.NativeGuid;

    /// <summary>
    /// Gets the object.
    /// </summary>
    public T O => (T)(GCHandle.FromIntPtr(((nint*)Unsafe.AsPointer(ref this.vtbl))[1]).Target
                      ?? throw new ObjectDisposedException(nameof(T)));

    /// <inheritdoc/>
    public HRESULT QueryInterface(Guid* riid, void** ppvObject) => this.O.QueryInterface(riid, ppvObject);

    /// <inheritdoc/>
    public uint AddRef() => this.O.AddRef();

    /// <inheritdoc/>
    public uint Release() => this.O.Release();
}
