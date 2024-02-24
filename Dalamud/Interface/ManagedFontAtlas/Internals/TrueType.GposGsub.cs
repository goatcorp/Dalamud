using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Dalamud.Interface.ManagedFontAtlas.Internals;

/// <summary>
/// Deals with TrueType.
/// </summary>
internal static partial class TrueTypeUtils
{
    [Flags]
    private enum LookupFlags : byte
    {
        RightToLeft = 1 << 0,
        IgnoreBaseGlyphs = 1 << 1,
        IgnoreLigatures = 1 << 2,
        IgnoreMarks = 1 << 3,
        UseMarkFilteringSet = 1 << 4,
    }

    private enum LookupType : ushort
    {
        SingleAdjustment = 1,
        PairAdjustment = 2,
        CursiveAttachment = 3,
        MarkToBaseAttachment = 4,
        MarkToLigatureAttachment = 5,
        MarkToMarkAttachment = 6,
        ContextPositioning = 7,
        ChainedContextPositioning = 8,
        ExtensionPositioning = 9,
    }

    private readonly struct ClassDefTable
    {
        public readonly PointerSpan<byte> Memory;

        public ClassDefTable(PointerSpan<byte> memory) => this.Memory = memory;

        public ushort Format => this.Memory.ReadU16Big(0);

        public Format1ClassArray Format1 => new(this.Memory);

        public Format2ClassRanges Format2 => new(this.Memory);

        public IEnumerable<(ushort Class, ushort GlyphId)> Enumerate()
        {
            switch (this.Format)
            {
                case 1:
                {
                    var format1 = this.Format1;
                    var startId = format1.StartGlyphId;
                    var count = format1.GlyphCount;
                    var classes = format1.ClassValueArray;
                    for (var i = 0; i < count; i++)
                        yield return (classes[i], (ushort)(i + startId));

                    break;
                }

                case 2:
                {
                    foreach (var range in this.Format2.ClassValueArray)
                    {
                        var @class = range.Class;
                        var startId = range.StartGlyphId;
                        var count = range.EndGlyphId - startId + 1;
                        for (var i = 0; i < count; i++)
                            yield return (@class, (ushort)(startId + i));
                    }

                    break;
                }
            }
        }

        [Pure]
        public ushort GetClass(ushort glyphId)
        {
            switch (this.Format)
            {
                case 1:
                {
                    var format1 = this.Format1;
                    var startId = format1.StartGlyphId;
                    if (startId <= glyphId && glyphId < startId + format1.GlyphCount)
                        return this.Format1.ClassValueArray[glyphId - startId];

                    break;
                }

                case 2:
                {
                    var rangeSpan = this.Format2.ClassValueArray;
                    var i = rangeSpan.BinarySearch(new Format2ClassRanges.ClassRangeRecord { EndGlyphId = glyphId });
                    if (i >= 0 && rangeSpan[i].ContainsGlyph(glyphId))
                        return rangeSpan[i].Class;

                    break;
                }
            }

            return 0;
        }

        public readonly struct Format1ClassArray
        {
            public readonly PointerSpan<byte> Memory;

            public Format1ClassArray(PointerSpan<byte> memory) => this.Memory = memory;

            public ushort Format => this.Memory.ReadU16Big(0);

            public ushort StartGlyphId => this.Memory.ReadU16Big(2);

            public ushort GlyphCount => this.Memory.ReadU16Big(4);

            public BigEndianPointerSpan<ushort> ClassValueArray => new(
                this.Memory[6..].As<ushort>(this.GlyphCount),
                BinaryPrimitives.ReverseEndianness);
        }

        public readonly struct Format2ClassRanges
        {
            public readonly PointerSpan<byte> Memory;

            public Format2ClassRanges(PointerSpan<byte> memory) => this.Memory = memory;

            public ushort ClassRangeCount => this.Memory.ReadU16Big(2);

            public BigEndianPointerSpan<ClassRangeRecord> ClassValueArray => new(
                this.Memory[4..].As<ClassRangeRecord>(this.ClassRangeCount),
                ClassRangeRecord.ReverseEndianness);

            public struct ClassRangeRecord : IComparable<ClassRangeRecord>
            {
                public ushort StartGlyphId;
                public ushort EndGlyphId;
                public ushort Class;

                public static ClassRangeRecord ReverseEndianness(ClassRangeRecord value) => new()
                {
                    StartGlyphId = BinaryPrimitives.ReverseEndianness(value.StartGlyphId),
                    EndGlyphId = BinaryPrimitives.ReverseEndianness(value.EndGlyphId),
                    Class = BinaryPrimitives.ReverseEndianness(value.Class),
                };

                public int CompareTo(ClassRangeRecord other) => this.EndGlyphId.CompareTo(other.EndGlyphId);

                public bool ContainsGlyph(ushort glyphId) =>
                    this.StartGlyphId <= glyphId && glyphId <= this.EndGlyphId;
            }
        }
    }

