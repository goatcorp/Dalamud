using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Dalamud.Interface.ImGuiSeStringRenderer.Internal.TextProcessing;

/// <summary>Stores unicode data.</summary>
internal static class UnicodeData
{
    /// <summary>Line break classes.</summary>
    public static readonly UnicodeLineBreakClass[] LineBreak;

    /// <summary>East asian width classes.</summary>
    public static readonly UnicodeEastAsianWidthClass[] EastAsianWidth;

    /// <summary>General categories.</summary>
    public static readonly UnicodeGeneralCategory[] GeneralCategory;

    /// <summary>Emoji properties.</summary>
    public static readonly UnicodeEmojiProperty[] EmojiProperty;

    static UnicodeData()
    {
        // File is from https://www.unicode.org/Public/UCD/latest/ucd/LineBreak.txt
        LineBreak =
            Parse(
                typeof(UnicodeData).Assembly.GetManifestResourceStream("LineBreak.txt")!,
                UnicodeLineBreakClass.XX);

        // https://www.unicode.org/Public/UCD/latest/ucd/EastAsianWidth.txt
        EastAsianWidth =
            Parse(
                typeof(UnicodeData).Assembly.GetManifestResourceStream("EastAsianWidth.txt")!,
                UnicodeEastAsianWidthClass.N);

        // https://www.unicode.org/Public/UCD/latest/ucd/extracted/DerivedGeneralCategory.txt
        GeneralCategory =
            Parse(
                typeof(UnicodeData).Assembly.GetManifestResourceStream("DerivedGeneralCategory.txt")!,
                UnicodeGeneralCategory.Cn);

        // https://www.unicode.org/Public/UCD/latest/ucd/emoji/emoji-data.txt
        EmojiProperty =
            Parse(
                typeof(UnicodeData).Assembly.GetManifestResourceStream("emoji-data.txt")!,
                default(UnicodeEmojiProperty));
    }

    private static T[] Parse<T>(Stream stream, T defaultValue)
        where T : unmanaged, Enum
    {
        if (Unsafe.SizeOf<T>() != 1)
            throw new InvalidOperationException("Enum must be of size 1 byte");

        var isFlag = typeof(T).GetCustomAttribute<FlagsAttribute>() is not null;
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
            var entry = Enum.Parse<T>(span[(i + 1)..].Trim());

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

            if (from > char.MaxValue)
                continue;

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
