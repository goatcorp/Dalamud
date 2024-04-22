using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Memory.Exceptions;

using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Client.System.String;

using Microsoft.Extensions.ObjectPool;

using static Dalamud.NativeFunctions;

using LPayloadType = Lumina.Text.Payloads.PayloadType;
using LSeString = Lumina.Text.SeString;

// Heavily inspired from Reloaded (https://github.com/Reloaded-Project/Reloaded.Memory)

namespace Dalamud.Memory;

/// <summary>
/// A simple class that provides read/write access to arbitrary memory.
/// </summary>
public static unsafe class MemoryHelper
{
    private static readonly ObjectPool<StringBuilder> StringBuilderPool =
        ObjectPool.Create(new StringBuilderPooledObjectPolicy());

    #region Cast

    /// <summary>Casts the given memory address as the reference to the live object.</summary>
    /// <param name="memoryAddress">The memory address.</param>
    /// <typeparam name="T">The unmanaged type.</typeparam>
    /// <returns>The reference to the live object.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T Cast<T>(nint memoryAddress) where T : unmanaged => ref *(T*)memoryAddress;

    /// <summary>Casts the given memory address as the span of the live object(s).</summary>
    /// <param name="memoryAddress">The memory address.</param>
    /// <param name="length">The number of items.</param>
    /// <typeparam name="T">The unmanaged type.</typeparam>
    /// <returns>The span containing reference to the live object(s).</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<T> Cast<T>(nint memoryAddress, int length) where T : unmanaged =>
        new((void*)memoryAddress, length);

    /// <summary>Casts the given memory address as the span of the live object(s), until it encounters a zero.</summary>
    /// <param name="memoryAddress">The memory address.</param>
    /// <param name="maxLength">The maximum number of items.</param>
    /// <typeparam name="T">The unmanaged type.</typeparam>
    /// <returns>The span containing reference to the live object(s).</returns>
    /// <remarks>If <typeparamref name="T"/> is <c>byte</c> or <c>char</c> and <paramref name="maxLength"/> is not
    /// specified, consider using <see cref="MemoryMarshal.CreateReadOnlySpanFromNullTerminated(byte*)"/> or
    /// <see cref="MemoryMarshal.CreateReadOnlySpanFromNullTerminated(char*)"/>.</remarks>
    public static Span<T> CastNullTerminated<T>(nint memoryAddress, int maxLength = int.MaxValue)
        where T : unmanaged, IEquatable<T>
    {
        var typedPointer = (T*)memoryAddress;
        var length = 0;
        while (length < maxLength && !default(T).Equals(*typedPointer++))
            length++;
        return new((void*)memoryAddress, length);
    }

    #endregion

    #region Read

    /// <summary>
    /// Reads a generic type from a specified memory address.
    /// </summary>
    /// <typeparam name="T">An individual struct type of a class with an explicit StructLayout.LayoutKind attribute.</typeparam>
    /// <param name="memoryAddress">The memory address to read from.</param>
    /// <returns>The read in struct.</returns>
    /// <remarks>If you do not need to make a copy, use <see cref="Cast{T}(nint)"/> instead.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Read<T>(nint memoryAddress) where T : unmanaged
        => Read<T>(memoryAddress, false);

    /// <summary>
    /// Reads a generic type from a specified memory address.
    /// </summary>
    /// <typeparam name="T">An individual struct type of a class with an explicit StructLayout.LayoutKind attribute.</typeparam>
    /// <param name="memoryAddress">The memory address to read from.</param>
    /// <param name="marshal">Set this to true to enable struct marshalling.</param>
    /// <returns>The read in struct.</returns>
    /// <remarks>If you do not need to make a copy and <paramref name="marshal"/> is <c>false</c>,
    /// use <see cref="Cast{T}(nint)"/> instead.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Read<T>(nint memoryAddress, bool marshal) =>
        marshal
            ? Marshal.PtrToStructure<T>(memoryAddress)
            : Unsafe.Read<T>((void*)memoryAddress);

    /// <summary>
    /// Reads a byte array from a specified memory address.
    /// </summary>
    /// <param name="memoryAddress">The memory address to read from.</param>
    /// <param name="length">The amount of bytes to read starting from the memoryAddress.</param>
    /// <returns>The read in byte array.</returns>
    /// <remarks>If you do not need to make a copy, use <see cref="Cast{T}(nint,int)"/> instead.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] ReadRaw(nint memoryAddress, int length) => Cast<byte>(memoryAddress, length).ToArray();

    /// <summary>
    /// Reads a generic type array from a specified memory address.
    /// </summary>
    /// <typeparam name="T">An individual struct type of a class with an explicit StructLayout.LayoutKind attribute.</typeparam>
    /// <param name="memoryAddress">The memory address to read from.</param>
    /// <param name="arrayLength">The amount of array items to read.</param>
    /// <returns>The read in struct array.</returns>
    /// <remarks>If you do not need to make a copy, use <see cref="Cast{T}(nint,int)"/> instead.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T[] Read<T>(nint memoryAddress, int arrayLength) where T : unmanaged
        => Cast<T>(memoryAddress, arrayLength).ToArray();

