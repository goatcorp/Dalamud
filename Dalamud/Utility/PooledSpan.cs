using System.Buffers;
using System.Runtime.CompilerServices;

namespace Dalamud.Utility;

/// <summary>A scratch span backed by stack memory, a reusable caller buffer, or <see cref="ArrayPool{T}.Shared"/>.</summary>
/// <typeparam name="T">The element type.</typeparam>
/// <remarks>The span is not cleared. Write before you read.</remarks>
internal ref struct PooledSpan<T>
{
    /// <summary>Maximum length to which a reusable buffer passed to <see cref="FromBuffer"/> grows.</summary>
    /// <remarks>Larger requests are rented per call so a single large input does not retain an oversized buffer permanently.</remarks>
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

    /// <summary>Takes a span from a reusable caller buffer, growing it up to <see cref="MaxBufferLength"/>.</summary>
    /// <param name="buffer">The reusable buffer.</param>
    /// <param name="length">The required length.</param>
    /// <returns>A span from <paramref name="buffer"/>, or rented from the pool if <paramref name="length"/> exceeds
    /// <see cref="MaxBufferLength"/>.</returns>
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

    /// <summary>Returns the rented array to <see cref="ArrayPool{T}.Shared"/>, if any.</summary>
    /// <remarks>No <c>try</c>/<c>finally</c> is needed. If an exception unwinds before <see cref="Recycle"/>, the rented
    /// array is left for GC.</remarks>
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
