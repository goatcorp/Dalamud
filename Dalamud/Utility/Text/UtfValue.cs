using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Dalamud.Utility.Text;

/// <summary>Represents a single value to be used in a UTF-N byte sequence.</summary>
[StructLayout(LayoutKind.Explicit, Size = 4)]
public readonly struct UtfValue : IEquatable<UtfValue>, IComparable<UtfValue>
{
    /// <summary>The unicode codepoint in <c>int</c>, that may not be in a valid range.</summary>
    [FieldOffset(0)]
    public readonly int IntValue;

    /// <summary>The unicode codepoint in <c>uint</c>, that may not be in a valid range.</summary>
    [FieldOffset(0)]
    public readonly uint UIntValue;

    /// <summary>Initializes a new instance of the <see cref="UtfValue"/> struct.</summary>
    /// <param name="value">The raw codepoint value.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UtfValue(uint value) => this.UIntValue = value;

    /// <inheritdoc cref="UtfValue(uint)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UtfValue(int value) => this.IntValue = value;

    /// <summary>Gets the length of this codepoint, encoded in UTF-8.</summary>
    public int Length8
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => GetEncodedLength8(this);
    }

    /// <summary>Gets the length of this codepoint, encoded in UTF-16.</summary>
    public int Length16
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => GetEncodedLength16(this);
    }

    /// <summary>Gets the short name, if supported.</summary>
    /// <returns>The buffer containing the short name, or empty if unsupported.</returns>
    public ReadOnlySpan<char> ShortName
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => GetShortName(this);
    }

    public static implicit operator uint(UtfValue c) => c.UIntValue;

    public static implicit operator int(UtfValue c) => c.IntValue;

    public static implicit operator UtfValue(byte c) => new(c);

    public static implicit operator UtfValue(sbyte c) => new(c);

    public static implicit operator UtfValue(ushort c) => new(c);

    public static implicit operator UtfValue(short c) => new(c);

    public static implicit operator UtfValue(uint c) => new(c);

    public static implicit operator UtfValue(int c) => new(c);

    public static implicit operator UtfValue(char c) => new(c);

    public static implicit operator UtfValue(Rune c) => new(c.Value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(UtfValue left, UtfValue right) => left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(UtfValue left, UtfValue right) => !left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <(UtfValue left, UtfValue right) => left.CompareTo(right) < 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >(UtfValue left, UtfValue right) => left.CompareTo(right) > 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <=(UtfValue left, UtfValue right) => left.CompareTo(right) <= 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >=(UtfValue left, UtfValue right) => left.CompareTo(right) >= 0;

    /// <summary>Gets the short name of the codepoint, for some select codepoints.</summary>
    /// <param name="codepoint">The codepoint.</param>
    /// <returns>The value.</returns>
    public static ReadOnlySpan<char> GetShortName(int codepoint) =>
        codepoint switch
        {
            0x00 => "NUL",
            0x01 => "SOH",
            0x02 => "STX",
            0x03 => "ETX",
            0x04 => "EOT",
            0x05 => "ENQ",
            0x06 => "ACK",
            0x07 => "BEL",
            0x08 => "BS",
            0x09 => "HT",
            0x0a => "LF",
            0x0b => "VT",
            0x0c => "FF",
            0x0d => "CR",
            0x0e => "SO",
            0x0f => "SI",

            0x10 => "DLE",
            0x11 => "DC1",
            0x12 => "DC2",
            0x13 => "DC3",
            0x14 => "DC4",
            0x15 => "NAK",
            0x16 => "SYN",
            0x17 => "SOH",
            0x18 => "CAN",
            0x19 => "EOM",
            0x1a => "SUB",
            0x1b => "ESC",
            0x1c => "FS",
            0x1d => "GS",
            0x1e => "RS",
            0x1f => "US",

            0x80 => "PAD",
            0x81 => "HOP",
            0x82 => "BPH",
            0x83 => "NBH",
            0x84 => "IND",
            0x85 => "NEL",
            0x86 => "SSA",
            0x87 => "ESA",
            0x88 => "HTS",
            0x89 => "HTJ",
            0x8a => "VTS",
            0x8b => "PLD",
            0x8c => "PLU",
            0x8d => "RI",
            0x8e => "SS2",
            0x8f => "SS3",

            0x90 => "DCS",
            0x91 => "PU1",
            0x92 => "PU2",
            0x93 => "STS",
            0x94 => "CCH",
            0x95 => "MW",
            0x96 => "SPA",
            0x97 => "EPA",
            0x98 => "SOS",
            0x99 => "SGC",
            0x9a => "SCI",
            0x9b => "CSI",
            0x9c => "ST",
            0x9d => "OSC",
            0x9e => "PM",
            0x9f => "APC",

            0xa0 => "NBSP",
            0xad => "SHY",

            _ => default,
        };

    /// <summary>Gets the length of the codepoint, when encoded in UTF-8.</summary>
    /// <param name="codepoint">The codepoint to encode.</param>
    /// <returns>The length.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetEncodedLength8(int codepoint) => (uint)codepoint switch
    {
        < 1u << 7 => 1,
        < 1u << 11 => 2,
        < 1u << 16 => 3,
        < 1u << 21 => 4,
        // Not a valid Unicode codepoint anymore below.
        < 1u << 26 => 5,
        < 1u << 31 => 6,
        _ => 7,
    };

    /// <summary>Gets the length of the codepoint, when encoded in UTF-16.</summary>
    /// <param name="codepoint">The codepoint to encode.</param>
    /// <returns>The length.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetEncodedLength16(int codepoint) => (uint)codepoint switch
    {
        < 0x10000 => 2,
        < 0x10000 + (1 << 20) => 4,
        // Not a valid Unicode codepoint anymore below.
        < 0x10000 + (1 << 30) => 6,
        _ => 8,
    };

    /// <inheritdoc cref="TryDecode8(ReadOnlySpan{byte}, out UtfValue, out int)"/>
    /// <remarks>Trims <paramref name="source"/> at beginning by <paramref name="length"/>.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryDecode8(ref ReadOnlySpan<byte> source, out UtfValue value, out int length)
    {
        var v = TryDecode8(source, out value, out length);
        source = source[length..];
        return v;
    }

    /// <summary>Attempts to decode a value from a UTF-8 byte sequence.</summary>
    /// <param name="source">The span to decode from.</param>
    /// <param name="value">The decoded value.</param>
    /// <param name="length">The length of the consumed bytes. <c>1</c> if sequence is broken.</param>
    /// <returns><c>true</c> if <paramref name="source"/> is successfully decoded.</returns>
    /// <remarks>Codepoints that results in <c>false</c> from <see cref="Rune.IsValid(int)"/> can still be returned,
    /// including unpaired surrogate characters, or codepoints above U+10FFFFF. This function returns a value only
    /// indicating whether the sequence could be decoded into a number, without being too short.</remarks>
    public static unsafe bool TryDecode8(ReadOnlySpan<byte> source, out UtfValue value, out int length)
    {
        if (source.IsEmpty)
        {
            value = default;
            length = 0;
            return false;
        }

        fixed (byte* ptr = source)
        {
            if ((ptr[0] & 0x80) == 0)
            {
                length = 1;
                value = ptr[0];
            }
            else if ((ptr[0] & 0b11100000) == 0b11000000 && source.Length >= 2
                     && ((uint)ptr[1] & 0b11000000) == 0b10000000)
            {
                length = 2;
                value = (((uint)ptr[0] & 0x1F) << 6) |
                        (((uint)ptr[1] & 0x3F) << 0);
            }
            else if (((uint)ptr[0] & 0b11110000) == 0b11100000 && source.Length >= 3
                     && ((uint)ptr[1] & 0b11000000) == 0b10000000
                     && ((uint)ptr[2] & 0b11000000) == 0b10000000)
            {
                length = 3;
                value = (((uint)ptr[0] & 0x0F) << 12) |
                        (((uint)ptr[1] & 0x3F) << 6) |
                        (((uint)ptr[2] & 0x3F) << 0);
            }
            else if (((uint)ptr[0] & 0b11111000) == 0b11110000 && source.Length >= 4
                     && ((uint)ptr[1] & 0b11000000) == 0b10000000
                     && ((uint)ptr[2] & 0b11000000) == 0b10000000
                     && ((uint)ptr[3] & 0b11000000) == 0b10000000)
            {
                length = 4;
                value = (((uint)ptr[0] & 0x07) << 18) |
                        (((uint)ptr[1] & 0x3F) << 12) |
                        (((uint)ptr[2] & 0x3F) << 6) |
                        (((uint)ptr[3] & 0x3F) << 0);
            }
            else if (((uint)ptr[0] & 0b11111100) == 0b11111000 && source.Length >= 5
                     && ((uint)ptr[1] & 0b11000000) == 0b10000000
                     && ((uint)ptr[2] & 0b11000000) == 0b10000000
                     && ((uint)ptr[3] & 0b11000000) == 0b10000000
                     && ((uint)ptr[4] & 0b11000000) == 0b10000000)
            {
                length = 5;
                value = (((uint)ptr[0] & 0x03) << 24) |
                        (((uint)ptr[1] & 0x3F) << 18) |
                        (((uint)ptr[2] & 0x3F) << 12) |
                        (((uint)ptr[3] & 0x3F) << 6) |
                        (((uint)ptr[4] & 0x3F) << 0);
            }
            else if (((uint)ptr[0] & 0b11111110) == 0b11111100 && source.Length >= 6
                     && ((uint)ptr[1] & 0b11000000) == 0b10000000
                     && ((uint)ptr[2] & 0b11000000) == 0b10000000
                     && ((uint)ptr[3] & 0b11000000) == 0b10000000
                     && ((uint)ptr[4] & 0b11000000) == 0b10000000
                     && ((uint)ptr[5] & 0b11000000) == 0b10000000)
            {
                length = 6;
                value = (((uint)ptr[0] & 0x01) << 30) |
                        (((uint)ptr[1] & 0x3F) << 24) |
                        (((uint)ptr[2] & 0x3F) << 18) |
                        (((uint)ptr[3] & 0x3F) << 12) |
                        (((uint)ptr[4] & 0x3F) << 6) |
                        (((uint)ptr[5] & 0x3F) << 0);
            }
            else if (((uint)ptr[0] & 0b11111111) == 0b11111110 && source.Length >= 7
                     && ((uint)ptr[1] & 0b11111100) == 0b10000000
                     && ((uint)ptr[2] & 0b11000000) == 0b10000000
                     && ((uint)ptr[3] & 0b11000000) == 0b10000000
                     && ((uint)ptr[4] & 0b11000000) == 0b10000000
                     && ((uint)ptr[5] & 0b11000000) == 0b10000000
                     && ((uint)ptr[6] & 0b11000000) == 0b10000000)
            {
                length = 7;
                value = (((uint)ptr[1] & 0x03) << 30) |
                        (((uint)ptr[2] & 0x3F) << 24) |
                        (((uint)ptr[3] & 0x3F) << 18) |
                        (((uint)ptr[4] & 0x3F) << 12) |
                        (((uint)ptr[5] & 0x3F) << 6) |
                        (((uint)ptr[6] & 0x3F) << 0);
            }
            else
            {
                length = 1;
                value = default;
                return false;
            }

            return true;
        }
    }

    /// <inheritdoc cref="TryDecode16(ReadOnlySpan{byte}, bool, out UtfValue, out int)"/>
    /// <remarks>Trims <paramref name="source"/> at beginning by <paramref name="length"/>.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryDecode16(ref ReadOnlySpan<byte> source, bool be, out UtfValue value, out int length)
    {
        var v = TryDecode16(source, be, out value, out length);
        source = source[length..];
        return v;
    }

    /// <summary>Attempts to decode a value from a UTF-16 byte sequence.</summary>
    /// <param name="source">The span to decode from.</param>
    /// <param name="be">Whether to use big endian.</param>
    /// <param name="value">The decoded value.</param>
    /// <param name="length">The length of the consumed bytes. <c>1</c> if cut short.
    /// <c>2</c> if sequence is broken.</param>
    /// <returns><c>true</c> if <paramref name="source"/> is successfully decoded.</returns>
    /// <remarks>Codepoints that results in <c>false</c> from <see cref="Rune.IsValid(int)"/> can still be returned,
    /// including unpaired surrogate characters, or codepoints above U+10FFFFF. This function returns a value only
    /// indicating whether the sequence could be decoded into a number, without being too short.</remarks>
    public static unsafe bool TryDecode16(ReadOnlySpan<byte> source, bool be, out UtfValue value, out int length)
    {
        if (source.Length < 2)
        {
            value = default;
            length = source.Length;
            return false;
        }

        fixed (byte* ptr = source)
        {
            var p16 = (ushort*)ptr;
            var val = be == BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(*p16) : *p16;
            if (char.IsHighSurrogate((char)val))
            {
                var lookahead1 = source.Length >= 4 ? p16[1] : 0;
                var lookahead2 = source.Length >= 6 ? p16[2] : 0;
                var lookahead3 = source.Length >= 8 ? p16[3] : 0;
                if (char.IsLowSurrogate((char)lookahead1))
                {
                    // Not a valid Unicode codepoint anymore inside the block below.
                    if (char.IsLowSurrogate((char)lookahead2))
                    {
                        if (char.IsLowSurrogate((char)lookahead3))
                        {
                            value = 0x10000
                                    + (((val & 0x3) << 30) |
                                       ((lookahead1 & 0x3FF) << 20) |
                                       ((lookahead2 & 0x3FF) << 10) |
                                       ((lookahead3 & 0x3FF) << 0));
                            length = 8;
                            return true;
                        }

                        value = 0x10000
                                + (((val & 0x3FF) << 20) |
                                   ((lookahead1 & 0x3FF) << 10) |
                                   ((lookahead2 & 0x3FF) << 0));
                        length = 6;
                        return true;
                    }

                    value = 0x10000 +
                            (((val & 0x3FF) << 10) |
                             ((lookahead1 & 0x3FF) << 0));
                    length = 4;
                    return true;
                }
            }

            // Calls are supposed to handle unpaired surrogates.
            value = val;
            length = 2;
            return true;
        }
    }

    /// <inheritdoc cref="TryDecode32(ReadOnlySpan{byte}, bool, out UtfValue, out int)"/>
    /// <remarks>Trims <paramref name="source"/> at beginning by <paramref name="length"/>.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryDecode32(ref ReadOnlySpan<byte> source, bool be, out UtfValue value, out int length)
    {
        var v = TryDecode32(source, be, out value, out length);
        source = source[length..];
        return v;
    }

    /// <summary>Attempts to decode a value from a UTF-32 byte sequence.</summary>
    /// <param name="source">The span to decode from.</param>
    /// <param name="be">Whether to use big endian.</param>
    /// <param name="value">The decoded value.</param>
    /// <param name="length">The length of the consumed bytes. <c>1 to 3</c> if cut short.
    /// <c>4</c> if sequence is broken.</param>
    /// <returns><c>true</c> if <paramref name="source"/> is successfully decoded.</returns>
    /// <remarks>Codepoints that results in <c>false</c> from <see cref="Rune.IsValid(int)"/> can still be returned,
    /// including unpaired surrogate characters, or codepoints above U+10FFFFF. This function returns a value only
    /// indicating whether the sequence could be decoded into a number, without being too short.</remarks>
    public static bool TryDecode32(ReadOnlySpan<byte> source, bool be, out UtfValue value, out int length)
    {
        if (source.Length < 4)
        {
            value = default;
            length = source.Length;
            return false;
        }

        length = 4;
        if ((be && BinaryPrimitives.TryReadInt32BigEndian(source, out var i32))
            || (!be && BinaryPrimitives.TryReadInt32LittleEndian(source, out i32)))
        {
            value = i32;
            return true;
        }
        
        value = default;
        return false;
    }

    /// <summary>Encodes the codepoint to the target in UTF-8.</summary>
    /// <param name="target">The target stream.</param>
    /// <param name="codepoint">The codepoint to encode.</param>
    /// <returns>The length of the encoded data.</returns>
    /// <remarks>Trims <paramref name="target"/> at beginning by the length.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Encode8(Stream target, int codepoint)
    {
        Span<byte> buf = stackalloc byte[7];
        Encode8(buf, codepoint, out var length);
        target.Write(buf[..length]);
        return length;
    }

    /// <summary>Encodes the codepoint to the target in UTF-8.</summary>
    /// <param name="target">The target byte span.</param>
    /// <param name="codepoint">The codepoint to encode.</param>
    /// <returns>The length of the encoded data.</returns>
    /// <remarks>Trims <paramref name="target"/> at beginning by the length.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Encode8(ref Span<byte> target, int codepoint)
    {
        target = Encode8(target, codepoint, out var length);
        return length;
    }

    /// <summary>Encodes the codepoint to the target in UTF-8.</summary>
    /// <param name="target">The optional target byte span.</param>
    /// <param name="codepoint">The codepoint to encode.</param>
    /// <param name="length">The length of the encoded data.</param>
    /// <returns>The remaning region of <paramref name="target"/>.</returns>
    public static Span<byte> Encode8(Span<byte> target, int codepoint, out int length)
    {
        var value = (uint)codepoint;
        length = GetEncodedLength8(codepoint);
        if (target.IsEmpty)
            return target;

        switch (length)
        {
            case 1:
                target[0] = (byte)value;
                return target[1..];
            case 2:
                target[0] = (byte)(0xC0 | ((value >> 6) & 0x1F));
                target[1] = (byte)(0x80 | ((value >> 0) & 0x3F));
                return target[2..];
            case 3:
                target[0] = (byte)(0xE0 | ((value >> 12) & 0x0F));
                target[1] = (byte)(0x80 | ((value >> 6) & 0x3F));
                target[2] = (byte)(0x80 | ((value >> 0) & 0x3F));
                return target[3..];
            case 4:
                target[0] = (byte)(0xF0 | ((value >> 18) & 0x07));
                target[1] = (byte)(0x80 | ((value >> 12) & 0x3F));
                target[2] = (byte)(0x80 | ((value >> 6) & 0x3F));
                target[3] = (byte)(0x80 | ((value >> 0) & 0x3F));
                return target[4..];
            case 5:
                target[0] = (byte)(0xF8 | ((value >> 24) & 0x03));
                target[1] = (byte)(0x80 | ((value >> 18) & 0x3F));
                target[2] = (byte)(0x80 | ((value >> 12) & 0x3F));
                target[3] = (byte)(0x80 | ((value >> 6) & 0x3F));
                target[4] = (byte)(0x80 | ((value >> 0) & 0x3F));
                return target[5..];
            case 6:
                target[0] = (byte)(0xFC | ((value >> 30) & 0x01));
                target[1] = (byte)(0x80 | ((value >> 24) & 0x3F));
                target[2] = (byte)(0x80 | ((value >> 18) & 0x3F));
                target[3] = (byte)(0x80 | ((value >> 12) & 0x3F));
                target[4] = (byte)(0x80 | ((value >> 6) & 0x3F));
                target[5] = (byte)(0x80 | ((value >> 0) & 0x3F));
                return target[6..];
            case 7:
                target[0] = 0xFE;
                target[1] = (byte)(0x80 | ((value >> 30) & 0x03));
                target[2] = (byte)(0x80 | ((value >> 24) & 0x3F));
                target[3] = (byte)(0x80 | ((value >> 18) & 0x3F));
                target[4] = (byte)(0x80 | ((value >> 12) & 0x3F));
                target[5] = (byte)(0x80 | ((value >> 6) & 0x3F));
                target[6] = (byte)(0x80 | ((value >> 0) & 0x3F));
                return target[7..];
            default:
                Debug.Assert(false, $"{nameof(Length8)} property should have produced all possible cases.");
                return target;
        }
    }

    /// <summary>Encodes the codepoint to the target in UTF-16.</summary>
    /// <param name="target">The target stream.</param>
    /// <param name="codepoint">The codepoint to encode.</param>
    /// <param name="be">Whether to use big endian.</param>
    /// <returns>The length of the encoded data.</returns>
    /// <remarks>Trims <paramref name="target"/> at beginning by the length.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Encode16(Stream target, int codepoint, bool be)
    {
        Span<byte> buf = stackalloc byte[8];
        Encode16(buf, codepoint, be, out var length);
        target.Write(buf[..length]);
        return length;
    }

    /// <summary>Encodes the codepoint to the target in UTF-16.</summary>
    /// <param name="target">The target byte span.</param>
    /// <param name="codepoint">The codepoint to encode.</param>
    /// <param name="be">Whether to use big endian.</param>
    /// <returns>The length of the encoded data.</returns>
    /// <remarks>Trims <paramref name="target"/> at beginning by the length.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Encode16(ref Span<byte> target, int codepoint, bool be)
    {
        target = Encode16(target, codepoint, be, out var length);
        return length;
    }

    /// <summary>Encodes the codepoint to the target in UTF-16.</summary>
    /// <param name="target">The optional target byte span.</param>
    /// <param name="codepoint">The codepoint to encode.</param>
    /// <param name="be">Whether to use big endian.</param>
    /// <param name="length">The length of the encoded data.</param>
    /// <returns>The remaning region of <paramref name="target"/>.</returns>
    public static Span<byte> Encode16(Span<byte> target, int codepoint, bool be, out int length)
    {
        var value = (uint)codepoint;
        length = GetEncodedLength16(codepoint);
        if (target.IsEmpty)
            return target;

        if (be)
        {
            switch (length)
            {
                case 2:
                    BinaryPrimitives.WriteUInt16BigEndian(target[0..], (ushort)value);
                    return target[2..];
                case 4:
                    value -= 0x10000;
                    BinaryPrimitives.WriteUInt16BigEndian(target[0..], (ushort)(0xD800 | ((value >> 10) & 0x3FF)));
                    BinaryPrimitives.WriteUInt16BigEndian(target[2..], (ushort)(0xDC00 | ((value >> 00) & 0x3FF)));
                    return target[4..];
                case 6:
                    value -= 0x10000;
                    BinaryPrimitives.WriteUInt16BigEndian(target[0..], (ushort)(0xD800 | ((value >> 20) & 0x3FF)));
                    BinaryPrimitives.WriteUInt16BigEndian(target[2..], (ushort)(0xDC00 | ((value >> 10) & 0x3FF)));
                    BinaryPrimitives.WriteUInt16BigEndian(target[4..], (ushort)(0xDC00 | ((value >> 00) & 0x3FF)));
                    return target[6..];
                case 8:
                    value -= 0x10000;
                    BinaryPrimitives.WriteUInt16BigEndian(target[0..], (ushort)(0xD800 | ((value >> 30) & 0x3)));
                    BinaryPrimitives.WriteUInt16BigEndian(target[2..], (ushort)(0xDC00 | ((value >> 20) & 0x3FF)));
                    BinaryPrimitives.WriteUInt16BigEndian(target[4..], (ushort)(0xDC00 | ((value >> 10) & 0x3FF)));
                    BinaryPrimitives.WriteUInt16BigEndian(target[6..], (ushort)(0xDC00 | ((value >> 00) & 0x3FF)));
                    return target[8..];
                default:
                    Debug.Assert(false, $"{nameof(Length16)} property should have produced all possible cases.");
                    return target;
            }
        }

        switch (length)
        {
            case 2:
                BinaryPrimitives.WriteUInt16LittleEndian(target[0..], (ushort)value);
                return target[2..];
            case 4:
                value -= 0x10000;
                BinaryPrimitives.WriteUInt16LittleEndian(target[0..], (ushort)(0xD800 | ((value >> 10) & 0x3FF)));
                BinaryPrimitives.WriteUInt16LittleEndian(target[2..], (ushort)(0xDC00 | ((value >> 00) & 0x3FF)));
                return target[4..];
            case 6:
                value -= 0x10000;
                BinaryPrimitives.WriteUInt16LittleEndian(target[0..], (ushort)(0xD800 | ((value >> 20) & 0x3FF)));
                BinaryPrimitives.WriteUInt16LittleEndian(target[2..], (ushort)(0xDC00 | ((value >> 10) & 0x3FF)));
                BinaryPrimitives.WriteUInt16LittleEndian(target[4..], (ushort)(0xDC00 | ((value >> 00) & 0x3FF)));
                return target[6..];
            case 8:
                value -= 0x10000;
                BinaryPrimitives.WriteUInt16LittleEndian(target[0..], (ushort)(0xD800 | ((value >> 30) & 0x3)));
                BinaryPrimitives.WriteUInt16LittleEndian(target[2..], (ushort)(0xDC00 | ((value >> 20) & 0x3FF)));
                BinaryPrimitives.WriteUInt16LittleEndian(target[4..], (ushort)(0xDC00 | ((value >> 10) & 0x3FF)));
                BinaryPrimitives.WriteUInt16LittleEndian(target[6..], (ushort)(0xDC00 | ((value >> 00) & 0x3FF)));
                return target[8..];
            default:
                Debug.Assert(false, $"{nameof(Length16)} property should have produced all possible cases.");
                return target;
        }
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(UtfValue other) => this.IntValue.CompareTo(other.IntValue);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(UtfValue other) => this.IntValue == other.IntValue;

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj) => obj is UtfValue other && this.Equals(other);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => this.IntValue;

    /// <summary>Attempts to get the corresponding rune.</summary>
    /// <param name="rune">The retrieved rune.</param>
    /// <returns><c>true</c> if retrieved.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetRune(out Rune rune)
    {
        if (Rune.IsValid(this.IntValue))
        {
            rune = new(this.IntValue);
            return true;
        }

        rune = default;
        return false;
    }

    /// <summary>Encodes the codepoint to the target.</summary>
    /// <param name="target">The target byte span.</param>
    /// <returns>The remaning region of <paramref name="target"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> Encode8(Span<byte> target) => Encode8(target, this, out _);
}