    /// <summary>
    /// Reads a generic type array from a specified memory address.
    /// </summary>
    /// <typeparam name="T">An individual struct type of a class with an explicit StructLayout.LayoutKind attribute.</typeparam>
    /// <param name="memoryAddress">The memory address to read from.</param>
    /// <param name="arrayLength">The amount of array items to read.</param>
    /// <param name="marshal">Set this to true to enable struct marshalling.</param>
    /// <returns>The read in struct array.</returns>
    /// <remarks>If you do not need to make a copy and <paramref name="marshal"/> is <c>false</c>,
    /// use <see cref="Cast{T}(nint,int)"/> instead.</remarks>
    public static T[] Read<T>(nint memoryAddress, int arrayLength, bool marshal)
    {
        var structSize = SizeOf<T>(marshal);
        var value = new T[arrayLength];

        for (var i = 0; i < arrayLength; i++)
        {
            Read(memoryAddress, out T result, marshal);
            value[i] = result;
            memoryAddress += structSize;
        }

        return value;
    }

    /// <summary>
    /// Reads a null-terminated byte array from a specified memory address.
    /// </summary>
    /// <param name="memoryAddress">The memory address to read from.</param>
    /// <returns>The read in byte array.</returns>
    /// <remarks>If you do not need to make a copy, use <see cref="CastNullTerminated{T}(nint,int)"/> instead.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] ReadRawNullTerminated(nint memoryAddress) =>
        MemoryMarshal.CreateReadOnlySpanFromNullTerminated((byte*)memoryAddress).ToArray();

    #endregion

    #region Read(out)

    /// <summary>
    /// Reads a generic type from a specified memory address.
    /// </summary>
    /// <typeparam name="T">An individual struct type of a class with an explicit StructLayout.LayoutKind attribute.</typeparam>
    /// <param name="memoryAddress">The memory address to read from.</param>
    /// <param name="value">Local variable to receive the read in struct.</param>
    /// <remarks>If you do not need to make a copy, use <see cref="Cast{T}(nint)"/> instead.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Read<T>(nint memoryAddress, out T value) where T : unmanaged
        => value = Read<T>(memoryAddress);

    /// <summary>
    /// Reads a generic type from a specified memory address.
    /// </summary>
    /// <typeparam name="T">An individual struct type of a class with an explicit StructLayout.LayoutKind attribute.</typeparam>
    /// <param name="memoryAddress">The memory address to read from.</param>
    /// <param name="value">Local variable to receive the read in struct.</param>
    /// <param name="marshal">Set this to true to enable struct marshalling.</param>
    /// <remarks>If you do not need to make a copy and <paramref name="marshal"/> is <c>false</c>,
    /// use <see cref="Cast{T}(nint)"/> instead.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Read<T>(nint memoryAddress, out T value, bool marshal)
        => value = Read<T>(memoryAddress, marshal);

    /// <summary>
    /// Reads raw data from a specified memory address.
    /// </summary>
    /// <param name="memoryAddress">The memory address to read from.</param>
    /// <param name="length">The amount of bytes to read starting from the memoryAddress.</param>
    /// <param name="value">Local variable to receive the read in bytes.</param>
    /// <remarks>If you do not need to make a copy, use <see cref="Cast{T}(nint,int)"/> instead.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadRaw(nint memoryAddress, int length, out byte[] value)
        => value = ReadRaw(memoryAddress, length);

    /// <summary>
    /// Reads a generic type array from a specified memory address.
    /// </summary>
    /// <typeparam name="T">An individual struct type of a class with an explicit StructLayout.LayoutKind attribute.</typeparam>
    /// <param name="memoryAddress">The memory address to read from.</param>
    /// <param name="arrayLength">The amount of array items to read.</param>
    /// <param name="value">The read in struct array.</param>
    /// <remarks>If you do not need to make a copy, use <see cref="Cast{T}(nint,int)"/> instead.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Read<T>(nint memoryAddress, int arrayLength, out T[] value) where T : unmanaged
        => value = Read<T>(memoryAddress, arrayLength);

    /// <summary>
    /// Reads a generic type array from a specified memory address.
    /// </summary>
    /// <typeparam name="T">An individual struct type of a class with an explicit StructLayout.LayoutKind attribute.</typeparam>
    /// <param name="memoryAddress">The memory address to read from.</param>
    /// <param name="arrayLength">The amount of array items to read.</param>
    /// <param name="marshal">Set this to true to enable struct marshalling.</param>
    /// <param name="value">The read in struct array.</param>
    /// <remarks>If you do not need to make a copy and <paramref name="marshal"/> is <c>false</c>,
    /// use <see cref="Cast{T}(nint,int)"/> instead.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Read<T>(nint memoryAddress, int arrayLength, bool marshal, out T[] value)
        => value = Read<T>(memoryAddress, arrayLength, marshal);

    #endregion

    #region ReadString

