using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Dalamud.Interface.ManagedFontAtlas.Internals;

/// <summary>
/// Deals with TrueType.
/// </summary>
[SuppressMessage("ReSharper", "NotAccessedField.Local", Justification = "TrueType specification defined fields")]
[SuppressMessage("ReSharper", "UnusedType.Local", Justification = "TrueType specification defined types")]
[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "Internal")]
internal static partial class TrueTypeUtils
{
    [Flags]
    private enum ValueFormat : ushort
    {
        PlacementX = 1 << 0,
        PlacementY = 1 << 1,
        AdvanceX = 1 << 2,
        AdvanceY = 1 << 3,
        PlacementDeviceOffsetX = 1 << 4,
        PlacementDeviceOffsetY = 1 << 5,
        AdvanceDeviceOffsetX = 1 << 6,
        AdvanceDeviceOffsetY = 1 << 7,

        ValidBits = 0
                    | PlacementX | PlacementY
                    | AdvanceX | AdvanceY
                    | PlacementDeviceOffsetX | PlacementDeviceOffsetY
                    | AdvanceDeviceOffsetX | AdvanceDeviceOffsetY,
    }

    private static int NumBytes(this ValueFormat value) =>
        ushort.PopCount((ushort)(value & ValueFormat.ValidBits)) * 2;

