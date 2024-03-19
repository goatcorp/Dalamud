using System.Runtime.InteropServices;

using Dalamud.Utility.Text;

namespace Dalamud.Utility;

/// <summary>Extension methods for <see cref="Span{T}"/> and <see cref="ReadOnlySpan{T}"/>.</summary>
public static class SpanExtensions
{
    /// <summary>Creates an enumerable that can be used to iterate unicode codepoints in a span.</summary>
    /// <param name="data">The sequence to reinterpret.</param>
    /// <param name="flags">Whether to treat codepoints that are invalid in Unicode as errors.</param>
    /// <returns>The enumerable/enumerator-like ref struct.</returns>
    public static UtfEnumerator EnumerateUtf(this ReadOnlySpan<byte> data, UtfEnumeratorFlags flags) =>
        new(data, flags);

    /// <inheritdoc cref="EnumerateUtf(ReadOnlySpan{byte}, UtfEnumeratorFlags)"/>
    public static UtfEnumerator EnumerateUtf(this Span<byte> data, UtfEnumeratorFlags flags) =>
        new(data, flags);

    /// <inheritdoc cref="EnumerateUtf(ReadOnlySpan{byte}, UtfEnumeratorFlags)"/>
    public static UtfEnumerator EnumerateUtf(this ReadOnlyMemory<byte> data, UtfEnumeratorFlags flags) =>
        new(data.Span, flags);

    /// <inheritdoc cref="EnumerateUtf(ReadOnlySpan{byte}, UtfEnumeratorFlags)"/>
    public static UtfEnumerator EnumerateUtf(this Memory<byte> data, UtfEnumeratorFlags flags) =>
        new(data.Span, flags);
    
    /// <inheritdoc cref="EnumerateUtf(ReadOnlySpan{byte}, UtfEnumeratorFlags)"/>
    public static UtfEnumerator EnumerateUtf(this ReadOnlySpan<sbyte> data, UtfEnumeratorFlags flags) =>
        new(MemoryMarshal.Cast<sbyte, byte>(data), flags);

    /// <inheritdoc cref="EnumerateUtf(ReadOnlySpan{byte}, UtfEnumeratorFlags)"/>
    public static UtfEnumerator EnumerateUtf(this Span<sbyte> data, UtfEnumeratorFlags flags) =>
        new(MemoryMarshal.Cast<sbyte, byte>(data), flags);

    /// <inheritdoc cref="EnumerateUtf(ReadOnlySpan{byte}, UtfEnumeratorFlags)"/>
    public static UtfEnumerator EnumerateUtf(this ReadOnlyMemory<sbyte> data, UtfEnumeratorFlags flags) =>
        new(MemoryMarshal.Cast<sbyte, byte>(data.Span), flags);

    /// <inheritdoc cref="EnumerateUtf(ReadOnlySpan{byte}, UtfEnumeratorFlags)"/>
    public static UtfEnumerator EnumerateUtf(this Memory<sbyte> data, UtfEnumeratorFlags flags) =>
        new(MemoryMarshal.Cast<sbyte, byte>(data.Span), flags);
    
    /// <inheritdoc cref="EnumerateUtf(ReadOnlySpan{byte}, UtfEnumeratorFlags)"/>
    public static UtfEnumerator EnumerateUtf(this ReadOnlySpan<ushort> data, UtfEnumeratorFlags flags) =>
        new(MemoryMarshal.Cast<ushort, byte>(data), flags);

    /// <inheritdoc cref="EnumerateUtf(ReadOnlySpan{byte}, UtfEnumeratorFlags)"/>
    public static UtfEnumerator EnumerateUtf(this Span<ushort> data, UtfEnumeratorFlags flags) =>
        new(MemoryMarshal.Cast<ushort, byte>(data), flags);

    /// <inheritdoc cref="EnumerateUtf(ReadOnlySpan{byte}, UtfEnumeratorFlags)"/>
    public static UtfEnumerator EnumerateUtf(this ReadOnlyMemory<ushort> data, UtfEnumeratorFlags flags) =>
        new(MemoryMarshal.Cast<ushort, byte>(data.Span), flags);

    /// <inheritdoc cref="EnumerateUtf(ReadOnlySpan{byte}, UtfEnumeratorFlags)"/>
    public static UtfEnumerator EnumerateUtf(this Memory<ushort> data, UtfEnumeratorFlags flags) =>
        new(MemoryMarshal.Cast<ushort, byte>(data.Span), flags);
    
    /// <inheritdoc cref="EnumerateUtf(ReadOnlySpan{byte}, UtfEnumeratorFlags)"/>
    public static UtfEnumerator EnumerateUtf(this ReadOnlySpan<short> data, UtfEnumeratorFlags flags) =>
        new(MemoryMarshal.Cast<short, byte>(data), flags);

    /// <inheritdoc cref="EnumerateUtf(ReadOnlySpan{byte}, UtfEnumeratorFlags)"/>
    public static UtfEnumerator EnumerateUtf(this Span<short> data, UtfEnumeratorFlags flags) =>
        new(MemoryMarshal.Cast<short, byte>(data), flags);