    /// <summary>
    /// Compares if the given char span equals to the null-terminated string at <paramref name="memoryAddress"/>.
    /// </summary>
    /// <param name="charSpan">The character span.</param>
    /// <param name="memoryAddress">The address of null-terminated string.</param>
    /// <param name="encoding">The encoding of the null-terminated string.</param>
    /// <param name="maxLength">The maximum length of the null-terminated string.</param>
    /// <returns>Whether they are equal.</returns>
    public static bool EqualsZeroTerminatedString(
        ReadOnlySpan<char> charSpan,
        nint memoryAddress,
        Encoding? encoding = null,
        int maxLength = int.MaxValue)
    {
        encoding ??= Encoding.UTF8;
        maxLength = Math.Min(maxLength, charSpan.Length + 4);

        var pmem = ((byte*)memoryAddress)!;
        var length = 0;
        while (length < maxLength && pmem[length] != 0)
            length++;

        var mem = new Span<byte>(pmem, length);
        var memCharCount = encoding.GetCharCount(mem);
        if (memCharCount != charSpan.Length)
            return false;

        if (memCharCount < 1024)
        {
            Span<char> chars = stackalloc char[memCharCount];
            encoding.GetChars(mem, chars);
            return charSpan.SequenceEqual(chars);
        }
        else
        {
            var rented = ArrayPool<char>.Shared.Rent(memCharCount);
            var chars = rented.AsSpan(0, memCharCount);
            encoding.GetChars(mem, chars);
            var equals = charSpan.SequenceEqual(chars);
            ArrayPool<char>.Shared.Return(rented);
            return equals;
        }
    }

    /// <summary>
    /// Read a UTF-8 encoded string from a specified memory address.
    /// </summary>
    /// <remarks>
    /// Attention! If this is an <see cref="SeString"/>, use the applicable helper methods to decode.
    /// </remarks>
    /// <param name="memoryAddress">The memory address to read from.</param>
    /// <returns>The read in string.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ReadStringNullTerminated(nint memoryAddress)
        => Encoding.UTF8.GetString(MemoryMarshal.CreateReadOnlySpanFromNullTerminated((byte*)memoryAddress));

    /// <summary>
    /// Read a string with the given encoding from a specified memory address.
    /// </summary>
    /// <remarks>
    /// Attention! If this is an <see cref="SeString"/>, use the applicable helper methods to decode.
    /// </remarks>
    /// <param name="memoryAddress">The memory address to read from.</param>
    /// <param name="encoding">The encoding to use to decode the string.</param>
    /// <returns>The read in string.</returns>
    public static string ReadStringNullTerminated(nint memoryAddress, Encoding encoding)
    {
        switch (encoding)
        {
            case UTF8Encoding:
            case var _ when encoding.IsSingleByte:
                return encoding.GetString(MemoryMarshal.CreateReadOnlySpanFromNullTerminated((byte*)memoryAddress));
            case UnicodeEncoding:
                // Note that it may be in little or big endian, so using `new string(...)` is not always correct.
                return encoding.GetString(
                    MemoryMarshal.Cast<char, byte>(
                        MemoryMarshal.CreateReadOnlySpanFromNullTerminated((char*)memoryAddress)));
            case UTF32Encoding:
                return encoding.GetString(MemoryMarshal.Cast<int, byte>(CastNullTerminated<int>(memoryAddress)));
            default:
                // For correctness' sake; if there does not exist an encoding which will contain a (byte)0 for a
                // non-null character, then this branch can be merged with UTF8Encoding one.
                return encoding.GetString(ReadRawNullTerminated(memoryAddress));
        }
    }

    /// <summary>
    /// Read a UTF-8 encoded string from a specified memory address.
    /// </summary>
    /// <remarks>
    /// Attention! If this is an <see cref="SeString"/>, use the applicable helper methods to decode.
    /// </remarks>
    /// <param name="memoryAddress">The memory address to read from.</param>
    /// <param name="maxLength">The maximum number of bytes to read.
    /// Note that this is NOT the maximum length of the returned string.</param>
    /// <returns>The read in string.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ReadString(nint memoryAddress, int maxLength)
        => Encoding.UTF8.GetString(CastNullTerminated<byte>(memoryAddress, maxLength));

    /// <summary>
    /// Read a string with the given encoding from a specified memory address.
    /// </summary>
    /// <remarks>
    /// Attention! If this is an <see cref="SeString"/>, use the applicable helper methods to decode.
    /// </remarks>
    /// <param name="memoryAddress">The memory address to read from.</param>
    /// <param name="encoding">The encoding to use to decode the string.</param>
    /// <param name="maxLength">The maximum number of bytes to read.
    /// Note that this is NOT the maximum length of the returned string.</param>
    /// <returns>The read in string.</returns>
    public static string ReadString(nint memoryAddress, Encoding encoding, int maxLength)
    {
        if (maxLength <= 0)
            return string.Empty;

        switch (encoding)
        {
            case UTF8Encoding:
            case var _ when encoding.IsSingleByte:
                return encoding.GetString(CastNullTerminated<byte>(memoryAddress, maxLength));
            case UnicodeEncoding:
                return encoding.GetString(
                    MemoryMarshal.Cast<char, byte>(CastNullTerminated<char>(memoryAddress, maxLength / 2)));
            case UTF32Encoding:
                return encoding.GetString(
                    MemoryMarshal.Cast<int, byte>(CastNullTerminated<int>(memoryAddress, maxLength / 4)));
            default:
                // For correctness' sake; if there does not exist an encoding which will contain a (byte)0 for a
                // non-null character, then this branch can be merged with UTF8Encoding one.
                var data = encoding.GetString(Cast<byte>(memoryAddress, maxLength));
                var eosPos = data.IndexOf('\0');
                return eosPos >= 0 ? data[..eosPos] : data;
        }
    }