    private readonly struct Cmap
    {
        // https://docs.microsoft.com/en-us/typography/opentype/spec/cmap
        // https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6cmap.html

        public static readonly TagStruct DirectoryTableTag = new('c', 'm', 'a', 'p');

        public readonly PointerSpan<byte> Memory;

        public Cmap(SfntFile file)
            : this(file[DirectoryTableTag])
        {
        }

        public Cmap(PointerSpan<byte> memory) => this.Memory = memory;

        public ushort Version => this.Memory.ReadU16Big(0);

        public ushort RecordCount => this.Memory.ReadU16Big(2);

        public BigEndianPointerSpan<EncodingRecord> Records => new(
            this.Memory[4..].As<EncodingRecord>(this.RecordCount),
            EncodingRecord.ReverseEndianness);

        public EncodingRecord? UnicodeEncodingRecord =>
            this.Records.Select(x => (EncodingRecord?)x).FirstOrDefault(
                x => x!.Value.PlatformAndEncoding is
                         { Platform: PlatformId.Unicode, UnicodeEncoding: UnicodeEncodingId.Unicode_2_0_Bmp })
            ??
            this.Records.Select(x => (EncodingRecord?)x).FirstOrDefault(
                x => x!.Value.PlatformAndEncoding is
                         { Platform: PlatformId.Unicode, UnicodeEncoding: UnicodeEncodingId.Unicode_2_0_Full })
            ??
            this.Records.Select(x => (EncodingRecord?)x).FirstOrDefault(
                x => x!.Value.PlatformAndEncoding is
                         { Platform: PlatformId.Unicode, UnicodeEncoding: UnicodeEncodingId.UnicodeFullRepertoire })
            ??
            this.Records.Select(x => (EncodingRecord?)x).FirstOrDefault(
                x => x!.Value.PlatformAndEncoding is
                         { Platform: PlatformId.Windows, WindowsEncoding: WindowsEncodingId.UnicodeBmp })
            ??
            this.Records.Select(x => (EncodingRecord?)x).FirstOrDefault(
                x => x!.Value.PlatformAndEncoding is
                         { Platform: PlatformId.Windows, WindowsEncoding: WindowsEncodingId.UnicodeFullRepertoire });

        public CmapFormat? UnicodeTable => this.GetTable(this.UnicodeEncodingRecord);

        public CmapFormat? GetTable(EncodingRecord? encodingRecord) =>
            encodingRecord is { } record
                ? this.Memory.ReadU16Big(record.SubtableOffset) switch
                {
                    0 => new CmapFormat0(this.Memory[record.SubtableOffset..]),
                    2 => new CmapFormat2(this.Memory[record.SubtableOffset..]),
                    4 => new CmapFormat4(this.Memory[record.SubtableOffset..]),
                    6 => new CmapFormat6(this.Memory[record.SubtableOffset..]),
                    8 => new CmapFormat8(this.Memory[record.SubtableOffset..]),
                    10 => new CmapFormat10(this.Memory[record.SubtableOffset..]),
                    12 or 13 => new CmapFormat12And13(this.Memory[record.SubtableOffset..]),
                    _ => null,
                }
                : null;

        public struct EncodingRecord
        {
            public PlatformAndEncoding PlatformAndEncoding;
            public int SubtableOffset;

            public EncodingRecord(PointerSpan<byte> span)
            {
                this.PlatformAndEncoding = new(span);
                var offset = Unsafe.SizeOf<PlatformAndEncoding>();
                span.ReadBig(ref offset, out this.SubtableOffset);
            }

            public static EncodingRecord ReverseEndianness(EncodingRecord value) => new()
            {
                PlatformAndEncoding = PlatformAndEncoding.ReverseEndianness(value.PlatformAndEncoding),
                SubtableOffset = BinaryPrimitives.ReverseEndianness(value.SubtableOffset),
            };
        }

        public struct MapGroup : IComparable<MapGroup>
        {
            public int StartCharCode;
            public int EndCharCode;
            public int GlyphId;

            public MapGroup(PointerSpan<byte> span)
            {
                var offset = 0;
                span.ReadBig(ref offset, out this.StartCharCode);
                span.ReadBig(ref offset, out this.EndCharCode);
                span.ReadBig(ref offset, out this.GlyphId);
            }

            public static MapGroup ReverseEndianness(MapGroup obj) => new()
            {
                StartCharCode = BinaryPrimitives.ReverseEndianness(obj.StartCharCode),
                EndCharCode = BinaryPrimitives.ReverseEndianness(obj.EndCharCode),
                GlyphId = BinaryPrimitives.ReverseEndianness(obj.GlyphId),
            };

            public int CompareTo(MapGroup other)
            {
                var endCharCodeComparison = this.EndCharCode.CompareTo(other.EndCharCode);
                if (endCharCodeComparison != 0) return endCharCodeComparison;

                var startCharCodeComparison = this.StartCharCode.CompareTo(other.StartCharCode);
                if (startCharCodeComparison != 0) return startCharCodeComparison;

                return this.GlyphId.CompareTo(other.GlyphId);
            }
        }

        public abstract class CmapFormat : IReadOnlyDictionary<int, ushort>
        {
            public int Count => this.Count(x => x.Value != 0);

            public IEnumerable<int> Keys => this.Select(x => x.Key);

            public IEnumerable<ushort> Values => this.Select(x => x.Value);

            public ushort this[int key] => throw new NotImplementedException();

            public abstract ushort CharToGlyph(int c);

            public abstract IEnumerator<KeyValuePair<int, ushort>> GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

            public bool ContainsKey(int key) => this.CharToGlyph(key) != 0;

            public bool TryGetValue(int key, out ushort value)
            {
                value = this.CharToGlyph(key);
                return value != 0;
            }
        }

        public class CmapFormat0 : CmapFormat
        {
            public readonly PointerSpan<byte> Memory;

            public CmapFormat0(PointerSpan<byte> memory) => this.Memory = memory;

            public ushort Format => this.Memory.ReadU16Big(0);

            public ushort Length => this.Memory.ReadU16Big(2);

            public ushort Language => this.Memory.ReadU16Big(4);

            public PointerSpan<byte> GlyphIdArray => this.Memory.Slice(6, 256);

            public override ushort CharToGlyph(int c) => c is >= 0 and < 256 ? this.GlyphIdArray[c] : (byte)0;

            public override IEnumerator<KeyValuePair<int, ushort>> GetEnumerator()
            {
                for (var codepoint = 0; codepoint < 256; codepoint++)
                {
                    if (this.GlyphIdArray[codepoint] is var glyphId and not 0)
                        yield return new(codepoint, glyphId);
                }
            }
        }

        public class CmapFormat2 : CmapFormat
        {
            public readonly PointerSpan<byte> Memory;

            public CmapFormat2(PointerSpan<byte> memory) => this.Memory = memory;

            public ushort Format => this.Memory.ReadU16Big(0);

            public ushort Length => this.Memory.ReadU16Big(2);

            public ushort Language => this.Memory.ReadU16Big(4);

            public BigEndianPointerSpan<ushort> SubHeaderKeys => new(
                this.Memory[6..].As<ushort>(256),
                BinaryPrimitives.ReverseEndianness);

            public PointerSpan<byte> Data => this.Memory[518..];

            public bool TryGetSubHeader(
                int keyIndex, out SubHeader subheader, out BigEndianPointerSpan<ushort> glyphSpan)
            {
                if (keyIndex < 0 || keyIndex >= this.SubHeaderKeys.Count)
                {
                    subheader = default;
                    glyphSpan = default;
                    return false;
                }

                var offset = this.SubHeaderKeys[keyIndex];
                if (offset + Unsafe.SizeOf<SubHeader>() > this.Data.Length)
                {
                    subheader = default;
                    glyphSpan = default;
                    return false;
                }

                subheader = new(this.Data[offset..]);
                glyphSpan = new(
                    this.Data[(offset + Unsafe.SizeOf<SubHeader>() + subheader.IdRangeOffset)..]
                        .As<ushort>(subheader.EntryCount),
                    BinaryPrimitives.ReverseEndianness);

                return true;
            }

            public override ushort CharToGlyph(int c)
            {
                if (!this.TryGetSubHeader(c >> 8, out var sh, out var glyphSpan))
                    return 0;

                c = (c & 0xFF) - sh.FirstCode;
                if (c > 0 || c >= glyphSpan.Count)
                    return 0;

                var res = glyphSpan[c];
                return res == 0 ? (ushort)0 : unchecked((ushort)(res + sh.IdDelta));
            }

            public override IEnumerator<KeyValuePair<int, ushort>> GetEnumerator()
            {
                for (var i = 0; i < this.SubHeaderKeys.Count; i++)
                {
                    if (!this.TryGetSubHeader(i, out var sh, out var glyphSpan))
                        continue;

                    for (var j = 0; j < glyphSpan.Count; j++)
                    {
                        var res = glyphSpan[j];
                        if (res == 0)
                            continue;

                        var glyphId = unchecked((ushort)(res + sh.IdDelta));
                        if (glyphId == 0)
                            continue;

                        var codepoint = (i << 8) | (sh.FirstCode + j);
                        yield return new(codepoint, glyphId);
                    }
                }
            }

            public struct SubHeader
            {
                public ushort FirstCode;
                public ushort EntryCount;
                public ushort IdDelta;
                public ushort IdRangeOffset;

                public SubHeader(PointerSpan<byte> span)
                {
                    var offset = 0;
                    span.ReadBig(ref offset, out this.FirstCode);
                    span.ReadBig(ref offset, out this.EntryCount);
                    span.ReadBig(ref offset, out this.IdDelta);
                    span.ReadBig(ref offset, out this.IdRangeOffset);
                }
            }
        }

        public class CmapFormat4 : CmapFormat
        {
            public const int EndCodesOffset = 14;

            public readonly PointerSpan<byte> Memory;

            public CmapFormat4(PointerSpan<byte> memory) => this.Memory = memory;

            public ushort Format => this.Memory.ReadU16Big(0);

            public ushort Length => this.Memory.ReadU16Big(2);

            public ushort Language => this.Memory.ReadU16Big(4);

            public ushort SegCountX2 => this.Memory.ReadU16Big(6);

            public ushort SearchRange => this.Memory.ReadU16Big(8);

            public ushort EntrySelector => this.Memory.ReadU16Big(10);

            public ushort RangeShift => this.Memory.ReadU16Big(12);

            public BigEndianPointerSpan<ushort> EndCodes => new(
                this.Memory.Slice(EndCodesOffset, this.SegCountX2).As<ushort>(),
                BinaryPrimitives.ReverseEndianness);

            public BigEndianPointerSpan<ushort> StartCodes => new(
                this.Memory.Slice(EndCodesOffset + 2 + (1 * this.SegCountX2), this.SegCountX2).As<ushort>(),
                BinaryPrimitives.ReverseEndianness);

            public BigEndianPointerSpan<ushort> IdDeltas => new(
                this.Memory.Slice(EndCodesOffset + 2 + (2 * this.SegCountX2), this.SegCountX2).As<ushort>(),
                BinaryPrimitives.ReverseEndianness);

            public BigEndianPointerSpan<ushort> IdRangeOffsets => new(
                this.Memory.Slice(EndCodesOffset + 2 + (3 * this.SegCountX2), this.SegCountX2).As<ushort>(),
                BinaryPrimitives.ReverseEndianness);

            public BigEndianPointerSpan<ushort> GlyphIds => new(
                this.Memory.Slice(EndCodesOffset + 2 + (4 * this.SegCountX2), this.SegCountX2).As<ushort>(),
                BinaryPrimitives.ReverseEndianness);

            public override ushort CharToGlyph(int c)
            {
                if (c is < 0 or >= 0x10000)
                    return 0;

                var i = this.EndCodes.BinarySearch((ushort)c);
                if (i < 0)
                    return 0;

                var startCode = this.StartCodes[i];
                var endCode = this.EndCodes[i];
                if (c < startCode || c > endCode)
                    return 0;

                var idRangeOffset = this.IdRangeOffsets[i];
                var idDelta = this.IdDeltas[i];
                if (idRangeOffset == 0)
                    return unchecked((ushort)(c + idDelta));

                var ptr = EndCodesOffset + 2 + (3 * this.SegCountX2) + i * 2 + idRangeOffset;
                if (ptr > this.Memory.Length)
                    return 0;

                var glyphs = new BigEndianPointerSpan<ushort>(
                    this.Memory[ptr..].As<ushort>(endCode - startCode + 1),
                    BinaryPrimitives.ReverseEndianness);

                var glyph = glyphs[c - startCode];
                return unchecked(glyph == 0 ? (ushort)0 : (ushort)(idDelta + glyph));
            }

            public override IEnumerator<KeyValuePair<int, ushort>> GetEnumerator()
            {
                var startCodes = this.StartCodes;
                var endCodes = this.EndCodes;
                var idDeltas = this.IdDeltas;
                var idRangeOffsets = this.IdRangeOffsets;

                for (var i = 0; i < this.SegCountX2 / 2; i++)
                {
                    var startCode = startCodes[i];
                    var endCode = endCodes[i];
                    var idRangeOffset = idRangeOffsets[i];
                    var idDelta = idDeltas[i];

                    if (idRangeOffset == 0)
                    {
                        for (var c = (int)startCode; c <= endCode; c++)
                            yield return new(c, (ushort)(c + idDelta));
                    }
                    else
                    {
                        var ptr = EndCodesOffset + 2 + (3 * this.SegCountX2) + i * 2 + idRangeOffset;
                        if (ptr >= this.Memory.Length)
                            continue;

                        var glyphs = new BigEndianPointerSpan<ushort>(
                            this.Memory[ptr..].As<ushort>(endCode - startCode + 1),
                            BinaryPrimitives.ReverseEndianness);

                        for (var j = 0; j < glyphs.Count; j++)
                        {
                            var glyphId = glyphs[j];
                            if (glyphId == 0)
                                continue;

                            glyphId += idDelta;
                            if (glyphId == 0)
                                continue;

                            yield return new(startCode + j, glyphId);
                        }
                    }
                }
            }
        }

        public class CmapFormat6 : CmapFormat
        {
            public readonly PointerSpan<byte> Memory;

            public CmapFormat6(PointerSpan<byte> memory) => this.Memory = memory;

            public ushort Format => this.Memory.ReadU16Big(0);

            public ushort Length => this.Memory.ReadU16Big(2);

            public ushort Language => this.Memory.ReadU16Big(4);

            public ushort FirstCode => this.Memory.ReadU16Big(6);

            public ushort EntryCount => this.Memory.ReadU16Big(8);

            public BigEndianPointerSpan<ushort> GlyphIds => new(
                this.Memory[10..].As<ushort>(this.EntryCount),
                BinaryPrimitives.ReverseEndianness);

            public override ushort CharToGlyph(int c)
            {
                var glyphIds = this.GlyphIds;
                if (c < this.FirstCode || c >= this.FirstCode + this.GlyphIds.Count)
                    return 0;

                return glyphIds[c - this.FirstCode];
            }

            public override IEnumerator<KeyValuePair<int, ushort>> GetEnumerator()
            {
                var glyphIds = this.GlyphIds;
                for (var i = 0; i < this.GlyphIds.Length; i++)
                {
                    var g = glyphIds[i];
                    if (g != 0)
                        yield return new(this.FirstCode + i, g);
                }
            }
        }

        public class CmapFormat8 : CmapFormat
        {
            public readonly PointerSpan<byte> Memory;

            public CmapFormat8(PointerSpan<byte> memory) => this.Memory = memory;

            public int Format => this.Memory.ReadI32Big(0);

            public int Length => this.Memory.ReadI32Big(4);

            public int Language => this.Memory.ReadI32Big(8);

            public PointerSpan<byte> Is32 => this.Memory.Slice(12, 8192);

            public int NumGroups => this.Memory.ReadI32Big(8204);

            public BigEndianPointerSpan<MapGroup> Groups =>
                new(this.Memory[8208..].As<MapGroup>(), MapGroup.ReverseEndianness);

            public override ushort CharToGlyph(int c)
            {
                var groups = this.Groups;

                var i = groups.BinarySearch((in MapGroup value) => c.CompareTo(value.EndCharCode));
                if (i < 0)
                    return 0;

                var group = groups[i];
                if (c < group.StartCharCode || c > group.EndCharCode)
                    return 0;

                return unchecked((ushort)(group.GlyphId + c - group.StartCharCode));
            }

            public override IEnumerator<KeyValuePair<int, ushort>> GetEnumerator()
            {
                foreach (var group in this.Groups)
                {
                    for (var j = group.StartCharCode; j <= group.EndCharCode; j++)
                    {
                        var glyphId = (ushort)(group.GlyphId + j - group.StartCharCode);
                        if (glyphId == 0)
                            continue;

                        yield return new(j, glyphId);
                    }
                }
            }
        }

        public class CmapFormat10 : CmapFormat
        {
            public readonly PointerSpan<byte> Memory;

            public CmapFormat10(PointerSpan<byte> memory) => this.Memory = memory;

            public int Format => this.Memory.ReadI32Big(0);

            public int Length => this.Memory.ReadI32Big(4);

            public int Language => this.Memory.ReadI32Big(8);

            public int StartCharCode => this.Memory.ReadI32Big(12);

            public int NumChars => this.Memory.ReadI32Big(16);

            public BigEndianPointerSpan<ushort> GlyphIdArray => new(
                this.Memory.Slice(20, this.NumChars * 2).As<ushort>(),
                BinaryPrimitives.ReverseEndianness);

            public override ushort CharToGlyph(int c)
            {
                if (c < this.StartCharCode || c >= this.StartCharCode + this.GlyphIdArray.Count)
                    return 0;

                return this.GlyphIdArray[c];
            }

            public override IEnumerator<KeyValuePair<int, ushort>> GetEnumerator()
            {
                for (var i = 0; i < this.GlyphIdArray.Count; i++)
                {
                    var glyph = this.GlyphIdArray[i];
                    if (glyph != 0)
                        yield return new(this.StartCharCode + i, glyph);
                }
            }
        }

        public class CmapFormat12And13 : CmapFormat
        {
            public readonly PointerSpan<byte> Memory;

            public CmapFormat12And13(PointerSpan<byte> memory) => this.Memory = memory;

            public ushort Format => this.Memory.ReadU16Big(0);

            public int Length => this.Memory.ReadI32Big(4);

            public int Language => this.Memory.ReadI32Big(8);

            public int NumGroups => this.Memory.ReadI32Big(12);

            public BigEndianPointerSpan<MapGroup> Groups => new(
                this.Memory[16..].As<MapGroup>(this.NumGroups),
                MapGroup.ReverseEndianness);

            public override ushort CharToGlyph(int c)
            {
                var groups = this.Groups;

                var i = groups.BinarySearch(new MapGroup() { EndCharCode = c });
                if (i < 0)
                    return 0;

                var group = groups[i];
                if (c < group.StartCharCode || c > group.EndCharCode)
                    return 0;

                if (this.Format == 12)
                    return (ushort)(group.GlyphId + c - group.StartCharCode);
                else
                    return (ushort)group.GlyphId;
            }

            public override IEnumerator<KeyValuePair<int, ushort>> GetEnumerator()
            {
                var groups = this.Groups;
                if (this.Format == 12)
                {
                    foreach (var group in groups)
                    {
                        for (var j = group.StartCharCode; j <= group.EndCharCode; j++)
                        {
                            var glyphId = (ushort)(group.GlyphId + j - group.StartCharCode);
                            if (glyphId == 0)
                                continue;

                            yield return new(j, glyphId);
                        }
                    }
                }
                else
                {
                    foreach (var group in groups)
                    {
                        if (group.GlyphId == 0)
                            continue;

                        for (var j = group.StartCharCode; j <= group.EndCharCode; j++)
                            yield return new(j, (ushort)group.GlyphId);
                    }
                }
            }
        }
    }

