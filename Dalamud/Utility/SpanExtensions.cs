using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Dalamud.Utility;

/// <summary>
/// Provides helper methods for <see cref="Span{T}"/> class.
/// </summary>
internal static class SpanExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref readonly T Max<T>(this ReadOnlySpan<T> span)
        where T : IComparisonOperators<T, T, bool>
    {
        // JIT inlining quirks
        static void ThrowEx()
        {
            throw new ArgumentException("Span is empty", nameof(span));
        }

        if (span.IsEmpty)
        {
            ThrowEx();
        }

        ref readonly var max = ref span[0];

        for (var i = 0; i < span.Length; i++)
        {
            if (max < span[i])
            {
                max = ref span[i];
            }
        }

        return ref max;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T Max<T>(this Span<T> span)
        where T : IComparisonOperators<T, T, bool>
    {
        // JIT inlining quirks
        static void ThrowEx()
        {
            throw new ArgumentException("Span is empty", nameof(span));
        }

        if (span.IsEmpty)
        {
            ThrowEx();
        }

        ref var max = ref span[0];

        for (var i = 0; i < span.Length; i++)
        {
            if (max < span[i])
            {
                max = ref span[i];
            }
        }

        return ref max;
    }
}