    private readonly struct CoverageTable
    {
        public readonly PointerSpan<byte> Memory;

        public CoverageTable(PointerSpan<byte> memory) => this.Memory = memory;

        public enum CoverageFormat : ushort
        {
            Glyphs = 1,
            RangeRecords = 2,
        }

        public CoverageFormat Format => this.Memory.ReadEnumBig<CoverageFormat>(0);

        public ushort Count => this.Memory.ReadU16Big(2);

        public BigEndianPointerSpan<ushort> Glyphs =>
            this.Format == CoverageFormat.Glyphs
                ? new(this.Memory[4..].As<ushort>(this.Count), BinaryPrimitives.ReverseEndianness)
                : default(BigEndianPointerSpan<ushort>);

        public BigEndianPointerSpan<RangeRecord> RangeRecords =>
            this.Format == CoverageFormat.RangeRecords
                ? new(this.Memory[4..].As<RangeRecord>(this.Count), RangeRecord.ReverseEndianness)
                : default(BigEndianPointerSpan<RangeRecord>);

        public int GetCoverageIndex(ushort glyphId)
        {
            switch (this.Format)
            {
                case CoverageFormat.Glyphs:
                    return this.Glyphs.BinarySearch(glyphId);

                case CoverageFormat.RangeRecords:
                {
                    var index = this.RangeRecords.BinarySearch(
                        (in RangeRecord record) => glyphId.CompareTo(record.EndGlyphId));

                    if (index >= 0 && this.RangeRecords[index].ContainsGlyph(glyphId))
                        return index;

                    return -1;
                }

                default:
                    return -1;
            }
        }

        public struct RangeRecord
        {
            public ushort StartGlyphId;
            public ushort EndGlyphId;
            public ushort StartCoverageIndex;

            public static RangeRecord ReverseEndianness(RangeRecord value) => new()
            {
                StartGlyphId = BinaryPrimitives.ReverseEndianness(value.StartGlyphId),
                EndGlyphId = BinaryPrimitives.ReverseEndianness(value.EndGlyphId),
                StartCoverageIndex = BinaryPrimitives.ReverseEndianness(value.StartCoverageIndex),
            };

            public bool ContainsGlyph(ushort glyphId) =>
                this.StartGlyphId <= glyphId && glyphId <= this.EndGlyphId;
        }
    }

    private readonly struct LookupTable : IEnumerable<PointerSpan<byte>>
    {
        public readonly PointerSpan<byte> Memory;

        public LookupTable(PointerSpan<byte> memory) => this.Memory = memory;

        public LookupType Type => this.Memory.ReadEnumBig<LookupType>(0);

        public byte MarkAttachmentType => this.Memory[2];

        public LookupFlags Flags => (LookupFlags)this.Memory[3];

        public ushort SubtableCount => this.Memory.ReadU16Big(4);

        public BigEndianPointerSpan<ushort> SubtableOffsets => new(
            this.Memory[6..].As<ushort>(this.SubtableCount),
            BinaryPrimitives.ReverseEndianness);

        public PointerSpan<byte> this[int index] => this.Memory[this.SubtableOffsets[this.EnsureIndex(index)] ..];

        public IEnumerator<PointerSpan<byte>> GetEnumerator()
        {
            foreach (var i in Enumerable.Range(0, this.SubtableCount))
                yield return this.Memory[this.SubtableOffsets[i] ..];
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        private int EnsureIndex(int index) => index >= 0 && index < this.SubtableCount
                                                  ? index
                                                  : throw new IndexOutOfRangeException();
    }
}