    private readonly struct Gpos
    {
        // https://docs.microsoft.com/en-us/typography/opentype/spec/gpos

        public static readonly TagStruct DirectoryTableTag = new('G', 'P', 'O', 'S');

        public readonly PointerSpan<byte> Memory;

        public Gpos(SfntFile file)
            : this(file[DirectoryTableTag])
        {
        }

        public Gpos(PointerSpan<byte> memory) => this.Memory = memory;

        public Fixed Version => new(this.Memory);

        public ushort ScriptListOffset => this.Memory.ReadU16Big(4);

        public ushort FeatureListOffset => this.Memory.ReadU16Big(6);

        public ushort LookupListOffset => this.Memory.ReadU16Big(8);

        public uint FeatureVariationsOffset => this.Version.CompareTo(new(1, 1)) >= 0
                                                   ? this.Memory.ReadU32Big(10)
                                                   : 0;

        public BigEndianPointerSpan<ushort> LookupOffsetList => new(
            this.Memory[(this.LookupListOffset + 2)..].As<ushort>(
                this.Memory.ReadU16Big(this.LookupListOffset)),
            BinaryPrimitives.ReverseEndianness);

        public IEnumerable<LookupTable> EnumerateLookupTables()
        {
            foreach (var offset in this.LookupOffsetList)
                yield return new(this.Memory[(this.LookupListOffset + offset)..]);
        }

        public IEnumerable<KerningPair> ExtractAdvanceX() =>
            this.EnumerateLookupTables()
                .SelectMany(
                    lookupTable => lookupTable.Type switch
                    {
                        LookupType.PairAdjustment =>
                            lookupTable.SelectMany(y => new PairAdjustmentPositioning(y).ExtractAdvanceX()),
                        LookupType.ExtensionPositioning =>
                            lookupTable
                                .Where(y => y.ReadU16Big(0) == 1)
                                .Select(y => new ExtensionPositioningSubtableFormat1(y))
                                .Where(y => y.ExtensionLookupType == LookupType.PairAdjustment)
                                .SelectMany(y => new PairAdjustmentPositioning(y.ExtensionData).ExtractAdvanceX()),
                        _ => Array.Empty<KerningPair>(),
                    });

        public struct ValueRecord
        {
            public short PlacementX;
            public short PlacementY;
            public short AdvanceX;
            public short AdvanceY;
            public short PlacementDeviceOffsetX;
            public short PlacementDeviceOffsetY;
            public short AdvanceDeviceOffsetX;
            public short AdvanceDeviceOffsetY;

            public ValueRecord(PointerSpan<byte> pointerSpan, ValueFormat valueFormat)
            {
                var offset = 0;
                if ((valueFormat & ValueFormat.PlacementX) != 0)
                    pointerSpan.ReadBig(ref offset, out this.PlacementX);

                if ((valueFormat & ValueFormat.PlacementY) != 0)
                    pointerSpan.ReadBig(ref offset, out this.PlacementY);

                if ((valueFormat & ValueFormat.AdvanceX) != 0) pointerSpan.ReadBig(ref offset, out this.AdvanceX);
                if ((valueFormat & ValueFormat.AdvanceY) != 0) pointerSpan.ReadBig(ref offset, out this.AdvanceY);
                if ((valueFormat & ValueFormat.PlacementDeviceOffsetX) != 0)
                    pointerSpan.ReadBig(ref offset, out this.PlacementDeviceOffsetX);

                if ((valueFormat & ValueFormat.PlacementDeviceOffsetY) != 0)
                    pointerSpan.ReadBig(ref offset, out this.PlacementDeviceOffsetY);

                if ((valueFormat & ValueFormat.AdvanceDeviceOffsetX) != 0)
                    pointerSpan.ReadBig(ref offset, out this.AdvanceDeviceOffsetX);

                if ((valueFormat & ValueFormat.AdvanceDeviceOffsetY) != 0)
                    pointerSpan.ReadBig(ref offset, out this.AdvanceDeviceOffsetY);
            }
        }

        public readonly struct PairAdjustmentPositioning
        {
            public readonly PointerSpan<byte> Memory;

            public PairAdjustmentPositioning(PointerSpan<byte> memory) => this.Memory = memory;

            public ushort Format => this.Memory.ReadU16Big(0);

            public IEnumerable<KerningPair> ExtractAdvanceX() => this.Format switch
            {
                1 => new Format1(this.Memory).ExtractAdvanceX(),
                2 => new Format2(this.Memory).ExtractAdvanceX(),
                _ => Array.Empty<KerningPair>(),
            };

            public readonly struct Format1
            {
                public readonly PointerSpan<byte> Memory;

                public Format1(PointerSpan<byte> memory) => this.Memory = memory;

                public ushort Format => this.Memory.ReadU16Big(0);

                public ushort CoverageOffset => this.Memory.ReadU16Big(2);

                public ValueFormat ValueFormat1 => this.Memory.ReadEnumBig<ValueFormat>(4);

                public ValueFormat ValueFormat2 => this.Memory.ReadEnumBig<ValueFormat>(6);

                public ushort PairSetCount => this.Memory.ReadU16Big(8);

                public BigEndianPointerSpan<ushort> PairSetOffsets => new(
                    this.Memory[10..].As<ushort>(this.PairSetCount),
                    BinaryPrimitives.ReverseEndianness);

                public CoverageTable CoverageTable => new(this.Memory[this.CoverageOffset..]);

                public PairSet this[int index] => new(
                    this.Memory[this.PairSetOffsets[index] ..],
                    this.ValueFormat1,
                    this.ValueFormat2);

                public IEnumerable<KerningPair> ExtractAdvanceX()
                {
                    if ((this.ValueFormat1 & ValueFormat.AdvanceX) == 0 &&
                        (this.ValueFormat2 & ValueFormat.AdvanceX) == 0)
                    {
                        yield break;
                    }

                    var coverageTable = this.CoverageTable;
                    switch (coverageTable.Format)
                    {
                        case CoverageTable.CoverageFormat.Glyphs:
                        {
                            var glyphSpan = coverageTable.Glyphs;
                            foreach (var coverageIndex in Enumerable.Range(0, glyphSpan.Count))
                            {
                                var glyph1Id = glyphSpan[coverageIndex];
                                PairSet pairSetView;
                                try
                                {
                                    pairSetView = this[coverageIndex];
                                }
                                catch (ArgumentOutOfRangeException)
                                {
                                    yield break;
                                }
                                catch (IndexOutOfRangeException)
                                {
                                    yield break;
                                }

                                foreach (var pairIndex in Enumerable.Range(0, pairSetView.Count))
                                {
                                    var pair = pairSetView[pairIndex];
                                    var adj = (short)(pair.Record1.AdvanceX + pair.Record2.PlacementX);
                                    if (adj >= 10000)
                                        System.Diagnostics.Debugger.Break();

                                    if (adj != 0)
                                        yield return new(glyph1Id, pair.SecondGlyph, adj);
                                }
                            }

                            break;
                        }

                        case CoverageTable.CoverageFormat.RangeRecords:
                        {
                            foreach (var rangeRecord in coverageTable.RangeRecords)
                            {
                                var startGlyphId = rangeRecord.StartGlyphId;
                                var endGlyphId = rangeRecord.EndGlyphId;
                                var startCoverageIndex = rangeRecord.StartCoverageIndex;
                                var glyphCount = endGlyphId - startGlyphId + 1;
                                foreach (var glyph1Id in Enumerable.Range(startGlyphId, glyphCount))
                                {
                                    PairSet pairSetView;
                                    try
                                    {
                                        pairSetView = this[startCoverageIndex + glyph1Id - startGlyphId];
                                    }
                                    catch (ArgumentOutOfRangeException)
                                    {
                                        yield break;
                                    }
                                    catch (IndexOutOfRangeException)
                                    {
                                        yield break;
                                    }

                                    foreach (var pairIndex in Enumerable.Range(0, pairSetView.Count))
                                    {
                                        var pair = pairSetView[pairIndex];
                                        var adj = (short)(pair.Record1.AdvanceX + pair.Record2.PlacementX);
                                        if (adj != 0)
                                            yield return new((ushort)glyph1Id, pair.SecondGlyph, adj);
                                    }
                                }
                            }

                            break;
                        }
                    }
                }

                public readonly struct PairSet
                {
                    public readonly PointerSpan<byte> Memory;
                    public readonly ValueFormat ValueFormat1;
                    public readonly ValueFormat ValueFormat2;
                    public readonly int PairValue1Size;
                    public readonly int PairValue2Size;
                    public readonly int PairSize;

                    public PairSet(
                        PointerSpan<byte> memory,
                        ValueFormat valueFormat1,
                        ValueFormat valueFormat2)
                    {
                        this.Memory = memory;
                        this.ValueFormat1 = valueFormat1;
                        this.ValueFormat2 = valueFormat2;
                        this.PairValue1Size = this.ValueFormat1.NumBytes();
                        this.PairValue2Size = this.ValueFormat2.NumBytes();
                        this.PairSize = 2 + this.PairValue1Size + this.PairValue2Size;
                    }

                    public ushort Count => this.Memory.ReadU16Big(0);

                    public PairValueRecord this[int index]
                    {
                        get
                        {
                            var pvr = this.Memory.Slice(2 + (this.PairSize * index), this.PairSize);
                            return new()
                            {
                                SecondGlyph = pvr.ReadU16Big(0),
                                Record1 = new(pvr.Slice(2, this.PairValue1Size), this.ValueFormat1),
                                Record2 = new(
                                    pvr.Slice(2 + this.PairValue1Size, this.PairValue2Size),
                                    this.ValueFormat2),
                            };
                        }
                    }

                    public struct PairValueRecord
                    {
                        public ushort SecondGlyph;
                        public ValueRecord Record1;
                        public ValueRecord Record2;
                    }
                }
            }

            public readonly struct Format2
            {
                public readonly PointerSpan<byte> Memory;
                public readonly int PairValue1Size;
                public readonly int PairValue2Size;
                public readonly int PairSize;

                public Format2(PointerSpan<byte> memory)
                {
                    this.Memory = memory;
                    this.PairValue1Size = this.ValueFormat1.NumBytes();
                    this.PairValue2Size = this.ValueFormat2.NumBytes();
                    this.PairSize = this.PairValue1Size + this.PairValue2Size;
                }

                public ushort Format => this.Memory.ReadU16Big(0);

                public ushort CoverageOffset => this.Memory.ReadU16Big(2);

                public ValueFormat ValueFormat1 => this.Memory.ReadEnumBig<ValueFormat>(4);

                public ValueFormat ValueFormat2 => this.Memory.ReadEnumBig<ValueFormat>(6);

                public ushort ClassDef1Offset => this.Memory.ReadU16Big(8);

                public ushort ClassDef2Offset => this.Memory.ReadU16Big(10);

                public ushort Class1Count => this.Memory.ReadU16Big(12);

                public ushort Class2Count => this.Memory.ReadU16Big(14);

                public ClassDefTable ClassDefTable1 => new(this.Memory[this.ClassDef1Offset..]);

                public ClassDefTable ClassDefTable2 => new(this.Memory[this.ClassDef2Offset..]);

                public (ValueRecord Record1, ValueRecord Record2) this[(int Class1Index, int Class2Index) v] =>
                    this[v.Class1Index, v.Class2Index];

                public (ValueRecord Record1, ValueRecord Record2) this[int class1Index, int class2Index]
                {
                    get
                    {
                        if (class1Index < 0 || class1Index >= this.Class1Count)
                            throw new IndexOutOfRangeException();

                        if (class2Index < 0 || class2Index >= this.Class2Count)
                            throw new IndexOutOfRangeException();

                        var offset = 16 + (this.PairSize * ((class1Index * this.Class2Count) + class2Index));
                        return (
                                   new(this.Memory.Slice(offset, this.PairValue1Size), this.ValueFormat1),
                                   new(
                                       this.Memory.Slice(offset + this.PairValue1Size, this.PairValue2Size),
                                       this.ValueFormat2));
                    }
                }

                public IEnumerable<KerningPair> ExtractAdvanceX()
                {
                    if ((this.ValueFormat1 & ValueFormat.AdvanceX) == 0 &&
                        (this.ValueFormat2 & ValueFormat.AdvanceX) == 0)
                    {
                        yield break;
                    }

                    var classes1 = this.ClassDefTable1.Enumerate()
                                       .GroupBy(x => x.Class, x => x.GlyphId)
                                       .ToImmutableDictionary(x => x.Key, x => x.ToImmutableSortedSet());

                    var classes2 = this.ClassDefTable2.Enumerate()
                                       .GroupBy(x => x.Class, x => x.GlyphId)
                                       .ToImmutableDictionary(x => x.Key, x => x.ToImmutableSortedSet());

                    foreach (var class1 in Enumerable.Range(0, this.Class1Count))
                    {
                        if (!classes1.TryGetValue((ushort)class1, out var glyphs1))
                            continue;

                        foreach (var class2 in Enumerable.Range(0, this.Class2Count))
                        {
                            if (!classes2.TryGetValue((ushort)class2, out var glyphs2))
                                continue;

                            (ValueRecord, ValueRecord) record;
                            try
                            {
                                record = this[class1, class2];
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                yield break;
                            }
                            catch (IndexOutOfRangeException)
                            {
                                yield break;
                            }

                            var val = record.Item1.AdvanceX + record.Item2.PlacementX;
                            if (val == 0)
                                continue;

                            foreach (var glyph1 in glyphs1)
                            {
                                foreach (var glyph2 in glyphs2)
                                {
                                    yield return new(glyph1, glyph2, (short)val);
                                }
                            }
                        }
                    }
                }
            }
        }

        public readonly struct ExtensionPositioningSubtableFormat1
        {
            public readonly PointerSpan<byte> Memory;

            public ExtensionPositioningSubtableFormat1(PointerSpan<byte> memory) => this.Memory = memory;

            public ushort Format => this.Memory.ReadU16Big(0);

            public LookupType ExtensionLookupType => this.Memory.ReadEnumBig<LookupType>(2);

            public int ExtensionOffset => this.Memory.ReadI32Big(4);

            public PointerSpan<byte> ExtensionData => this.Memory[this.ExtensionOffset..];
        }
    }