    /// <summary>
    /// Read a null-terminated SeString from a specified memory address.
    /// </summary>
    /// <param name="memoryAddress">The memory address to read from.</param>
    /// <returns>The read in string.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SeString ReadSeStringNullTerminated(nint memoryAddress) =>
        SeString.Parse(MemoryMarshal.CreateReadOnlySpanFromNullTerminated((byte*)memoryAddress));

    /// <summary>
    /// Read an SeString from a specified memory address.
    /// </summary>
    /// <param name="memoryAddress">The memory address to read from.</param>
    /// <param name="maxLength">The maximum length of the string.</param>
    /// <returns>The read in string.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SeString ReadSeString(nint memoryAddress, int maxLength) =>
        // Note that a valid SeString never contains a null character, other than for the sequence terminator purpose.
        SeString.Parse(CastNullTerminated<byte>(memoryAddress, maxLength));

    /// <summary>
    /// Read an SeString from a specified Utf8String structure.
    /// </summary>
    /// <param name="utf8String">The memory address to read from.</param>
    /// <returns>The read in string.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SeString ReadSeString(Utf8String* utf8String) =>
        utf8String == null ? string.Empty : SeString.Parse(utf8String->AsSpan());

    /// <summary>
    /// Reads an SeString from a specified memory address, and extracts the outermost string.<br />
    /// If the SeString is malformed, behavior is undefined.
    /// </summary>
    /// <param name="containsNonRepresentedPayload">Whether the SeString contained a non-represented payload.</param>
    /// <param name="memoryAddress">The memory address to read from.</param>
    /// <param name="maxLength">The maximum length of the string.</param>
    /// <param name="stopOnFirstNonRepresentedPayload">Stop reading on encountering the first non-represented payload.
    /// What payloads are represented via this function may change.</param>
    /// <param name="nonRepresentedPayloadReplacement">Replacement for non-represented payloads.</param>
    /// <returns>The read in string.</returns>
    public static string ReadSeStringAsString(
        out bool containsNonRepresentedPayload,
        nint memoryAddress,
        int maxLength = int.MaxValue,
        bool stopOnFirstNonRepresentedPayload = false,
        string nonRepresentedPayloadReplacement = "*")
    {
        var sb = StringBuilderPool.Get();
        sb.EnsureCapacity(maxLength = CastNullTerminated<byte>(memoryAddress, maxLength).Length);

        // 1 utf-8 codepoint can spill up to 2 characters.
        Span<char> tmp = stackalloc char[2];

        var pin = (byte*)memoryAddress;
        containsNonRepresentedPayload = false;
        while (*pin != 0 && maxLength > 0)
        {
            if (*pin != LSeString.StartByte)
            {
                var len = *pin switch
                {
                    < 0x80 => 1,
                    >= 0b11000000 and <= 0b11011111 => 2,
                    >= 0b11100000 and <= 0b11101111 => 3,
                    >= 0b11110000 and <= 0b11110111 => 4,
                    _ => 0,
                };
                if (len == 0 || len > maxLength)
                    break;

                var numChars = Encoding.UTF8.GetChars(new(pin, len), tmp);
                sb.Append(tmp[..numChars]);
                pin += len;
                maxLength -= len;
                continue;
            }

            // Start byte
            ++pin;
            --maxLength;

            // Payload type
            var payloadType = (LPayloadType)(*pin++);

            // Payload length
            if (!ReadIntExpression(ref pin, ref maxLength, out var expressionLength))
                break;
            if (expressionLength > maxLength)
                break;
            pin += expressionLength;
            maxLength -= unchecked((int)expressionLength);

            // End byte
            if (*pin++ != LSeString.EndByte)
                break;
            --maxLength;

            switch (payloadType)
            {
                case LPayloadType.NewLine:
                    sb.AppendLine();
                    break;
                case LPayloadType.Hyphen:
                    sb.Append('–');
                    break;
                case LPayloadType.SoftHyphen:
                    sb.Append('\u00AD');
                    break;
                default:
                    sb.Append(nonRepresentedPayloadReplacement);
                    containsNonRepresentedPayload = true;
                    if (stopOnFirstNonRepresentedPayload)
                        maxLength = 0;
                    break;
            }
        }

        var res = sb.ToString();
        StringBuilderPool.Return(sb);
        return res;

        static bool ReadIntExpression(ref byte* p, ref int maxLength, out uint value)
        {
            if (maxLength <= 0)
            {
                value = 0;
                return false;
            }

            var typeByte = *p++;
            --maxLength;

            switch (typeByte)
            {
                case > 0 and < 0xD0:
                    value = (uint)typeByte - 1;
                    return true;
                case >= 0xF0 and <= 0xFE:
                    ++typeByte;
                    value = 0u;
                    if ((typeByte & 8) != 0)
                    {
                        if (maxLength <= 0 || *p == 0)
                            return false;
                        value |= (uint)*p++ << 24;
                    }

                    if ((typeByte & 4) != 0)
                    {
                        if (maxLength <= 0 || *p == 0)
                            return false;
                        value |= (uint)*p++ << 16;
                    }

                    if ((typeByte & 2) != 0)
                    {
                        if (maxLength <= 0 || *p == 0)
                            return false;
                        value |= (uint)*p++ << 8;
                    }

                    if ((typeByte & 1) != 0)
                    {
                        if (maxLength <= 0 || *p == 0)
                            return false;
                        value |= *p++;
                    }

                    return true;
                default:
                    value = 0;
                    return false;
            }
        }
    }

