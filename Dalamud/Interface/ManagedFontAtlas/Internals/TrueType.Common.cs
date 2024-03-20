using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text;

namespace Dalamud.Interface.ManagedFontAtlas.Internals;

/// <summary>
/// Deals with TrueType.
/// </summary>
internal static partial class TrueTypeUtils
{
    private struct Fixed : IComparable<Fixed>
    {
        public ushort Major;
        public ushort Minor;

        public Fixed(ushort major, ushort minor)
        {
            this.Major = major;
            this.Minor = minor;
        }

        public Fixed(PointerSpan<byte> span)
        {
            var offset = 0;
            span.ReadBig(ref offset, out this.Major);
            span.ReadBig(ref offset, out this.Minor);
        }

        public int CompareTo(Fixed other)
        {
            var majorComparison = this.Major.CompareTo(other.Major);
            return majorComparison != 0 ? majorComparison : this.Minor.CompareTo(other.Minor);
        }
    }

    private struct KerningPair : IEquatable<KerningPair>
    {
        public ushort Left;
        public ushort Right;
        public short Value;

        public KerningPair(PointerSpan<byte> span)
        {
            var offset = 0;
            span.ReadBig(ref offset, out this.Left);
            span.ReadBig(ref offset, out this.Right);
            span.ReadBig(ref offset, out this.Value);
        }

        public KerningPair(ushort left, ushort right, short value)
        {
            this.Left = left;
            this.Right = right;
            this.Value = value;
        }

        public static bool operator ==(KerningPair left, KerningPair right) => left.Equals(right);

        public static bool operator !=(KerningPair left, KerningPair right) => !left.Equals(right);

        public static KerningPair ReverseEndianness(KerningPair pair) => new()
        {
            Left = BinaryPrimitives.ReverseEndianness(pair.Left),
            Right = BinaryPrimitives.ReverseEndianness(pair.Right),
            Value = BinaryPrimitives.ReverseEndianness(pair.Value),
        };

        public bool Equals(KerningPair other) =>
            this.Left == other.Left && this.Right == other.Right && this.Value == other.Value;

        public override bool Equals(object? obj) => obj is KerningPair other && this.Equals(other);

        public override int GetHashCode() => HashCode.Combine(this.Left, this.Right, this.Value);

        public override string ToString() => $"KerningPair[{this.Left}, {this.Right}] = {this.Value}";
    }

    [StructLayout(LayoutKind.Explicit, Size = 4)]
    private struct PlatformAndEncoding
    {
        [FieldOffset(0)]
        public PlatformId Platform;

        [FieldOffset(2)]
        public UnicodeEncodingId UnicodeEncoding;

        [FieldOffset(2)]
        public MacintoshEncodingId MacintoshEncoding;

        [FieldOffset(2)]
        public IsoEncodingId IsoEncoding;

        [FieldOffset(2)]
        public WindowsEncodingId WindowsEncoding;

        public PlatformAndEncoding(PointerSpan<byte> source)
        {
            var offset = 0;
            source.ReadBig(ref offset, out this.Platform);
            source.ReadBig(ref offset, out this.UnicodeEncoding);
        }

        public static PlatformAndEncoding ReverseEndianness(PlatformAndEncoding value) => new()
        {
            Platform = (PlatformId)BinaryPrimitives.ReverseEndianness((ushort)value.Platform),
            UnicodeEncoding = (UnicodeEncodingId)BinaryPrimitives.ReverseEndianness((ushort)value.UnicodeEncoding),
        };

        public readonly string Decode(Span<byte> data)
        {
            switch (this.Platform)
            {
                case PlatformId.Unicode:
                    switch (this.UnicodeEncoding)
                    {
                        case UnicodeEncodingId.Unicode_2_0_Bmp:
                        case UnicodeEncodingId.Unicode_2_0_Full:
                            return Encoding.BigEndianUnicode.GetString(data);
                    }

                    break;

                case PlatformId.Macintosh:
                    switch (this.MacintoshEncoding)
                    {
                        case MacintoshEncodingId.Roman:
                            return Encoding.ASCII.GetString(data);
                    }

                    break;

                case PlatformId.Windows:
                    switch (this.WindowsEncoding)
                    {
                        case WindowsEncodingId.Symbol:
                        case WindowsEncodingId.UnicodeBmp:
                        case WindowsEncodingId.UnicodeFullRepertoire:
                            return Encoding.BigEndianUnicode.GetString(data);
                    }

                    break;
            }

            throw new NotSupportedException();
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct TagStruct : IEquatable<TagStruct>, IComparable<TagStruct>
    {
        [FieldOffset(0)]
        public unsafe fixed byte Tag[4];

        [FieldOffset(0)]
        public uint NativeValue;

        public unsafe TagStruct(char c1, char c2, char c3, char c4)
        {
            this.Tag[0] = checked((byte)c1);
            this.Tag[1] = checked((byte)c2);
            this.Tag[2] = checked((byte)c3);
            this.Tag[3] = checked((byte)c4);
        }

        public unsafe TagStruct(PointerSpan<byte> span)
        {
            this.Tag[0] = span[0];
            this.Tag[1] = span[1];
            this.Tag[2] = span[2];
            this.Tag[3] = span[3];
        }

        public unsafe TagStruct(ReadOnlySpan<byte> span)
        {
            this.Tag[0] = span[0];
            this.Tag[1] = span[1];
            this.Tag[2] = span[2];
            this.Tag[3] = span[3];
        }

        public unsafe byte this[int index]
        {
            get => this.Tag[index];
            set => this.Tag[index] = value;
        }

        public static bool operator ==(TagStruct left, TagStruct right) => left.Equals(right);

        public static bool operator !=(TagStruct left, TagStruct right) => !left.Equals(right);

        public bool Equals(TagStruct other) => this.NativeValue == other.NativeValue;

        public override bool Equals(object? obj) => obj is TagStruct other && this.Equals(other);

        public override int GetHashCode() => (int)this.NativeValue;

        public int CompareTo(TagStruct other) => this.NativeValue.CompareTo(other.NativeValue);

        public override unsafe string ToString() =>
            $"0x{this.NativeValue:08X} \"{(char)this.Tag[0]}{(char)this.Tag[1]}{(char)this.Tag[2]}{(char)this.Tag[3]}\"";
    }
}
