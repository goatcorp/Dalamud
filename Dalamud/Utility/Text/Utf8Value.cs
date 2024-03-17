using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Dalamud.Utility.Text;

/// <summary>Represents a single value to be used in a UTF-8 byte sequence.</summary>
[StructLayout(LayoutKind.Explicit, Size = 4)]
public readonly struct Utf8Value : IEquatable<Utf8Value>, IComparable<Utf8Value>
{
    /// <summary>The unicode codepoint in <c>int</c>, that may not be in a valid range.</summary>
    [FieldOffset(0)]
    public readonly int IntValue;

    /// <summary>The unicode codepoint in <c>uint</c>, that may not be in a valid range.</summary>
    [FieldOffset(0)]
    public readonly uint UIntValue;

    /// <summary>Initializes a new instance of the <see cref="Utf8Value"/> struct.</summary>
    /// <param name="value">The raw codepoint value.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Utf8Value(uint value) => this.UIntValue = value;

    /// <inheritdoc cref="Utf8Value(uint)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Utf8Value(int value) => this.IntValue = value;

    /// <summary>Gets the length of this codepoint, encoded in UTF-8.</summary>
    public int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => GetEncodedLength(this);
    }

    /// <summary>Gets the short name, if supported.</summary>
    /// <returns>The buffer containing the short name, or empty if unsupported.</returns>
    public ReadOnlySpan<char> ShortName
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => GetShortName(this);
    }

    public static implicit operator uint(Utf8Value c) => c.UIntValue;

    public static implicit operator int(Utf8Value c) => c.IntValue;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Utf8Value left, Utf8Value right) => left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Utf8Value left, Utf8Value right) => !left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <(Utf8Value left, Utf8Value right) => left.CompareTo(right) < 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >(Utf8Value left, Utf8Value right) => left.CompareTo(right) > 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <=(Utf8Value left, Utf8Value right) => left.CompareTo(right) <= 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >=(Utf8Value left, Utf8Value right) => left.CompareTo(right) >= 0;

    /// <summary>Gets the short name of the codepoint.</summary>
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

    /// <summary>Gets the length of the codepoint, when encoded.</summary>
    /// <param name="codepoint">The codepoint to encode.</param>
    /// <returns>The length.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetEncodedLength(int codepoint) => (uint)codepoint switch
    {
        < 1u << 7 => 1,
        < 1u << 11 => 2,
        < 1u << 16 => 3,
        < 1u << 21 => 4,
        < 1u << 26 => 5,
        < 1u << 31 => 6,
        _ => 7,
    };

    /// <inheritdoc cref="TryDecode(ReadOnlySpan{byte}, out Utf8Value, out int)"/>
    /// <remarks>Trims <paramref name="source"/> at beginning by <paramref name="length"/>.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryDecode(ref ReadOnlySpan<byte> source, out Utf8Value value, out int length)
    {
        var v = TryDecode(source, out value, out length);
        source = source[length..];
        return v;
    }

    /// <summary>Attempts to decode a value from a UTF-8 byte sequence.</summary>
    /// <param name="source">The span to decode from.</param>
    /// <param name="value">The decoded value.</param>
    /// <param name="length">The length of the consumed value. <c>1</c> if sequence is broken.</param>
    /// <returns><c>true</c> if <paramref name="source"/> is successfully decoded.</returns>
    public static unsafe bool TryDecode(ReadOnlySpan<byte> source, out Utf8Value value, out int length)
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
                value = new(ptr[0]);
            }
            else if ((ptr[0] & 0b11100000) == 0b11000000 && source.Length >= 2
                     && ((uint)ptr[1] & 0b11000000) == 0b10000000)
            {
                length = 2;
                value = new(
                    (((uint)ptr[0] & 0x1F) << 6) |
                    (((uint)ptr[1] & 0x3F) << 0));
            }
            else if (((uint)ptr[0] & 0b11110000) == 0b11100000 && source.Length >= 3
                     && ((uint)ptr[1] & 0b11000000) == 0b10000000
                     && ((uint)ptr[2] & 0b11000000) == 0b10000000)
            {
                length = 3;
                value = new(
                    (((uint)ptr[0] & 0x0F) << 12) |
                    (((uint)ptr[1] & 0x3F) << 6) |
                    (((uint)ptr[2] & 0x3F) << 0));
            }
            else if (((uint)ptr[0] & 0b11111000) == 0b11110000 && source.Length >= 4
                     && ((uint)ptr[1] & 0b11000000) == 0b10000000
                     && ((uint)ptr[2] & 0b11000000) == 0b10000000
                     && ((uint)ptr[3] & 0b11000000) == 0b10000000)
            {
                length = 4;
                value = new(
                    (((uint)ptr[0] & 0x07) << 18) |
                    (((uint)ptr[1] & 0x3F) << 12) |
                    (((uint)ptr[2] & 0x3F) << 6) |
                    (((uint)ptr[3] & 0x3F) << 0));
            }
            else if (((uint)ptr[0] & 0b11111100) == 0b11111000 && source.Length >= 5
                     && ((uint)ptr[1] & 0b11000000) == 0b10000000
                     && ((uint)ptr[2] & 0b11000000) == 0b10000000
                     && ((uint)ptr[3] & 0b11000000) == 0b10000000
                     && ((uint)ptr[4] & 0b11000000) == 0b10000000)
            {
                length = 5;
                value = new(
                    (((uint)ptr[0] & 0x03) << 24) |
                    (((uint)ptr[1] & 0x3F) << 18) |
                    (((uint)ptr[2] & 0x3F) << 12) |
                    (((uint)ptr[3] & 0x3F) << 6) |
                    (((uint)ptr[4] & 0x3F) << 0));
            }
            else if (((uint)ptr[0] & 0b11111110) == 0b11111100 && source.Length >= 6
                     && ((uint)ptr[1] & 0b11000000) == 0b10000000
                     && ((uint)ptr[2] & 0b11000000) == 0b10000000
                     && ((uint)ptr[3] & 0b11000000) == 0b10000000
                     && ((uint)ptr[4] & 0b11000000) == 0b10000000
                     && ((uint)ptr[5] & 0b11000000) == 0b10000000)
            {
                length = 6;
                value = new(
                    (((uint)ptr[0] & 0x01) << 30) |
                    (((uint)ptr[1] & 0x3F) << 24) |
                    (((uint)ptr[2] & 0x3F) << 18) |
                    (((uint)ptr[3] & 0x3F) << 12) |
                    (((uint)ptr[4] & 0x3F) << 6) |
                    (((uint)ptr[5] & 0x3F) << 0));
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
                value = new(
                    (((uint)ptr[1] & 0x03) << 30) |
                    (((uint)ptr[2] & 0x3F) << 24) |
                    (((uint)ptr[3] & 0x3F) << 18) |
                    (((uint)ptr[4] & 0x3F) << 12) |
                    (((uint)ptr[5] & 0x3F) << 6) |
                    (((uint)ptr[6] & 0x3F) << 0));
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

    /// <summary>Encodes the codepoint to the target.</summary>
    /// <param name="target">The target stream.</param>
    /// <param name="codepoint">The codepoint to encode.</param>
    /// <returns>The length of the encoded data.</returns>
    /// <remarks>Trims <paramref name="target"/> at beginning by the length.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Encode(Stream target, int codepoint)
    {
        Span<byte> buf = stackalloc byte[7];
        Encode(buf, codepoint, out var length);
        target.Write(buf[..length]);
        return length;
    }

    /// <summary>Encodes the codepoint to the target.</summary>
    /// <param name="target">The target byte span.</param>
    /// <param name="codepoint">The codepoint to encode.</param>
    /// <returns>The length of the encoded data.</returns>
    /// <remarks>Trims <paramref name="target"/> at beginning by the length.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Encode(ref Span<byte> target, int codepoint)
    {
        target = Encode(target, codepoint, out var length);
        return length;
    }

    /// <summary>Encodes the codepoint to the target.</summary>
    /// <param name="target">The optional target byte span.</param>
    /// <param name="codepoint">The codepoint to encode.</param>
    /// <param name="length">The length of the encoded data.</param>
    /// <returns>The remaning region of <paramref name="target"/>.</returns>
    public static Span<byte> Encode(Span<byte> target, int codepoint, out int length)
    {
        var value = (uint)codepoint;
        length = GetEncodedLength(codepoint);
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
                Debug.Assert(false, "Length property should have produced all possible cases.");
                return target;
        }
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(Utf8Value other) => this.IntValue.CompareTo(other.IntValue);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Utf8Value other) => this.IntValue == other.IntValue;

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj) => obj is Utf8Value other && this.Equals(other);

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
    public Span<byte> Encode(Span<byte> target) => Encode(target, this, out _);

    /// <summary>Appends the string representation of this codepoint to the given string builder.</summary>
    /// <param name="sb">The string builder to append to.</param>
    public void AppendRepresentationTo(StringBuilder sb)
    {
        if (!Rune.IsValid(this.UIntValue) || Rune.IsControl(new(this)))
        {
            switch (this.UIntValue)
            {
                case < 0x100:
                    sb.Append($"\\x{this.UIntValue:X02}");
                    break;
                case < 0x10000:
                    sb.Append($"\\u{this.UIntValue:X04}");
                    break;
                default:
                    sb.Append($"\\U{this.UIntValue:X08}");
                    break;
            }

            return;
        }

        switch (this.UIntValue)
        {
            case 0x0027:
                sb.Append("\\'");
                break;
            case 0x0022:
                sb.Append("\\\"");
                break;
            case 0x005C:
                sb.Append(@"\\");
                break;
            case 0x0000:
                sb.Append("\\0");
                break;
            case 0x0007:
                sb.Append("\\a");
                break;
            case 0x0008:
                sb.Append("\\b");
                break;
            case 0x000C:
                sb.Append("\\f");
                break;
            case 0x000A:
                sb.Append("\\n");
                break;
            case 0x000D:
                sb.Append("\\r");
                break;
            case 0x0009:
                sb.Append("\\t");
                break;
            case 0x000B:
                sb.Append("\\v");
                break;
            case < 0x20:
                sb.Append($"\\x{this.UIntValue:X02}");
                break;
            default:
                var rune = new Rune(this);
                Span<char> buf = stackalloc char[2];
                sb.Append(buf[..rune.EncodeToUtf16(buf)]);
                break;
        }
    }
}