    #endregion

    #region ReadString(out)

    /// <summary>
    /// Read a UTF-8 encoded string from a specified memory address.
    /// </summary>
    /// <remarks>
    /// Attention! If this is an <see cref="SeString"/>, use the applicable helper methods to decode.
    /// </remarks>
    /// <param name="memoryAddress">The memory address to read from.</param>
    /// <param name="value">The read in string.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadStringNullTerminated(nint memoryAddress, out string value)
        => value = ReadStringNullTerminated(memoryAddress);

    /// <summary>
    /// Read a string with the given encoding from a specified memory address.
    /// </summary>
    /// <remarks>
    /// Attention! If this is an <see cref="SeString"/>, use the applicable helper methods to decode.
    /// </remarks>
    /// <param name="memoryAddress">The memory address to read from.</param>
    /// <param name="encoding">The encoding to use to decode the string.</param>
    /// <param name="value">The read in string.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadStringNullTerminated(nint memoryAddress, Encoding encoding, out string value)
        => value = ReadStringNullTerminated(memoryAddress, encoding);

    /// <summary>
    /// Read a UTF-8 encoded string from a specified memory address.
    /// </summary>
    /// <remarks>
    /// Attention! If this is an <see cref="SeString"/>, use the applicable helper methods to decode.
    /// </remarks>
    /// <param name="memoryAddress">The memory address to read from.</param>
    /// <param name="value">The read in string.</param>
    /// <param name="maxLength">The maximum length of the string.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadString(nint memoryAddress, out string value, int maxLength)
        => value = ReadString(memoryAddress, maxLength);

    /// <summary>
    /// Read a string with the given encoding from a specified memory address.
    /// </summary>
    /// <remarks>
    /// Attention! If this is an <see cref="SeString"/>, use the applicable helper methods to decode.
    /// </remarks>
    /// <param name="memoryAddress">The memory address to read from.</param>
    /// <param name="encoding">The encoding to use to decode the string.</param>
    /// <param name="maxLength">The maximum length of the string.</param>
    /// <param name="value">The read in string.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadString(nint memoryAddress, Encoding encoding, int maxLength, out string value)
        => value = ReadString(memoryAddress, encoding, maxLength);

    /// <summary>
    /// Read a null-terminated SeString from a specified memory address.
    /// </summary>
    /// <param name="memoryAddress">The memory address to read from.</param>
    /// <param name="value">The read in SeString.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadSeStringNullTerminated(nint memoryAddress, out SeString value)
        => value = ReadSeStringNullTerminated(memoryAddress);

    /// <summary>
    /// Read an SeString from a specified memory address.
    /// </summary>
    /// <param name="memoryAddress">The memory address to read from.</param>
    /// <param name="maxLength">The maximum length of the string.</param>
    /// <param name="value">The read in SeString.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadSeString(nint memoryAddress, int maxLength, out SeString value)
        => value = ReadSeString(memoryAddress, maxLength);

    /// <summary>
    /// Read an SeString from a specified Utf8String structure.
    /// </summary>
    /// <param name="utf8String">The memory address to read from.</param>
    /// <param name="value">The read in string.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void ReadSeString(Utf8String* utf8String, out SeString value)
        => value = ReadSeString(utf8String);

    #endregion

    #region Write

    /// <summary>
    /// Writes a generic type to a specified memory address.
    /// </summary>
    /// <typeparam name="T">An individual struct type of a class with an explicit StructLayout.LayoutKind attribute.</typeparam>
    /// <param name="memoryAddress">The memory address to read from.</param>
    /// <param name="item">The item to write to the address.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write<T>(nint memoryAddress, T item) where T : unmanaged
        => Write(memoryAddress, item, false);

    /// <summary>
    /// Writes a generic type to a specified memory address.
    /// </summary>
    /// <typeparam name="T">An individual struct type of a class with an explicit StructLayout.LayoutKind attribute.</typeparam>
    /// <param name="memoryAddress">The memory address to read from.</param>
    /// <param name="item">The item to write to the address.</param>
    /// <param name="marshal">Set this to true to enable struct marshalling.</param>
    public static void Write<T>(nint memoryAddress, T item, bool marshal)
    {
        if (marshal)
            Marshal.StructureToPtr(item, memoryAddress, false);
        else
            Unsafe.Write((void*)memoryAddress, item);
    }