    private readonly struct Head
    {
        // https://docs.microsoft.com/en-us/typography/opentype/spec/head
        // https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6head.html

        public const uint MagicNumberValue = 0x5F0F3CF5;
        public static readonly TagStruct DirectoryTableTag = new('h', 'e', 'a', 'd');

        public readonly PointerSpan<byte> Memory;

        public Head(SfntFile file)
            : this(file[DirectoryTableTag])
        {
        }

        public Head(PointerSpan<byte> memory) => this.Memory = memory;

        [Flags]
        public enum HeadFlags : ushort
        {
            BaselineForFontAtZeroY = 1 << 0,
            LeftSideBearingAtZeroX = 1 << 1,
            InstructionsDependOnPointSize = 1 << 2,
            ForcePpemsInteger = 1 << 3,
            InstructionsAlterAdvanceWidth = 1 << 4,
            VerticalLayout = 1 << 5,
            Reserved6 = 1 << 6,
            RequiresLayoutForCorrectLinguisticRendering = 1 << 7,
            IsAatFont = 1 << 8,
            ContainsRtlGlyph = 1 << 9,
            ContainsIndicStyleRearrangementEffects = 1 << 10,
            Lossless = 1 << 11,
            ProduceCompatibleMetrics = 1 << 12,
            OptimizedForClearType = 1 << 13,
            IsLastResortFont = 1 << 14,
            Reserved15 = 1 << 15,
        }

        [Flags]
        public enum MacStyleFlags : ushort
        {
            Bold = 1 << 0,
            Italic = 1 << 1,
            Underline = 1 << 2,
            Outline = 1 << 3,
            Shadow = 1 << 4,
            Condensed = 1 << 5,
            Extended = 1 << 6,
        }

        public Fixed Version => new(this.Memory);

        public Fixed FontRevision => new(this.Memory[4..]);

        public uint ChecksumAdjustment => this.Memory.ReadU32Big(8);

        public uint MagicNumber => this.Memory.ReadU32Big(12);

        public HeadFlags Flags => this.Memory.ReadEnumBig<HeadFlags>(16);

        public ushort UnitsPerEm => this.Memory.ReadU16Big(18);

        public ulong CreatedTimestamp => this.Memory.ReadU64Big(20);

        public ulong ModifiedTimestamp => this.Memory.ReadU64Big(28);

        public ushort MinX => this.Memory.ReadU16Big(36);

        public ushort MinY => this.Memory.ReadU16Big(38);

        public ushort MaxX => this.Memory.ReadU16Big(40);

        public ushort MaxY => this.Memory.ReadU16Big(42);

        public MacStyleFlags MacStyle => this.Memory.ReadEnumBig<MacStyleFlags>(44);

        public ushort LowestRecommendedPpem => this.Memory.ReadU16Big(46);

        public ushort FontDirectionHint => this.Memory.ReadU16Big(48);

        public ushort IndexToLocFormat => this.Memory.ReadU16Big(50);

        public ushort GlyphDataFormat => this.Memory.ReadU16Big(52);
    }

