using System.Buffers;
using System.Runtime.CompilerServices;

namespace Dalamud.Utility;

/// <summary>A scratch span on the stack, in a kept buffer, or rented from <see cref="ArrayPool{T}.Shared"/>.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
/// <remarks>The span is not cleared. Write before you read.</remarks>
internal ref struct PooledSpan<T>
{
    /// <summary>The length a kept buffer grows to at most.</summary>
    /// <remarks>Longer spans are rented for the call, so that one long text does not keep memory for good.</remarks>
    public const int MaxBufferLength = 4096;

    private T[]? rented;

    /// <summary>Initializes a new instance of the <see cref="PooledSpan{T}"/> struct.</summary>
    /// <param name="stack">Stack memory to use if it is long enough, or default.</param>
    /// <param name="length">The length.</param>
    public PooledSpan(Span<T> stack, int length)
    {
        if (length <= stack.Length)
        {
            this.Span = stack[..length];
        }
        else
        {
            this.rented = ArrayPool<T>.Shared.Rent(length);
            this.Span = this.rented.AsSpan(0, length);
        }
    }

    private PooledSpan(Span<T> span) => this.Span = span;

    /// <summary>Gets the span.</summary>
    public Span<T> Span { get; }

    /// <summary>Rents a span from the pool.</summary>
    /// <param name="length">The length.</param>
    /// <returns>The span.</returns>
    public static PooledSpan<T> Rent(int length) => new(default, length);

    /// <summary>Takes a span from a buffer kept by the caller, growing it up to <see cref="MaxBufferLength"/>.
    /// </summary>
    /// <param name="buffer">The buffer.</param>
    /// <param name="length">The length.</param>
    /// <returns>The span, from the pool if the buffer would get longer than <see cref="MaxBufferLength"/>.</returns>
    /// <remarks>The buffer must not be null. A nested call or another thread must not use the same buffer while the
    /// span is in use.</remarks>
    public static PooledSpan<T> FromBuffer(ref T[] buffer, int length)
    {
        if (length <= buffer.Length)
            return new(buffer.AsSpan(0, length));
        if (length > MaxBufferLength)
            return Rent(length);
        buffer = GC.AllocateUninitializedArray<T>(GrownLength(buffer, length));
        return new(buffer.AsSpan(0, length));
    }

    /// <summary>Gives a rented array back to the pool. Does nothing otherwise.</summary>
    /// <remarks>If an exception is thrown before this is called, the array is not given back, and the pool makes
    /// another.</remarks>
    public void Recycle()
    {
        if (this.rented is null)
            return;
        ArrayPool<T>.Shared.Return(this.rented, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
        this.rented = null;
    }

    private static int GrownLength(T[] buffer, int length) =>
        Math.Max(length, Math.Min(buffer.Length * 2, MaxBufferLength));
}
