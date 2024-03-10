using System.Collections;
using System.Collections.Generic;

using TerraFX.Interop.Windows;

namespace Dalamud.Utility.TerraFxCom;

/// <summary>Managed iterator for <see cref="IEnumUnknown"/>.</summary>
/// <typeparam name="T">The unknown type.</typeparam>
internal sealed class ManagedIEnumUnknownEnumerator<T> : IEnumerator<ComPtr<T>>
    where T : unmanaged, IUnknown.Interface
{
    private ComPtr<IEnumUnknown> unknownEnumerator;
    private ComPtr<T> current;

    /// <summary>Initializes a new instance of the <see cref="ManagedIEnumUnknownEnumerator{T}"/> class.</summary>
    /// <param name="unknownEnumerator">An instance of <see cref="IEnumUnknown"/>. Ownership is transferred.</param>
    public ManagedIEnumUnknownEnumerator(ComPtr<IEnumUnknown> unknownEnumerator) =>
        this.unknownEnumerator = unknownEnumerator;

    /// <summary>Finalizes an instance of the <see cref="ManagedIEnumUnknownEnumerator{T}"/> class.</summary>
    ~ManagedIEnumUnknownEnumerator() => this.ReleaseUnmanagedResources();

    /// <inheritdoc/>
    public ComPtr<T> Current => this.current;

    /// <inheritdoc/>
    object IEnumerator.Current => this.current;

    /// <inheritdoc/>
    public void Dispose()
    {
        this.ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    public unsafe bool MoveNext()
    {
        using var punk = default(ComPtr<IUnknown>);
        var fetched = 0u;
        while (this.unknownEnumerator.Get()->Next(1u, punk.ReleaseAndGetAddressOf(), &fetched) == S.S_OK && fetched == 1)
        {
            if (punk.As(ref this.current).SUCCEEDED)
                return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public unsafe void Reset() => this.unknownEnumerator.Get()->Reset().ThrowOnError();

    private void ReleaseUnmanagedResources()
    {
        this.unknownEnumerator.Reset();
        this.current.Reset();
    }
}