    /// <summary>
    /// Writes raw data to a specified memory address.
    /// </summary>
    /// <param name="memoryAddress">The memory address to read from.</param>
    /// <param name="data">The bytes to write to memoryAddress.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteRaw(nint memoryAddress, byte[] data) => Marshal.Copy(data, 0, memoryAddress, data.Length);

    /// <summary>
    /// Writes a generic type array to a specified memory address.
    /// </summary>
    /// <typeparam name="T">An individual struct type of a class with an explicit StructLayout.LayoutKind attribute.</typeparam>
    /// <param name="memoryAddress">The memory address to write to.</param>
    /// <param name="items">The array of items to write to the address.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write<T>(nint memoryAddress, T[] items) where T : unmanaged
        => Write(memoryAddress, items, false);

    /// <summary>
    /// Writes a generic type array to a specified memory address.
    /// </summary>
    /// <typeparam name="T">An individual struct type of a class with an explicit StructLayout.LayoutKind attribute.</typeparam>
    /// <param name="memoryAddress">The memory address to write to.</param>
    /// <param name="items">The array of items to write to the address.</param>
    /// <param name="marshal">Set this to true to enable struct marshalling.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write<T>(nint memoryAddress, T[] items, bool marshal)
    {
        var structSize = SizeOf<T>(marshal);

        for (var i = 0; i < items.Length; i++)
        {
            var address = memoryAddress + (structSize * i);
            Write(address, items[i], marshal);
        }
    }

    #endregion

    #region WriteString

    /// <summary>
    /// Write a UTF-8 encoded string to a specified memory address.
    /// </summary>
    /// <remarks>
    /// Attention! If this is an <see cref="SeString"/>, use the applicable helper methods to decode.
    /// </remarks>
    /// <param name="memoryAddress">The memory address to write to.</param>
    /// <param name="value">The string to write.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteString(nint memoryAddress, string? value)
        => WriteString(memoryAddress, value, Encoding.UTF8);

    /// <summary>
    /// Write a string with the given encoding to a specified memory address.
    /// </summary>
    /// <remarks>
    /// Attention! If this is an <see cref="SeString"/>, use the applicable helper methods to decode.
    /// </remarks>
    /// <param name="memoryAddress">The memory address to write to.</param>
    /// <param name="value">The string to write.</param>
    /// <param name="encoding">The encoding to use.</param>
    public static void WriteString(nint memoryAddress, string? value, Encoding encoding)
    {
        var ptr = 0;
        if (value is not null)
            ptr = encoding.GetBytes(value, Cast<byte>(memoryAddress, encoding.GetMaxByteCount(value.Length)));
        encoding.GetBytes("\0", Cast<byte>(memoryAddress + ptr, 4));
    }

    /// <summary>
    /// Write an SeString to a specified memory address.
    /// </summary>
    /// <param name="memoryAddress">The memory address to write to.</param>
    /// <param name="value">The SeString to write.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteSeString(nint memoryAddress, SeString? value)
    {
        if (value is null)
            return;

        WriteRaw(memoryAddress, value.Encode());
    }

    #endregion

    #region ApiWrappers

    /// <summary>
    /// Allocates fixed size of memory inside the target memory source via Windows API calls.
    /// Returns the address of newly allocated memory.
    /// </summary>
    /// <param name="length">Amount of bytes to be allocated.</param>
    /// <returns>Address to the newly allocated memory.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static nint Allocate(int length)
    {
        var address = VirtualAlloc(
            nint.Zero,
            (nuint)length,
            AllocationType.Commit | AllocationType.Reserve,
            MemoryProtection.ExecuteReadWrite);

        if (address == nint.Zero)
            throw new MemoryAllocationException($"Unable to allocate {length} bytes.");

        return address;
    }

    /// <summary>
    /// Allocates fixed size of memory inside the target memory source via Windows API calls.
    /// Returns the address of newly allocated memory.
    /// </summary>
    /// <param name="length">Amount of bytes to be allocated.</param>
    /// <param name="memoryAddress">Address to the newly allocated memory.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Allocate(int length, out nint memoryAddress)
        => memoryAddress = Allocate(length);

    /// <summary>
    /// Frees memory previously allocated with Allocate via Windows API calls.
    /// </summary>
    /// <param name="memoryAddress">The address of the memory to free.</param>
    /// <returns>True if the operation is successful.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Free(nint memoryAddress)
    {
        return VirtualFree(memoryAddress, nuint.Zero, AllocationType.Release);
    }