    /// <inheritdoc cref="EnumerateUtf(ReadOnlySpan{byte}, UtfEnumeratorFlags)"/>
    public static UtfEnumerator EnumerateUtf(this ReadOnlyMemory<short> data, UtfEnumeratorFlags flags) =>
        new(MemoryMarshal.Cast<short, byte>(data.Span), flags);

    /// <inheritdoc cref="EnumerateUtf(ReadOnlySpan{byte}, UtfEnumeratorFlags)"/>
    public static UtfEnumerator EnumerateUtf(this Memory<short> data, UtfEnumeratorFlags flags) =>
        new(MemoryMarshal.Cast<short, byte>(data.Span), flags);
    
    /// <inheritdoc cref="EnumerateUtf(ReadOnlySpan{byte}, UtfEnumeratorFlags)"/>
    public static UtfEnumerator EnumerateUtf(this ReadOnlySpan<uint> data, UtfEnumeratorFlags flags) =>
        new(MemoryMarshal.Cast<uint, byte>(data), flags);

    /// <inheritdoc cref="EnumerateUtf(ReadOnlySpan{byte}, UtfEnumeratorFlags)"/>
    public static UtfEnumerator EnumerateUtf(this Span<uint> data, UtfEnumeratorFlags flags) =>
        new(MemoryMarshal.Cast<uint, byte>(data), flags);

    /// <inheritdoc cref="EnumerateUtf(ReadOnlySpan{byte}, UtfEnumeratorFlags)"/>
    public static UtfEnumerator EnumerateUtf(this ReadOnlyMemory<uint> data, UtfEnumeratorFlags flags) =>
        new(MemoryMarshal.Cast<uint, byte>(data.Span), flags);

    /// <inheritdoc cref="EnumerateUtf(ReadOnlySpan{byte}, UtfEnumeratorFlags)"/>
    public static UtfEnumerator EnumerateUtf(this Memory<uint> data, UtfEnumeratorFlags flags) =>
        new(MemoryMarshal.Cast<uint, byte>(data.Span), flags);
    
    /// <inheritdoc cref="EnumerateUtf(ReadOnlySpan{byte}, UtfEnumeratorFlags)"/>
    public static UtfEnumerator EnumerateUtf(this ReadOnlySpan<int> data, UtfEnumeratorFlags flags) =>
        new(MemoryMarshal.Cast<int, byte>(data), flags);

    /// <inheritdoc cref="EnumerateUtf(ReadOnlySpan{byte}, UtfEnumeratorFlags)"/>
    public static UtfEnumerator EnumerateUtf(this Span<int> data, UtfEnumeratorFlags flags) =>
        new(MemoryMarshal.Cast<int, byte>(data), flags);

    /// <inheritdoc cref="EnumerateUtf(ReadOnlySpan{byte}, UtfEnumeratorFlags)"/>
    public static UtfEnumerator EnumerateUtf(this ReadOnlyMemory<int> data, UtfEnumeratorFlags flags) =>
        new(MemoryMarshal.Cast<int, byte>(data.Span), flags);

    /// <inheritdoc cref="EnumerateUtf(ReadOnlySpan{byte}, UtfEnumeratorFlags)"/>
    public static UtfEnumerator EnumerateUtf(this Memory<int> data, UtfEnumeratorFlags flags) =>
        new(MemoryMarshal.Cast<int, byte>(data.Span), flags);
    
    /// <inheritdoc cref="EnumerateUtf(ReadOnlySpan{byte}, UtfEnumeratorFlags)"/>
    public static UtfEnumerator EnumerateUtf(this ReadOnlySpan<char> data, UtfEnumeratorFlags flags) =>
        new(MemoryMarshal.Cast<char, byte>(data), flags);

    /// <inheritdoc cref="EnumerateUtf(ReadOnlySpan{byte}, UtfEnumeratorFlags)"/>
    public static UtfEnumerator EnumerateUtf(this Span<char> data, UtfEnumeratorFlags flags) =>
        new(MemoryMarshal.Cast<char, byte>(data), flags);

    /// <inheritdoc cref="EnumerateUtf(ReadOnlySpan{byte}, UtfEnumeratorFlags)"/>
    public static UtfEnumerator EnumerateUtf(this ReadOnlyMemory<char> data, UtfEnumeratorFlags flags) =>
        new(MemoryMarshal.Cast<char, byte>(data.Span), flags);

    /// <inheritdoc cref="EnumerateUtf(ReadOnlySpan{byte}, UtfEnumeratorFlags)"/>
    public static UtfEnumerator EnumerateUtf(this Memory<char> data, UtfEnumeratorFlags flags) =>
        new(MemoryMarshal.Cast<char, byte>(data.Span), flags);

    /// <inheritdoc cref="EnumerateUtf(ReadOnlySpan{byte}, UtfEnumeratorFlags)"/>
    public static UtfEnumerator EnumerateUtf(this string data, UtfEnumeratorFlags flags) =>
        new(MemoryMarshal.Cast<char, byte>(data.AsSpan()), flags);
}
