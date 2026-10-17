using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;

namespace Dalamud.Utility.Unicode;

/// <summary>Stores unicode data.</summary>
public static class UnicodeData
{
    /// <summary>Line break classes.</summary>
    private static readonly UnicodeLineBreakClass[] LineBreakValue;

    /// <summary>East asian width classes.</summary>
    private static readonly UnicodeEastAsianWidthClass[] EastAsianWidthValue;

    /// <summary>General categories.</summary>
    private static readonly UnicodeGeneralCategory[] GeneralCategoryValue;

    /// <summary>Emoji properties.</summary>
    private static readonly UnicodeEmojiProperty[] EmojiPropertyValue;

    /// <summary>Grapheme cluster break properties.</summary>
    private static readonly UnicodeGraphemeClusterBreak[] GraphemeClusterBreakValue;

    /// <summary>Indic conjunct break properties.</summary>
    private static readonly UnicodeIndicConjunctBreak[] IndicConjunctBreakValue;

    static UnicodeData()
    {
        // File is from https://www.unicode.org/Public/UCD/latest/ucd/LineBreak.txt
        LineBreakValue =
            Parse(
                typeof(UnicodeData).Assembly.GetManifestResourceStream("LineBreak.txt")!,
                UnicodeLineBreakClass.XX);

        // https://www.unicode.org/Public/UCD/latest/ucd/EastAsianWidth.txt
        EastAsianWidthValue =
            Parse(
                typeof(UnicodeData).Assembly.GetManifestResourceStream("EastAsianWidth.txt")!,
                UnicodeEastAsianWidthClass.N);

        // https://www.unicode.org/Public/UCD/latest/ucd/extracted/DerivedGeneralCategory.txt
        GeneralCategoryValue =
            Parse(
                typeof(UnicodeData).Assembly.GetManifestResourceStream("DerivedGeneralCategory.txt")!,
                UnicodeGeneralCategory.Cn);

        // https://www.unicode.org/Public/UCD/latest/ucd/emoji/emoji-data.txt
        EmojiPropertyValue =
            Parse(
                typeof(UnicodeData).Assembly.GetManifestResourceStream("emoji-data.txt")!,
                default(UnicodeEmojiProperty));

        // https://www.unicode.org/Public/UCD/latest/ucd/auxiliary/GraphemeBreakProperty.txt
        GraphemeClusterBreakValue =
            Parse(
                typeof(UnicodeData).Assembly.GetManifestResourceStream("GraphemeBreakProperty.txt")!,
                UnicodeGraphemeClusterBreak.Other);

        // The Indic_Conjunct_Break section of https://www.unicode.org/Public/UCD/latest/ucd/DerivedCoreProperties.txt
        IndicConjunctBreakValue =
            Parse(
                typeof(UnicodeData).Assembly.GetManifestResourceStream("IndicConjunctBreak.txt")!,
                UnicodeIndicConjunctBreak.None);
    }

    /// <inheritdoc cref="LineBreakValue"/>
    public static ReadOnlySpan<UnicodeLineBreakClass> LineBreak => LineBreakValue;

    /// <inheritdoc cref="EastAsianWidthValue"/>
    public static ReadOnlySpan<UnicodeEastAsianWidthClass> EastAsianWidth => EastAsianWidthValue;

    /// <inheritdoc cref="GeneralCategoryValue"/>
    public static ReadOnlySpan<UnicodeGeneralCategory> GeneralCategory => GeneralCategoryValue;

    /// <inheritdoc cref="EmojiPropertyValue"/>
    public static ReadOnlySpan<UnicodeEmojiProperty> EmojiProperty => EmojiPropertyValue;

    /// <inheritdoc cref="GraphemeClusterBreakValue"/>
    public static ReadOnlySpan<UnicodeGraphemeClusterBreak> GraphemeClusterBreak => GraphemeClusterBreakValue;

    /// <inheritdoc cref="IndicConjunctBreakValue"/>
    public static ReadOnlySpan<UnicodeIndicConjunctBreak> IndicConjunctBreak => IndicConjunctBreakValue;

    private static T[] Parse<T>(Stream stream, T defaultValue)
        where T : unmanaged, Enum
    {
        if (Unsafe.SizeOf<T>() != 1)
            throw new InvalidOperationException("Enum must be of size 1 byte");

        var isFlag = Attribute.IsDefined(typeof(T), typeof(FlagsAttribute));
        using var sr = new StreamReader(stream);
        var res = new T[0x110000];
        res.AsSpan().Fill(defaultValue);
        for (string? line; (line = sr.ReadLine()) != null;)
        {
            var span = line.AsSpan();

            // strip comment
            var i = span.IndexOf('#');
            if (i != -1)
                span = span[..i];

            span = span.Trim();
            if (span.IsEmpty)
                continue;

            // find delimiter
            i = span.IndexOf(';');
            if (i == -1)
                throw new InvalidDataException();

            var range = span[..i].Trim();
            var entry = Enum.Parse<T>(span[(span.LastIndexOf(';') + 1)..].Trim());

            i = range.IndexOf("..");
            int from, to;
            if (i == -1)
            {
                from = int.Parse(range, NumberStyles.HexNumber);
                to = from + 1;
            }
            else
            {
                from = int.Parse(range[..i], NumberStyles.HexNumber);
                to = int.Parse(range[(i + 2)..], NumberStyles.HexNumber) + 1;
            }

            from = Math.Min(from, res.Length);
            to = Math.Min(to, res.Length);
            if (isFlag)
            {
                foreach (ref var v in res.AsSpan()[from..to])
                {
                    unsafe
                    {
                        fixed (void* p = &v)
                            *(byte*)p |= *(byte*)&entry;
                    }
                }
            }
            else
            {
                res.AsSpan()[from..to].Fill(entry);
            }
        }

        return res;
    }
}
