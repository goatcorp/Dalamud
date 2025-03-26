using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Dalamud.Interface.ManagedFontAtlas.Internals;

/// <summary>
/// Deals with TrueType.
/// </summary>
[SuppressMessage("ReSharper", "NotAccessedField.Local", Justification = "TrueType specification defined fields")]
[SuppressMessage("ReSharper", "UnusedType.Local", Justification = "TrueType specification defined types")]
[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "Internal")]
[SuppressMessage(
    "StyleCop.CSharp.NamingRules",
    "SA1310:Field names should not contain underscore",
    Justification = "Version name")]
[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Version name")]
internal static partial class TrueTypeUtils
{
    private readonly struct SfntFile : IReadOnlyDictionary<TagStruct, PointerSpan<byte>>
    {
        // http://formats.kaitai.io/ttf/ttf.svg

        public static readonly TagStruct FileTagTrueType1 = new('1', '\0', '\0', '\0');
        public static readonly TagStruct FileTagType1 = new('t', 'y', 'p', '1');
        public static readonly TagStruct FileTagOpenTypeWithCff = new('O', 'T', 'T', 'O');
        public static readonly TagStruct FileTagOpenType1_0 = new('\0', '\x01', '\0', '\0');
        public static readonly TagStruct FileTagTrueTypeApple = new('t', 'r', 'u', 'e');

        public readonly PointerSpan<byte> Memory;
        public readonly int OffsetInCollection;
        public readonly ushort TableCount;

        public SfntFile(PointerSpan<byte> memory, int offsetInCollection = 0)
        {
            var span = memory.Span;
            this.Memory = memory;
            this.OffsetInCollection = offsetInCollection;
            this.TableCount = BinaryPrimitives.ReadUInt16BigEndian(span[4..]);
        }

        public int Count => this.TableCount;

        public IEnumerable<TagStruct> Keys => this.Select(x => x.Key);

        public IEnumerable<PointerSpan<byte>> Values => this.Select(x => x.Value);

        public PointerSpan<byte> this[TagStruct key] => this.First(x => x.Key == key).Value;

        public IEnumerator<KeyValuePair<TagStruct, PointerSpan<byte>>> GetEnumerator()
        {
            var offset = 12;
            for (var i = 0; i < this.TableCount; i++)
            {
                var dte = new DirectoryTableEntry(this.Memory[offset..]);
                yield return new(dte.Tag, this.Memory.Slice(dte.Offset - this.OffsetInCollection, dte.Length));

                offset += Unsafe.SizeOf<DirectoryTableEntry>();
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        public bool ContainsKey(TagStruct key) => this.Any(x => x.Key == key);

        public bool TryGetValue(TagStruct key, out PointerSpan<byte> value)
        {
            foreach (var (k, v) in this)
            {
                if (k == key)
                {
                    value = v;
                    return true;
                }
            }

            value = default;
            return false;
        }

        public readonly struct DirectoryTableEntry
        {
            public readonly PointerSpan<byte> Memory;

            public DirectoryTableEntry(PointerSpan<byte> span) => this.Memory = span;

            public TagStruct Tag => new(this.Memory);

            public uint Checksum => this.Memory.ReadU32Big(4);

            public int Offset => this.Memory.ReadI32Big(8);

            public int Length => this.Memory.ReadI32Big(12);
        }
    }

    private readonly struct TtcFile : IReadOnlyList<SfntFile>
    {
        public static readonly TagStruct FileTag = new('t', 't', 'c', 'f');

        public readonly PointerSpan<byte> Memory;
        public readonly TagStruct Tag;
        public readonly ushort MajorVersion;
        public readonly ushort MinorVersion;
        public readonly int FontCount;

        public TtcFile(PointerSpan<byte> memory)
        {
            var span = memory.Span;
            this.Memory = memory;
            this.Tag = new(span);
            if (this.Tag != FileTag)
                throw new InvalidOperationException();

            this.MajorVersion = BinaryPrimitives.ReadUInt16BigEndian(span[4..]);
            this.MinorVersion = BinaryPrimitives.ReadUInt16BigEndian(span[6..]);
            this.FontCount = BinaryPrimitives.ReadInt32BigEndian(span[8..]);
        }

        public int Count => this.FontCount;

        public SfntFile this[int index]
        {
            get
            {
                if (index < 0 || index >= this.FontCount)
                {
                    throw new IndexOutOfRangeException(
                        $"The requested font #{index} does not exist in this .ttc file.");
                }

                var offset = BinaryPrimitives.ReadInt32BigEndian(this.Memory.Span[(12 + 4 * index)..]);
                return new(this.Memory[offset..], offset);
            }
        }

        public IEnumerator<SfntFile> GetEnumerator()
        {
            for (var i = 0; i < this.FontCount; i++)
                yield return this[i];
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}
