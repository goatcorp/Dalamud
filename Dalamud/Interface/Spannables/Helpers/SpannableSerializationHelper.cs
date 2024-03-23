using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Dalamud.Interface.Spannables.Helpers;

/// <summary>Helper utilities for serializing spannables.</summary>
public static class SpannableSerializationHelper
{
    /// <summary>Writes an unmanaged data to a buffer, and advances the buffer.</summary>
    /// <param name="buffer">Buffer to write data to.</param>
    /// <param name="value">Value to serialize.</param>
    /// <typeparam name="T">An unmanaged value type.</typeparam>
    /// <returns>Number of bytes written (or required, if buffer was empty.)</returns>
    public static unsafe int Write<T>(ref Span<byte> buffer, in T value) where T : unmanaged
    {
        if (!buffer.IsEmpty)
        {
            fixed (void* p = &value)
                new Span<byte>(p, sizeof(T)).CopyTo(buffer);
        }

        return sizeof(T);
    }

    /// <summary>Writes a span of unmanaged data to a buffer, and advances the buffer.</summary>
    /// <param name="buffer">Buffer to write data to.</param>
    /// <param name="values">Span of values to serialize.</param>
    /// <typeparam name="T">An unmanaged value type.</typeparam>
    /// <returns>Number of bytes written (or required, if buffer was empty.)</returns>
    /// <remarks>This does not store the number of items in the list.</remarks>
    public static int Write<T>(ref Span<byte> buffer, ReadOnlySpan<T> values) where T : unmanaged
    {
        var bytes = MemoryMarshal.Cast<T, byte>(values);
        if (!buffer.IsEmpty)
            bytes.CopyTo(buffer);
        return bytes.Length;
    }

    /// <inheritdoc cref="Write{T}(ref Span{byte}, ReadOnlySpan{T})"/>
    public static int Write<T>(ref Span<byte> buffer, List<T> values) where T : unmanaged =>
        Write<T>(ref buffer, CollectionsMarshal.AsSpan(values));

    /// <inheritdoc cref="Write{T}(ref Span{byte}, ReadOnlySpan{T})"/>
    public static int Write<T>(ref Span<byte> buffer, IReadOnlyCollection<T> values) where T : unmanaged
    {
        var length = Write(ref buffer, values.Count);
        foreach (var v in values)
            length += Write(ref buffer, v);
        return length;
    }

    /// <summary>Writes states of children spannables, for supported spannables.</summary>
    /// <param name="buffer">Buffer to write data to.</param>
    /// <param name="children">Children to serialize the state.</param>
    /// <returns>Number of bytes written (or required, if buffer was empty.)</returns>
    public static int Write(ref Span<byte> buffer, IReadOnlyCollection<ISpannable?> children)
    {
        var length = Write(ref buffer, children.Count);
        foreach (var v in children)
        {
            if (v is not ISpannableSerializable ss)
                continue;
            var len = ss.SerializeState(buffer);
            buffer = buffer[len..];
            length += len;
        }

        return length;
    }

    /// <summary>Reads back an unmanaged data from a buffer.</summary>
    /// <param name="buffer">Buffer to read data from.</param>
    /// <param name="value">Deserialized value.</param>
    /// <typeparam name="T">An unmanaged value type.</typeparam>
    /// <returns><c>true</c> on success.</returns>
    public static unsafe bool TryRead<T>(ref ReadOnlySpan<byte> buffer, out T value) where T : unmanaged
    {
        if (buffer.Length < sizeof(T))
        {
            value = default;
            return false;
        }

        value = MemoryMarshal.Cast<byte, T>(buffer)[0];
        buffer = buffer[sizeof(T)..];
        return true;
    }

    /// <summary>Reads back states of children spannables, for supported spananbles.</summary>
    /// <param name="buffer">Buffer to read data from.</param>
    /// <param name="children">Children to write states to.</param>
    /// <returns><c>true</c> on success.</returns>
    public static bool TryRead(ref ReadOnlySpan<byte> buffer, IReadOnlyCollection<ISpannable?> children)
    {
        if (!TryRead(ref buffer, out int numChildren) || numChildren != children.Count)
            return false;
        foreach (var v in children)
        {
            if (v is not ISpannableSerializable ss)
                continue;
            if (!ss.TryDeserializeState(buffer, out var len))
                return false;
            buffer = buffer[len..];
        }

        return true;
    }
}