    /// <summary>
    /// Changes the page permissions for a specified combination of address and length via Windows API calls.
    /// </summary>
    /// <param name="memoryAddress">The memory address for which to change page permissions for.</param>
    /// <param name="length">The region size for which to change permissions for.</param>
    /// <param name="newPermissions">The new permissions to set.</param>
    /// <returns>The old page permissions.</returns>
    public static MemoryProtection ChangePermission(nint memoryAddress, int length, MemoryProtection newPermissions)
    {
        var result = VirtualProtect(memoryAddress, (nuint)length, newPermissions, out var oldPermissions);

        if (!result)
            throw new MemoryPermissionException($"Unable to change permissions at 0x{memoryAddress.ToInt64():X} of length {length} and permission {newPermissions} (result={result})");

        var last = Marshal.GetLastWin32Error();
        if (last > 0)
            throw new MemoryPermissionException($"Unable to change permissions at 0x{memoryAddress.ToInt64():X} of length {length} and permission {newPermissions} (error={last})");

        return oldPermissions;
    }

    /// <summary>
    /// Changes the page permissions for a specified combination of address and length via Windows API calls.
    /// </summary>
    /// <param name="memoryAddress">The memory address for which to change page permissions for.</param>
    /// <param name="length">The region size for which to change permissions for.</param>
    /// <param name="newPermissions">The new permissions to set.</param>
    /// <param name="oldPermissions">The old page permissions.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ChangePermission(
        nint memoryAddress, int length, MemoryProtection newPermissions, out MemoryProtection oldPermissions)
        => oldPermissions = ChangePermission(memoryAddress, length, newPermissions);

    /// <summary>
    /// Changes the page permissions for a specified combination of address and element from which to deduce size via Windows API calls.
    /// </summary>
    /// <typeparam name="T">An individual struct type of a class with an explicit StructLayout.LayoutKind attribute.</typeparam>
    /// <param name="memoryAddress">The memory address for which to change page permissions for.</param>
    /// <param name="baseElement">The struct element from which the region size to change permissions for will be calculated.</param>
    /// <param name="newPermissions">The new permissions to set.</param>
    /// <param name="marshal">Set to true to calculate the size of the struct after marshalling instead of before.</param>
    /// <returns>The old page permissions.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MemoryProtection ChangePermission<T>(
        nint memoryAddress, ref T baseElement, MemoryProtection newPermissions, bool marshal)
        => ChangePermission(memoryAddress, SizeOf<T>(marshal), newPermissions);

    /// <summary>
    /// Reads raw data from a specified memory address via Windows API calls.
    /// This is noticably slower than Unsafe or Marshal.
    /// </summary>
    /// <param name="memoryAddress">The memory address to read from.</param>
    /// <param name="length">The amount of bytes to read starting from the memoryAddress.</param>
    /// <returns>The read in bytes.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] ReadProcessMemory(nint memoryAddress, int length)
    {
        var value = new byte[length];
        ReadProcessMemory(memoryAddress, ref value);
        return value;
    }

    /// <summary>
    /// Reads raw data from a specified memory address via Windows API calls.
    /// This is noticably slower than Unsafe or Marshal.
    /// </summary>
    /// <param name="memoryAddress">The memory address to read from.</param>
    /// <param name="length">The amount of bytes to read starting from the memoryAddress.</param>
    /// <param name="value">The read in bytes.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadProcessMemory(nint memoryAddress, int length, out byte[] value)
        => value = ReadProcessMemory(memoryAddress, length);

    /// <summary>
    /// Reads raw data from a specified memory address via Windows API calls.
    /// This is noticably slower than Unsafe or Marshal.
    /// </summary>
    /// <param name="memoryAddress">The memory address to read from.</param>
    /// <param name="value">The read in bytes.</param>
    public static void ReadProcessMemory(nint memoryAddress, ref byte[] value)
    {
        unchecked
        {
            var length = value.Length;
            var result = NativeFunctions.ReadProcessMemory((nint)0xFFFFFFFF, memoryAddress, value, length, out _);

            if (!result)
                throw new MemoryReadException($"Unable to read memory at 0x{memoryAddress.ToInt64():X} of length {length} (result={result})");

            var last = Marshal.GetLastWin32Error();
            if (last > 0)
                throw new MemoryReadException($"Unable to read memory at 0x{memoryAddress.ToInt64():X} of length {length} (error={last})");
        }
    }

    /// <summary>
    /// Writes raw data to a specified memory address via Windows API calls.
    /// This is noticably slower than Unsafe or Marshal.
    /// </summary>
    /// <param name="memoryAddress">The memory address to write to.</param>
    /// <param name="data">The bytes to write to memoryAddress.</param>
    public static void WriteProcessMemory(nint memoryAddress, byte[] data)
    {
        unchecked
        {
            var length = data.Length;
            var result = NativeFunctions.WriteProcessMemory((nint)0xFFFFFFFF, memoryAddress, data, length, out _);

            if (!result)
                throw new MemoryWriteException($"Unable to write memory at 0x{memoryAddress.ToInt64():X} of length {length} (result={result})");

            var last = Marshal.GetLastWin32Error();
            if (last > 0)
                throw new MemoryWriteException($"Unable to write memory at 0x{memoryAddress.ToInt64():X} of length {length} (error={last})");
        }
    }

    #endregion

    #region Sizing

    /// <summary>
    /// Returns the size of a specific primitive or struct type.
    /// </summary>
    /// <typeparam name="T">An individual struct type of a class with an explicit StructLayout.LayoutKind attribute.</typeparam>
    /// <returns>The size of the primitive or struct.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int SizeOf<T>()
        => SizeOf<T>(false);

    /// <summary>
    /// Returns the size of a specific primitive or struct type.
    /// </summary>
    /// <typeparam name="T">An individual struct type of a class with an explicit StructLayout.LayoutKind attribute.</typeparam>
    /// <param name="marshal">If set to true; will return the size of an element after marshalling.</param>
    /// <returns>The size of the primitive or struct.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int SizeOf<T>(bool marshal)
        => marshal ? Marshal.SizeOf<T>() : Unsafe.SizeOf<T>();

    /// <summary>
    /// Returns the size of a specific primitive or struct type.
    /// </summary>
    /// <typeparam name="T">An individual struct type of a class with an explicit StructLayout.LayoutKind attribute.</typeparam>
    /// <param name="elementCount">The number of array elements present.</param>
    /// <returns>The size of the primitive or struct array.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int SizeOf<T>(int elementCount) where T : unmanaged
        => SizeOf<T>() * elementCount;

    /// <summary>
    /// Returns the size of a specific primitive or struct type.
    /// </summary>
    /// <typeparam name="T">An individual struct type of a class with an explicit StructLayout.LayoutKind attribute.</typeparam>
    /// <param name="elementCount">The number of array elements present.</param>
    /// <param name="marshal">If set to true; will return the size of an element after marshalling.</param>
    /// <returns>The size of the primitive or struct array.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int SizeOf<T>(int elementCount, bool marshal)
        => SizeOf<T>(marshal) * elementCount;

    #endregion

    #region Game

    /// <summary>
    /// Allocate memory in the game's UI memory space.
    /// </summary>
    /// <param name="size">Amount of bytes to allocate.</param>
    /// <param name="alignment">The alignment of the allocation.</param>
    /// <returns>Pointer to the allocated region.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static nint GameAllocateUi(ulong size, ulong alignment = 0)
    {
        return new nint(IMemorySpace.GetUISpace()->Malloc(size, alignment));
    }

    /// <summary>
    /// Allocate memory in the game's default memory space.
    /// </summary>
    /// <param name="size">Amount of bytes to allocate.</param>
    /// <param name="alignment">The alignment of the allocation.</param>
    /// <returns>Pointer to the allocated region.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static nint GameAllocateDefault(ulong size, ulong alignment = 0)
    {
        return new nint(IMemorySpace.GetDefaultSpace()->Malloc(size, alignment));
    }

    /// <summary>
    /// Allocate memory in the game's animation memory space.
    /// </summary>
    /// <param name="size">Amount of bytes to allocate.</param>
    /// <param name="alignment">The alignment of the allocation.</param>
    /// <returns>Pointer to the allocated region.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static nint GameAllocateAnimation(ulong size, ulong alignment = 0)
    {
        return new nint(IMemorySpace.GetAnimationSpace()->Malloc(size, alignment));
    }

    /// <summary>
    /// Allocate memory in the game's apricot memory space.
    /// </summary>
    /// <param name="size">Amount of bytes to allocate.</param>
    /// <param name="alignment">The alignment of the allocation.</param>
    /// <returns>Pointer to the allocated region.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static nint GameAllocateApricot(ulong size, ulong alignment = 0)
    {
        return new nint(IMemorySpace.GetApricotSpace()->Malloc(size, alignment));
    }

    /// <summary>
    /// Allocate memory in the game's sound memory space.
    /// </summary>
    /// <param name="size">Amount of bytes to allocate.</param>
    /// <param name="alignment">The alignment of the allocation.</param>
    /// <returns>Pointer to the allocated region.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static nint GameAllocateSound(ulong size, ulong alignment = 0)
    {
        return new nint(IMemorySpace.GetSoundSpace()->Malloc(size, alignment));
    }

    /// <summary>
    /// Free memory in the game's memory space.
    /// </summary>
    /// <remarks>The memory you are freeing must be allocated with game allocators.</remarks>
    /// <param name="ptr">Position at which the memory to be freed is located.</param>
    /// <param name="size">Amount of bytes to free.</param>
    public static void GameFree(ref nint ptr, ulong size)
    {
        if (ptr == nint.Zero)
        {
            return;
        }

        IMemorySpace.Free((void*)ptr, size);
        ptr = nint.Zero;
    }

    #endregion

    #region Utility

    /// <summary>
    /// Null-terminate a byte array.
    /// </summary>
    /// <param name="bytes">The byte array to terminate.</param>
    /// <returns>The terminated byte array.</returns>
    public static byte[] NullTerminate(this byte[] bytes)
    {
        if (bytes.Length == 0 || bytes[^1] != 0)
        {
            var newBytes = new byte[bytes.Length + 1];
            Array.Copy(bytes, newBytes, bytes.Length);
            newBytes[^1] = 0;

            return newBytes;
        }

        return bytes;
    }

    #endregion
}