    private readonly struct Kern
    {
        // https://docs.microsoft.com/en-us/typography/opentype/spec/kern
        // https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6kern.html

        public static readonly TagStruct DirectoryTableTag = new('k', 'e', 'r', 'n');

        public readonly PointerSpan<byte> Memory;

        public Kern(SfntFile file)
            : this(file[DirectoryTableTag])
        {
        }

        public Kern(PointerSpan<byte> memory) => this.Memory = memory;

        public ushort Version => this.Memory.ReadU16Big(0);

        public IEnumerable<KerningPair> EnumerateHorizontalPairs() => this.Version switch
        {
            0 => new Version0(this.Memory).EnumerateHorizontalPairs(),
            1 => new Version1(this.Memory).EnumerateHorizontalPairs(),
            _ => Array.Empty<KerningPair>(),
        };

        public readonly struct Format0
        {
            public readonly PointerSpan<byte> Memory;

            public Format0(PointerSpan<byte> memory) => this.Memory = memory;

            public ushort PairCount => this.Memory.ReadU16Big(0);

            public ushort SearchRange => this.Memory.ReadU16Big(2);

            public ushort EntrySelector => this.Memory.ReadU16Big(4);

            public ushort RangeShift => this.Memory.ReadU16Big(6);

            public BigEndianPointerSpan<KerningPair> Pairs => new(
                this.Memory[8..].As<KerningPair>(this.PairCount),
                KerningPair.ReverseEndianness);
        }

        public readonly struct Version0
        {
            public readonly PointerSpan<byte> Memory;

            public Version0(PointerSpan<byte> memory) => this.Memory = memory;

            [Flags]
            public enum CoverageFlags : byte
            {
                Horizontal = 1 << 0,
                Minimum = 1 << 1,
                CrossStream = 1 << 2,
                Override = 1 << 3,
            }

            public ushort Version => this.Memory.ReadU16Big(0);

            public ushort NumSubtables => this.Memory.ReadU16Big(2);

            public PointerSpan<byte> Data => this.Memory[4..];

            public IEnumerable<Subtable> EnumerateSubtables()
            {
                var data = this.Data;
                for (var i = 0; i < this.NumSubtables && !data.IsEmpty; i++)
                {
                    var st = new Subtable(data);
                    data = data[st.Length..];
                    yield return st;
                }
            }

            public IEnumerable<KerningPair> EnumerateHorizontalPairs()
            {
                var accumulator = new Dictionary<(ushort Left, ushort Right), short>();
                foreach (var subtable in this.EnumerateSubtables())
                {
                    var isOverride = (subtable.Flags & CoverageFlags.Override) != 0;
                    var isMinimum = (subtable.Flags & CoverageFlags.Minimum) != 0;
                    foreach (var t in subtable.EnumeratePairs())
                    {
                        if (isOverride)
                        {
                            accumulator[(t.Left, t.Right)] = t.Value;
                        }
                        else if (isMinimum)
                        {
                            accumulator[(t.Left, t.Right)] = Math.Max(
                                accumulator.GetValueOrDefault((t.Left, t.Right), t.Value),
                                t.Value);
                        }
                        else
                        {
                            accumulator[(t.Left, t.Right)] = (short)(
                                                                        accumulator.GetValueOrDefault(
                                                                            (t.Left, t.Right)) + t.Value);
                        }
                    }
                }

                return accumulator.Select(
                    x => new KerningPair { Left = x.Key.Left, Right = x.Key.Right, Value = x.Value });
            }

            public readonly struct Subtable
            {
                public readonly PointerSpan<byte> Memory;

                public Subtable(PointerSpan<byte> memory) => this.Memory = memory;

                public ushort Version => this.Memory.ReadU16Big(0);

                public ushort Length => this.Memory.ReadU16Big(2);

                public byte Format => this.Memory[4];

                public CoverageFlags Flags => this.Memory.ReadEnumBig<CoverageFlags>(5);

                public PointerSpan<byte> Data => this.Memory[6..];

                public IEnumerable<KerningPair> EnumeratePairs() => this.Format switch
                {
                    0 => new Format0(this.Data).Pairs,
                    _ => Array.Empty<KerningPair>(),
                };
            }
        }

        public readonly struct Version1
        {
            public readonly PointerSpan<byte> Memory;

            public Version1(PointerSpan<byte> memory) => this.Memory = memory;

            [Flags]
            public enum CoverageFlags : byte
            {
                Vertical = 1 << 0,
                CrossStream = 1 << 1,
                Variation = 1 << 2,
            }

            public Fixed Version => new(this.Memory);

            public int NumSubtables => this.Memory.ReadI16Big(4);

            public PointerSpan<byte> Data => this.Memory[8..];

            public IEnumerable<Subtable> EnumerateSubtables()
            {
                var data = this.Data;
                for (var i = 0; i < this.NumSubtables && !data.IsEmpty; i++)
                {
                    var st = new Subtable(data);
                    data = data[st.Length..];
                    yield return st;
                }
            }

            public IEnumerable<KerningPair> EnumerateHorizontalPairs() => this
                                                                          .EnumerateSubtables()
                                                                          .Where(x => x.Flags == 0)
                                                                          .SelectMany(x => x.EnumeratePairs());

            public readonly struct Subtable
            {
                public readonly PointerSpan<byte> Memory;

                public Subtable(PointerSpan<byte> memory) => this.Memory = memory;

                public int Length => this.Memory.ReadI32Big(0);

                public byte Format => this.Memory[4];

                public CoverageFlags Flags => this.Memory.ReadEnumBig<CoverageFlags>(5);

                public ushort TupleIndex => this.Memory.ReadU16Big(6);

                public PointerSpan<byte> Data => this.Memory[8..];

                public IEnumerable<KerningPair> EnumeratePairs() => this.Format switch
                {
                    0 => new Format0(this.Data).Pairs,
                    _ => Array.Empty<KerningPair>(),
                };
            }
        }
    }

