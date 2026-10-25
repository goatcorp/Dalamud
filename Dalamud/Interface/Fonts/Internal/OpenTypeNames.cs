using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Dalamud.Interface.Fonts.Internal;

/// <summary>Reads the family and face names from the name table of an OpenType font.</summary>
internal static class OpenTypeNames
{
    private const uint CollectionTag = 0x74746366; // 'ttcf'
    private const uint NameTag = 0x6E616D65; // 'name'
    private const int MaxNameTableLength = 1 << 20;

    private const ushort FamilyId = 1;
    private const ushort SubfamilyId = 2;
    private const ushort TypographicFamilyId = 16;
    private const ushort TypographicSubfamilyId = 17;

    /// <summary>Reads the names of a face.</summary>
    /// <param name="stream">The font file, seekable, from its start.</param>
    /// <param name="fontNo">Index of the face in a collection.</param>
    /// <returns>The typographic family name, else the family name, and likewise the face name. Null where the font
    /// has none.</returns>
    /// <exception cref="InvalidDataException">If the data is not an OpenType font.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If there is no face of the index.</exception>
    public static (LocalizedName? Family, LocalizedName? Face) Read(Stream stream, int fontNo)
    {
        Span<byte> header = stackalloc byte[16];
        stream.ReadExactly(header[..12]);
        long offset = 0;
        var count = 1u;
        if (BinaryPrimitives.ReadUInt32BigEndian(header) == CollectionTag)
        {
            count = BinaryPrimitives.ReadUInt32BigEndian(header[8..]);
            if ((uint)fontNo < count)
            {
                stream.Position = 12 + (4L * fontNo);
                stream.ReadExactly(header[..4]);
                offset = BinaryPrimitives.ReadUInt32BigEndian(header);
            }
        }

        if ((uint)fontNo >= count)
            throw new ArgumentOutOfRangeException(nameof(fontNo), fontNo, "The font has no face of the index.");

        stream.Position = offset;
        stream.ReadExactly(header[..12]);
        // TrueType 1.0 (0x00010000), OpenType CFF ('OTTO'), or Apple TrueType ('true').
        if (BinaryPrimitives.ReadUInt32BigEndian(header) is not (0x00010000 or 0x4F54544F or 0x74727565))
            throw new InvalidDataException("The data is not an OpenType font.");

        var tables = BinaryPrimitives.ReadUInt16BigEndian(header[4..]);
        var directory = new byte[16 * tables];
        stream.ReadExactly(directory);
        for (var i = 0; i < tables; i++)
        {
            var record = directory.AsSpan(16 * i, 16);
            if (BinaryPrimitives.ReadUInt32BigEndian(record) != NameTag)
                continue;
            var length = BinaryPrimitives.ReadUInt32BigEndian(record[12..]);
            if (length > MaxNameTableLength)
                throw new InvalidDataException("The name table is too large.");
            var table = new byte[length];
            stream.Position = BinaryPrimitives.ReadUInt32BigEndian(record[8..]);
            stream.ReadExactly(table);
            return ReadTable(table);
        }

        return (null, null);
    }

    private static (LocalizedName? Family, LocalizedName? Face) ReadTable(ReadOnlySpan<byte> table)
    {
        var names = new Dictionary<ushort, Collected>();
        var count = BinaryPrimitives.ReadUInt16BigEndian(table[2..]);
        var storage = BinaryPrimitives.ReadUInt16BigEndian(table[4..]);
        for (var i = 0; i < count; i++)
        {
            var record = table.Slice(6 + (12 * i), 12);
            var platform = BinaryPrimitives.ReadUInt16BigEndian(record);
            var encoding = BinaryPrimitives.ReadUInt16BigEndian(record[2..]);
            var language = BinaryPrimitives.ReadUInt16BigEndian(record[4..]);
            var nameId = BinaryPrimitives.ReadUInt16BigEndian(record[6..]);
            if (nameId is not (FamilyId or SubfamilyId or TypographicFamilyId or TypographicSubfamilyId))
                continue;
            var start = storage + BinaryPrimitives.ReadUInt16BigEndian(record[10..]);
            var length = BinaryPrimitives.ReadUInt16BigEndian(record[8..]);
            if (start + length > table.Length)
                continue;
            var bytes = table.Slice(start, length);

            if (!names.TryGetValue(nameId, out var collected))
                names.Add(nameId, collected = new());
            // Platform 0 is Unicode, 3 is Windows (encodings 0 Symbol, 1 BMP, 10 UCS-4, LCID 0x0409 en-US), and 1 is Mac Roman.
            switch (platform)
            {
                case 0:
                case 3 when encoding is 0 or 1 or 10:
                    var text = Encoding.BigEndianUnicode.GetString(bytes);
                    if (platform == 0)
                    {
                        collected.Unicode ??= text;
                    }
                    else if (GetLocale(language) is { } locale)
                    {
                        collected.ByLocale.TryAdd(locale, text);
                        if (language == 0x409)
                            collected.English ??= text;
                    }

                    collected.First ??= text;
                    break;
                case 1 when encoding == 0 && language == 0:
                    collected.Mac ??= Encoding.Latin1.GetString(bytes);
                    break;
            }
        }

        return (Pick(names, TypographicFamilyId) ?? Pick(names, FamilyId),
                Pick(names, TypographicSubfamilyId) ?? Pick(names, SubfamilyId));
    }

    private static LocalizedName? Pick(Dictionary<ushort, Collected> names, ushort nameId) =>
        names.TryGetValue(nameId, out var c) && (c.English ?? c.Unicode ?? c.Mac ?? c.First) is { } english
            ? new(english, c.ByLocale.Count == 0 ? null : c.ByLocale)
            : null;

    private static string? GetLocale(ushort language)
    {
        try
        {
            var name = CultureInfo.GetCultureInfo(language).Name;
            return name.Length == 0 ? null : name.ToLowerInvariant();
        }
        catch (CultureNotFoundException)
        {
            return null;
        }
    }

    private sealed class Collected
    {
        public Dictionary<string, string> ByLocale { get; } = [];

        public string? English { get; set; }

        public string? Unicode { get; set; }

        public string? Mac { get; set; }

        public string? First { get; set; }
    }
}