    private readonly struct Name
    {
        // https://docs.microsoft.com/en-us/typography/opentype/spec/name
        // https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6name.html

        public static readonly TagStruct DirectoryTableTag = new('n', 'a', 'm', 'e');

        public readonly PointerSpan<byte> Memory;

        public Name(SfntFile file)
            : this(file[DirectoryTableTag])
        {
        }

        public Name(PointerSpan<byte> memory) => this.Memory = memory;

        public ushort Version => this.Memory.ReadU16Big(0);

        public ushort Count => this.Memory.ReadU16Big(2);

        public ushort StorageOffset => this.Memory.ReadU16Big(4);

        public BigEndianPointerSpan<NameRecord> NameRecords => new(
            this.Memory[6..].As<NameRecord>(this.Count),
            NameRecord.ReverseEndianness);

        public ushort LanguageCount =>
            this.Version == 0 ? (ushort)0 : this.Memory.ReadU16Big(6 + this.NameRecords.ByteCount);

        public BigEndianPointerSpan<LanguageRecord> LanguageRecords => this.Version == 0
                                                                           ? default
                                                                           : new(
                                                                               this.Memory[
                                                                                       (8 + this.NameRecords
                                                                                                   .ByteCount)..]
                                                                                   .As<LanguageRecord>(
                                                                                       this.LanguageCount),
                                                                               LanguageRecord.ReverseEndianness);

        public PointerSpan<byte> Storage => this.Memory[this.StorageOffset..];

        public string this[in NameRecord record] =>
            record.PlatformAndEncoding.Decode(this.Storage.Span.Slice(record.StringOffset, record.Length));

        public string this[in LanguageRecord record] =>
            Encoding.ASCII.GetString(this.Storage.Span.Slice(record.LanguageTagOffset, record.Length));

        public struct NameRecord
        {
            public PlatformAndEncoding PlatformAndEncoding;
            public ushort LanguageId;
            public NameId NameId;
            public ushort Length;
            public ushort StringOffset;

            public NameRecord(PointerSpan<byte> span)
            {
                this.PlatformAndEncoding = new(span);
                var offset = Unsafe.SizeOf<PlatformAndEncoding>();
                span.ReadBig(ref offset, out this.LanguageId);
                span.ReadBig(ref offset, out this.NameId);
                span.ReadBig(ref offset, out this.Length);
                span.ReadBig(ref offset, out this.StringOffset);
            }

            public static NameRecord ReverseEndianness(NameRecord value) => new()
            {
                PlatformAndEncoding = PlatformAndEncoding.ReverseEndianness(value.PlatformAndEncoding),
                LanguageId = BinaryPrimitives.ReverseEndianness(value.LanguageId),
                NameId = (NameId)BinaryPrimitives.ReverseEndianness((ushort)value.NameId),
                Length = BinaryPrimitives.ReverseEndianness(value.Length),
                StringOffset = BinaryPrimitives.ReverseEndianness(value.StringOffset),
            };
        }

        public struct LanguageRecord
        {
            public ushort Length;
            public ushort LanguageTagOffset;

            public LanguageRecord(PointerSpan<byte> span)
            {
                var offset = 0;
                span.ReadBig(ref offset, out this.Length);
                span.ReadBig(ref offset, out this.LanguageTagOffset);
            }

            public static LanguageRecord ReverseEndianness(LanguageRecord value) => new()
            {
                Length = BinaryPrimitives.ReverseEndianness(value.Length),
                LanguageTagOffset = BinaryPrimitives.ReverseEndianness(value.LanguageTagOffset),
            };
        }
    }
}
